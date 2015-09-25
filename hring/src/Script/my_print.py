#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string

def print_double_array (x):
	for x_i in x:
		sys.stdout.write(str("%.2f" % x_i) + ' ')
	print "\n"
	sys.stdout.flush()
	
def print_int_array (x):
	for x_i in x:
		sys.stdout.write(str(x_i) + ' ')
	print "\n"
	sys.stdout.flush()

def print_stat_dict (my_stat):		
	for key, value in iter(sorted(my_stat.iteritems())):
		if type(value) is not list:		
			print key.ljust(20), value.ljust(20)
#		else:
#			for element in value:
#				print element

def print_power (stat):
	output_str = '\n\n#############    Power Distribution  ################\n\n'
	output_str = output_str + ''.ljust(15) + 'Static'.ljust(20) + 'Dynamic'.ljust(20) + 'Overall'.ljust(20) + '\n'
	## print BLESS
	static_percent = "{:.2f}".format(stat[0]/stat[2]*100)
	dynamic_percent = "{:.2f}".format(stat[1]/stat[2]*100)
	output_str = output_str + 'BLESS'.ljust(15) + ('%s (%s%%)'%("{:.2f}".format(stat[0]),static_percent)).ljust(20) + ('%s (%s%%)'%("{:.2f}".format(stat[1]),dynamic_percent)).ljust(20) + str(stat[2]).ljust(20) + '\n'
	# print MBNoC
	static_percent = "{:.2f}".format(stat[3]/stat[5]*100)
	dynamic_percent = "{:.2f}".format(stat[4]/stat[5]*100)
	output_str = output_str + 'MBNoC'.ljust(15) + ('%s (%s%%)'%("{:.2f}".format(stat[3]),static_percent)).ljust(20) + ('%s (%s%%)'%("{:.2f}".format(stat[4]),dynamic_percent)).ljust(20) + str(stat[5]).ljust(20)
	output_str = output_str + '\n'	

	print output_str





def print_power_breakdown (stat):
	output_str = '\n\n#############    Power Breakdown   ################\n\n'
	output_str = output_str + ''.ljust(15) + 'Static'.ljust(20) + 'Dynamic'.ljust(20) + 'Overall'.ljust(20) + '\n'
	output_str = output_str + 'Component'.ljust(15) + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + '\n'
	print_order = ['DFF', 'portAlloc', 'RC', 'Xbar', 'Local', 'permNet', 'link']
	for component in range (0, 7):
		output_str = output_str + print_order[component].ljust(15)
		for metric in stat:
			output_str = output_str + str(metric[component+1]).ljust(10)
		output_str = output_str + '\n'	

	print output_str

def print_final_stat (stat):
	output_str = '\n\n#############    Overall    ################\n\n'
	output_str = output_str + ''.ljust(20) + 'weighted_speedup'.ljust(20) + 'Energy'.ljust(20) + 'Throughput'.ljust(20) + 'Defection Rate'.ljust(20) + '\n'
	output_str = output_str + 'Load'.ljust(10) + 'Count'.ljust(10) 
	for i in range (0, 4):
		output_str = output_str + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) 
	output_str = output_str + '\n' + 'Low'.ljust(10)	
	for metric in stat[0]:
		output_str = output_str + str(metric).ljust(10)
	output_str = output_str + '\n'
	output_str = output_str + 'Medium'.ljust(10)	
	for metric in stat[1]:
		output_str = output_str + str(metric).ljust(10)
	output_str = output_str + '\n'
	output_str = output_str + 'High'.ljust(10)	
	for metric in stat[2]:
		output_str = output_str + str(metric).ljust(10)
	output_str = output_str + '\n'
	output_str = output_str + 'Average'.ljust(10)
	for metric in stat[3]:
		output_str = output_str + str(metric).ljust(10)
	output_str = output_str + '\n'
	print output_str
	return output_str

def print_for_plot (stat):
	output_str = '\n\n#############    Print for plot    ################\n\n'
	output_str = output_str + 'Baseline of each metrics of interest is 1.\nEach metric is normailized to BLESS with the same network size.\n\n'
	output_str = output_str + 'Load'.ljust(8) + 'Count'.ljust(8) + 'ws'.ljust(8) + '4x4'.ljust(8) + '8x8'.ljust(8) + '16x6'.ljust(8) + 'engy'.ljust(8) + '4x4'.ljust(8) + '8x8'.ljust(8) + '16x16'.ljust(8) + 'th'.ljust(8) + '4x4'.ljust(8) + '8x8'.ljust(8) + '16x16'.ljust(8) + 'defl'.ljust(8) + '4x4'.ljust(8) + '8x8'.ljust(8) + '16x16'.ljust(8) + '\n'
	groups = ['Low','Medium','High','Average']
	i = 0
	for element in stat:
		output_str = output_str + groups[i].ljust(8)
		for metric in element:	
			output_str = output_str + str(metric).ljust(8)
		i = i + 1
		output_str = output_str + '\n'
		
	print output_str
	return output_str

def print_synth (stat):
	traffic = str(stat.pop(0))
	network = str(stat.pop(0))
	output_str = '\n\n#############    ' + "Traffic = " + traffic.ljust(20) + "Network = " + network.ljust(20) + '   ################\n\n'
	output_str = output_str + 'Inject_rate'.ljust(20) + 'Energy'.ljust(20) + 'Latency'.ljust(20) + 'Deflect_rate'.ljust(20) + 'Throughput'.ljust(20) + '\n\n'
	output_str = output_str + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + '\n'
	for i in range (0, len(stat[0])):
		for j in range (0, len(stat)):
			output_str = output_str + str(stat[j][i]).ljust(10)
		output_str = output_str + '\n'
	output_str = output_str + '********* Based on %u data points ************' % len(stat[0])
	print output_str
		
def print_synth_varied_subnet (stat):
	traffic = str(stat.pop(0))
	network = str(stat.pop(0))
	output_str = '\n\n#############    ' + "Traffic = " + traffic.ljust(20) + "Network = " + network.ljust(20) + '   ################\n\n'
	output_str = output_str + 'Inject_rate'.ljust(30) + 'Energy'.ljust(30) + 'Latency'.ljust(30) + 'Deflect_rate'.ljust(30) + 'Throughput'.ljust(30) + '\n\n'
	output_str = output_str + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + 'MBNoC4'.ljust(10) + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + 'MBNoC4'.ljust(10) + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + 'MBNoC4'.ljust(10) + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + 'MBNoC4'.ljust(10) + 'BLESS'.ljust(10) + 'MBNoC'.ljust(10) + 'MBNoC4'.ljust(10) + '\n'	
	for i in range (0, len(stat[0])):
		for j in range (0, len(stat)):
			if i < len(stat[j]):
				output_str = output_str + str(stat[j][i]).ljust(10)
			else:
				output_str = output_str + '0.00'.ljust(10)
		output_str = output_str + '\n'
	output_str = output_str + '********* Based on %u data points ************' % len(stat[0])
	print output_str
























	
