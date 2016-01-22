#!/usr/bin/python
# compute and collect the ws, hs, uf from all the simulation

import sys
import os
import get

# My MacPro
#PATH_WORKLOAD = "/Users/Anderson/Desktop/FinalEval/workload_list/homo_mem_8x8_ipc"
#PATH_DESIGN = "/Users/Anderson/Desktop/FinalEval/results/homo/8x8/design/"
#PATH_BASELINE = "/Users/Anderson/Desktop/FinalEval/results/homo/8x8/baseline/"
#NUM_SIM = 20

# Uni MacPro
PATH_WORKLOAD = "/Users/xiyuexiang/Desktop/FinalEval/workload_list/homo_mem_4x4_ipc"
PATH_DESIGN = "/Users/xiyuexiang/Desktop/FinalEval/results/homo/4x4/design/"
PATH_BASELINE = "/Users/xiyuexiang/Desktop/FinalEval/results/homo/4x4/baseline/"
NUM_SIM = 30
NODE = 16
ACTIVE_CYCLE = 900000

ws_norm = 0
hs_norm = 0
uf_norm = 0
l1miss_aggr = 0

for index in range (1, NUM_SIM+1):
  ipc_alone = get.get_ipc_alone (PATH_WORKLOAD, index)
  stat_share_design = get.get_stat (PATH_DESIGN + "sim_" + str(index) + ".out")
  stat_share_baseline = get.get_stat (PATH_BASELINE + "sim_" + str(index) + ".out")
  ipc_share_design = get.get_ipc_share (stat_share_design)
  ipc_share_baseline = get.get_ipc_share (stat_share_baseline)
  (ws_design, hs_design, uf_design) = get.cmp_metric (ipc_alone, ipc_share_design)
  (ws_baseline, hs_baseline, uf_baseline) = get.cmp_metric (ipc_alone, ipc_share_baseline)

  #print "ws_norm = " + str(ws_design/ws_baseline)
  #print "hs_norm = " + str (hs_design/hs_baseline)
  #print "uf_norm = " + str (uf_design/uf_baseline)

  ws_norm = ws_norm + ws_design/ws_baseline
  hs_norm = hs_norm + hs_design/hs_baseline
  uf_norm = uf_norm + uf_design/uf_baseline

  # profile sum of l1miss
  l1miss_new = get.get_l1miss_sum (stat_share_baseline)
  l1miss_aggr = l1miss_new + l1miss_aggr

# Average l1 miss per cycle
L1MPC_avg = float(l1miss_aggr) / (NUM_SIM*NODE*ACTIVE_CYCLE)
print L1MPC_avg

print "Average Normalized Weighted Speedup (>1 is good) = " + str("%.3f"%(ws_norm/NUM_SIM))
print "Average Normalized Harmonic Speedup (>1 is good) = " + str("%.3f"%(hs_norm/NUM_SIM))
print "Average Normalized Unfairness (<1 is good) = " + str("%.3f"%(uf_norm/NUM_SIM))

