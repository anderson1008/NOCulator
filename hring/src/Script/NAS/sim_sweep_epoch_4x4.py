#!/usr/bin/python

import sys
import os

workload_dir = "../workload_list/"
workload = "homo_4x4"

sweep_interval = 20000

for i in range (1, 7):
  parameter = str(sweep_interval * i)
  out_dir_design = "./results/homo/4x4/design/"
  out_dir_design = out_dir_design + parameter + "/"
  if not os.path.exists(out_dir_design):
    os.makedirs(out_dir_design)

  for sim_index in range(1, 11, 1):
    out_file = "sim_" + str(sim_index) + ".out"
    command_line = "mono /home/xiyue/sim.exe -config ../config_qos.txt -output " + out_dir_design + out_file + " -workload " + workload_dir + workload + ' ' + str(sim_index)+ " -throttle_enable true -slowdown_epoch " + parameter
    os.system (command_line)
    
