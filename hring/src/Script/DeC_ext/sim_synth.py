#!/usr/bin/python

import sys
import os

# 4x4, uniform random
workload_dir = "../../bin/workload_list/"
workload = "workloads_null"
traffic = 'uniform_random' # [bit_complement, transpose, uniform_random]
synth_rate_set = [0.01, 0.02, 0.05, 0.1, 0.15, 0.20, 0.25, 0.30, 0.35, 0.4, 0.45, 0.5, 0.55, 0.6, 0.65, 0.7, 0.75, 0.8, 0.85, 0.9]
synthPattern = "UR"
router_maxPacketSize = "4"
network_nrX = "4"
network_nrY = "4"
subnet_sel_rand = "false" #False: inject to subnet with lower load; True: randomly select a subnet
uniform_size = "1" # in packet; # of flits will be determined by the number of subnets
sub_net = "1"
uniform_size_enable = "false" # assume 50% of control + 50% of data
num_bypass = "1"
meshEjectTrial = "1"
partial_sort = "true"

def run():
    HEADER="\n\
#########################################################################\n\
###########################    New Simulation   #########################\n\
#########################################################################\n"
    if not os.path.exists(out_dir):
        os.makedirs(out_dir)
    for sim_index in range(0, len(synth_rate_set)):
#        print HEADER
        synth_rate = str(synth_rate_set[sim_index])
        out_file = out_dir + synth_rate + ".out"
        command_line = "mono ../../bin/sim.exe -output " + out_file + " -workload " + workload_dir + workload + ' 1' + " -router.algorithm " + router_algorithm + " -router.addrPacketSize " + router_addrPacketSize + " -router.dataPacketSize " + router_dataPacketSize + " -router.maxPacketSize " + router_maxPacketSize + " -network_nrX " + network_nrX + " -network_nrY " + network_nrY + " -topology " + topology + " -synth_rate " + synth_rate + ' -sub_net ' + sub_net + ' -subnet_sel_rand ' + subnet_sel_rand + " -uniform_size_enable " + uniform_size_enable + " -uniform_size " + uniform_size + " -synthPattern " + synthPattern + " -meshEjectTrial " + meshEjectTrial + " -num_bypass " + num_bypass + " -partial_sort " + partial_sort
#        print command_line
        os.system (command_line)



"""
Test 1

Motivate the benefit of using a wider channel
         - control flit = 128b
         - data flit = 512b
         - synth_rate: sweep
         - channel size = 128b, 256b, 512b
            emulate by changing data size to 512b, 256b, and 128b (4, 2, and 1 control flit)
         - Assume 50% of control + 50% of data
      Conclusion: wider channels provide more bandwidth which dramatically speed up the packet delivery.
      Problem: channel will be underutilized when transferring control flit.
"""

router_algorithm = "BLESS_BYPASS"
topology = "Mesh_Multi"
router_addrPacketSize = "1"
channel_size_dict = {'128b':'4', '256b':'2', '512b':'1',}
for channel_width, router_dataPacketSize in channel_size_dict.items():
    out_dir = "./results/DeC/channel_width/"+channel_width+"/"
    run()

"""
Test 2
Prove using a multiple network can better utilize the channel
        - control flit = 128b
        - data flit = 512b
        - synth_rate: sweep
        Curve 1: channel size = 256b
           - sub_net = 1, data flit size = 2
           - sub_net = 2, data flit size = 2
           - sub_net = 4, data flit size = 2
           - BLESS, data flit size = 2
           - minbd, data flit size = 2
        Curve 2: channel size = 512b
           - sub_net = 1, data flit size = 1
           - sub_net = 2, data flit size = 1
           - sub_net = 4, data flit size = 1
           - BLESS, data flit size = 1
           - minbd, data flit size = 1
           
"""
router_algorithm = "BLESS_BYPASS"
topology = "Mesh_Multi"
router_addrPacketSize = "1"
router_dataPacketSize = "2"
channel_size = "256b"
sub_net_array = ["1", "2", "4"]
for sub_net in sub_net_array:
    out_dir = "./results/DeC/multi_subnet/chnlwidth256b/"+sub_net+"/"
    run()

router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
topology = "Mesh"
partial_sort = "false"
sub_net = "1" # Aggregated link width = 512b
out_dir = "./results/DeC/multi_subnet/chnlwidth256b/bless/"
run()

router_algorithm = "Router_MinBD"
topology = "Mesh"
sub_net = "1" # Aggregated link width = 512b
num_bypass = "0"
meshEjectTrial = "1"
partial_sort = "true"
out_dir = "./results/DeC/multi_subnet/chnlwidth256b/minbd/"
run()

router_algorithm = "BLESS_BYPASS"
topology = "Mesh_Multi"
router_addrPacketSize = "1"
router_dataPacketSize = "1"
channel_size = "512b"
synth_rate_set = [0.01, 0.02, 0.05, 0.1, 0.15, 0.20, 0.25, 0.30, 0.35, 0.4, 0.45, 0.5, 0.55, 0.6, 0.65, 0.7, 0.75, 0.8, 0.85, 0.9, 1.0, 1.1, 1.2, 1.3]
sub_net_array = ["1", "2", "4"]
for sub_net in sub_net_array:
    out_dir = "./results/DeC/multi_subnet/chnlwidth512b/"+sub_net+"/"
    run()

router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
topology = "Mesh"
partial_sort = "false"
sub_net = "1" # Aggregated link width = 512b
out_dir = "./results/DeC/multi_subnet/chnlwidth512b/bless/"
run()

router_algorithm = "Router_MinBD"
topology = "Mesh"
sub_net = "1" # Aggregated link width = 512b
num_bypass = "0"
meshEjectTrial = "1"
partial_sort = "true"
out_dir = "./results/DeC/multi_subnet/chnlwidth512b/minbd/"
run()

"""
Test 3
Using DeC with 1 subnet to compare with minBD and BLESS
assume the same channel width and same flit size
"""
uniform_size_enable = "true" 

# Test 3.1: DeC with 1 subnet
router_algorithm = "BLESS_BYPASS"
topology = "Mesh_Multi"
sub_net = "1" 
out_dir = "./results/router_cmp/DeC/"
run()

# Test 3.2: BLESS synth_rate_set = [0.01, 0.02, 0.05, 0.1, 0.15, 0.20, 0.25, 0.30, 0.35, 0.4, 0.45, 0.5, 0.55, 0.6, 0.65, 0.7]
router_algorithm = "DR_FLIT_SWITCHED_OLDEST_FIRST"
topology = "Mesh"
partial_sort = "false"
sub_net = "1" # Aggregated link width = 512b
out_dir = "./results/router_cmp/bless/"
run()

# Test 3.3: minbd
synth_rate_set = [0.01, 0.02, 0.05, 0.1, 0.15, 0.20, 0.25, 0.30, 0.35, 0.4, 0.45, 0.5, 0.55, 0.6, 0.65, 0.7]
router_algorithm = "Router_MinBD"
topology = "Mesh"
sub_net = "1" # Aggregated link width = 512b
num_bypass = "0"
meshEjectTrial = "1"
partial_sort = "true"
out_dir = "./results/router_cmp/minbd/"
run()

