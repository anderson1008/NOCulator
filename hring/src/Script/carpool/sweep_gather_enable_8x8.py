#!/usr/bin/python

import sys
import os

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
mc_degree = "0"
mergeEnable = "true"

# will be overriden in each run
## Sweep unicast uniform random traffic
out_dir = "./preliminary/synthSweep/uc/bs/"
multicast = "false"
synth_rate = 0
mc_rate = 0
hs_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 25, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        synth_rate = synth_rate + 0.02
	command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable
#	os.system (command_line)

## Sweep unicast injection rate under specified hs_rate
### hs_rate = 0.1, 0.2, 0.3, 0.4, 0.5
out_dir = "./preliminary/synthSweep/uc/merge/hs_0.01/"
synth_rate = 0
hs_rate = 0.1

if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 16, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        synth_rate = synth_rate + 0.02
	command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable 
	os.system (command_line)

out_dir = "./preliminary/synthSweep/uc/merge/hs_0.2/"
synth_rate = 0
hs_rate = 0.2
if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 15, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        synth_rate = synth_rate + 0.02
	command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable 
	os.system (command_line)

out_dir = "./preliminary/synthSweep/uc/merge/hs_0.3/"
synth_rate = 0
hs_rate = 0.3
if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 15, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        synth_rate = synth_rate + 0.02
	command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable 
	os.system (command_line)


out_dir = "./preliminary/synthSweep/uc/merge/hs_0.4/"
synth_rate = 0
hs_rate = 0.4
if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 15, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        synth_rate = synth_rate + 0.02
	command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable 
	os.system (command_line)


out_dir = "./preliminary/synthSweep/uc/merge/hs_0.5/"
synth_rate = 0
hs_rate = 0.5
if not os.path.exists(out_dir):
  os.makedirs(out_dir)

for sim_index in range(1, 15, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        synth_rate = synth_rate + 0.02
	command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable 
	os.system (command_line)


## Sweep hotspot 0.1-0.5 with 0.05 increment
### under unicast rate of 0.1, 0.2, 0.3, 0.4, 0.5 
out_dir = "./preliminary/synthSweep/hs/merge/uc_0.1/"
synth_rate = 0.1
hs_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)
for sim_index in range(1, 11, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        hs_rate = hs_rate + 0.05
	command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable 
	os.system (command_line)

out_dir = "./preliminary/synthSweep/hs/merge/uc_0.2/"
synth_rate = 0.2
hs_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)
for sim_index in range(1, 11, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        hs_rate = hs_rate + 0.05
	command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable 
	os.system (command_line)

out_dir = "./preliminary/synthSweep/hs/merge/uc_0.3/"
synth_rate = 0.3
hs_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)
for sim_index in range(1, 11, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        hs_rate = hs_rate + 0.05
	command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate)  + " -mergeEnable " + mergeEnable 
	os.system (command_line)

out_dir = "./preliminary/synthSweep/hs/merge/uc_0.4/"
synth_rate = 0.4
hs_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)
for sim_index in range(1, 11, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        hs_rate = hs_rate + 0.05
	command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable 
	os.system (command_line)


out_dir = "./preliminary/synthSweep/hs/merge/uc_0.5/"
synth_rate = 0.5
hs_rate = 0
if not os.path.exists(out_dir):
  os.makedirs(out_dir)
for sim_index in range(1, 11, 1):
	print ("New Simulation!")
	out_file = "sim_" + str(sim_index) + ".out"
        hs_rate = hs_rate + 0.05
	command_line = "mono ../../bin/sim.exe -config ../../bin/workload_list/config_mc.txt -output " + out_dir + out_file + " -workload " + workload_dir + workload + " 1 -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -randomize_defl " + randomize_defl + " -mc_degree " + mc_degree + " -multicast " + multicast + " -synth_rate " + str(synth_rate) + " -mc_rate " + str(mc_rate) + " -hs_rate " + str(hs_rate) + " -mergeEnable " + mergeEnable 
	os.system (command_line)

