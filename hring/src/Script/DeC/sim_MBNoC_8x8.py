#!/usr/bin/python

import sys
import os

workload_dir = "../bin/workload_list/"
SIM_NUM = 100

# 64-node MBNoC
out_dir = "../results/MBNoC/8x8/"
workload = "hetero_workload_8x8"
network_nrX = "8"
network_nrY = "8"
router_addrPacketSize = "2"
router_dataPacketSize = "8"
router_maxPacketSize = "8"
topology = "Mesh_Multi"
router_algorithm = "BLESS_BYPASS"

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' ' + str (sim_index) + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology
	os.system (command_line)

