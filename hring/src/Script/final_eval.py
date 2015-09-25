#!/usr/bin/python

# To compute and extract the final result from SPEC simulation.
# consider 3 network size: 4x4, 8x8, 16x16
# consider 2 designs: BLESS and MBNoC


import MBNoC_collect_data
import my_print

# diectory need to set every time MBNoC_collect_data.py is called

print ("-------------------------------------    16 Node   ------------------------------------")
MBNoC_collect_data.node = 16
MBNoC_collect_data.subnet = 2
MBNoC_collect_data.class_factor = [280,330]
MBNoC_collect_data.file_dir_bless = '/home/anderson/Desktop/NOCulator/hring/src/results/BLESS/4x4/'
MBNoC_collect_data.file_dir_mbnoc = '/home/anderson/Desktop/NOCulator/hring/src/results/MBNoC/4x4/'
MBNoC_collect_data.evaluation()
final_stat_16node = MBNoC_collect_data.final_stat
break_down_power_16node  = MBNoC_collect_data.break_down_power
overall_power_16node  = MBNoC_collect_data.overall_power
my_print.print_power_breakdown(break_down_power_16node )
my_print.print_power(overall_power_16node )
my_print.print_final_stat(final_stat_16node)

print ("-------------------------------------   64 Node   -------------------------------------")
MBNoC_collect_data.node = 64
MBNoC_collect_data.subnet = 2
MBNoC_collect_data.class_factor = [300,350]
MBNoC_collect_data.file_dir_bless = '/home/anderson/Desktop/NOCulator/hring/src/results/BLESS/8x8/'
MBNoC_collect_data.file_dir_mbnoc = '/home/anderson/Desktop/NOCulator/hring/src/results/MBNoC/8x8/'
MBNoC_collect_data.evaluation()
final_stat_64node = MBNoC_collect_data.final_stat
break_down_power_64node  = MBNoC_collect_data.break_down_power
overall_power_64node  = MBNoC_collect_data.overall_power
my_print.print_power_breakdown(break_down_power_64node )
my_print.print_power(overall_power_64node )
my_print.print_final_stat(final_stat_64node)

print ("-------------------------------------    256 Node   -------------------------------------")
MBNoC_collect_data.node = 256
MBNoC_collect_data.subnet = 2
MBNoC_collect_data.class_factor = [50,100]
MBNoC_collect_data.insns_count = 1000000
MBNoC_collect_data.file_dir_bless = '/home/anderson/Desktop/NOCulator/hring/src/results/BLESS/16x16/'
MBNoC_collect_data.file_dir_mbnoc = '/home/anderson/Desktop/NOCulator/hring/src/results/MBNoC/16x16/'
MBNoC_collect_data.evaluation()
final_stat_256node = MBNoC_collect_data.final_stat
break_down_power_256node  = MBNoC_collect_data.break_down_power
overall_power_256node  = MBNoC_collect_data.overall_power
my_print.print_power_breakdown(break_down_power_256node )
my_print.print_power(overall_power_256node )
my_print.print_final_stat(final_stat_256node)


print ("-------------------------------------    Final Result   -------------------------------------")
overall_stat = []
#  count BLESS ws_4x4 ws_8x8 ws_16x6 BLESS energy_4x4 energy_8x8 energy16x16 ......
for i in range (0,4): # 4 Groups
	count = final_stat_16node[i][0] + final_stat_64node[i][0] + final_stat_256node[i][0]
	stat = (count, 1, final_stat_16node[i][2], final_stat_64node[i][2], final_stat_256node[i][2], 1, final_stat_16node[i][4], final_stat_64node[i][4], final_stat_256node[i][4], 1,  final_stat_16node[i][6], final_stat_64node[i][6], final_stat_256node[i][6], 1, final_stat_16node[i][8], final_stat_64node[i][8], final_stat_256node[i][8])
	overall_stat.append(stat)
my_print.print_for_plot (overall_stat)
print ("Max Deflection Rate: %.2f deflection per flit"% MBNoC_collect_data.max_defl)

print ("DONE :)")
