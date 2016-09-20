#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string

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
  #print ws
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
  #print hs
  return hs

#compute the actual slowdown
def cmp_real_sd (ipc_alone, ipc_share):
  slowdown = []
  for i,j in zip (ipc_alone, ipc_share):
    slowdown = slowdown + [round(i/j,3)]
  #print slowdown
  return slowdown

#compute unfairness
def cmp_uf (ipc_alone, ipc_share):
  if len(ipc_alone) != len(ipc_share):
    raise Exception ("not enough ipc element")
  slowdown = cmp_real_sd(ipc_alone, ipc_share)
  #unfairness = max (slowdown) - min (slowdown)
  unfairness = max (slowdown)
  print unfairness
  return unfairness

