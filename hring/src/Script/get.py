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

	

