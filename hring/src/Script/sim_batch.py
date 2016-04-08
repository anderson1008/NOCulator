#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string

workload_dir = "/Users/xiyuexiang/GoogleDrive/NOCulator/hring/src/bin/"
workload = "mix_app"

insns_count = 1000000
ipc_alone = [2.16, 2.75, 2.08, 1.91, 2.16, 2.75, 2.08, 1.91, 2.16, 2.75, 2.08, 1.91, 2.16, 2.75, 2.08, 1.91]
ipc_share = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]

out_dir = "/Users/xiyuexiang/GoogleDrive/NOCulator/hring/src/bin/sweep_batch/"
filename = '../bin/sweep_batch_period.txt'
filename_out = str(filename)
if os.path.exists(filename_out) == True:
	os.remove(filename_out)
fo_out = open(filename_out, "a")

fo_out.write('\n\n' + 'sweep packet batching period (epoch = 100000)' + '\n\n')
fo_out.write('period'.ljust(15) + 'w_speedup'.ljust(15) + 'h_speedup'.ljust(15))
fo_out.write('\n')


for sim_index in range(100, 5100, 100):
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config " + workload_dir + "config_0.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' 3 ' + "-STC_batchPeriod " + str(sim_index)
	os.system (command_line)
	
	# collect result

	result_file = open (out_dir + out_file, 'r')
	result = result_file.read()
	result_file.close()

	searchObj = re.search(r'(?:"active_cycles":\[(.*?)])',result)
	splitObj = re.split('\W+',searchObj.group(1))
	active_cycles = splitObj

	
	weighted_speedup = 0
	temp0 = 0
	for i in range (0, 16, 1):
		ipc_share [i] = float(insns_count) / int(active_cycles[i])
		weighted_speedup = ipc_share[i] / ipc_alone[i] + weighted_speedup
		temp0 = ipc_alone[i] / ipc_share[i] + temp0
		harmonic_speedup = 16 / temp0
	
	print str(sim_index) + "     " + str("%.2f" % weighted_speedup) + "   " + str("%.2f" % harmonic_speedup)
	fo_out.write('\n')
	fo_out.write(str(sim_index).ljust(15) + str(weighted_speedup).ljust(15) + str(harmonic_speedup).ljust(15))
	fo_out.write('\n')
	
fo_out.close()
