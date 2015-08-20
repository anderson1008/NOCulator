#!/usr/bin/python

import sys
import os
import re
import fnmatch


# slowdown_estimation

workload_dir = "/home/xiyue/Desktop/NOCulator/hring/src/bin/workload_list/"
ipc_ref_file_name = workload_dir + "hetero_ipc_ref"
raw_out_dir = "/home/xiyue/Desktop/results/perfCache/aug10_no_throttle/"

ref_ipc_file = open (ipc_ref_file_name)
ref_ipc = ref_ipc_file.readlines()

insns_count = 100000
num_file = 49
slowdown_error_accum = 0
ipc_alone_error_accum = 0

for sim_index in range (1, num_file + 1, 1):
	
	raw_out_file_name = "sim_" + str(sim_index) + ".out"

	for file in os.listdir(raw_out_dir):
    		if fnmatch.fnmatch(file, raw_out_file_name):
        		fo_in = open(raw_out_dir + file, "r")
			content = fo_in.read();
			fo_in.close()

			searchObj = re.search(r'(?:"active_cycles":\[(.*?)])',content)
			splitObj = re.split('\W+',searchObj.group(1))
			active_cycles = splitObj


			searchObj = re.search(r'(?:"non_overlap_penalty":\[(.*?)])',content)
			splitObj = re.split('\W+',searchObj.group(1))
			non_overlap_penalty = splitObj
			for i in range (0, 16, 1):
				ipc_alone = float(insns_count) / (int(active_cycles[i]) - int(non_overlap_penalty[i]))
				ipc_ref = re.split('[ ]',ref_ipc[sim_index])
				ipc_alone_error = (ipc_alone - float(ipc_ref[i])) / float(ipc_ref[i])
				#print ipc_alone_error 
				ipc_alone_error_accum = ipc_alone_error_accum + abs(ipc_alone_error)

				ipc_share = float(insns_count) / int(active_cycles[i])
				est_slowdown = ipc_alone / ipc_share
				actual_slowdown = float(ipc_ref[i]) / ipc_share
				slowdown_error = (est_slowdown - actual_slowdown) / actual_slowdown
				print slowdown_error
				slowdown_error_accum = slowdown_error_accum + abs(slowdown_error)

avg_slowdown_error = slowdown_error_accum / num_file / 16
print str("%.2f" % avg_slowdown_error)
			

	

