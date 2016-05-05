#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string
import my_print
import matplotlib.pyplot as plt
import get

#ipc_alone = [2.03, 2.16, 2.00, 1.90, 2.03, 2.16, 2.00, 1.90, 2.03, 2.16, 2.00, 1.90, 2.03, 2.16, 2.00, 1.90]
ipc_alone = [2.07, 2.07, 1.33, 1.33, 2.07, 2.07, 1.33, 1.13, 1.83, 1.83, 1.90, 1.90, 1.83, 1.83, 1.90, 1.90]
work_dir = "/Users/Anderson/GoogleDrive/NOCulator/hring/src/bin"
input_file = "nas_2.out"

#ipc_alone = [] 
#ipc_alone = [2.02, 1.85, 1.91, 2.51, 2.02, 1.85, 1.91, 2.51, 2.02, 1.85, 1.91, 2.51, 2.02, 1.85, 1.91, 2.51] # heter_app 1
#ipc_alone = [2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91] # mix_app 2 in 16 node

#ipc_alone = [1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73, 1.30, 1.65, 1.43, 1.73]
#ipc_alone = [1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93, 1.43, 1.88, 1.30, 0.93] # mix_app 1 in 64 node



def get_stat (file_name):
  result_file = open (file_name, 'r')
  result = result_file.read()
  result_file.close()
  return result

def get_active_cycles (stat):
  searchObj = re.search(r'(?:"active_cycles":\[(.*?)])',stat)
  splitObj = re.split('\W+',searchObj.group(1))
  active_cycles = splitObj
  return active_cycles

def get_insns_persrc (stat):
  searchObj = re.search(r'(?:"insns_persrc":\[(.*?)])',stat)
  splitObj = re.split('\W+',searchObj.group(1))
  insns_persrc = splitObj
  return insns_persrc

def get_est_sd (stat):
  est_sd = []
  searchObj = re.search(r'(?:"estimated_slowdown":\[(.*)],\n"L1miss_persrc_period")',stat,re.DOTALL)
  searchList = re.finditer(r'(?:\{(.*)\},*)',searchObj.group(1))
  for item in searchList:
    splitObj = re.split(',',item.group(1))
    sd_per_core = []
    for i in splitObj:
        if i is '' or i is '0':
            continue
        sd_per_core.append(float(i));
    est_sd = est_sd + [sd_per_core]
  return est_sd

def get_est_L1miss (stat):
  l1miss  = []
  searchObj = re.search(r'(?:"L1miss_persrc_period":\[(.*)],\n"noc_stc")',stat,re.DOTALL)
  searchList = re.finditer(r'(?:\{(.*)\},*)',searchObj.group(1))
  for item in searchList:
    splitObj = re.split(',',item.group(1))
    l1miss_per_period = []
    for i in splitObj:
        if i is '' or i is '0':
            continue
        l1miss_per_period.append(float(i));
    l1miss = l1miss + [l1miss_per_period]
  return l1miss

def cmp_ipc (insns_persrc, active_cycle):
  ipc = []
  for i,j in zip (insns_persrc, active_cycle):
    ipc = ipc + [ round (float (i) / float(j),3)]
  #print ipc
  return ipc

# compute mpki
def cmp_mpki (stat):
  insn_persrc = get_insns_persrc(stat)
  l1_miss_persrc = get.get_l1miss(stat_shared)
  #print l1_miss_persrc
  #print insn_persrc
  mpki = []
  for i,j in zip (insn_persrc, l1_miss_persrc):
    mpki = mpki + [round(float(j)/float(i)*1000,3)]
  #print "MPKI:"
  #print mpki
  return mpki

# compute the application avg of some metric for each type of application 
def cmp_app_avg (metric):
  # assuming 4 applications
  num_element = len(metric)
  loop_index = 0
  avg = [0]*4
  while (loop_index < num_element / 4):
    avg [0] = avg [0] + float(metric [loop_index*4])
    avg [1] = avg [1] + float(metric [loop_index*4+1])
    avg [2] = avg [2] + float(metric [loop_index*4+2])
    avg [3] = avg [3] + float(metric [loop_index*4+3]) 
    loop_index = loop_index + 1 
  avg[0] = round(avg[0]/(num_element/4),3)
  avg[1] = round(avg[1]/(num_element/4),3)
  avg[2] = round(avg[2]/(num_element/4),3)
  avg[3] = round(avg[3]/(num_element/4),3)
  return avg


# compute weighted speedup
def cmp_ws (ipc_alone, ipc_share):
  if len(ipc_alone) != len(ipc_share):
    raise Exception ("not enough ipc element")
  ws = 0
  for i,j in zip (ipc_alone, ipc_share):
    if i == 0:
      raise Exception ("ipc_alone is 0")
    ws = ws + j/i
  return ws

#compute harmonic speedup
def cmp_hs (ipc_alone, ipc_share):
  if len(ipc_alone) != len(ipc_share):
    raise Exception ("not enough ipc element")
  temp = 0
  for i,j in zip (ipc_alone, ipc_share):
    if j == 0:
      raise Exception ("ipc_share is 0")
    temp = temp + i/j
  if temp == 0:
    raise Exception ("temp in cmp_hs() is 0")
  hs = len (ipc_share) / temp
  return hs

#compute the actual slowdown
def cmp_real_sd (ipc_alone, ipc_share):
  slowdown = []
  for i,j in zip (ipc_alone, ipc_share):
    slowdown = slowdown + [round(i/j,3)]
  print "Each app slowdown:"
  print slowdown
  return slowdown

#compute unfairness
def cmp_uf (ipc_alone, ipc_share):
  if len(ipc_alone) != len(ipc_share):
    raise Exception ("not enough ipc element")
  slowdown = cmp_real_sd(ipc_alone, ipc_share)
  #unfairness = max (slowdown) - min (slowdown)
  print "Avg app slowdown"
  print cmp_app_avg(slowdown)
  unfairness = max (slowdown)
  return unfairness



#work_dir = str(input('Please input your work dir: '))
#input_file = str(input('Please input the file name (*.out): ' ))
test_file_name = work_dir + "/" + input_file
stat_shared = get_stat (test_file_name)
act_t_shared = get_active_cycles(stat_shared)
insns_shared = get_insns_persrc(stat_shared)
ipc_shared = cmp_ipc (insns_shared, act_t_shared)
mpki=cmp_mpki(stat_shared)
print "Avg app mpki"
print cmp_app_avg(mpki)
#est_sd = get_est_sd(stat_shared)
#l1_miss = get_est_L1miss(stat_shared)

#plt.figure(1)
#plt.subplot(2,1,1)
#my_print.print_period(est_sd)
#plt.subplot(2,1,2)
#my_print.print_period(l1_miss)
#plt.show() # enable to show the plot

#print est_sd

ws = cmp_ws (ipc_alone, ipc_shared)
hs = cmp_hs (ipc_alone, ipc_shared)
uf = cmp_uf (ipc_alone, ipc_shared)

print "Weighted Speedup = " + str("%.3f" % ws)
print "Harmonic Speedup = " + str("%.3f" % hs)
print "Unfairness = " + str("%.3f" % uf)
			

	

