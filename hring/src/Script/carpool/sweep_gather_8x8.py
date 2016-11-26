#!/usr/bin/python

import sys
import os


def sweep_uc ():
	workload_dir = "../../bin/workload_list/"

	# 64-node BLESS with gather enabled
	workload = "workloads_null"
	network_nrX = "8"
	network_nrY = "8"
	router_addrPacketSize = "1"
	router_dataPacketSize = "4"
	router_maxPacketSize = "4"
	topology = "Mesh"
	router_algorithm = "DR_FLIT_SW_OF_MC"
	randomize_defl = "true"
        adaptiveMC = "false"
        mc_degree = "0"
        scatterEnable = "false"
        multicast = "false"
	mergeEnable = "true"
	synthPattern = "HS"
	mc_rate = 0

	global out_dir, hs_rate

	if not os.path.exists(out_dir):
		os.makedirs(out_dir)

	synth_rate = 0
	for sim_index in range(1, 16, 1):
		print ("New Simulation!")
		out_file = "sim_" + str(sim_index) + ".out"
        	synth_rate = synth_rate + 0.02
		command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable + " -adaptiveMC " + adaptiveMC + " -scatterEnable " + scatterEnable + " -synthPattern " + synthPattern
		os.system (command_line)

def sweep_hs ():
	workload_dir = "../../bin/workload_list/"

	# 64-node BLESS with gather enabled
	workload = "workloads_null"
	network_nrX = "8"
	network_nrY = "8"
	router_addrPacketSize = "1"
	router_dataPacketSize = "4"
	router_maxPacketSize = "4"
	topology = "Mesh"
	router_algorithm = "DR_FLIT_SW_OF_MC"
	randomize_defl = "true"
        adaptiveMC = "false"
        mc_degree = "0"
        scatterEnable = "false"
        multicast = "false"
	mergeEnable = "true"
	synthPattern = "HS"
        mc_rate = 0
	global out_dir, synth_rate

	if not os.path.exists(out_dir):
		os.makedirs(out_dir)

	hs_rate = 0
	for sim_index in range(1, 11, 1):
		print ("New Simulation!")
		out_file = "sim_" + str(sim_index) + ".out"
        	hs_rate = hs_rate + 0.05
		command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable + " -adaptiveMC " + adaptiveMC + " -scatterEnable " + scatterEnable + " -synthPattern " + synthPattern
		os.system (command_line)

## Sweep unicast injection rate under specified hs_rate
### hs_rate = 0.1, 0.2, 0.3, 0.4, 0.5
hs_rate = 0
for i in range (1, 6, 1):
	hs_rate = + hs_rate + 0.1
	out_dir = "./preliminary/synthSweep/carpool/hotspot/uc_sweep/hs_" + str(hs_rate) +"/"
	sweep_uc()

## Sweep hotspot 0.1-0.5 with 0.05 increment
### under unicast rate of 0.1, 0.2, 0.3, 0.4, 0.5 
synth_rate = 0
for i in range (1, 6, 1):
	synth_rate = synth_rate + 0.1
	out_dir = "./preliminary/synthSweep/carpool/hotspot/hs_sweep/uc_" + str(synth_rate) + "/"
	sweep_mc()
