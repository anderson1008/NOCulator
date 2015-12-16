#!/usr/bin/python
# use to profile the application running solo.

import re
import os
import sys
import fnmatch
import matplotlib.pyplot as plt
	
def print_est_sd(est_sd):
    # est_sd is an iterator or array
    for item in est_sd:
        plt.plot(item)
        plt.legend()
    plt.show()
    print "^_^"
