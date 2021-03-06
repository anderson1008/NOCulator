#!/usr/bin/python

import sys
import os

workload_dir = "../bin/workload_list/"
SIM_NUM = 100

# 16-node MBNoC
out_dir = "../results/MBNoC_noBypass/4x4/"
workload = "hetero_workload_4x4"
router_algorithm = "BLESS_BYPASS"
router_addrPacketSize = "2"
router_dataPacketSize = "8"
router_maxPacketSize = "8"
network_nrX = "4"
network_nrY = "4"
topology = "Mesh_Multi"
bypass_enable = "false" # disable the bypass channel

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' ' + str (sim_index) + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + ' -bypass_enable ' + bypass_enable
	os.system (command_line)
