#!/usr/bin/python

import sys
import os

# 4x4, uniform random
workload_dir = "../../bin/workload_list/"
workload = "workloads_null"
traffic = 'uniform_random' # [bit_complement, transpose, uniform_random]
synth_rate_set = [0.01, 0.02, 0.05, 0.1, 0.15, 0.20, 0.25, 0.30, 0.35, 0.4, 0.45, 0.5, 0.55, 0.6, 0.65, 0.7, 0.8, 0.9]
synthPattern = "UR"
router_addrPacketSize = "4"
router_dataPacketSize = "4"
router_maxPacketSize = "4"
network_nrX = "4"
network_nrY = "4"
subnet_sel_rand = "false" #False: inject to subnet with lower load; True: randomly select a subnet
uniform_size_enable = "true"
uniform_size = "1" # in packet; # of flits will be determined by the number of subnets

####### Test 1: DeC with 4 subnets
router_algorithm = "BLESS_BYPASS"
topology = "Mesh_Multi"
sub_net = "4" # Aggregated link width = 512b
out_dir = "./results/DeC/4x4/subnet" + sub_net + "/" + traffic + "/"
if not os.path.exists(out_dir):
    os.makedirs(out_dir)

for sim_index in range(0, len(synth_rate_set)):
    print ("############## New Simulation! ###############")
    synth_rate = str(synth_rate_set[sim_index])
    out_file = out_dir + synth_rate + ".out"
    command_line = "mono ../../bin/sim.exe -output " + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -synth_rate " + synth_rate + ' -sub_net ' + sub_net + ' -subnet_sel_rand ' + subnet_sel_rand + " -uniform_size_enable " + uniform_size_enable + " -uniform_size " + uniform_size + " -synthPattern " + synthPattern
    print command_line
    os.system (command_line)

####### Test 2: minBD
synth_rate_set = [0.01, 0.02, 0.05, 0.1, 0.15, 0.20, 0.25, 0.30, 0.35, 0.4, 0.45, 0.5, 0.55, 0.6, 0.65, 0.7]
router_algorithm = "Router_MinBD"
topology = "Mesh"
sub_net = "1" # Aggregated link width = 512b
out_dir = "./results/minbd/4x4/" + traffic + "/"
if not os.path.exists(out_dir):
    os.makedirs(out_dir)

for sim_index in range(0, len(synth_rate_set)):
    print ("############## New Simulation! ###############")
    synth_rate = str(synth_rate_set[sim_index])
    out_file = out_dir + synth_rate + ".out"
    command_line = "mono ../../bin/sim.exe -output " + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -synth_rate " + synth_rate + ' -sub_net ' + sub_net + ' -subnet_sel_rand ' + subnet_sel_rand + " -uniform_size_enable " + uniform_size_enable + " -uniform_size " + uniform_size + " -synthPattern " + synthPattern
    print command_line
    os.system (command_line)

