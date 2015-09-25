#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string

def collect (sim_index, node, file_dir, insns_count):	
	# Collect desired metrics from the raw output file obtained through simulation.
	# return stat_out which is a dictionary including all desired metrics.
	file_out = "sim_" + str(sim_index) + ".out"
	found = False
	for file in os.listdir(file_dir):
    		if fnmatch.fnmatch(file, file_out):
        		fo_in = open(file_dir + file, "r")
			content = fo_in.read();
			fo_in.close()
			found = True
			break
		else:
			continue
	
	if found == False:
		stat_out = None
	else:		
		sum_active_cycle = 0
		searchObj = re.search(r'(?:"active_cycles":\[(.*?)])',content)
		splitObj = re.split('\W+',searchObj.group(1))
		active_cycles = splitObj
		if node == 16:
			ipc = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
		elif node == 64:	
			ipc = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
		elif node == 256:
			ipc = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
		for i in range (0, node, 1):
			ipc[i] = float(insns_count) / int(active_cycles[i])
			sum_active_cycle = sum_active_cycle + int(active_cycles[i])
			avg_time = sum_active_cycle / node
	
		searchObj = re.search(r'packet net latency: (.*?) | [.*]',content)
		pkt_net_latency = searchObj.group(1)

		searchObj = re.search(r'packet tot latency: (.*?) | [.*]',content)
		pkt_tot_latency = searchObj.group(1)

		searchObj = re.search(r'throughput: (.*?) flits per cycle',content)
		throughput = searchObj.group(1)

		searchObj = re.search(r'injections: (.*?) flits',content)
		inj_flit = searchObj.group(1)

		searchObj = re.search(r'permute: (.*?)\n',content)
		permute = searchObj.group(1)

		searchObj = re.search(r'traversal: (.*?) \(unproductive traversal (.*?%)\)',content) # unproducive traversal in the original stat = deflection rate; it is corrected in the latest version.
		traversal = searchObj.group(1)
		deflection_rate = searchObj.group(2)

		searchObj = re.search(r'deflections: (.*?) \(rate (.*?) per cycle, each flit is deflected for (.*?) times\)',content)
		deflection = searchObj.group(1)
		deflect_flit_per_cyle = searchObj.group(2)
		deflect_per_flit = searchObj.group(3)

		searchObj = re.search(r'bypass: (.*?) \(rate (.*?) per cycle, each flit is bypassed for (.*?) times\)',content)
		bypass = searchObj.group(1)
		bypass_flit_per_cyle = searchObj.group(2)
		bypass_per_flit = searchObj.group(3)

		searchObj = re.search(r'ctrl packet: (.*?)\n',content)
		ctrl_pkt = searchObj.group(1)

		searchObj = re.search(r'data packet: (.*?)\n',content)
		data_pkt = searchObj.group(1)

		searchObj = re.search(r'mpki: (.*?)\n',content)
		mpki = searchObj.group(1)

		searchObj = re.search(r'net utilization: (.*?%)',content)
		net_utilization = searchObj.group(1)

		inject_rate = str(float(inj_flit) / avg_time / node)

		stat_out ={'active_cycles': active_cycles, 'inject_rate':inject_rate, 'ipc':ipc, 'pkt_net_latency':pkt_net_latency, 'pkt_tot_latency':pkt_tot_latency, 'throughput':throughput, 'inj_flit':inj_flit, 'permute':permute, 'traversal':traversal, 'deflection_rate':deflection_rate, 'deflection':deflection, 'deflect_flit_per_cycle':deflect_flit_per_cyle, 'deflect_per_flit':deflect_per_flit, 'bypass':bypass, 'bypass_flit_per_cyle':bypass_flit_per_cyle, 'bypass_per_flit':bypass_per_flit, 'ctrl_pkt':ctrl_pkt, 'data_pkt':data_pkt, 'net_utilization':net_utilization, 'mpki':mpki} # print order is determined by the computation order.

	return stat_out








			

			

	

