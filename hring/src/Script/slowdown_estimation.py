#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string


# slowdown_estimation

workload_dir = "/home/anderson/Desktop/NOCulator/hring/src/bin/workload_list/"
ipc_ref_file_name = workload_dir + "hetero_ipc_ref"
workload_file_name = workload_dir + "hetero_workload"
raw_out_dir = "/home/anderson/Desktop/NOCulator/hring/src/results/system_perfCache/error_rate/"

ref_ipc_file = open (ipc_ref_file_name)
ref_ipc = ref_ipc_file.readlines()

workload_file = open (workload_file_name)
workload = workload_file.readlines()

insns_count = 100000
num_file = 100
slowdown_error_accum = 0
ipc_alone_error_accum = 0

num_app = 26
spec_workload = ["400.perlbench.bin.gz ","401.bzip2.bin.gz ","403.gcc.bin.gz ","429.mcf.bin.gz ","433.milc.bin.gz ","435.gromacs.bin.gz ","436.cactusADM.bin.gz ",\
"437.leslie3d.bin.gz ","444.namd.bin.gz ","445.gobmk.bin.gz ","447.dealII.bin.gz ","450.soplex.bin.gz ","453.povray.bin.gz ","454.calculix.bin.gz ",\
"456.hmmer.bin.gz ","458.sjeng.bin.gz ","459.GemsFDTD.bin.gz ","462.libquantum.bin.gz ","464.h264ref.bin.gz ","465.tonto.bin.gz ","470.lbm.bin.gz ",\
"471.omnetpp.bin.gz ","473.astar.bin.gz ","481.wrf.bin.gz ","482.sphinx3.bin.gz ","483.xalancbmk.bin.gz "]
error_rate_per_app_sum = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
error_rate_count = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
avg_error_per_app =  [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]

def compare (s1, s2):
	remove = string.whitespace
	return s1.translate(None, remove) == s2.translate (None, remove)

def compute (error_sum, error_count):
	for j in range (0, num_app, 1):
		if error_count[j] != 0:
			avg_error_per_app[j] = error_sum[j] / error_count[j]
		workload_out = re.search(r'\d+\.(\w+)',spec_workload[j])
		print workload_out.group(1).ljust(30) + str("%.2f" % avg_error_per_app[j]) 
	

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

			workload_array = re.split('[ ]', workload[sim_index])
			#print workload_array

			ipc_ref = re.split('[ ]',ref_ipc[sim_index])
			#print ipc_ref

			for i in range (0, 16, 1):
				ipc_alone = float(insns_count) / (int(active_cycles[i]) - int(non_overlap_penalty[i]))
				ipc_alone_error = (ipc_alone - float(ipc_ref[i])) / float(ipc_ref[i])
				#print ipc_alone_error 
				ipc_alone_error_accum = ipc_alone_error_accum + abs(ipc_alone_error)

				ipc_share = float(insns_count) / int(active_cycles[i])
				est_slowdown = ipc_alone / ipc_share
				actual_slowdown = float(ipc_ref[i]) / ipc_share
				slowdown_error = (est_slowdown - actual_slowdown) / actual_slowdown
				#print slowdown_error
				slowdown_error_accum = slowdown_error_accum + abs(slowdown_error)

				for j in range (0, num_app, 1):
					if compare (workload_array [i], spec_workload[j]):
						error_rate_per_app_sum [j] = slowdown_error + error_rate_per_app_sum [j]
						error_rate_count [j] = error_rate_count [j] + 1


compute (error_rate_per_app_sum, error_rate_count)				

#avg_slowdown_error = slowdown_error_accum / num_file / 16
#print str("%.2f" % avg_slowdown_error)
			

	
