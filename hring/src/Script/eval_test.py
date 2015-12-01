#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string

dir_macpro = "/Users/xiyuexiang/GoogleDrive/NOCulator/hring/src/bin/"
dir_canpc = "/home/anderson/Desktop/NOCulator/hring/src/"
work_dir = dir_macpro

#ipc_alone = [2.02, 1.85, 1.91, 2.51, 2.02, 1.85, 1.91, 2.51, 2.02, 1.85, 1.91, 2.51, 2.02, 1.85, 1.91, 2.51] # heter_app 1
ipc_alone = [2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91, 2.08, 2.16, 1.77, 1.91] # mix_app 1 in 16 node
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

def cmp_ipc (insns_persrc, active_cycle):
  ipc = []
  for i,j in zip (insns_persrc, active_cycle):
    ipc = ipc + [ round (float (i) / float(j),3)]
  #print ipc
  return ipc

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
  print slowdown
  return slowdown

#compute unfairness
def cmp_uf (ipc_alone, ipc_share):
  if len(ipc_alone) != len(ipc_share):
    raise Exception ("not enough ipc element")
  slowdown = cmp_real_sd(ipc_alone, ipc_share)
  #unfairness = max (slowdown) - min (slowdown)
  unfairness = max (slowdown)
  return unfairness


input_file = input('please input file_shared name: ' )
test_file_name = work_dir + str(input_file)
stat_shared = get_stat (test_file_name)
act_t_shared = get_active_cycles(stat_shared)
insns_shared = get_insns_persrc(stat_shared)
ipc_shared = cmp_ipc (insns_shared, act_t_shared)

ws = cmp_ws (ipc_alone, ipc_shared)
hs = cmp_hs (ipc_alone, ipc_shared)
uf = cmp_uf (ipc_alone, ipc_shared)

print "Weighted Speedup = " + str("%.3f" % ws)
print "Harmonic Speedup = " + str("%.3f" % hs)
print "Unfairness = " + str("%.3f" % uf)
			

	

