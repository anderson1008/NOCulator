#!/usr/bin/python

import sys
import os
import re
import fnmatch
import string


# evaluate test.out 

work_dir = "/home/anderson/Desktop/NOCulator/hring/src/results/"
input_file = input('please input the file name: ' )
test_file_name = work_dir + input_file
print input_file

result_file = open (test_file_name, 'r')
result = result_file.read()
result_file.close()

insns_count = 1000000

searchObj = re.search(r'(?:"active_cycles":\[(.*?)])',result)
splitObj = re.split('\W+',searchObj.group(1))
active_cycles = splitObj
print active_cycles

searchObj = re.search(r'(?:"non_overlap_penalty":\[(.*?)])',result)
splitObj = re.split('\W+',searchObj.group(1))
non_overlap_penalty = splitObj

ipc_alone = [2.16, 2.75, 2.08, 1.91, 2.16, 2.75, 2.08, 1.91, 2.16, 2.75, 2.08, 1.91, 2.16, 2.75, 2.08, 1.91]
ipc_share = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
weighted_speedup = 0
temp0 = 0
for i in range (0, 16, 1):
	ipc_share [i] = float(insns_count) / int(active_cycles[i])
	weighted_speedup = ipc_share[i] / ipc_alone[i] + weighted_speedup
	temp0 = ipc_alone[i] / ipc_share[i] + temp0
harmonic_speedup = 16 / temp0

print str("%.2f" % weighted_speedup)
print str("%.2f" % harmonic_speedup)
			

	

