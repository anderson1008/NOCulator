#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string
import compute
import get




def get_batch_stat (path, numSim):

  pkt_lat_tot_avg_array = []
  pkt_lat_tot_max_array = []
  pkt_lat_net_avg_array = []
  pkt_lat_net_max_array = []
  throughput_array = []
  deflect_array = []
  for sim_index in range(1, numSim+1, 1):
    input_file = path + "sim_" + str(sim_index) + ".out"
    exist = os.path.exists(input_file)
    if (exist is False):
      print "Fail to find " + str (sim_index) + ".out or its counterpart."
      continue

    stat = get.get_stat (input_file)
    [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_tot (stat)
    pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
    pkt_lat_tot_max_array.append(pkt_lat_tot_max)
    [pkt_lat_net_avg, pkt_lat_net_max] = get.get_pkt_lat_net (stat)
    pkt_lat_net_avg_array.append(pkt_lat_net_avg)
    pkt_lat_net_max_array.append(pkt_lat_net_max)
    throughput = get.get_throughput(stat)
    throughput_array.append(throughput)
    deflect = get.get_deflect(stat)
    deflect_array.append(deflect)
  return pkt_lat_tot_avg_array, pkt_lat_net_avg_array, throughput_array, deflect_array




result_dir = "/Users/Anderson/GoogleDrive/NOCulator/hring/src/Script/carpool/preliminary/synthSweep/uc/"
title = '{:>10} {:>10} {:>10} {:>10} {:>10} {:>10} {:>10} {:>10} {:>10}'.format("injRate", "myTotAvgLat", "bsTotAvgLat", "myNetAvgLat", "bsToAvgLat", "my_throughput", "bs_throughput", "my_deflect", "bs_deflect")

#path should has "/" in the end
[my_tot_lat_avg, my_net_lat_avg, my_throughput, my_deflect] = get_batch_stat(result_dir+"scatter/mc_0.1/", 15)
[bs_tot_lat_avg, bs_net_lat_avg, bs_throughput, bs_deflect] = get_batch_stat(result_dir+"no_scatter/mc_0.1/", 15)
inj_rate = 0
print "hs_rate=0.2, sweep uc_rate"
print title
for a0, a1, b0, b1, c0, c1, d0, d1 in zip(my_tot_lat_avg, bs_tot_lat_avg, my_net_lat_avg, bs_net_lat_avg, my_throughput, bs_throughput, my_deflect, bs_deflect):
  inj_rate = inj_rate + 0.02
  print '{:>10} {:>10} {:>10} {:>10} {:>10} {:>10} {:>10} {:>10} {:>10}'.format(inj_rate, a0, a1, b0, b1, c0, c1, d0, d1)


