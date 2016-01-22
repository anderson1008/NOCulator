#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string


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
    if float(i) == 0:
      raise Exception ("ipc_alone is 0")
    ws = ws + j/float(i)
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
  return ipc_shared

# compute metrics
def cmp_metric (ipc_alone, ipc_share):
  ws = cmp_ws (ipc_alone, ipc_share)
  hs = cmp_hs (ipc_alone, ipc_share)
  uf = cmp_uf (ipc_alone, ipc_share)
  return (ws, hs, uf)











