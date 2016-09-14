#!/usr/bin/python

import sys
import os

workload_dir = "../bin/workload_list/"
SIM_NUM = 500
insns = "100000"

# # # # # # # # # # # # # # # # # # # # # # # # # # 16-node BLESS # # # # # # # # # # # # # # # # # # # # # # # # # #

####################################    uniform_random    #########################################


traffic = 'uniform_random' # [bit_complement, transpose, uniform_random]
out_dir = "../results/Synthetic_16B/" + traffic + "/MBNoC3/4x4/"
synth_reads_fraction = 0.8
synth_rate_base = 0.0005
bSynthBitComplement = "false"
bSynthTranspose = "false"
bSynthHotspot = "false"
randomHotspot = "false"

workload = "workloads_null"
router_algorithm = "BLESS_BYPASS"
router_addrPacketSize = "3"
router_dataPacketSize = "3"
router_maxPacketSize = "3"
network_nrX = "4"
network_nrY = "4"
topology = "Mesh_Multi"
sub_net = "3"
subnet_sel_rand = "false"
bridge_subnet = "true"

#  Injection rate sweep: 0.0005 - 0.1 at 0.0005 internal

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	synth_rate = str(synth_rate_base * sim_index)
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " +  out_dir + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " +  network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -synth_rate " + synth_rate + ' -insns ' + insns + ' -bSynthBitComplement ' + bSynthBitComplement + ' -bSynthTranspose ' + bSynthTranspose + ' -bSynthHotspot ' + bSynthHotspot + ' -randomHotspot ' + randomHotspot + ' -sub_net ' + sub_net + ' -subnet_sel_rand ' + subnet_sel_rand + ' -bridge_subnet ' + bridge_subnet
	os.system (command_line)

out_dir = "../results/Synthetic_16B/" + traffic + "/MBNoC3/8x8/"
network_nrX = "8"
network_nrY = "8"

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	synth_rate = str(synth_rate_base * sim_index)
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " +  out_dir + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " +  network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -synth_rate " + synth_rate + ' -insns ' + insns + ' -bSynthBitComplement ' + bSynthBitComplement + ' -bSynthTranspose ' + bSynthTranspose + ' -bSynthHotspot ' + bSynthHotspot + ' -randomHotspot ' + randomHotspot + ' -sub_net ' + sub_net + ' -subnet_sel_rand ' + subnet_sel_rand + ' -bridge_subnet ' + bridge_subnet
	os.system (command_line)



