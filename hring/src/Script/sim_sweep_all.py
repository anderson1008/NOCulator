#!/usr/bin/python

import sys
import os

workload_dir = "../workload_list/"
workload = "homo_4x4"

# up, down, l1_miss
config = {\
('1','1','0.01'),\
('1','1','0.02'),\
('1','1','0.03'),\
('1','1','0.04'),\
('1','1','0.05'),\
('1','1','0.06'),\
('4','4','0.01'),\
('4','4','0.02'),\
('4','4','0.03'),\
('4','4','0.04'),\
('4','4','0.05'),\
('4','4','0.06')
}

config_index = 0
total_sim = 0
for config_i in config:
  out_dir_design = "./results/homo/4x4/design/"
  config_index += 1
  out_dir_design = out_dir_design + str(config_index) + "/"
  if not os.path.exists(out_dir_design):
    os.makedirs(out_dir_design)

  for sim_index in range(1, 6, 1):
    out_file = "sim_" + str(sim_index) + ".out"
    command_line = "mono /home/xiyue/sim.exe -config ../config_qos.txt -output " + out_dir_design + out_file + " -workload " + workload_dir + workload + ' ' + str(sim_index)+ " -throttle_enable true -thrt_up_slow_app " + config_i[0] + ' -thrt_down_stc_app ' + config_i[1] + ' -curr_L1miss_threshold ' + config_i[2] + ' -throt_min 0.35 -th_unfairness 0.2 -slowdown_epoch 100000 &'
    total_sim += 1
    os.system (command_line)
