#!/usr/bin/python

# Use to compute and extract the final result for synthetic simulation.


import MBNoC_collect_data
import my_print
import collect_stat
import sys

# CHANGE ME
varied_subnet = True # set it to True while comparing BLESS, MBNOC1-4


# Each metric of interest is installed as: metric (traffic, design, network)

if varied_subnet is True:
	traffic = ['uniform_random']
	network = ['4x4']
	node = [16]
else:
	#traffic = ['bit_complement', 'transpose', 'uniform_random']
	traffic = ['uniform_random']
	network = ['4x4', '8x8', '16x16']
	node = [16, 64, 256]

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
	
	energy = remove_dup (inj_rate, energy)
	latency = remove_dup (inj_rate, latency)
	deflect = remove_dup (inj_rate, deflect)
	throughput = remove_dup (inj_rate, throughput)
	inj_rate = remove_dup (inj_rate, inj_rate)
	return [inj_rate, energy, latency, deflect, throughput]	

def remove_dup (inject_rate, stat):
# to remove duplicated entry and form a dictionary such that we can extract and compare the metric under the same injection rate
	dict_stat = {}
	for x, y in zip(inject_rate, stat):
		if x in dict_stat:
			if y > dict_stat[x]:
				dict_stat[x] = y # pick the larger one
		else:		
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

def avg_dict_stat (my_stat, cutoff_load):
	sum_stat = 0
	effective_count = 0
	for key, value in my_stat.items():
		if float(key) < cutoff_load:
			sum_stat = sum_stat + float(value)
			effective_count = effective_count + 1
	if effective_count > 0: return float(sum_stat) / effective_count
	return 0

def get_cutoff_load (latency):
	cutoff_load = 1
	zero_load_latency = sys.float_info.max	
	
	for key, value in latency.items():
		if float(value) < zero_load_latency and float(value) > 0: 
			zero_load_latency = float(value)

	largest_cutoff_load = -1
	saturate_latency = zero_load_latency * 3
	for key, value in latency.items():
		if float(key) > 0.8:  # just to remove the outliners
			continue
		if float(value) < saturate_latency:
			if float(value) > largest_cutoff_load:
				largest_cutoff_load = float(value)
				cutoff_load = float(key)
		else:
			continue
	return 1
	#return cutoff_load
			
	

def cmp_stat (traffic, size, network, stat_dir, subnet, scale):
# extract mbnoc4 stat (subnet=4)
	stat_out = []
	if scale is True: scale_factor = MBNoC_collect_data.clk_scale_factor
	else: scale_factor = 1
	for sim_index in range (1, MBNoC_collect_data.SIM_COUNT + 1, 1):
		stat = collect_stat.collect(sim_index, size, stat_dir, MBNoC_collect_data.insns_count)
		if stat is None:
			continue
		inject_rate = "{:.2f}".format(float(stat['inject_rate'])/subnet) # must be scaled based on the flit size	
		latency = "{:.2f}".format(float(stat['pkt_net_latency'])/scale_factor)
		deflect_flit_per_cycle = "{:.2f}".format(float(stat['deflect_flit_per_cycle'])/subnet)
		throughput = "{:.2f}".format(float(stat['throughput'])/subnet*scale_factor)
		stat_out.append((inject_rate, 0.00, latency, throughput, deflect_flit_per_cycle)) # energy is 0, unless I wanna have some fun to get it.
	return stat_out

def extract_delta (baseline, mydesign):
	old_value_baseline = 0
	old_value_mydesign = 0
	effective_count = 0
	delta_sum = 0
	for i in range (1, 100, 1):
		load = "{:.2f}".format(float(i)/100)
		if load not in baseline and load  not in mydesign:
			continue

		effective_count = effective_count + 1
		if load in baseline:
			new_value_baseline = float(baseline[load])
			old_value_baseline = float(baseline[load])
		else:
			new_value_baseline = old_value_baseline

		if load in mydesign:
			new_value_mydesign = float(mydesign[load])
			old_value_mydesign = float(mydesign[load])
		else:
			new_value_mydesign = old_value_mydesign

		delta_sum = delta_sum + float(new_value_baseline - new_value_mydesign)/new_value_baseline

	if effective_count is 0: return 0

	return delta_sum / effective_count
			


kk = 0
for i in traffic:
	for j,k in zip (node, network):		
		mbnoc_nobypass_dir = '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + i + '/MBNoC_NoBypass/' + k + '/'
		mbnoc_noBridgeSubnet_dir = '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + i + '/MBNoC_noBridgeSubnet/' + k + '/'
		mbnoc1_dir = '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + i + '/MBNoC1/' + k + '/'
		mbnoc2_dir = '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + i + '/MBNoC/' + k + '/'
		mbnoc3_dir = '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + i + '/MBNoC3/' + k + '/'
		mbnoc4_dir =  '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + i + '/MBNoC4/' + k + '/'
		bless_dir =  '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + i + '/BLESS/' + k + '/'
		
		if varied_subnet is True:
			synth_entry_bless = cmp_stat(i,j,k,bless_dir,1, False)
			synth_entry_mbnoc1 = cmp_stat(i,j,k,mbnoc1_dir ,1, True)
			synth_entry_mbnoc2 = cmp_stat(i,j,k,mbnoc2_dir ,2, True)
			synth_entry_mbnoc3 = cmp_stat(i,j,k,mbnoc3_dir ,3, True)
			synth_entry_mbnoc4 = cmp_stat(i,j,k,mbnoc4_dir ,4, True)

			[inject_rate_bless, energy_bless, latency_bless, deflect_bless, throughput_bless] = split (synth_entry_bless)
			[inject_rate_mbnoc1, energy_mbnoc1, latency_mbnoc1, deflect_mbnoc1, throughput_mbnoc1] = split (synth_entry_mbnoc1)
			[inject_rate_mbnoc2, energy_mbnoc2, latency_mbnoc2, deflect_mbnoc2, throughput_mbnoc2] = split (synth_entry_mbnoc2)
			[inject_rate_mbnoc3, energy_mbnoc3, latency_mbnoc3, deflect_mbnoc3, throughput_mbnoc3] = split (synth_entry_mbnoc3)
			[inject_rate_mbnoc4, energy_mbnoc4, latency_mbnoc4, deflect_mbnoc4, throughput_mbnoc4] = split (synth_entry_mbnoc4)

			split_stat = [i, k, latency_bless, latency_mbnoc1, latency_mbnoc2, latency_mbnoc3, latency_mbnoc4, throughput_bless, throughput_mbnoc1, throughput_mbnoc2, throughput_mbnoc3, throughput_mbnoc4, deflect_bless, deflect_mbnoc1, deflect_mbnoc2, deflect_mbnoc3, deflect_mbnoc4]
			design = ['BLESS', 'MBNoC1', 'MBNoC2', 'MBNoC3', 'MBNoC4']
			my_print.print_synth_wrt_load (split_stat, design)

			delta_latency_mbnoc1 = extract_delta (latency_bless, latency_mbnoc1)
			delta_latency_mbnoc2 = extract_delta (latency_bless, latency_mbnoc2)
			delta_latency_mbnoc3 = extract_delta (latency_bless, latency_mbnoc3)
			delta_latency_mbnoc4 = extract_delta (latency_bless, latency_mbnoc4)
			delta_throughput_mbnoc1 = extract_delta (throughput_bless, throughput_mbnoc1)
			delta_throughput_mbnoc2 = extract_delta (throughput_bless, throughput_mbnoc2)
			delta_throughput_mbnoc3 = extract_delta (throughput_bless, throughput_mbnoc3)
			delta_throughput_mbnoc4 = extract_delta (throughput_bless, throughput_mbnoc4)
			delta_deflect_mbnoc1 = extract_delta (deflect_bless, deflect_mbnoc1)
			delta_deflect_mbnoc2 = extract_delta (deflect_bless, deflect_mbnoc2)
			delta_deflect_mbnoc3 = extract_delta (deflect_bless, deflect_mbnoc3)
			delta_deflect_mbnoc4 = extract_delta (deflect_bless, deflect_mbnoc4)			
			

			#cutoff_load_bless = get_cutoff_load(latency_bless)
			#cutoff_load_mbnoc1 = get_cutoff_load(latency_mbnoc1)
			#cutoff_load_mbnoc2 = get_cutoff_load(latency_mbnoc2)
			#cutoff_load_mbnoc4 = get_cutoff_load(latency_mbnoc4)

			# Normalized Comparison (w.r.t. BLESS)

			#avg_latency_bless = avg_dict_stat(latency_bless, cutoff_load_bless)
			#avg_latency_mbnoc1 = avg_dict_stat(latency_mbnoc1, cutoff_load_mbnoc1)
			#avg_latency_mbnoc2 = avg_dict_stat(latency_mbnoc2, cutoff_load_mbnoc2)
			#avg_latency_mbnoc4 = avg_dict_stat(latency_mbnoc4, cutoff_load_mbnoc4, )

			#avg_throughput_bless = avg_dict_stat(throughput_bless, cutoff_load_bless)
			#avg_throughput_mbnoc1 = avg_dict_stat(throughput_mbnoc1, cutoff_load_mbnoc1)
			#avg_throughput_mbnoc2 = avg_dict_stat(throughput_mbnoc2, cutoff_load_mbnoc2)
			#avg_throughput_mbnoc4 = avg_dict_stat(throughput_mbnoc4, cutoff_load_mbnoc4, )

			#avg_deflect_bless = avg_dict_stat(deflect_bless, cutoff_load_bless)
			#avg_deflect_mbnoc1 = avg_dict_stat(deflect_mbnoc1, cutoff_load_mbnoc1)
			#avg_deflect_mbnoc2 = avg_dict_stat(deflect_mbnoc2, cutoff_load_mbnoc2)
			#avg_deflect_mbnoc4 = avg_dict_stat(deflect_mbnoc4, cutoff_load_mbnoc4)


			# Compare latency
			# in the order specified in design variable : ['BLESS', 'MBNoC1', 'MBNoC2', 'MBNoC4', 'dualRing', 'noBypass']
			#avg_latency = [avg_latency_bless, avg_latency_mbnoc1, avg_latency_mbnoc2, avg_latency_mbnoc4]
			#avg_throughput = [avg_throughput_bless, avg_throughput_mbnoc1, avg_throughput_mbnoc2, avg_throughput_mbnoc4]
			#avg_deflect = [avg_deflect_bless, avg_deflect_mbnoc1, avg_deflect_mbnoc2, avg_deflect_mbnoc4]
			design.pop(0)
			print '\n\n ---------------   Latency Reduction ----------------------------- \n'
			my_print.print_final ([delta_latency_mbnoc1, delta_latency_mbnoc2, delta_latency_mbnoc3, delta_latency_mbnoc4],design)
			#my_print.print_synth_avg_reduction (avg_latency, design)
			print '\n\n ---------------   Throughput Improvement ----------------------------- \n'
			my_print.print_final ([-delta_throughput_mbnoc1, -delta_throughput_mbnoc2, -delta_throughput_mbnoc3, -delta_throughput_mbnoc4],design)
			#my_print.print_synth_avg_gain (avg_throughput, design)
			print '\n\n ---------------   Deflection Rate Reduction ----------------------------- \n'
			my_print.print_final ([delta_deflect_mbnoc1, delta_deflect_mbnoc2, delta_deflect_mbnoc3, delta_deflect_mbnoc4],design)
			#my_print.print_synth_avg_reduction (avg_deflect, design)						
		else:
			kk = kk + 1
			design = ['BLESS', 'MBNoC']
			MBNoC_collect_data.synthetic = True
			MBNoC_collect_data.subnet = 2
			MBNoC_collect_data.insns_count = 100000
			MBNoC_collect_data.SIM_COUNT = 500

			MBNoC_collect_data.node = j
			MBNoC_collect_data.file_dir_bless = '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + i + '/BLESS/' + k + '/'
			MBNoC_collect_data.file_dir_mbnoc = '/home/anderson/Desktop/NOCulator/hring/src/results/Synthetic/' + i + '/MBNoC/' + k + '/'
			MBNoC_collect_data.evaluation()
			synth_entry_mbnoc = MBNoC_collect_data.synth_entry_mbnoc
			synth_entry_bless = MBNoC_collect_data.synth_entry_bless
		
			[inject_rate_bless, energy_bless, latency_bless, deflect_bless, throughput_bless] = split (synth_entry_bless)
			[inject_rate_mbnoc, energy_mbnoc, latency_mbnoc, deflect_mbnoc, throughput_mbnoc] = split (synth_entry_mbnoc)
			# may need to sort metric based on injection rate
			split_stat = [i, k, energy_bless, energy_mbnoc, latency_bless, latency_mbnoc, deflect_bless, deflect_mbnoc, throughput_bless, throughput_mbnoc]
			my_print.print_synth(split_stat, design) 
			# compute the avg gain/reduction
			#avg_reduce_energy = average_reduce(energy_mbnoc, inject_rate_mbnoc, energy_bless, inject_rate_bless)
			avg_reduce_energy = extract_delta(energy_bless, energy_mbnoc)
			print "Energy reduce by %.2f%%" % (avg_reduce_energy*100)
			#avg_reduce_latency = average_reduce(latency_mbnoc, inject_rate_mbnoc, latency_bless, inject_rate_bless)
			avg_reduce_latency = extract_delta(latency_bless, latency_mbnoc)
			print "Latency reduce by %.2f%%" % (avg_reduce_latency*100)
			#avg_reduce_deflect = average_reduce(deflect_mbnoc, inject_rate_mbnoc, deflect_bless, inject_rate_bless)
			avg_reduce_deflect = extract_delta(deflect_bless, deflect_mbnoc)
			print "Deflection rate reduce by %.2f%%" % (avg_reduce_deflect*100)
			#avg_gain_throughput = average_gain(throughput_mbnoc, inject_rate_mbnoc, throughput_bless, inject_rate_bless)
			avg_gain_throughput = extract_delta(throughput_bless, throughput_mbnoc)
			print "Improve throughput by %.2f%%" % (-avg_gain_throughput*100)





	
#print kk
print ("DONE :)")
