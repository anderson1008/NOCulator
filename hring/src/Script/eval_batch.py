#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string
import compute
import get

dir_canpc = "/home/anderson/Desktop/results/homo_mem/"
dir_alone = dir_canpc + "baseline/4x4_insn100K/"
dir_share = dir_canpc + "mshr/"

sum_ws = 0
sum_hs = 0
sum_uf = 0
eff_count = 0
ipc_alone = [2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91]

for sim_index in range(1, 27, 1):
  input_file_alone = dir_alone + "sim_" + str(sim_index) + ".out"
  input_file_share = dir_share + "sim_" + str(sim_index) + ".out"
  exist_alone = os.path.exists(input_file_alone)
  exist_share = os.path.exists(input_file_share)
  if (exist_alone is False or exist_share is False):
    print "Fail to find " + str (sim_index) + ".out or its counterpart."
    continue
  #print input_file_alone + " v.s. " + input_file_share
  stat_alone = get.get_stat (input_file_alone)
  stat_share = get.get_stat (input_file_share)
  insns_alone = get.get_insns_persrc (stat_alone)
  insns_share = get.get_insns_persrc (stat_share)
  act_t_alone = get.get_active_cycles (stat_alone)
  act_t_share = get.get_active_cycles (stat_share)
  #ipc_alone = compute.cmp_ipc (insns_alone, act_t_alone)
  
  ipc_share = compute.cmp_ipc (insns_share, act_t_share)
  ws = compute.cmp_ws (ipc_alone, ipc_share)
  hs = compute.cmp_hs (ipc_alone, ipc_share)
  uf = compute.cmp_uf (ipc_alone, ipc_share)
  eff_count = eff_count + 1
  sum_ws = sum_ws + ws
  sum_hs = sum_hs + hs
  sum_uf = sum_uf + uf
  #print "Weighted Speedup = " + str("%.3f" % ws)
  #print "Harmonic Speedup = " + str("%.3f" % hs)
  #print "Unfairness = " + str("%.3f" % uf)
			
avg_ws = sum_ws / eff_count
avg_hs = sum_hs / eff_count
avg_uf = sum_uf / eff_count
print "Weighted Speedup = " + str("%.3f" % avg_ws)
print "Harmonic Speedup = " + str("%.3f" % avg_hs)
print "Unfairness = " + str("%.3f" % avg_uf)
print "Based on " + str(eff_count) + " pairs of workloads."


