#!/usr/bin/python

import sys
import os

workload_dir = "./workload_list/"
workload = "hetero_4x4"
out_dir = "./results/hetero/baseline/4x4/"


for sim_index in range(1, 31, 1):
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "./sim.exe -config ./config_qos.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' ' + str (sim_index)
	os.system (command_line)





