#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string
import get
import matplotlib.pyplot as plt
import matplotlib
from matplotlib.ticker import FuncFormatter
from math import log, exp

# slowdown_estimation
ipc_alone_error_accum = 0
NODE = 0
num_app = 26
spec_workload = ["400.perlbench.bin.gz ","401.bzip2.bin.gz ","403.gcc.bin.gz ","429.mcf.bin.gz ","433.milc.bin.gz ","435.gromacs.bin.gz ","436.cactusADM.bin.gz ",\
"437.leslie3d.bin.gz ","444.namd.bin.gz ","445.gobmk.bin.gz ","447.dealII.bin.gz ","450.soplex.bin.gz ","453.povray.bin.gz ","454.calculix.bin.gz ",\
"456.hmmer.bin.gz ","458.sjeng.bin.gz ","459.GemsFDTD.bin.gz ","462.libquantum.bin.gz ","464.h264ref.bin.gz ","465.tonto.bin.gz ","470.lbm.bin.gz ",\
"471.omnetpp.bin.gz ","473.astar.bin.gz ","481.wrf.bin.gz ","482.sphinx3.bin.gz ","483.xalancbmk.bin.gz "]
error_rate_per_app = []
for i in range (num_app):
  error_rate_per_app.append([])
error_rate_count = [0] * num_app
plt_error = []
total_num_of_sample_point = 0

def compare (s1, s2):
  remove = string.whitespace
  return s1.translate(None, remove) == s2.translate (None, remove)

# compute the average error rate in terms of each application
def compute (error_per_app, error_count):
  print "--------------------------- " + network_size + " -------------------------------"
  print "*********** The error rate of each application **********"
  geo_avg_error_per_app =  [0] * num_app
  for j in range (0, num_app, 1):
    if error_count[j] != 0:
      geo_avg_error_per_app [j] = cmp_geo_avg(error_per_app[j])  
      #avg_error_per_app[j] = error_sum[j] / error_count[j]
      workload_out = re.search(r'\d+\.(\w+)',spec_workload[j])
      print workload_out.group(1).ljust(30) + str("%.4f" % geo_avg_error_per_app[j])
  print "--------------------------------------------------------------------------------"

def compute_error_rate (num_file):
  global raw_out_dir
  global workload
  global ref_ipc
  global NODE
  global error_rate_count
  global error_rate_per_app
  global ipc_alone_error_accum 
  global plt_error
  global total_num_of_sample_point

  slowdown_error_raw = []
  slowdown_error_accum = 0
  for sim_index in range (1, num_file + 1, 1):
    raw_out_file_name = "sim_" + str(sim_index) + ".out"
    for file in os.listdir(raw_out_dir):
      if fnmatch.fnmatch(file, raw_out_file_name):
        fo_in = open(raw_out_dir + file, "r")
        content = fo_in.read()
        fo_in.close()
    insns_persrc = get.get_insns_persrc (content)
    active_cycles = get.get_active_cycles (content)
    non_overlap_penalty = get.get_non_overlap_penalty (content)
    workload_array = re.split('[ ]', workload[sim_index])
    ipc_ref = re.split('[ ]',ref_ipc[sim_index])

    for i in range (0, NODE, 1):
      est_ipc_alone = float(insns_persrc[i]) / (int(active_cycles[i]) - int(non_overlap_penalty[i]))
      ipc_alone_error = (est_ipc_alone - float(ipc_ref[i])) / float(ipc_ref[i])
      #print ipc_alone_error
      ipc_alone_error_accum = ipc_alone_error_accum + abs(ipc_alone_error)

      ipc_share = float(insns_persrc[i]) / int(active_cycles[i])
      est_slowdown = est_ipc_alone / ipc_share
      actual_slowdown = float(ipc_ref[i]) / ipc_share
      #print actual_slowdown
      slowdown_error = (est_slowdown - actual_slowdown) / actual_slowdown
      #print slowdown_error
      
      # slowdown error distribution profiling 
      plt_error = plt_error + [abs(slowdown_error)]
      slowdown_error_raw = slowdown_error_raw + [abs(slowdown_error)]
      slowdown_error_accum = slowdown_error_accum + abs(slowdown_error)
      total_num_of_sample_point = total_num_of_sample_point + 1
      
      for j in range (0, num_app, 1):
        if compare (workload_array [i], spec_workload[j]):
          error_rate_per_app [j] = [abs(slowdown_error)] + error_rate_per_app [j]
          error_rate_count [j] = error_rate_count [j] + 1
  return [slowdown_error_raw, slowdown_error_accum]      

network_size = ''
NODE = -1

def cmd_input ():
  # getting the input
  #input_workload = raw_input('please input workload (homo_mem, hetero): ' )
  global network_size
  global NODE
  network_size = raw_input('please input network size (4x4, 8x8):' )
  if network_size == "4x4":
    NODE = 16
  elif network_size == "8x8":
    NODE = 64
  if (NODE == 0):
    raise Exception ("Size of network is undefined")


def comp_avg_error (num_file):
  # compute the average error
  global input_workload 
  global number_file
  global workload_dir
  global raw_out_dir
  global workload
  global ref_ipc

  ipc_ref_file_name = workload_dir + "workload_list/" + input_workload + "_" + network_size + "_ipc"
  workload_file_name = workload_dir + "workload_list/" + input_workload + "_" + network_size
  raw_out_dir = workload_dir + "/" + input_workload + "/" + network_size + "/baseline/"
  ref_ipc_file = open (ipc_ref_file_name)
  ref_ipc = ref_ipc_file.readlines()
  ref_ipc_file.close()
  workload_file = open (workload_file_name)
  workload = workload_file.readlines()
  workload_file.close()
  [error_rate_raw, error_rate_sum] = compute_error_rate(num_file)
  return [error_rate_raw, error_rate_sum]

def to_percent(y, position):
  # Ignore the passed in position. This has the effect of scaling the default
   # tick locations.
  s = str(100 * y)

  # The percent symbol needs escaping in latex
  if matplotlib.rcParams['text.usetex'] is True:
    return s + r'$\%$'
  else:
    return s + '%'

def cmp_geo_avg (error_raw):
  # error rate has to be an array or list
  new_error_raw = [log(x) for x in error_raw]
  return exp(sum (new_error_raw)/len(new_error_raw))

			
#cmd_input()
network_size = "4x4"
NODE =16

input_workload = "random"
num_file_0 = 30
workload_dir = "/Users/xiyuexiang/Desktop/SlowdownError/"
#print "Going to simulate " + input_workload + " workload (file counts) : " + str(num_file_0)
[error_raw_0, error_sum_0] = comp_avg_error(num_file_0)
avg_slowdown_error = error_sum_0 / num_file_0 / NODE
geo_avg_slowdown_error = cmp_geo_avg (error_raw_0)
#print "**********   Average Error Rate of " + input_workload + " (Low network intensity)************ \n%.4f\n" % avg_slowdown_error
print "**********   Average Geometric Error Rate of " + input_workload + " (Low network intensity, 4x4)************ \n%.4f" % geo_avg_slowdown_error

input_workload = "homo_mem"
num_file_1 =  30
workload_dir = "/Users/xiyuexiang/Desktop/SlowdownError/"
#print "Go toing simulate " + input_workload + " workload (file counts) : " + str(num_file_1)
[error_raw_1, error_sum_1] = comp_avg_error(num_file_1)
avg_slowdown_error = error_sum_1 / num_file_1 / NODE
geo_avg_slowdown_error = cmp_geo_avg (error_raw_1)
#print "**********   Average Error Rate of " + input_workload + " (High network intensity)******** \n%.4f\n" % avg_slowdown_error
print "**********   Average Geometric Error Rate of " + input_workload + " (High network intensity, 4x4)************ \n%.4f" % geo_avg_slowdown_error

input_workload = "hetero"
num_file_2 = 30
workload_dir = "/Users/xiyuexiang/Desktop/SlowdownError/"
#print "Go toing simulate " + input_workload + " workload (file counts) : " + str(num_file_2)
[error_raw_2,error_sum_2] = comp_avg_error(num_file_2)
avg_slowdown_error = error_sum_2 / num_file_2 / NODE
geo_avg_slowdown_error = cmp_geo_avg (error_raw_2)
#print "******** Average Error Rate of " + input_workload + " (Medium network intensity) ******** \n%.4f\n" % avg_slowdown_error
print "**********   Average Geometric Error Rate of " + input_workload + " (Medium network intensity, 4x4)************ \n%.4f" % geo_avg_slowdown_error

overall_error_raw = error_raw_0 + error_raw_1 + error_raw_2
overall_geo_avg = cmp_geo_avg (overall_error_raw)
print str("**********  The overall geometric average error rate *************\n%.4f" % overall_geo_avg)
print "-------------------------------------------------------------------------------------------------"
compute (error_rate_per_app, error_rate_count)

network_size = "8x8"
NODE = 64
error_rate_count = [0] * num_app
error_rate_per_app = []
for i in range (num_app):
  error_rate_per_app.append([])

input_workload = "random"
num_file_0 = 30
workload_dir = "/Users/xiyuexiang/Desktop/SlowdownError/"
#print "Going to simulate " + input_workload + " workload (file counts) : " + str(num_file_0)
[error_raw_0, error_sum_0] = comp_avg_error(num_file_0)
avg_slowdown_error = error_sum_0 / num_file_0 / NODE
geo_avg_slowdown_error = cmp_geo_avg (error_raw_0)
#print "**********   Average Error Rate of " + input_workload + " (Low network intensity)************ \n%.4f\n" % avg_slowdown_error
print "**********   Average Geometric Error Rate of " + input_workload + " (Low network intensity, 8x8)************ \n%.4f" % geo_avg_slowdown_error

input_workload = "homo_mem"
num_file_1 =  30
workload_dir = "/Users/xiyuexiang/Desktop/SlowdownError/"
#print "Go toing simulate " + input_workload + " workload (file counts) : " + str(num_file_1)
[error_raw_1, error_sum_1] = comp_avg_error(num_file_1)
avg_slowdown_error = error_sum_1 / num_file_1 / NODE
geo_avg_slowdown_error = cmp_geo_avg (error_raw_1)
#print "**********   Average Error Rate of " + input_workload + " (High network intensity)******** \n%.4f\n" % avg_slowdown_error
print "**********   Average Geometric Error Rate of " + input_workload + " (High network intensity, 8x8)************ \n%.4f" % geo_avg_slowdown_error

input_workload = "hetero"
num_file_2 = 30
workload_dir = "/Users/xiyuexiang/Desktop/SlowdownError/"
#print "Go toing simulate " + input_workload + " workload (file counts) : " + str(num_file_2)
[error_raw_2,error_sum_2] = comp_avg_error(num_file_2)
avg_slowdown_error = error_sum_2 / num_file_2 / NODE
geo_avg_slowdown_error = cmp_geo_avg (error_raw_2)
#print "******** Average Error Rate of " + input_workload + " (Medium network intensity) ******** \n%.4f\n" % avg_slowdown_error
print "**********   Average Geometric Error Rate of " + input_workload + " (Medium network intensity, 8x8)************ \n%.4f" % geo_avg_slowdown_error
print "---------------------------------------------------------------------------------------------------"

overall_error_raw = error_raw_0 + error_raw_1 + error_raw_2
overall_geo_avg = cmp_geo_avg (overall_error_raw)
print str("**********  The overall geometric average error rate *************\n%.4f" % overall_geo_avg)
print "---------------------------------------------------------------------------------------------------"

# compute the error rate of each application
compute (error_rate_per_app, error_rate_count)

frequency, num_bin, patches = plt.hist(plt_error,bins=20)
plt.show()
#print frequency
#print num_bin
#print patches

