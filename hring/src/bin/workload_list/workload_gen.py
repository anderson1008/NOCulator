#!/usr/bin/python

import sys
import random
import os

number_of_workload = 30
nodes = 16

spec_workload = [\
"400.perlbench.bin.gz ","401.bzip2.bin.gz ","403.gcc.bin.gz ","429.mcf.bin.gz ","433.milc.bin.gz ","435.gromacs.bin.gz ","436.cactusADM.bin.gz ",\
"437.leslie3d.bin.gz ","444.namd.bin.gz ","445.gobmk.bin.gz ","447.dealII.bin.gz ","450.soplex.bin.gz ","453.povray.bin.gz ","454.calculix.bin.gz ",\
"456.hmmer.bin.gz ","458.sjeng.bin.gz ","459.GemsFDTD.bin.gz ","462.libquantum.bin.gz ","464.h264ref.bin.gz ","465.tonto.bin.gz ","470.lbm.bin.gz ",\
"471.omnetpp.bin.gz ","473.astar.bin.gz ","481.wrf.bin.gz ","482.sphinx3.bin.gz ","483.xalancbmk.bin.gz "]

spec_ipc = [
"2.17 ", "2.13 ", "1.83 ", "2.08 ", "2.16 ", "1.87 ", "1.38 ", \
"2.75 ", "2.36 ", "2.33 ", "1.91 ", "1.77 ", "1.88 ", "1.71 ", \
"1.60 ", "2.18 ", "1.33 ", "2.73 ", "1.73 ", "1.85 ", "1.91 ", \
"2.65 ", "2.04 ", "2.92 ", "2.51 ", "2.02 "]

spec_mpki = [
"0.40", "6.12", "4.32", "92.90", "57.06", "7.87", "11.08", \
"67.19", "5.33", "5.19", "5.36", "39.43", "5.90", "1.79", \
"9.71", "3.60", "111.23", "50.02", "24.72", "2.22", "54.04", \
"52.89", "10.84", "1.43", "26.60", "17.49" ]

# Categorize applications based on their MPKI
mem_intensive_workload = []
mem_intensive_ipc = []
non_mem_intensive_workload = []
non_mem_intensive_ipc = []

for i,j,k in zip (spec_ipc, spec_workload, spec_mpki):
  if float(k) < 30:
    non_mem_intensive_workload = non_mem_intensive_workload + [j];
    non_mem_intensive_ipc = non_mem_intensive_ipc + [i];
  else:
    mem_intensive_workload = mem_intensive_workload + [j];
    mem_intensive_ipc = mem_intensive_ipc + [i];
# end categorization

# Fuction to generate workload file
def work_gen (filename, app_array, app_ipc_array, nodes):
  file_ipc = ''
  if nodes == 16 :
    filename = filename + '_4x4'
    file_ipc = filename + '_ipc_4x4'
  elif nodes == 64 :
    filename = filename + '_8x8'
    file_ipc = filename + '_ipc_8x8'
  elif nodes == 256:
    filename = filename + '_16x16'
    file_ipc = filename + '_ipc_16x16'
  else:
    raise Exception ("network size is not defined")

  filename_out = str(filename)
  if os.path.exists(filename_out) == True:
    os.remove(filename_out)
  workload_out = open(filename_out, "w")

  filename_out = str(file_ipc)
  if os.path.exists(filename_out) == True:
    os.remove(filename_out)
  ipc_out = open(filename_out, "w")

  #specify the dir of trace files
  workload_out.write("/home/anderson/Desktop/blesstraces")
  workload_out.write(" /home/xxx1698/blesstraces")  # on canr310

  workload_index = 0
  app_count = len(app_array)

  while (workload_index < number_of_workload):
 
    i = random.randint(0, app_count-1)
    j = random.randint(0, app_count-1)
    k = random.randint(0, app_count-1)
    m = random.randint(0, app_count-1)

    while (j == i):
      j = random.randint(0, app_count-1)
    while (k == i or k == j):
      k = random.randint(0, app_count-1)
    while (m == i or m == j or m == k):
      m = random.randint(0, app_count-1)
	
    workload = '\n'
    ipc = '\n'
    for x in range (0, nodes/4):
      workload = workload + app_array [i] + app_array [j] + app_array [k] + app_array [m]
      ipc = ipc + app_ipc_array [i] + app_ipc_array [j] + app_ipc_array [k] + app_ipc_array [m]

    workload_out.write(workload)
    ipc_out.write(ipc)
    workload_index = workload_index + 1

  workload_out.close()
  ipc_out.close()
# end work_gen()


# generate workload
filename = 'homo_mem'
work_gen (filename, mem_intensive_workload, mem_intensive_ipc, nodes)
filename = 'homo_non_mem'
work_gen (filename, non_mem_intensive_workload, non_mem_intensive_ipc, nodes)
filename = 'hetero'
work_gen (filename, spec_workload, spec_ipc, nodes)

