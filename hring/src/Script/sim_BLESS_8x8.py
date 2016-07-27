#!/usr/bin/python

import sys
import os

workload_dir = "../bin/workload_list/"

# 64-node BLESS
workload = "workloads_null"
network_nrX = "8"
network_nrY = "8"
router_addrPacketSize = "1"
router_dataPacketSize = "4"
router_maxPacketSize = "4"
topology = "Mesh"
router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
randomize_defl = "true"

# will be overriden in each run
## Sweep unicast uniform random traffic
out_dir = "../preliminary/synthSweep/uc/"
mc_degree = "16"
multicast = "false"
synth_rate = 0
mc_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 25, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        synth_rate = synth_rate + 0.02
	command_line = "mono ../bin/sim.exe -config ../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate)
#	os.system (command_line)
## Sweep unicast injection rate under specified mc_rate
### mc_rate = 0.01, 0.02, 0.05, 0.1
out_dir = "../preliminary/synthSweep/uc/mc_0.01/"
mc_rate = 0.01
multicast = "true"
synth_rate = 0

if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 16, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        synth_rate = synth_rate + 0.02
	command_line = "mono ../bin/sim.exe -config ../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate)
	os.system (command_line)

out_dir = "../preliminary/synthSweep/uc/mc_0.02/"
mc_rate = 0.02
multicast = "true"
synth_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 15, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        synth_rate = synth_rate + 0.02
	command_line = "mono ../bin/sim.exe -config ../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate)
	os.system (command_line)

synth_rate = 0
mc_rate = 0.05
out_dir = "../preliminary/synthSweep/uc/mc_0.05/"
multicast = "true"
if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 11, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        synth_rate = synth_rate + 0.02
	command_line = "mono ../bin/sim.exe -config ../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate)
	os.system (command_line)

synth_rate = 0
mc_rate = 0.1
out_dir = "../preliminary/synthSweep/uc/mc_0.1/"
multicast = "true"
if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 11, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        synth_rate = synth_rate + 0.02
	command_line = "mono ../bin/sim.exe -config ../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate)
	os.system (command_line)

## Sweep multicast 0.01-0.1 with 0.01 increment
### under unicast rate of 0.1, 0.2, 0.3, 0.4, 0.5 
out_dir = "../preliminary/synthSweep/mc/uc_0.1/"
mc_degree = "16"
multicast = "true"
synth_rate = 0.1
mc_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)
for sim_index in range(1, 26, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        mc_rate = mc_rate + 0.01
	command_line = "mono ../bin/sim.exe -config ../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate)
	os.system (command_line)

out_dir = "../preliminary/synthSweep/mc/uc_0.2/"
synth_rate = 0.2
mc_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)
for sim_index in range(1, 11, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        mc_rate = mc_rate + 0.01
	command_line = "mono ../bin/sim.exe -config ../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate)
#	os.system (command_line)

out_dir = "../preliminary/synthSweep/mc/uc_0.3/"
synth_rate = 0.3
mc_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)
for sim_index in range(1, 11, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        mc_rate = mc_rate + 0.01
	command_line = "mono ../bin/sim.exe -config ../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate)
#	os.system (command_line)

out_dir = "../preliminary/synthSweep/mc/uc_0.4/"
synth_rate = 0.4
mc_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)
for sim_index in range(1, 11, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        mc_rate = mc_rate + 0.01
	command_line = "mono ../bin/sim.exe -config ../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate)
#	os.system (command_line)


