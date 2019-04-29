#!/usr/bin/python

import sys
import os

workload_dir = "/home/xiyue/workload_list/"
workload = "homo_8x8"

sweep_interval = 4 

for i in range (1, 9):
  parameter = str(sweep_interval * i)
  out_dir_design = "./results/homo/8x8/design/"
  out_dir_design = out_dir_design + parameter + "/"
  if not os.path.exists(out_dir_design):
    os.makedirs(out_dir_design)

  for sim_index in range(1, 11, 1):
    out_file = "sim_" + str(sim_index) + ".out"
    command_line = "mono /home/xiyue/sim.exe -config ../config_qos.txt -output " + out_dir_design + out_file + " -workload " + workload_dir + workload + ' ' + str(sim_index)+ " -throttle_enable true -thrt_down_stc_app " + parameter
    os.system (command_line)
    
