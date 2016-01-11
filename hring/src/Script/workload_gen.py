#!/usr/bin/python

import sys
import random
import os

number_of_workload = 30
nodes = 64
workload_high_mem_intsty = []
ipc_high_mem_intsty = []
workload_mid_mem_intsty = []
ipc_mid_mem_intsty = []
workload_low_mem_intsty = []
ipc_low_mem_intsty = []
# spec_ipc
spec_mpki = []
file_name = []
file_ipc = []

spec_workload = [\
"400.perlbench.bin.gz ","401.bzip2.bin.gz ","403.gcc.bin.gz ","429.mcf.bin.gz ","433.milc.bin.gz ","435.gromacs.bin.gz ","436.cactusADM.bin.gz ",\
"437.leslie3d.bin.gz ","444.namd.bin.gz ","445.gobmk.bin.gz ","447.dealII.bin.gz ","450.soplex.bin.gz ","453.povray.bin.gz ","454.calculix.bin.gz ",\
"456.hmmer.bin.gz ","458.sjeng.bin.gz ","459.GemsFDTD.bin.gz ","462.libquantum.bin.gz ","464.h264ref.bin.gz ","465.tonto.bin.gz ","470.lbm.bin.gz ",\
"471.omnetpp.bin.gz ","473.astar.bin.gz ","481.wrf.bin.gz ","482.sphinx3.bin.gz ","483.xalancbmk.bin.gz "]

spec_ipc_4x4 = [
"2.17 ", "2.13 ", "1.83 ", "2.08 ", "2.16 ", "1.87 ", "1.38 ", \
"2.75 ", "2.36 ", "2.33 ", "1.91 ", "1.77 ", "1.88 ", "1.71 ", \
"1.60 ", "2.18 ", "1.33 ", "2.73 ", "1.73 ", "1.85 ", "1.91 ", \
"2.65 ", "2.04 ", "2.92 ", "2.51 ", "2.02 "]

spec_mpki_4x4 = [
"0.40", "6.12", "4.32", "92.90", "57.06", "7.87", "11.08", \
"67.19", "5.33", "5.19", "5.36", "39.43", "5.90", "1.79", \
"9.71", "3.60", "111.23", "50.02", "24.72", "2.22", "54.04", \
"52.89", "10.84", "1.43", "26.60", "17.49" ]

spec_ipc_8x8 = [
"2.09 ", "2.06 ", "1.70 ", "1.30 ", "1.73 ", "1.73 ", "1.25 ", \
"1.88 ", "2.07 ", "2.04 ", "1.68 ", "1.47 ", "1.71 ", "1.61 ", \
"1.43 ", "2.00 ", "0.93 ", "2.00 ", "1.65 ", "1.71 ", "1.43 ", \
"2.00 ", "1.86 ", "2.94 ", "2.04 ", "1.78 "]

spec_mpki_8x8 = [
"1.36", "28.86", "19.02", "100.46", "57.30", "9.39", "14.97", \
"67.48", "22.72", "12.48", "14.44", "40.33", "20.95", "3.42", \
"21.20", "6.71", "117.62", "50.13", "57.45", "4.41", "56.49", \
"18.39", "12.00", "0.95", "28.61", "30.33"]


# Determine the reference
def ref_sel ():
  global spec_ipc
  global spec_mpki
  if nodes == 16 :
    spec_ipc = spec_ipc_4x4
    spec_mpki = spec_mpki_4x4
  elif nodes == 64 :
    spec_ipc = spec_ipc_8x8
    spec_mpki = spec_mpki_8x8
  else:
    raise Exception ("network size is not defined")


# Determine the network size
def file_sel ():
  global filename
  global file_ipc
  if nodes == 16 :
    filename = filename + '_4x4'
    file_ipc = filename + '_ipc'
  elif nodes == 64 :
    filename = filename + '_8x8'
    file_ipc = filename + '_ipc'
  else:
    raise Exception ("network size is not defined")
# end file_sel()


# start file_open()
def file_open (filename):
    filename_out = str(filename)
    if os.path.exists(filename_out) == True:
      os.remove(filename_out)
    return open(filename_out, "w")
# end file_open()

# Fuction to generate workload file
def work_gen_homo (app_array, app_ipc_array):
  file_sel ()
  workload_out = file_open (filename)
  ipc_out = file_open (file_ipc)
  
  #specify the dir of trace files
  workload_out.write("/Users/Anderson/Documents/blesstraces C:/Users/xiyuex/Documents/blesstraces /Users/xiyuexiang/Documents/blesstraces")
  workload_index = 0
  app_count = len(app_array)

  while (workload_index < number_of_workload):
 
    i = random.randint(0, app_count-1)
    j = random.randint(0, app_count-1)
    k = random.randint(0, app_count-1)
    m = random.randint(0, app_count-1)
    
    # randomly select 4 apps
    while (j == i):
      j = random.randint(0, app_count-1)
    while (k == i or k == j):
      k = random.randint(0, app_count-1)
    while (m == i or m == j or m == k):
      m = random.randint(0, app_count-1)
	
    # shuffle the apps and generate entry by entry
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


# begin work_gen_hetero()
def work_gen_hetero ():
    file_sel ()
    workload_out = file_open (filename)
    ipc_out = file_open (file_ipc)
    #specify the dir of trace files
    workload_out.write("/Users/Anderson/Documents/blesstraces C:/Users/xiyuex/Documents/blesstraces /Users/xiyuexiang/Documents/blesstraces")
    workload_index = 0
    high_mem_count = len(workload_high_mem_intsty)
    mid_mem_count = len(workload_mid_mem_intsty)

    while (workload_index < number_of_workload):
      i = random.randint(0, high_mem_count-1)
      j = random.randint(0, mid_mem_count-1)
      k = random.randint(0, high_mem_count-1)
      m = random.randint(0, mid_mem_count-1)

      # randomly select 4 apps and retry if an app has been chosen
      while (k == i):
        k = random.randint(0, high_mem_count-1)
      while (m == j):
        m = random.randint(0, mid_mem_count-1)
            
      # shuffle the apps and generate entry by entry
      workload = '\n'
      ipc = '\n'
      for x in range (0, nodes/4):
        workload = workload + workload_high_mem_intsty [i] + workload_mid_mem_intsty [j] + workload_high_mem_intsty [k] + workload_mid_mem_intsty [m]
        ipc = ipc + ipc_high_mem_intsty [i] + ipc_mid_mem_intsty [j] + ipc_high_mem_intsty [k] + ipc_mid_mem_intsty [m]

      workload_out.write(workload)
      ipc_out.write(ipc)
      workload_index = workload_index + 1
        
    workload_out.close()
    ipc_out.close()
# end work_gen_hetero()



# start main procedure

# Categorize applications based on their MPKI

ref_sel()
for i,j,k in zip (spec_ipc, spec_workload, spec_mpki):
  if float(k) <= 10:
    workload_low_mem_intsty = workload_low_mem_intsty + [j]
    ipc_low_mem_intsty = ipc_low_mem_intsty + [i]
  elif float(k) <= 30 and float (k) > 10:
    workload_mid_mem_intsty = workload_mid_mem_intsty + [j]
    ipc_mid_mem_intsty = ipc_mid_mem_intsty + [i]
  else:
    workload_high_mem_intsty = workload_high_mem_intsty + [j]
    ipc_high_mem_intsty = ipc_high_mem_intsty + [i]

#print "Low Memory Intensity Apps: "
#print workload_low_mem_intsty
#print "Mid Memory Intensity Apps: "
#print workload_mid_mem_intsty
#print "High Memory Intensity Apps: "
#print workload_high_mem_intsty

# generate workload
filename = 'homo_mem'
work_gen_homo (workload_high_mem_intsty, ipc_high_mem_intsty)
#filename = 'homo_non_mem'
#work_gen_homo (filename, workload_low_mem_intsty, ipc_low_mem_intsty, nodes)
filename = 'hetero'
work_gen_hetero ()

print "^_^"
# end main procedure


