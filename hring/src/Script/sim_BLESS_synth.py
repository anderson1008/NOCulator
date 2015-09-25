#!/usr/bin/python

import sys
import os

# # # # # # # # # # # # # # # # # # # # # # # # # # 16-node BLESS # # # # # # # # # # # # # # # # # # # # # # # # # # 

workload_dir = "../bin/workload_list/"
SIM_NUM = 200
insns = "1000000"

####################################    uniform_random    #########################################

traffic = 'uniform_random' # [bit_complement, transpose, uniform_random]
out_dir = "../results/Synthetic/" + traffic + "/BLESS/4x4/"
synth_reads_fraction = 0.8
synth_rate_base = 0.0005
bSynthBitComplement = "false"
bSynthTranspose = "false"
bSynthHotspot = "false"
randomHotspot = "false"

workload = "workloads_null"
router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
router_addrPacketSize = "1"
router_dataPacketSize = "4"
router_maxPacketSize = "4"
network_nrX = "4"
network_nrY = "4"
topology = "Mesh"
randomize_defl = "true"

#  Injection rate sweep: 0.0005 - 0.1 at 0.0005 internal

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	synth_rate = str(synth_rate_base * sim_index)
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -synth_rate " + synth_rate + ' -insns ' + insns + ' -bSynthBitComplement ' + bSynthBitComplement + ' -bSynthTranspose ' + bSynthTranspose + ' -bSynthHotspot ' + bSynthHotspot + ' -randomHotspot ' + randomHotspot
	os.system (command_line)


####################################    bit_complement    #########################################

traffic = 'bit_complement' # [bit_complement, transpose, uniform_random]
out_dir = "../results/Synthetic/" + traffic + "/BLESS/4x4/"
synth_reads_fraction = 0.8
synth_rate_base = 0.0005
bSynthBitComplement = "true"
bSynthTranspose = "false"
bSynthHotspot = "false"
randomHotspot = "false"

workload = "workloads_null"
router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
router_addrPacketSize = "1"
router_dataPacketSize = "4"
router_maxPacketSize = "4"
network_nrX = "4"
network_nrY = "4"
topology = "Mesh"
randomize_defl = "true"

#  Injection rate sweep: 0.0005 - 0.1 at 0.0005 internal

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	synth_rate = str(synth_rate_base * sim_index)
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -synth_rate " + synth_rate + ' -insns ' + insns + ' -bSynthBitComplement ' + bSynthBitComplement + ' -bSynthTranspose ' + bSynthTranspose + ' -bSynthHotspot ' + bSynthHotspot + ' -randomHotspot ' + randomHotspot
	os.system (command_line)

####################################    transpose    #########################################

traffic = 'transpose' # [bit_complement, transpose, uniform_random]
out_dir = "../results/Synthetic/" + traffic + "/BLESS/4x4/"
synth_reads_fraction = 0.8
synth_rate_base = 0.0005
bSynthBitComplement = "false"
bSynthTranspose = "true"
bSynthHotspot = "false"
randomHotspot = "false"

workload = "workloads_null"
router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
router_addrPacketSize = "1"
router_dataPacketSize = "4"
router_maxPacketSize = "4"
network_nrX = "4"
network_nrY = "4"
topology = "Mesh"
randomize_defl = "true"

#  Injection rate sweep: 0.0005 - 0.1 at 0.0005 internal

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	synth_rate = str(synth_rate_base * sim_index)
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -synth_rate " + synth_rate + ' -insns ' + insns + ' -bSynthBitComplement ' + bSynthBitComplement + ' -bSynthTranspose ' + bSynthTranspose + ' -bSynthHotspot ' + bSynthHotspot + ' -randomHotspot ' + randomHotspot
	os.system (command_line)

# # # # # # # # # # # # # # # # # # # # # # # # # # 64-node BLESS # # # # # # # # # # # # # # # # # # # # # # # # # # 

traffic = 'uniform_random' # [bit_complement, transpose, uniform_random]
out_dir = "../results/Synthetic/" + traffic + "/BLESS/8x8/"
synth_reads_fraction = 0.8
synth_rate_base = 0.0005
bSynthBitComplement = "false"
bSynthTranspose = "false"
bSynthHotspot = "false"
randomHotspot = "false"

workload = "workloads_null"
router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
router_addrPacketSize = "1"
router_dataPacketSize = "4"
router_maxPacketSize = "4"
network_nrX = "8"
network_nrY = "8"
topology = "Mesh"
randomize_defl = "true"

#  Injection rate sweep: 0.0005 - 0.1 at 0.0005 internal

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	synth_rate = str(synth_rate_base * sim_index)
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -synth_rate " + synth_rate + ' -insns ' + insns + ' -bSynthBitComplement ' + bSynthBitComplement + ' -bSynthTranspose ' + bSynthTranspose + ' -bSynthHotspot ' + bSynthHotspot + ' -randomHotspot ' + randomHotspot
	os.system (command_line)


####################################    bit_complement    #########################################

traffic = 'bit_complement' # [bit_complement, transpose, uniform_random]
out_dir = "../results/Synthetic/" + traffic + "/BLESS/8x8/"
synth_reads_fraction = 0.8
synth_rate_base = 0.0005
bSynthBitComplement = "true"
bSynthTranspose = "false"
bSynthHotspot = "false"
randomHotspot = "false"

workload = "workloads_null"
router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
router_addrPacketSize = "1"
router_dataPacketSize = "4"
router_maxPacketSize = "4"
network_nrX = "8"
network_nrY = "8"
topology = "Mesh"
randomize_defl = "true"

#  Injection rate sweep: 0.0005 - 0.1 at 0.0005 internal

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	synth_rate = str(synth_rate_base * sim_index)
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -synth_rate " + synth_rate + ' -insns ' + insns + ' -bSynthBitComplement ' + bSynthBitComplement + ' -bSynthTranspose ' + bSynthTranspose + ' -bSynthHotspot ' + bSynthHotspot + ' -randomHotspot ' + randomHotspot
	os.system (command_line)

####################################    transpose    #########################################

traffic = 'transpose' # [bit_complement, transpose, uniform_random]
out_dir = "../results/Synthetic/" + traffic + "/BLESS/8x8/"
synth_reads_fraction = 0.8
synth_rate_base = 0.0005
bSynthBitComplement = "false"
bSynthTranspose = "true"
bSynthHotspot = "false"
randomHotspot = "false"

workload = "workloads_null"
router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
router_addrPacketSize = "1"
router_dataPacketSize = "4"
router_maxPacketSize = "4"
network_nrX = "8"
network_nrY = "8"
topology = "Mesh"
randomize_defl = "true"

#  Injection rate sweep: 0.0005 - 0.1 at 0.0005 internal

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	synth_rate = str(synth_rate_base * sim_index)
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -synth_rate " + synth_rate + ' -insns ' + insns + ' -bSynthBitComplement ' + bSynthBitComplement + ' -bSynthTranspose ' + bSynthTranspose + ' -bSynthHotspot ' + bSynthHotspot + ' -randomHotspot ' + randomHotspot
	os.system (command_line)


# # # # # # # # # # # # # # # # # # # # # # # # # # 256-node BLESS # # # # # # # # # # # # # # # # # # # # # # # # # # 

traffic = 'uniform_random' # [bit_complement, transpose, uniform_random]
out_dir = "../results/Synthetic/" + traffic + "/BLESS/16x16/"
synth_reads_fraction = 0.8
synth_rate_base = 0.0005
bSynthBitComplement = "false"
bSynthTranspose = "false"
bSynthHotspot = "false"
randomHotspot = "false"

workload = "workloads_null"
router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
router_addrPacketSize = "1"
router_dataPacketSize = "4"
router_maxPacketSize = "4"
network_nrX = "16"
network_nrY = "16"
topology = "Mesh"
randomize_defl = "true"

#  Injection rate sweep: 0.0005 - 0.1 at 0.0005 internal

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	synth_rate = str(synth_rate_base * sim_index)
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -synth_rate " + synth_rate + ' -insns ' + insns + ' -bSynthBitComplement ' + bSynthBitComplement + ' -bSynthTranspose ' + bSynthTranspose + ' -bSynthHotspot ' + bSynthHotspot + ' -randomHotspot ' + randomHotspot
	os.system (command_line)


####################################    bit_complement    #########################################

traffic = 'bit_complement' # [bit_complement, transpose, uniform_random]
out_dir = "../results/Synthetic/" + traffic + "/BLESS/16x16/"
synth_reads_fraction = 0.8
synth_rate_base = 0.0005
bSynthBitComplement = "true"
bSynthTranspose = "false"
bSynthHotspot = "false"
randomHotspot = "false"

workload = "workloads_null"
router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
router_addrPacketSize = "1"
router_dataPacketSize = "4"
router_maxPacketSize = "4"
network_nrX = "16"
network_nrY = "16"
topology = "Mesh"
randomize_defl = "true"

#  Injection rate sweep: 0.0005 - 0.1 at 0.0005 internal

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	synth_rate = str(synth_rate_base * sim_index)
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -synth_rate " + synth_rate + ' -insns ' + insns + ' -bSynthBitComplement ' + bSynthBitComplement + ' -bSynthTranspose ' + bSynthTranspose + ' -bSynthHotspot ' + bSynthHotspot + ' -randomHotspot ' + randomHotspot
	os.system (command_line)

####################################    transpose    #########################################

traffic = 'transpose' # [bit_complement, transpose, uniform_random]
out_dir = "../results/Synthetic/" + traffic + "/BLESS/16x16/"
synth_reads_fraction = 0.8
synth_rate_base = 0.0005
bSynthBitComplement = "false"
bSynthTranspose = "true"
bSynthHotspot = "false"
randomHotspot = "false"

workload = "workloads_null"
router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
router_addrPacketSize = "1"
router_dataPacketSize = "4"
router_maxPacketSize = "4"
network_nrX = "16"
network_nrY = "16"
topology = "Mesh"
randomize_defl = "true"

#  Injection rate sweep: 0.0005 - 0.1 at 0.0005 internal

for sim_index in range(1, SIM_NUM+1, 1):
	print ("New Simulation!")
	synth_rate = str(synth_rate_base * sim_index)
	out_file = "sim_" + str(sim_index) + ".out"
	command_line = "mono ../bin/sim.exe -config ../bin/config.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -synth_rate " + synth_rate + ' -insns ' + insns + ' -bSynthBitComplement ' + bSynthBitComplement + ' -bSynthTranspose ' + bSynthTranspose + ' -bSynthHotspot ' + bSynthHotspot + ' -randomHotspot ' + randomHotspot
	os.system (command_line)



