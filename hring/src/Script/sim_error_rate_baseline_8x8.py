#!/usr/bin/python

import sys
import os

workload_dir = "./workload_list/"
workload = "homo_8x8"
out_dir_bs = "./homo/8x8/baseline/"

for sim_index in range(1, 31, 1):
  out_file = "sim_" + str(sim_index) + ".out"
  command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir_bs \
  + out_file + " -workload " + workload_dir + workload + " " + \
  str(sim_index) + " -throttle_enable false -network_nrX 8 -network_nrY 8"
  os.system (command_line)

workload = "hetero_8x8"
out_dir_bs = "./hetero/8x8/baseline/"

for sim_index in range(1, 31, 1):
  out_file = "sim_" + str(sim_index) + ".out"
  command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir_bs \
  + out_file + " -workload " + workload_dir + workload + " " + \
  str(sim_index) + " -throttle_enable false -network_nrX 8 -network_nrY 8"
  os.system (command_line)

workload = "random_8x8"
out_dir_bs = "./random/8x8/baseline/"

for sim_index in range(1, 31, 1):
  out_file = "sim_" + str(sim_index) + ".out"
  command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir_bs \
  + out_file + " -workload " + workload_dir + workload + " " + \
  str(sim_index) + " -throttle_enable false -network_nrX 8 -network_nrY 8"
  os.system (command_line)

