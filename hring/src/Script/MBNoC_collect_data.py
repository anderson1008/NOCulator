#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string
import collect_stat
import collections
import power
import my_print
import getopt

SIM_COUNT = 100
node = -1 # need to get from final_eval.py
subnet = -1# need to get from final_eval.py
insns_count = 10000001 # need to get from final_eval.py
class_factor = [0,0]
file_dir_bless = ''
file_dir_mbnoc = ''

# unit: power - uw; energy - uJ; time - second; deflection rate - flits/cycle; injection rate - flits/cycle/node; Throughput - flit/cycle
# component order: router, dff, portAllo, rc, xbar, local, permNet, link (if any)

mbnoc_clk_period = 1.18*10**(-9)
bless_clk_period = 1.44*10**(-9)
clk_scale_factor = bless_clk_period / mbnoc_clk_period
static_mbnoc = [206.58, 10.21, 1.92, 0.75, 93.14, 21.4, 20.73, 30.5]
static_bless = [306.87, 17.17, 2.69, 0.75, 159.36, 0.0, 50.58, 61.0]
switch_mbnoc = [3227.2, 14.53, 49.32, 12.13, 1971.7, 404.17, 902.81] # no link
switch_bless = [5417.4, 33.66, 60.02, 12.01, 3330.0, 0.0, 2392.3] # no link
internal_mbnoc = [9649.7, 411.74, 59.43, 18.09, 2958.7, 924.33, 1461.2] # no link
internal_bless = [16779.0, 687.57, 82.67, 17.84, 5294.9, 0.0, 3091.8] # no link
dynamic_mbnoc_link = 2650.5
dynamic_bless_link = 4344.6
default_toggle_rate = [0.1, 0.1, 0.1, 0.1, 0.1, 0.1, 0.1]
#global variables for output
final_stat = []
break_down_power = []
overall_power = []
synth_entry_mbnoc = []
synth_entry_bless = []
max_defl = 0

def main(argv):
	# to use it: [node, subnet] = main(sys.argv[1:])
	opts, args = getopt.getopt(argv,"n:s:", ['node=','subnet='])
	for opt, arg in opts:
		if opt in ("-n, --node"):
			node = int(arg)
		elif opt in ("-s, --subnet"):
			subnet = int(arg)  # only effective for MBNoC static power computation
	return [node, subnet]

def final(stat):
	# compute the final metrics of interest
	sum_ws = 0
	sum_energy = 0
	sum_throughput = 0
	sum_defl = 0
	for element in stat:
		sum_ws = sum_ws + element[1]
		sum_energy = sum_energy + element[2]
		sum_throughput = sum_throughput + element[3]
		sum_defl = sum_defl + element[4]
	if len(stat) is not 0:
		ws = float("{:.2f}".format(sum_ws / len(stat)/node * clk_scale_factor)) #normalized and scaled
		energy = float("{:.2f}".format(sum_energy / len(stat))) #normalized
		throughput = float("{:.2f}".format(sum_throughput / len(stat)))
		defl = float("{:.2f}".format(sum_defl / len(stat)))
	else:
		ws = 0
		energy = 0
		throughput = 0
		defl = 0
	
	return [len(stat), 1, ws, 1, energy, 1, throughput, 1, defl] # 1 represent the baseline; first element shows the number of data points


def evaluation():
	global synth_entry_mbnoc
	global synth_entry_bless
	global final_stat
	global break_down_power
	global overall_power
	global file_dir_bless
	global file_dir_mbnoc
	global max_defl

	synth_entry_mbnoc = [] # important to reset the global list.
	synth_entry_bless = [] # important to reset the global list.
	if node == 16:
		network = '4x4'
	elif node == 64:
		network = '8x8'
	elif node == 256:
		network = '16x16' 
	else:
		raise Exception('Undefined Network Size')

	if SIM_COUNT < 0:
		raise Exception('Simulate 0 File ?')
	if subnet < 1:
		raise Exception('NO subnet!')
	
	#file_dir_bless = file_dir_bless + network + '/'
	#file_dir_mbnoc = file_dir_mbnoc + network + '/'

	##insns_count = 10000001
	component_count_mbnoc = [0, 16, 1, 5, 1, 1, 1, 4] # only consider the inter-router links, not include local and bypass link
	component_count_mbnoc = [x*subnet for x in component_count_mbnoc]
	component_count_bless = [0, 14, 1, 5, 1, 1, 1, 4]
	port_on_compoment_mbnoc = [0, 1, 5, 1, 5, 5, 4, 1]
	port_on_compoment_bless = [0, 1, 5, 1, 5, 0, 4, 1]
	
	effective_count = 0
	overall_cycle_bless = 0
	overall_cycle_mbnoc = 0
	stat_aggr = []
	static_power_breakdown_mbnoc = [0, 0, 0, 0, 0, 0, 0, 0] 
	dynamic_power_breakdown_mbnoc  = [0, 0, 0, 0, 0, 0, 0, 0]
	overall_power_breakdown_mbnoc  = [0, 0, 0, 0, 0, 0, 0, 0] 
	static_power_breakdown_bless = [0, 0, 0, 0, 0, 0, 0, 0] 
	dynamic_power_breakdown_bless  = [0, 0, 0, 0, 0, 0, 0, 0]
	overall_power_breakdown_bless  = [0, 0, 0, 0, 0, 0, 0, 0] 


	for sim_index in range (1, SIM_COUNT+ 1, 1):
	
		## Collect result
		stat_bless = collect_stat.collect(sim_index, node, file_dir_bless, insns_count)
		stat_mbnoc = collect_stat.collect(sim_index, node, file_dir_mbnoc, insns_count)

		if (stat_bless == None or stat_mbnoc == None):
			continue			
	
		## comupte weighted speedup
		effective_count = effective_count + 1
		ipc_alone = stat_bless['ipc']
		ipc_share = stat_mbnoc['ipc']
		active_cycle_mbnoc = stat_mbnoc['active_cycles']
		active_cycle_bless = stat_bless['active_cycles']
		accm_ws = 0
		for i in range (0, node, 1):
			accm_ws = float(ipc_share [i]) / float(ipc_alone[i]) + accm_ws
			overall_cycle_bless = overall_cycle_bless + int(active_cycle_bless[i])
			overall_cycle_mbnoc = overall_cycle_mbnoc + int(active_cycle_mbnoc[i])
		w_speedup_mbnoc = float("{:.2f}".format(accm_ws))
		throughput_bless = float(stat_bless['throughput'])
		throughput_mbnoc = float(stat_mbnoc['throughput'])
		normalized_throughput = float("{:.2f}".format(throughput_mbnoc / throughput_bless/subnet*clk_scale_factor)) # scaled by subnet size because of the flit size is smaller
		deflect_flit_per_cycle_bless = float(stat_bless['deflect_flit_per_cycle'])
		deflect_flit_per_cycle_mbnoc = float(stat_mbnoc['deflect_flit_per_cycle'])/subnet
		if deflect_flit_per_cycle_bless	> 0: normalized_deflection_rate = float("{:.2f}".format(deflect_flit_per_cycle_mbnoc / deflect_flit_per_cycle_bless)) # scaled by subnet size because of the flit size is smaller
		else: normalized_deflection_rate = 1
		deflect_per_flit_bless =  float(stat_bless['deflect_per_flit'])
		if max_defl < deflect_per_flit_bless: max_defl = deflect_per_flit_bless
		mpki = float(stat_mbnoc['mpki'])

		## energy computation
		# event order: router, dff, portAllo, rc, xbar, local, permNet, link
		traversal_mbnoc = float(stat_mbnoc['traversal'])
		permute_mbnoc = float(stat_mbnoc['permute'])
		link_mbnoc = traversal_mbnoc - float(stat_mbnoc['inj_flit'])*2 - float(stat_mbnoc['bypass'])
		event_mbnoc = [0, traversal_mbnoc*3, traversal_mbnoc, traversal_mbnoc, traversal_mbnoc, traversal_mbnoc, permute_mbnoc, link_mbnoc]
		actual_toggle_rate_mbnoc = power.comp_toggle_rate (event_mbnoc, component_count_mbnoc, port_on_compoment_mbnoc, overall_cycle_mbnoc)
		link_toggle_rate_mbnoc = actual_toggle_rate_mbnoc.pop(7)

		traversal_bless = float(stat_bless['traversal'])
		permute_bless = float(stat_bless['permute'])
		link_bless = traversal_bless - float(stat_bless['inj_flit'])*2
		event_bless = [0, traversal_bless*3, traversal_bless, traversal_bless, traversal_bless, 0, permute_bless, link_bless]
		actual_toggle_rate_bless = power.comp_toggle_rate (event_bless, component_count_bless, port_on_compoment_bless, overall_cycle_bless)
		link_toggle_rate_bless = actual_toggle_rate_bless.pop(7)
	
		simul_time_mbnoc = mbnoc_clk_period * overall_cycle_mbnoc # in second
		simul_time_bless = bless_clk_period * overall_cycle_bless # in second

		dynamic_breakdown_mbnoc = power.comp_dynamic_power (switch_mbnoc, internal_mbnoc, actual_toggle_rate_mbnoc, dynamic_mbnoc_link, link_toggle_rate_mbnoc, simul_time_mbnoc, default_toggle_rate)
		dynamic_breakdown_bless = power.comp_dynamic_power (switch_bless, internal_bless, actual_toggle_rate_bless, dynamic_bless_link, link_toggle_rate_bless, simul_time_bless, default_toggle_rate)
		static_breakdown_mbnoc = power.comp_static_power (static_mbnoc, component_count_mbnoc, simul_time_mbnoc)
		static_breakdown_bless = power.comp_static_power (static_bless, component_count_bless, simul_time_bless)
		overall_breakdown_mbnoc = [x+y for x,y in zip(static_breakdown_mbnoc, dynamic_breakdown_mbnoc)]
		overall_breakdown_bless = [x+y for x,y in zip(static_breakdown_bless, dynamic_breakdown_bless)]
		sum_dynamic_mbnoc = sum (dynamic_breakdown_mbnoc)
		sum_dynamic_bless = sum (dynamic_breakdown_bless)
		sum_static_mbnoc = sum (static_breakdown_mbnoc)
		sum_static_bless = sum (static_breakdown_bless)
		energy_mbnoc = sum_dynamic_mbnoc + sum_static_mbnoc # mbnoc power
		energy_bless = sum_dynamic_bless + sum_static_bless # bless power
		sum_energy_mbnoc = sum_dynamic_mbnoc + energy_mbnoc # mbnoc power
		sum_energy_bless = sum_dynamic_bless + energy_bless # bless power
		
		static_power_breakdown_mbnoc = [x+y for x,y in zip (static_power_breakdown_mbnoc, static_breakdown_mbnoc)]
		static_power_breakdown_bless = [x+y for x,y in zip (static_power_breakdown_bless, static_breakdown_bless)]
		dynamic_power_breakdown_mbnoc = [x+y for x,y in zip (dynamic_power_breakdown_mbnoc, dynamic_breakdown_mbnoc)]
		dynamic_power_breakdown_bless = [x+y for x,y in zip (dynamic_power_breakdown_bless, dynamic_breakdown_bless)]
		overall_power_breakdown_mbnoc = [x+y for x,y in zip (overall_power_breakdown_mbnoc, overall_breakdown_mbnoc)]
		overall_power_breakdown_bless = [x+y for x,y in zip (overall_power_breakdown_bless, overall_breakdown_bless)]

		normalized_energy = sum_energy_mbnoc / sum_energy_bless
		
		stat_element = (mpki, w_speedup_mbnoc, float("{:.2f}".format(normalized_energy)), normalized_throughput, normalized_deflection_rate)
		stat_aggr.append(stat_element)

		###### For synthetic traffic
		inject_rate_mbnoc = "{:.2f}".format(float(stat_mbnoc['inject_rate'])/subnet) # must be scaled based on the flit size
		tot_latency_mbnoc = "{:.2f}".format(float(stat_mbnoc['pkt_tot_latency'])/clk_scale_factor)
		synth_entry_mbnoc.append((inject_rate_mbnoc, "{:.2f}".format(energy_mbnoc), tot_latency_mbnoc, str("{:.2f}".format(throughput_mbnoc/subnet*clk_scale_factor)), str(deflect_flit_per_cycle_mbnoc)))
		inject_rate_bless = "{:.2f}".format(float(stat_bless['inject_rate']))
		tot_latency_bless = "{:.2f}".format(float(stat_bless['pkt_tot_latency']))
		synth_entry_bless.append((inject_rate_bless, "{:.2f}".format(energy_bless), tot_latency_bless, str(throughput_bless), str(deflect_flit_per_cycle_bless)))

	sorted_stat = sorted(stat_aggr, key=lambda tup: tup[0])
	if effective_count is not 0:
		static_power_breakdown_mbnoc = [float("{:.2f}".format(x / effective_count)) for x in static_power_breakdown_mbnoc]
		dynamic_power_breakdown_mbnoc  = [float("{:.2f}".format(x / effective_count)) for x in dynamic_power_breakdown_mbnoc]
		overall_power_breakdown_mbnoc  = [float("{:.2f}".format(x / effective_count)) for x in overall_power_breakdown_mbnoc] 
		static_power_breakdown_bless = [float("{:.2f}".format(x / effective_count)) for x in static_power_breakdown_bless]
		dynamic_power_breakdown_bless  = [float("{:.2f}".format(x / effective_count)) for x in dynamic_power_breakdown_bless]
		overall_power_breakdown_bless  = [float("{:.2f}".format(x / effective_count)) for x in overall_power_breakdown_bless]
		overall_static_mbnoc = sum(static_power_breakdown_mbnoc)
		overall_dynamic_mbnoc = sum(dynamic_power_breakdown_mbnoc)
		overall_static_bless = sum(static_power_breakdown_bless)
		overall_dynamic_bless = sum(dynamic_power_breakdown_bless)
	else:
		static_power_breakdown_mbnoc = 0
		dynamic_power_breakdown_mbnoc  = 0
		overall_power_breakdown_mbnoc  = 0
		static_power_breakdown_bless = 0
		dynamic_power_breakdown_bless  = 0
		overall_power_breakdown_bless  = 0
		overall_static_mbnoc = 0
		overall_dynamic_mbnoc = 0	
		overall_static_bless = 0
		overall_dynamic_bless = 0
	overall_mbnoc = overall_static_mbnoc + overall_dynamic_mbnoc
	overall_bless = overall_static_bless + overall_dynamic_bless
	break_down_power = [static_power_breakdown_bless, static_power_breakdown_mbnoc,dynamic_power_breakdown_bless, dynamic_power_breakdown_mbnoc,overall_power_breakdown_bless,overall_power_breakdown_mbnoc]
	overall_power = [overall_static_bless, overall_dynamic_bless, overall_bless, overall_static_mbnoc, overall_dynamic_mbnoc, overall_mbnoc]
	#my_print.print_power_breakdown(break_down_power)
	#my_print.print_power(overall_power)

	## Categorize stat based on mpki
	low_stat = []
	medium_stat = []
	high_stat = []
	for element in sorted_stat:
		if element[0] <= class_factor[0]:
			low_stat.append(element)
		elif element[0] <= class_factor[1]:
			medium_stat.append(element)
		else:
			high_stat.append(element)

	final_low_stat = final (low_stat)
	final_med_stat = final (medium_stat)
	final_high_stat = final (high_stat)
	final_avg_stat = final (sorted_stat)
	final_stat = [final_low_stat, final_med_stat, final_high_stat, final_avg_stat]
	#final_output = my_print.print_final_stat(final_stat)
	return final_stat
## end of evaluation()
		
			

			

	

