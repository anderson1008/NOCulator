#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string
from math import log, exp

def get_stat (file_name):
  result_file = open (file_name, 'r')
  result = result_file.read()
  result_file.close()
  return result


def get_l1miss (stat):
  searchObj = re.search(r'(?:"L1_misses_persrc":\[(.*?)])',stat)
  splitObj = re.split('\W+',searchObj.group(1))
  return splitObj

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

# read the ipc array of specific workload in the reference file
def get_ipc_alone (file_name, line_number):
  with open(file_name, "r") as f:
    lines =f.readlines()
    line = lines[line_number].rstrip('\n') # strip the newline at the end of each line
    line = re.split('\s', line)
    line.remove('')
    return line

def get_non_overlap_penalty (stat):
  searchObj = re.search(r'(?:"non_overlap_penalty":\[(.*?)])',stat)
  splitObj = re.split('\W+',searchObj.group(1))
  non_overlap_penalty = splitObj
  return non_overlap_penalty

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
  #print ipc_alone
  #print ipc_share
  for i,j in zip (ipc_alone, ipc_share):
    if float(i) == 0:
      raise Exception ("ipc_alone is 0")
    new_ws = j/float(i)
   # print new_ws
    ws = ws + new_ws
  return ws

#compute harmonic speedup
def cmp_hs (ipc_alone, ipc_share):
  if len(ipc_alone) != len(ipc_share):
    raise Exception ("not enough ipc element")
  temp = 0
  for i,j in zip (ipc_alone, ipc_share):
    if j == 0:
      raise Exception ("ipc_share is 0")
    temp = temp + float(i)/j
  if temp == 0:
    raise Exception ("temp in cmp_hs() is 0")
  hs = len (ipc_share) / temp
  return hs 

#compute the actual slowdown
def cmp_real_sd (ipc_alone, ipc_share):
  slowdown = []
  for i,j in zip (ipc_alone, ipc_share):
    slowdown = slowdown + [round(float(i)/j,3)]
  #print slowdown
  return slowdown

#compute unfairness
def cmp_uf (ipc_alone, ipc_share):
  if len(ipc_alone) != len(ipc_share):
    raise Exception ("not enough ipc element")
  slowdown = cmp_real_sd(ipc_alone, ipc_share)
  #unfairness = max (slowdown) - min (slowdown)
  unfairness = max (slowdown)
  return unfairness

def get_l1miss_sum (stat_shared):
  sum = 0
  l1miss = get_l1miss(stat_shared)
  for i in l1miss:
    sum = sum + int (i)
  return sum

# get ipc_share
def get_ipc_share (stat_shared):
  act_t_shared = get_active_cycles(stat_shared)
  insns_shared = get_insns_persrc(stat_shared)
  ipc_shared = cmp_ipc (insns_shared, act_t_shared)
  #print ipc_shared
  return ipc_shared

# compute metrics
# use this if ipc_alone is the ipc_ref
def cmp_metric (ipc_alone, ipc_share):
  ws = cmp_ws (ipc_alone, ipc_share)
  hs = cmp_hs (ipc_alone, ipc_share)
  uf = cmp_uf (ipc_alone, ipc_share)
  return (ws, hs, uf)

# use this if ipc_alone is ipc_baseline
def cmp_metric_bs (ipc_alone, ipc_share_baseline, ipc_share_design):
  ws = cmp_ws (ipc_share_baseline, ipc_share_design)
  hs = cmp_hs (ipc_share_baseline, ipc_share_design)
  uf_design = cmp_uf (ipc_alone,ipc_share_design)
  uf_baseline = cmp_uf (ipc_alone, ipc_share_baseline)
  return (ws, hs, uf_design, uf_baseline)

# compute the geometric average
def cmp_geo_avg (data_set):
  # error rate has to be an array or list
  new_data_set = [log(x) for x in data_set]
  return exp(sum (new_data_set)/len(new_data_set))

#def cmp_geo_avg (error_raw):
#  # error rate has to be an array or list
#  new_error_raw = [log(x) for x in error_raw]
#  return exp(sum (new_error_raw)/len(new_error_raw))






