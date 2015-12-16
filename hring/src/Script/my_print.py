#!/usr/bin/python
# use to profile the application running solo.

import re
import os
import sys
import fnmatch
import matplotlib.pyplot as plt
	
def print_period(stat):
    # stat is an iterator or array
    i = 0
    for item in stat:
        plt.plot(item, label=str(i))
        plt.legend()
        i = i + 1



