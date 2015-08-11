#!/usr/bin/python

import sys
import os

workload_dir = "./workload_list/"
workload = "hetero_workload"
out_dir = "/home/xiyue/Desktop/results/aug10/"


for sim_index in range(1, 101, 1):
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "./sim.exe -config ./config_0.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' ' + str (sim_index)
	os.system (command_line)





