#!/usr/bin/python

import sys
import os

workload_dir = "./workload_list/"
workload = "mix_app_4x4"
out_dir_root = "~GoogleDrive/NOCulator/hring/src/results/para_sweep/4x4/"

out_dir = out_dir_root + "thrt_up/"
if not os.path.exists(out_dir):
    os.makedirs(out_dir)
    print "Dir " + out_dir + " is created."

#Run baseline without throttling
out_file = "bs.out"
command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1" + " -throttle_enable false"

# Sweep thrt_up_slow_app
for thrt_up_slow_app in range (1, 9):
  out_file = "sim_" + str(thrt_up_slow_app) + ".out"
  command_line = "mono ./sim.exe -config ./config_qos.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1" + " -throttle_enable true -thrt_up_slow_app " + str(thrt_up_slow_app)
  print "Command Line: " + command_line
  os.system (command_line)



