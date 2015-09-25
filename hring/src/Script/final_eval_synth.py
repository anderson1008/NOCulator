#!/usr/bin/python

import MBNoC_collect_data
import my_print
import collect_stat

# Use to compute and extract the final result for synthetic simulation.



# Each metric of interest is installed as: metric (traffic, design, network)
traffic = ['bit_complement', 'transpose', 'uniform_random']
design = ['BLESS', 'MBNoC']
network = ['4x4', '8x8', '16x16']
node = [16, 64, 256]
MBNoC_collect_data.subnet = 2
MBNoC_collect_data.insns_count = 1000000
MBNoC_collect_data.SIM_COUNT = 200
MBNoC_collect_data.synthetic = True
varied_subnet = False # set it to True while comparing BLESS, MBNOC and MBNOC4

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
	return [inj_rate, energy, latency, deflect, throughput]	

def remove_dup (inject_rate, stat):
# to remove duplicated entry and form a dictionary such that we can extract and compare the metric under the same injection rate
	dict_stat = {}
	for x, y in zip(inject_rate, stat):
		dict_stat [x] = y
	return dict_stat

def average_gain (my_stat, my_inject_rate, baseline_stat, baseline_inject_rate):
# compute the average improvement of a metric
	my_stat_0 = remove_dup(my_inject_rate, my_stat)
	baseline_stat_0 = remove_dup(baseline_inject_rate, baseline_stat)
	sum_gain = 0
	effective_count = 0
	inject_rate = baseline_stat_0.keys() # extract the injection rate
	for x in inject_rate:
		if my_stat_0.has_key(x) and baseline_stat_0.has_key(x): # only compare the metric with the same injection rate
			stat1 = baseline_stat_0[x]
			stat0 = my_stat_0[x]
			if float(stat1) > 0 and float(stat0) > 0:
				sum_gain = sum_gain + (float(stat0) - float(stat1))/float(stat1)
				effective_count = effective_count + 1
			else:
				sum_gain = sum_gain
		else:
			sum_gain = sum_gain	

	
	if effective_count > 0:
		avg_gain = float("{:.4f}".format(sum_gain / effective_count))
	else: 
		avg_gain = 0	
	return avg_gain

def average_reduce (my_stat, my_inject_rate, baseline_stat, baseline_inject_rate):
# compute the average reduction of a metric
	my_stat_0 = remove_dup(my_inject_rate, my_stat)
	baseline_stat_0 = remove_dup(baseline_inject_rate, baseline_stat)
	sum_reduce = 0
	effective_count = 0
	inject_rate = baseline_stat_0.keys()# extract the injection rate
	for x in inject_rate:
		if my_stat_0.has_key(x) and baseline_stat_0.has_key(x):  # only compare the metric with the same injection rate
			stat1 = baseline_stat_0[x]
			stat0 = my_stat_0[x]
			if float(stat1) > 0 and float(stat0) > 0:
				sum_reduce = sum_reduce + (float(stat1) - float(stat0))/float(stat1)
				effective_count = effective_count + 1
			else:
				sum_reduce = sum_reduce
		else:
			sum_reduce = sum_reduce	

	
	if effective_count > 0:
		avg_reduce = float("{:.4f}".format(sum_reduce / effective_count))
	else: 
		avg_reduce = 0	
	return avg_reduce

def mbnoc4_stat (traffic, size, network):
# extract mbnoc4 stat (subnet=4)
	stat_out = []
	for sim_index in range (1, MBNoC_collect_data.SIM_COUNT + 1, 1):
		subnet = 4
		stat_dir = '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + traffic + '/MBNoC4/' + network + '/'
		stat_mbnoc4 = collect_stat.collect(sim_index, size, stat_dir, MBNoC_collect_data.insns_count)
		if stat_mbnoc4 is None:
			continue
		inject_rate = "{:.2f}".format(float(stat_mbnoc4['inject_rate'])/subnet) # must be scaled based on the flit size	
		tot_latency = "{:.2f}".format(float(stat_mbnoc4['pkt_tot_latency'])/MBNoC_collect_data.clk_scale_factor)
		deflect_flit_per_cycle = "{:.2f}".format(float(stat_mbnoc4['deflect_flit_per_cycle'])/subnet)
		stat_out.append((inject_rate, 0.00, tot_latency, 0.00, deflect_flit_per_cycle)) # energy and throughput are coorelated with clock rate and other synthesis results. For MBNoC4, we don't have synthesis result. Therefore, energy and throughput are 0
	return stat_out


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
		
		[inject_rate_bless, energy_bless, latency_bless, deflect_bless, throughput_bless] = split (synth_entry_bless)
		[inject_rate_mbnoc, energy_mbnoc, latency_mbnoc, deflect_mbnoc, throughput_mbnoc] = split (synth_entry_mbnoc)
		

		#print split_stat
		
		if varied_subnet is True:
			synth_entry_mbnoc4 = mbnoc4_stat(i,j,k)
			[inject_rate_mbnoc4, energy_mbnoc4, latency_mbnoc4, deflect_mbnoc4, throughput_mbnoc4] = split (synth_entry_mbnoc4)
			
			split_stat = [i, k, inject_rate_bless, inject_rate_mbnoc, inject_rate_mbnoc4, energy_bless, energy_mbnoc, energy_mbnoc4, latency_bless, latency_mbnoc, latency_mbnoc4, deflect_bless, deflect_mbnoc, deflect_mbnoc4, throughput_bless, throughput_mbnoc, throughput_mbnoc4]
			my_print.print_synth_varied_subnet (split_stat)
			# MBNoC4 vs. BLESS
			print 'MBNoC4 vs. MBNoC'
			avg_reduce_energy = average_reduce(energy_mbnoc4, inject_rate_mbnoc4, energy_mbnoc, inject_rate_mbnoc)
			print "Energy reduce by %.2f%%" % (avg_reduce_energy*100)
			avg_reduce_latency = average_reduce(latency_mbnoc4, inject_rate_mbnoc4, latency_mbnoc, inject_rate_mbnoc)
			print "Latency reduce by %.2f%%" % (avg_reduce_latency*100)
			avg_reduce_deflect = average_reduce(deflect_mbnoc4, inject_rate_mbnoc4, deflect_mbnoc, inject_rate_mbnoc)
			print "Deflection rate reduce by %.2f%%" % (avg_reduce_deflect*100)
			avg_gain_throughput = average_gain(throughput_mbnoc4, inject_rate_mbnoc4, throughput_mbnoc, inject_rate_mbnoc)
			print "Improve throughput by %.2f%%" % (avg_gain_throughput*100)
			#MBNoC4 vs. BLESS
			print 'MBNoC4 vs. BLESS'
			avg_reduce_energy = average_reduce(energy_mbnoc4, inject_rate_mbnoc4, energy_bless, inject_rate_bless)
			print "Energy reduce by %.2f%%" % (avg_reduce_energy*100)
			avg_reduce_latency = average_reduce(latency_mbnoc4, inject_rate_mbnoc4, latency_bless, inject_rate_bless)
			print "Latency reduce by %.2f%%" % (avg_reduce_latency*100)
			avg_reduce_deflect = average_reduce(deflect_mbnoc4, inject_rate_mbnoc4, deflect_bless, inject_rate_bless)
			print "Deflection rate reduce by %.2f%%" % (avg_reduce_deflect*100)
			avg_gain_throughput = average_gain(throughput_mbnoc4, inject_rate_mbnoc4, throughput_bless, inject_rate_bless)
			print "Improve throughput by %.2f%%" % (avg_gain_throughput*100)						
		else:
			kk = kk + 1
			# may need to sort metric based on injection rate
			split_stat = [i, k, inject_rate_bless, inject_rate_mbnoc, energy_bless, energy_mbnoc, latency_bless, latency_mbnoc, deflect_bless, deflect_mbnoc, throughput_bless, throughput_mbnoc]
			my_print.print_synth(split_stat) 
			# compute the avg gain/reduction
			avg_reduce_energy = average_reduce(energy_mbnoc, inject_rate_mbnoc, energy_bless, inject_rate_bless)
			print "Energy reduce by %.2f%%" % (avg_reduce_energy*100)
			avg_reduce_latency = average_reduce(latency_mbnoc, inject_rate_mbnoc, latency_bless, inject_rate_bless)
			print "Latency reduce by %.2f%%" % (avg_reduce_latency*100)
			avg_reduce_deflect = average_reduce(deflect_mbnoc, inject_rate_mbnoc, deflect_bless, inject_rate_bless)
			print "Deflection rate reduce by %.2f%%" % (avg_reduce_deflect*100)
			avg_gain_throughput = average_gain(throughput_mbnoc, inject_rate_mbnoc, throughput_bless, inject_rate_bless)
			print "Improve throughput by %.2f%%" % (avg_gain_throughput*100)





	
#print kk
print ("DONE :)")
