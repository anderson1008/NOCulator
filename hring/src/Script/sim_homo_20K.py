#!/usr/bin/python

import sys
import os

workload_dir = "./workload_list/"
workload = "homo_mem_4x4"
out_dir_bs = "./results/homo/4x4/baseline/"
out_dir_design = "./results/homo/4x4/design/epoch20K/"

for sim_index in range(1, 11, 1):
  out_file = "sim_" + str(sim_index) + ".out"
  #command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir_bs + out_file + " -workload " + workload_dir + workload + " " + str(sim_index) + " -throttle_enable false"
  #os.system (command_line)
  command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir_design + out_file + " -workload " + workload_dir + workload + ' ' + str(sim_index)+ " -throttle_enable true  -slowdown_epoch 20000 -thrt_up_slow_app 1 -thrt_down_stc_app 1"
  os.system (command_line)





