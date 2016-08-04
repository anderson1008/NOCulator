#!/usr/bin/python

import sys
import os

workload_dir = "/home/xiyue/workload_list/"
workload = "hetero_8x8"
out_dir_bs = "./results/hetero/8x8/baseline/"
out_dir_design = "./results/hetero/8x8/design/"

if not os.path.exists(out_dir_bs):
  os.makedirs(out_dir_bs)

for sim_index in range(1, 31, 1):
  out_file = "sim_" + str(sim_index) + ".out"
  command_line = "mono /home/xiyue/sim.exe -config ./config_qos.txt -output " + out_dir_bs + out_file + " -workload " + workload_dir + workload + " " + str(sim_index) + " -throttle_enable false"
  os.system (command_line)
#  command_line = "mono /home/xiyue/sim.exe -config ./config_qos.txt -output " + out_dir_design + out_file + " -workload " + workload_dir + workload + ' ' + str(sim_index)+ " -throttle_enable true -curr_L1miss_threshold 0.04 -slowdown_epoch 100000 -thrt_up_slow_app 16 -thrt_down_stc_app 16 -th_unfairness 0.2"
#  os.system (command_line)





