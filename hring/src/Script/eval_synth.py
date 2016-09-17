#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string
import compute
import get


dir_0 = "../preliminary/synthSweep/uc/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 21, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_tot (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print  "-------Packet Total Latency --------"
print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.02
  print inj_rate, lat_avg, lat_max

print  "-------Packet Network Latency --------"

pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 21, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_net (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.02
  print inj_rate, lat_avg, lat_max

print "----------------------------------------------------------"
print "Multicast sweep under fixed unicast rate"
print "uc_rate = 0.1"
dir_0 = "../preliminary/synthSweep/mc/uc_0.1/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 26, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_tot (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print  "-------Packet Total Latency --------"
print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.01
  print inj_rate, lat_avg, lat_max

print  "-------Packet Network Latency --------"

dir_0 = "../preliminary/synthSweep/mc/uc_0.1/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 26, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_net (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.01
  print inj_rate, lat_avg, lat_max

print "----------------------------------"
print "uc_rate = 0.1"
dir_0 = "../preliminary/synthSweep/mc/uc_0.1/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 11, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_tot (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print  "-------Packet Total Latency --------"
print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.01
  print inj_rate, lat_avg, lat_max

print  "-------Packet Network Latency --------"

dir_0 = "../preliminary/synthSweep/mc/uc_0.1/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 11, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_net (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.01
  print inj_rate, lat_avg, lat_max


print "----------------------------------------"
print "uc_rate = 0.2"
dir_0 = "../preliminary/synthSweep/mc/uc_0.2/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 11, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_tot (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print  "-------Packet Total Latency --------"
print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.01
  print inj_rate, lat_avg, lat_max

print  "-------Packet Network Latency --------"

pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 11, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_net (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.01
  print inj_rate, lat_avg, lat_max

print "----------------------------------------"
print "uc_rate = 0.3"
dir_0 = "../preliminary/synthSweep/mc/uc_0.3/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 11, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_tot (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print  "-------Packet Total Latency --------"
print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.01
  print inj_rate, lat_avg, lat_max

print  "-------Packet Network Latency --------"

pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 11, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_net (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.01
  print inj_rate, lat_avg, lat_max

print "----------------------------------------"
print "uc_rate = 0.4"
dir_0 = "../preliminary/synthSweep/mc/uc_0.4/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 11, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_tot (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print  "-------Packet Total Latency --------"
print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.01
  print inj_rate, lat_avg, lat_max

print  "-------Packet Network Latency --------"

pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 11, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_net (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.01
  print inj_rate, lat_avg, lat_max


print "----------------------------"
print "Unicast sweep under fixed mc_rate = 0.01"
print "----------------------------"
dir_0 = "../preliminary/synthSweep/uc/mc_0.01/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 21, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_tot (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print  "-------Packet Total Latency --------"
print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.02
  print inj_rate, lat_avg, lat_max

print  "-------Packet Network Latency --------"

dir_0 = "../preliminary/synthSweep/uc/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 21, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_net (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.02
  print inj_rate, lat_avg, lat_max


print "----------------------------"
print "Unicast sweep under fixed mc_rate = 0.02"
print "----------------------------"
dir_0 = "../preliminary/synthSweep/uc/mc_0.02/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 21, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_tot (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print  "-------Packet Total Latency --------"
print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.02
  print inj_rate, lat_avg, lat_max

print  "-------Packet Network Latency --------"

dir_0 = "../preliminary/synthSweep/uc/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 21, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_net (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.02
  print inj_rate, lat_avg, lat_max

print "----------------------------"
print "Unicast sweep under fixed mc_rate = 0.05"
print "----------------------------"
dir_0 = "../preliminary/synthSweep/uc/mc_0.05/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 21, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_tot (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print  "-------Packet Total Latency --------"
print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.02
  print inj_rate, lat_avg, lat_max

print  "-------Packet Network Latency --------"

dir_0 = "../preliminary/synthSweep/uc/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 21, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_net (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.02
  print inj_rate, lat_avg, lat_max


print "----------------------------"
print "Unicast sweep under fixed mc_rate = 0.1"
print "----------------------------"
dir_0 = "../preliminary/synthSweep/uc/mc_0.1/"
pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 21, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_tot (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print  "-------Packet Total Latency --------"
print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.02
  print inj_rate, lat_avg, lat_max

print  "-------Packet Network Latency --------"

pkt_lat_tot_avg_array = []
pkt_lat_tot_max_array = []
for sim_index in range(1, 21, 1):
  input_file = dir_0 + "sim_" + str(sim_index) + ".out"
  exist = os.path.exists(input_file)
  if (exist is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  stat = get.get_stat (input_file)
  [pkt_lat_tot_avg, pkt_lat_tot_max] = get.get_pkt_lat_net (stat)
  pkt_lat_tot_avg_array.append(pkt_lat_tot_avg)
  pkt_lat_tot_max_array.append(pkt_lat_tot_max)

print "injRate latAvg latMax"
inj_rate = 0
for lat_avg, lat_max in zip(pkt_lat_tot_avg_array, pkt_lat_tot_max_array):
  inj_rate = inj_rate + 0.02
  print inj_rate, lat_avg, lat_max



