#!/bin/bash

bin_dir="/home/xiyue/Desktop/NOCulator/hring/src/bin"

declare -a simulation_file=("400.perlbench" "429.mcf" "433.milc" "450.soplex" "470.lbm" "471.omnetpp")
declare -a ipc=("2.029" "1.863" "2.480" "2.034" "1.901" "2.700")

#for ((epoch_index=1; epoch_index<=10; epoch_index++))
#do
	for ((workload_index=0; workload_index<=5; workload_index++))
	do
		for ((sim_index=2; sim_index<=16; sim_index++)) 
		do
	
			echo "###########################################"

			workload=${simulation_file[$((workload_index))]}

			#echo " number of applications : $sim_index"
	
			output_file="$workload"	
	
			output_file+="_$sim_index"

			slowdown_epoch=100000

			output_file+="_epoch$slowdown_epoch"
			
			output_file+=".out"

			#screen_output="screen"
	
			#screen_output+="$sim_index"
	
			#screen_output+=".out"

			echo " output file : ${output_file}"

			echo "###########################################"


			./sim.exe -config $bin_dir/config_0.txt -output $bin_dir/slowdown_estimate/$workload/$output_file -workload $bin_dir/$workload $sim_index -ref_ipc ${ipc[$((workload_index))]} -slowdown_epoch $slowdown_epoch
#			echo "./sim.exe -config $bin_dir/config_0.txt -output $bin_dir/slowdown_estimate/$workload/$output_file -workload $bin_dir/$workload $sim_index -ref_ipc ${ipc[$((workload_index))]} -slowdown_epoch $slowdown_epoch"
#	./sim.exe -config $bin_dir/config_0.txt -output $bin_dir/ipc_comp/$output_file -workload $bin_dir/single_app_spec2006 $sim_index 2>&1 | tee $bin_dir/mcf/$screen_output

		done 
	done
#done
