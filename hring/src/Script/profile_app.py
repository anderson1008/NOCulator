#!/usr/bin/python
# use to profile the application running solo.

import re
import os
import sys
import fnmatch
import matplotlib.pyplot as plt

raw_data_dir = '/home/anderson/Desktop/results/profile/8x8/'
MEM_INTEN_THESHLD = 45

spec_workload = ["400.perlbench.bin.gz ","401.bzip2.bin.gz ","403.gcc.bin.gz ","429.mcf.bin.gz ","433.milc.bin.gz ","435.gromacs.bin.gz ","436.cactusADM.bin.gz ",\
"437.leslie3d.bin.gz ","444.namd.bin.gz ","445.gobmk.bin.gz ","447.dealII.bin.gz ","450.soplex.bin.gz ","453.povray.bin.gz ","454.calculix.bin.gz ",\
"456.hmmer.bin.gz ","458.sjeng.bin.gz ","459.GemsFDTD.bin.gz ","462.libquantum.bin.gz ","464.h264ref.bin.gz ","465.tonto.bin.gz ","470.lbm.bin.gz ",\
"471.omnetpp.bin.gz ","473.astar.bin.gz ","481.wrf.bin.gz ","482.sphinx3.bin.gz ","483.xalancbmk.bin.gz "]

filename = 'profile_by_app.txt'
filename_out = raw_data_dir + filename
if os.path.exists(filename_out) == True:
	os.remove(filename_out)
fo_out_0 = open(filename_out, "a")
fo_out_0.write('\n\n' + 'Profile mpki, ipc to study the sensitivity to mshrs(concurrency) ' + '\n\n')

filename = 'profile_overall.txt'
filename_out = raw_data_dir + filename
if os.path.exists(filename_out) == True:
	os.remove(filename_out)
fo_out_1 = open(filename_out, "a")
fo_out_1.write('\n\n' + 'Profile mpki, ipc' + '\n\n')
fo_out_1.write('\n')
fo_out_1.write('Index'.ljust(5) + 'File Name'.ljust(15) + 'IPC'.ljust(10) + 'MPKI'.ljust(10))
fo_out_1.write('\n')

for app_index in range (1, 27):
  mshr_plt = []
  ipc_plt = []
  mpki_plt = []
  mpki_sum = 0
  ipc_sum = 0
  app = spec_workload [app_index-1].strip()
  fo_out_0.write('\n')
  fo_out_0.write('----------------------- '+app+' ------------------------')
  fo_out_0.write('\n' + 'MSHRs'.ljust(8) + 'IPC'.ljust(10) + 'MPKI'.ljust(10))
  if not os.path.exists(raw_data_dir + app):
    raise Exception ("dir" + raw_data_dir + app + "not found")
  file_count = 0
  for sim_index in range (1,17):
    file_name = raw_data_dir + app + "/sim_" + str(sim_index) + '.out'
    if not os.path.exists(file_name):
      raise Exception ("file " + file_name + " not found") 
    fo_in = open(file_name, "r")
    file_count = file_count + 1
    content = fo_in.read();

    searchObj = re.search(r'"mpki_bysrc":\[\n(?:\{"avg":.*?\},\n){31}(\{"avg":.*?\},)', content)
    searchObj = re.search(r'(?:\{"avg":([\w.]+),)', searchObj.group(1))
    mpki = "%.3f" % float(searchObj.group(1))

    searchObj = re.search(r'(?:"active_cycles":\[(.*?)])',content)
    splitObj = re.split('\W+',searchObj.group(1))
    active_cycle = float(splitObj[31])
	
    searchObj = re.search(r'(?:"insns_persrc":\[(.*?)])',content)
    splitObj = re.split('\W+',searchObj.group(1))
    insns_count = float(splitObj[31])

    fo_in.close()
    ipc = "%.3f" % (insns_count / active_cycle)
    ipc_str = str(ipc)

    mshr = sim_index
    mshr_plt = mshr_plt + [mshr]
    ipc_plt = ipc_plt + [ipc]
    mpki_plt = mpki_plt + [float(mpki)]
    ipc_sum = ipc_sum + float(ipc)
    mpki_sum = mpki_sum + float(mpki)

    fo_out_0.write('\n')
    fo_out_0.write(str(mshr).ljust(8) + ipc_str.ljust(10) + str(mpki).ljust(10))

  fo_out_1.write('\n')
  fo_out_1.write(str(app_index).ljust(5) + app.strip('.bin.gz').ljust(15) + str("%.3f" % (ipc_sum / file_count)).ljust(10) + str("%.3f" % (mpki_sum / file_count)).ljust(10))
  fo_out_1.write('\n')
	
  
  if mpki_sum / file_count > MEM_INTEN_THESHLD:
    plt.figure(1)
    plt.subplot(2,1,1)
    plt.plot(mshr_plt, ipc_plt, label=app.strip('.bin.gz'))
    plt.legend()
    plt.subplot(2,1,2)
    plt.plot(mshr_plt, mpki_plt, label=app.strip('.bin.gz'))
    plt.legend()
fo_out_0.close()
fo_out_1.close()
plt.show()
print "^_^"
