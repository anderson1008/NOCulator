#!/usr/bin/python
# compute and collect the ws, hs, uf from all the simulation

import sys
import os
import get

SIZE = "8x8"
TYPE = "homo"
NODE = 64 
ACTIVE_CYCLE = 5000000

# My MacPro
PATH_WORKLOAD = "/home/xiyue/workload_list/"+TYPE+"_"+SIZE+"_ipc"
PATH_DESIGN = "/home/xiyue/"+SIZE+"/FinalEval/results/"+TYPE+"/"+SIZE+"/design/"
#PATH_DESIGN = "/home/xiyue/ACT_8x8/homo/8x8/design/"
PATH_BASELINE = "/home/xiyue/"+SIZE+"/FinalEval/results_1/"+TYPE+"/"+SIZE+"/baseline/"
NUM_SIM = 30

# Uni MacPro
#ROOT_DIR = "/Users/xiyuexiang/Desktop/FinalEval/"
#PATH_WORKLOAD = "/Users/xiyuexiang/GoogleDrive/NOCulator/hring/src/bin/workload_list/homo_4x4_ipc"
##PATH_DESIGN = ROOT_DIR + "results/homo/4x4/design/"
#PATH_BASELINE = "/Users/xiyuexiang/Desktop/SlowdownError/homo/4x4/baseline/"
#NUM_SIM = 30 

ws_norm = 0
hs_norm = 0
uf_norm = 0
ws_norm_raw = []
hs_norm_raw = []
uf_norm_raw = []
max_ws = 0
max_hs = 0
min_uf = 100
l1miss_aggr = 0
num_sim = 0

for index in range (1, NUM_SIM+1):
  ipc_alone = get.get_ipc_alone (PATH_WORKLOAD, index)
  design_file = PATH_DESIGN + "sim_" + str(index) + ".out"
  baseline_file = PATH_BASELINE + "sim_" + str(index) + ".out"
  if os.path.isfile (design_file) is False or os.path.isfile (baseline_file) is False:
    print "The file doesn't exist."
    continue
  stat_share_design = get.get_stat (design_file)
  stat_share_baseline = get.get_stat (baseline_file)
  ipc_share_design = get.get_ipc_share (stat_share_design)
  ipc_share_baseline = get.get_ipc_share (stat_share_baseline)
  # normalized to the baseline design
  (ws_design, hs_design, uf_design, uf_baseline) = get.cmp_metric_bs (ipc_alone, ipc_share_baseline, ipc_share_design)
  ws_norm = ws_norm + ws_design/NODE
  hs_norm = hs_norm + hs_design
  uf_norm = uf_norm + float(uf_design)/uf_baseline
  ws_norm_raw = ws_norm_raw + [ws_design/NODE]
  hs_norm_raw = hs_norm_raw + [hs_design]
  uf_norm_raw = uf_norm_raw + [float(uf_design)/uf_baseline]
  num_sim = num_sim + 1
  if (ws_design/NODE > max_ws): max_ws = ws_design/NODE
  if (hs_design > max_hs):  max_hs = hs_design
  if (float(uf_design)/uf_baseline < min_uf): min_uf = float(uf_design)/uf_baseline
  #print ws_norm
  #print "uf_design"+str(uf_design)
  #print "uf_baseline"+str(uf_baseline)
  #print "{ws_norm, hs_norm, uf_norm} - " + str 
  # normalized to the baseline design

  # enable the code below to use reference IPC as ipc_alone
  #(ws_design, hs_design, uf_design) = get.cmp_metric (ipc_alone, ipc_share_design)
  #(ws_baseline, hs_baseline, uf_baseline) = get.cmp_metric (ipc_alone, ipc_share_baseline)
  #print "ws_norm = " + str(ws_design/ws_baseline)
  #print "hs_norm = " + str (hs_design/hs_baseline)
  #print "uf_norm = " + str (uf_design/uf_baseline)
  #ws_norm = ws_norm + ws_design/ws_baseline
  #hs_norm = hs_norm + hs_design/hs_baseline
  #uf_norm = uf_norm + uf_design/uf_baseline
  # enable the code above to use reference IPC as ipc_alone


  # profile sum of l1miss
  l1miss_new = get.get_l1miss_sum (stat_share_baseline)
  l1miss_aggr = l1miss_new + l1miss_aggr

# Average l1 miss per cycle
L1MPC_avg = float(l1miss_aggr) / (num_sim*NODE*ACTIVE_CYCLE)
print "Average L1MPC = " + str("%.3f"%L1MPC_avg)

#print "--- Arithmetic Average ---"
#print "Average Normalized Weighted Speedup (>1 is good) = " + str("%.3f"%(ws_norm/num_sim))
#print "Average Normalized Harmonic Speedup (>1 is good) = " + str("%.3f"%(hs_norm/num_sim))
#print "Average Normalized Unfairness (<1 is good) = " + str("%.3f"%(uf_norm/num_sim))

geo_avg_ws = get.cmp_geo_avg(ws_norm_raw)
geo_avg_hs = get.cmp_geo_avg(hs_norm_raw)
geo_avg_uf = get.cmp_geo_avg(uf_norm_raw)

print "--- Best case --- "
print "max_ws, max_hs, min_uf : " + str("%.3f / "%max_ws) + str("%.3f / "%max_hs) + str("%.3f"%min_uf)
print "--- Geometric Average --- "
print ws_norm_raw
print uf_norm_raw
print "Geometric Average (ws, hs, uf): " + str("%.3f / "%geo_avg_ws) + str("%.3f / "%geo_avg_hs) + str("%.3f"%geo_avg_uf)



