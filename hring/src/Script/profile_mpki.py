#!/usr/bin/python
# use to profile the application running solo.

import re
import os
import sys
import fnmatch

filename = 'profile.txt'
filename_out = str(filename)
if os.path.exists(filename_out) == True:
	os.remove(filename_out)
fo_out = open(filename_out, "a")

fo_out.write('\n\n' + 'Profile mpki, ipc' + '\n\n')
fo_out.write('ordered by mpki' + '\n\n')
fo_out.write('\n')
fo_out.write('Index'.ljust(5) + 'File Name'.ljust(15) + 'IPC'.ljust(10) + 'MPKI'.ljust(10))
fo_out.write('\n')


filelist = os.listdir(".")
file_count = 0
for file in filelist:
	if fnmatch.fnmatch(file,'*.out') != True:
		continue
	file_count = file_count + 1
	fo_in = open(file, "r")
	content = fo_in.read();
	file_name = re.search(r'(.*?).out',file).group(1)

	searchObj = re.search(r'"mpki_bysrc":\[(.*)\]', content)
	searchObj = re.search(r'(?:\{"avg":.*?\},){7}(\{"avg":.*?\},)', searchObj.group(1))	
	searchObj = re.search(r'(?:\{"avg":([\w.]+),)', searchObj.group(1))
	mpki = searchObj.group(1)

	searchObj = re.search(r'(?:"active_cycles":\[(.*?)])',content)
	splitObj = re.split('\W+',searchObj.group(1))
	active_cycle = float(splitObj[7])
	
	searchObj = re.search(r'(?:"insns_persrc":\[(.*?)])',content)
	splitObj = re.split('\W+',searchObj.group(1))
	insns_count = float(splitObj[7])

	fo_in.close()
	ipc = insns_count / active_cycle
	ipc_str = str("%.2f" % ipc)
	
	fo_out.write('\n')
	fo_out.write(str(file_count).ljust(5) + file_name.ljust(15) + ipc_str.ljust(10) + mpki.ljust(10))
	fo_out.write('\n')
	
fo_out.close()

