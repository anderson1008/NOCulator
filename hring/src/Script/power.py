#!/usr/bin/python


def comp_static_power (static, count, time):
	static_power = [x*y*time for x,y in zip (static, count)]
	return static_power

def comp_dynamic_power (switch, internal, acutal_toggle_rate, link, link_toggle_rate, time, default_toggle_rate): #eff: effective
	dynamic_power_synth =  [x+y for x,y in zip(switch, internal)]
	dynamic_power = [x / y * z for x,y,z in zip (dynamic_power_synth, default_toggle_rate, acutal_toggle_rate)]
	dynamic_link_power = link * link_toggle_rate
	dynamic_power = dynamic_power + [dynamic_link_power]
	dynamic_power = [x * time for x in dynamic_power]
	return dynamic_power

def comp_toggle_rate (event, component_count, port_on_compoment, cycle):
	result = []
	for x,y,z in zip(event, component_count, port_on_compoment):
		if y*z*cycle is not 0:
			result = result + [x/(y*z*cycle)] 
		else:
			result = result + [0]
	return result
	
