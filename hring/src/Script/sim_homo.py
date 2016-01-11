#!/usr/bin/python

import sys
import os

workload_dir = "./workload_list/"
workload = "homo_mem_8x8"
out_dir_bs = "./results/homo/8x8/baseline/"
out_dir_design = "./results/homo/8x8/design/"

for sim_index in range(1, 31, 1):
  out_file = "sim_" + str(sim_index) + ".out"
  command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir_bs + out_file + " -workload " + workload_dir + workload + " " + str(sim_index) + " -throttle_enable false"
  os.system (command_line)
  command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir_design + out_file + " -workload " + workload_dir + workload + ' ' + str(sim_index)+ " -throttle_enable true"
  os.system (command_line)





