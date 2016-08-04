#!/usr/bin/python

import sys
import os

workload_dir = "./workload_list/"
workload = "homo_4x4"
out_dir_bs = "./homo/4x4/baseline/"

if not os.path.exists(out_dir_bs):
  os.makedirs(out_dir_bs)


for sim_index in range(1, 31, 1):
  out_file = "sim_" + str(sim_index) + ".out"
  command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir_bs \
  + out_file + " -workload " + workload_dir + workload + " " + \
  str(sim_index) + " -throttle_enable false -network_nrX 4 -network_nrY 4"
  os.system (command_line)


workload = "hetero_4x4"
out_dir_bs = "./hetero/4x4/baseline/"
if not os.path.exists(out_dir_bs):
  os.makedirs(out_dir_bs)
for sim_index in range(1, 31, 1):
  out_file = "sim_" + str(sim_index) + ".out"
  command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir_bs \
  + out_file + " -workload " + workload_dir + workload + " " + \
  str(sim_index) + " -throttle_enable false -network_nrX 4 -network_nrY 4"
  os.system (command_line)


workload = "random_4x4"
out_dir_bs = "./random/4x4/baseline/"
if not os.path.exists(out_dir_bs):
  os.makedirs(out_dir_bs)
for sim_index in range(1, 31, 1):
  out_file = "sim_" + str(sim_index) + ".out"
  command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir_bs \
  + out_file + " -workload " + workload_dir + workload + " " + \
  str(sim_index) + " -throttle_enable false -network_nrX 4 -network_nrY 4"
  os.system (command_line)
