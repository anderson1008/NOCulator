#!/usr/bin/python
import re
import os
import sys
import fnmatch

#app_name = "429.mcf"
#epoch = "100000"
app_name = raw_input ("Go ahead enter the application name (e.g. 429.mcf) : ");
epoch = raw_input ("enter the epoch (10K - 1M with 10K interval) : ");

if os.path.isdir('./result') == False:
	os.mkdir('./result')
filename = './result/' + app_name + '_epoch' + epoch + '.slowdown'
filename_out = str(filename)
if os.path.exists(filename_out) == True:
	os.remove(filename_out)
fo_out = open(filename_out, "a")

line = 'Application = ' + app_name + '\n'
line = line + 'epoch = ' + epoch + '\n\n\n'
l = str(line)
fo_out.write(l)
line = ''

filelist = os.listdir(".")
for number_of_app in range(2, 16):
	filename_target = app_name + '_' + str(number_of_app) + '_epoch' + epoch + '.out'
	for file in filelist:
		if fnmatch.fnmatch(file,filename_target) != True:
			continue
	
		# Open a file

		fo_in = open(file, "r")

		filename = fo_in.name;

		#search_name = re.search(r'([^_]*)_(\d*)_epoch(100000)\.out',filename)
		#if search_name == None:
			#continue

		# only work on related files
		#app_name = search_name.group(1)
		#number_of_app = search_name.group(2)
		#epoch = search_name.group(3)
		#print app_name + number_of_app

		content = fo_in.read();

		# extract slowdown error for core 7
		if content.find("avg_slowdown_error") != -1:
			searchObj = re.search(r'\[(\{"avg":(\d*.\d*|\d*),[^\}]*\},){7,8}', content)	# the 8# occurence of searched pattern is from core 7
			avg_sl_error_rate = searchObj.group(2) # extract avg. error rate of core 7

		fo_in.close()

		line = line + 'number of applications = ' + str(number_of_app) + '\n'
		line = line + avg_sl_error_rate
		line = line + '\n************************************\n'
		l = str(line)
		fo_out.write(l)
		line = ''

fo_out.close()
