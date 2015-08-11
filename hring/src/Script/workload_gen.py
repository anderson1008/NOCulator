#!/usr/bin/python

import sys
import random
import os

number_of_workload = 100

spec_workload = ["400.perlbench.bin.gz ","401.bzip2.bin.gz ","403.gcc.bin.gz ","429.mcf.bin.gz ","433.milc.bin.gz ","435.gromacs.bin.gz ","436.cactusADM.bin.gz ",\
"437.leslie3d.bin.gz ","444.namd.bin.gz ","445.gobmk.bin.gz ","447.dealII.bin.gz ","450.soplex.bin.gz ","453.povray.bin.gz ","454.calculix.bin.gz ",\
"456.hmmer.bin.gz ","458.sjeng.bin.gz ","459.GemsFDTD.bin.gz ","462.libquantum.bin.gz ","464.h264ref.bin.gz ","465.tonto.bin.gz ","470.lbm.bin.gz ",\
"471.omnetpp.bin.gz ","473.astar.bin.gz ","481.wrf.bin.gz ","482.sphinx3.bin.gz ","483.xalancbmk.bin.gz "]

spec_ipc = ["2.17 ", "2.13 ", "1.83 ", "2.08 ", "2.16 ", "1.87 ", "1.38 ", "2.75 ", "2.36 ", "2.33 ", "1.91 ", "1.77 ", "1.88 ", "1.71 ", "1.6 ", "2.18 ", "1.33 ", "2.73 ", "1.73 ", "1.85 ", "1.91 ", "2.65 ", "2.04 ", "2.92 ", "2.51 ", "2.02 "]


filename = 'hetero_workload'
filename_out = str(filename)
if os.path.exists(filename_out) == True:
	os.remove(filename_out)
workload_out = open(filename_out, "w")
filename = 'hetero_ipc_ref'
filename_out = str(filename)
if os.path.exists(filename_out) == True:
	os.remove(filename_out)
ipc_out = open(filename_out, "w")

workload_out.write("/safari/dromedary/kevincha/traces/blesstraces")

workload_index = 0
while (workload_index < number_of_workload):

	i = random.randint(0, 25)
	j = random.randint(0, 25)
	k = random.randint(0, 25)
	m = random.randint(0, 25)

	while (j == i):
		j = random.randint(0, 25)
	while (k == i or k == j):
		k = random.randint(0, 25)
	while (m == i or m == j or m == k):
		m = random.randint(0, 25)

	workload = '\n' + spec_workload [i] + spec_workload [j] + spec_workload [k] + spec_workload [m] + spec_workload [i] + spec_workload [j] + spec_workload [k] + spec_workload [m] + spec_workload [i] + spec_workload [j] + spec_workload [k] + spec_workload [m] + spec_workload [i] + spec_workload [j] + spec_workload [k] + spec_workload [m]
	ipc = '\n' + spec_ipc [i] + spec_ipc [j] + spec_ipc [k] + spec_ipc [m] + spec_ipc [i] + spec_ipc [j] + spec_ipc [k] + spec_ipc [m] + spec_ipc [i] + spec_ipc [j] + spec_ipc [k] + spec_ipc [m] + spec_ipc [i] + spec_ipc [j] + spec_ipc [k] + spec_ipc [m]

	workload_out.write(workload)
	ipc_out.write(ipc)
	workload_index = workload_index + 1

workload_out.close()
ipc_out.close()
