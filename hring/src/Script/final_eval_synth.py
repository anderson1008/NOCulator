#!/usr/bin/python

import MBNoC_collect_data
import my_print

# Each metric of interest is installed as: metric (traffic, design, network)
traffic = ['bit_complement', 'transpose', 'uniform_random']
design = ['BLESS', 'MBNoC']
network = ['4x4', '8x8', '16x16']
node = [16, 64, 256]
MBNoC_collect_data.subnet = 2
MBNoC_collect_data.insns_count = 1000000
MBNoC_collect_data.COUNT = 60

def split(stat):
	inj_rate = []
	energy = []
	latency = []
	throughput = []
	deflect = []
	for element in stat:
		inj_rate.append(element[0])
		energy.append(element[1])
		latency.append(element[2])
		throughput.append(element[3])
		deflect.append(element[4])
	return [inj_rate, energy, latency, deflect]	

kk = 0
for i in traffic:
	for j,k in zip (node, network):
		MBNoC_collect_data.node = j
		MBNoC_collect_data.file_dir_bless = '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + i + '/BLESS/' + k + '/'
		MBNoC_collect_data.file_dir_mbnoc = '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + i + '/MBNoC/' + k + '/'
		item = (i,j)
		MBNoC_collect_data.evaluation()
		synth_entry_mbnoc = MBNoC_collect_data.synth_entry_mbnoc
		synth_entry_bless = MBNoC_collect_data.synth_entry_bless
		[inject_rate_bless, energy_bless, latency_bless, deflect_bless] = split (synth_entry_bless)
		[inject_rate_mbnoc, energy_mbnoc, latency_mbnoc, deflect_mbnoc] = split (synth_entry_mbnoc)
		# may need to sort metric based on injection rate
		split_stat = [i, k, inject_rate_bless, inject_rate_mbnoc, energy_bless, energy_mbnoc, latency_bless, latency_mbnoc, deflect_bless, deflect_mbnoc]
		#print split_stat
		
		kk = kk + 1
		my_print.print_synth(split_stat) 
#print kk
print ("DONE :)")
