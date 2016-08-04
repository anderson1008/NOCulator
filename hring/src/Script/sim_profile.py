#!/usr/bin/python

import sys
import os

workload_dir = "../workload_list/"
workload = "single_8x8"
out_dir_root = "./results/profile/8x8/"

spec_workload = ["400.perlbench.bin.gz ","401.bzip2.bin.gz ","403.gcc.bin.gz ","429.mcf.bin.gz ","433.milc.bin.gz ","435.gromacs.bin.gz ","436.cactusADM.bin.gz ",\
"437.leslie3d.bin.gz ","444.namd.bin.gz ","445.gobmk.bin.gz ","447.dealII.bin.gz ","450.soplex.bin.gz ","453.povray.bin.gz ","454.calculix.bin.gz ",\
"456.hmmer.bin.gz ","458.sjeng.bin.gz ","459.GemsFDTD.bin.gz ","462.libquantum.bin.gz ","464.h264ref.bin.gz ","465.tonto.bin.gz ","470.lbm.bin.gz ",\
"471.omnetpp.bin.gz ","473.astar.bin.gz ","481.wrf.bin.gz ","482.sphinx3.bin.gz ","483.xalancbmk.bin.gz "]

# index the application
for app_index in range(1, 27, 1):
  out_dir = out_dir_root
  app = spec_workload [app_index-1].strip()
  out_dir = out_dir + app + '/'
  if not os.path.exists(out_dir):
    os.makedirs(out_dir)
    print "Dir " + out_dir + " is created."
# sweep the mshr from 1 to 16
  for mshr in range(16, 17):
    out_file = "sim_" + str(mshr) + ".out"
    command_line = "mono /home/xiyue/sim.exe -config ./config_qos.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' ' + str (app_index) + ' -mshrs ' + str(mshr) + ' -throttle_enable false'
    #print command_line
    os.system (command_line)

