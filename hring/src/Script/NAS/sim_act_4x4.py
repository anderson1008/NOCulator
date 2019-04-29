#!/usr/bin/python

import sys
import os

workload_dir = "/Users/Anderson/GoogleDrive/NOCulator/hring/src/bin/workload_list/"
workload = "homo_4x4"
out_dir = "./results/homo/4x4/design/"

if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 31, 1):
  out_file = "sim_" + str(sim_index) + ".out"
  command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " " + str(sim_index)
  os.system (command_line)

