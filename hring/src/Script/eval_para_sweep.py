#!/usr/bin/python
# coding=UTF-8

import sys
import os
import re
import fnmatch
import string
import compute
import get
import matplotlib.pyplot as plt
import matplotlib
from matplotlib.ticker import FuncFormatter

# check the total number of the folder. It indicates how many comparison groups (i.e., with different parameter values) to analyse.
# Read in all parameter values from the folder name

# for each of parameter value
 # read all *.out in a folder.
 # compute the avergage of ws, hs, uf

# plot
 # x axis is all parameter values
 # y axis is either ws, hs, or uf
 # should get 10 points on each curve.


# target dir
NUM_WORKLOAD = 4 
num_value = 0
target_dir = "/Users/Anderson/Desktop/SweepB/results/homo/4x4/design/"
workload_dir = "/Users/Anderson/Desktop/workload_list_1/homo_4x4_ipc"
ws_plt = []
hs_plt = []
uf_plt = []
para_value_plt = []
total_num = 0 # for debug
for dir in os.listdir (target_dir):
  if dir == ".DS_Store":
    continue
  num_sim_file = 0 # number of workloads get simulated in each folder
  _ws_per_workload = [0] * NUM_WORKLOAD
  _hs_per_workload = [0] * NUM_WORKLOAD
  _uf_per_workload = [0] * NUM_WORKLOAD
  for sim_index in range (1, NUM_WORKLOAD+1, 1):
    sim_file = target_dir + dir + "/sim_" + str(sim_index) + ".out"
    if os.path.isfile (sim_file) is False:
      print "The file " + sim_file + " doesn't exist."
      continue

    # read in the referenced IPC
    ipc_alone = get.get_ipc_alone (workload_dir, sim_index)
    stat_share_design = get.get_stat (sim_file)
    ipc_share_design = get.get_ipc_share (stat_share_design)
    (ws_design, hs_design, uf_design) = get.cmp_metric (ipc_alone, ipc_share_design)
    _ws_per_workload [num_sim_file] = ws_design
    _hs_per_workload [num_sim_file] = hs_design
    _uf_per_workload [num_sim_file] = uf_design

    num_sim_file = num_sim_file + 1
    total_num = total_num + 1
  ws_plt = ws_plt + [get.cmp_geo_avg (_ws_per_workload)]
  hs_plt = hs_plt + [get.cmp_geo_avg (_hs_per_workload)]
  uf_plt = uf_plt + [get.cmp_geo_avg (_uf_per_workload)]
  para_value_plt = para_value_plt + [dir]
  num_value = num_value + 1 # increase the number of value being swept

print "number of value : " + str (num_value)
print "total files: " + str (total_num)
print ws_plt
print hs_plt
print uf_plt
plt.figure(1)
plt.subplot(3,1,1)
plt.plot (para_value_plt, ws_plt, 'co')
plt.subplot(3,1,2)
plt.plot (para_value_plt, hs_plt, 'gv')
plt.subplot(3,1,3)
plt.plot (para_value_plt, uf_plt, 'bs')
plt.show ()













