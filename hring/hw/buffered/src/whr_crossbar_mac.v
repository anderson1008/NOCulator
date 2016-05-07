// $Id: whr_crossbar_mac.v 1556 2009-09-22 22:52:47Z dub $

/*
Copyright (c) 2007-2009, Trustees of The Leland Stanford Junior University
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list
of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this 
list of conditions and the following disclaimer in the documentation and/or 
other materials provided with the distribution.
Neither the name of the Stanford University nor the names of its contributors 
may be used to endorse or promote products derived from this software without 
specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND 
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED 
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR 
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES 
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON 
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS 
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/



// configurable crossbar module for wormhole router
module whr_crossbar_mac
  (clk, reset, ctrl_op_ip, data_in_ip, data_out_op);
   
`include "c_constants.v"
   
   // number of input/output ports
   parameter num_in_ports = 5;
   parameter num_out_ports = 5;
   
   // width per port
   parameter width = 32;
   
   // select implementation variant
   parameter crossbar_type = `CROSSBAR_TYPE_MUX;
   
   parameter reset_type = `RESET_TYPE_ASYNC;
      
   input clk;
   input reset;
   
   // incoming control signals (request matrix)
   input [0:num_in_ports*num_out_ports-1] ctrl_op_ip;
   
   // vector of input data
   input [0:num_in_ports*width-1] data_in_ip;
   
   // vector of output data
   output [0:num_out_ports*width-1] data_out_op;
   wire [0:num_out_ports*width-1] data_out_op;
   
   wire [0:num_out_ports*num_in_ports-1] ctrl_op_ip_s, ctrl_op_ip_q;
   assign ctrl_op_ip_s = ctrl_op_ip;
   c_dff
     #(.width(num_out_ports*num_in_ports),
       .reset_type(reset_type))
   ctrl_op_ipq
     (.clk(clk),
      .reset(reset),
      .d(ctrl_op_ip_s),
      .q(ctrl_op_ip_q));
   
   wire [0:num_in_ports*num_out_ports-1] ctrl_ip_op_q;
   c_interleaver
     #(.width(num_out_ports*num_in_ports),
       .num_blocks(num_out_ports))
   ctrl_ip_op_q_intl
     (.data_in(ctrl_op_ip_q),
      .data_out(ctrl_ip_op_q));
   
   c_crossbar
     #(.num_in_ports(num_in_ports),
       .num_out_ports(num_out_ports),
       .width(width),
       .crossbar_type(crossbar_type))
   xbr
     (.ctrl_ip_op(ctrl_ip_op_q),
      .data_in_ip(data_in_ip),
      .data_out_op(data_out_op));
   
endmodule
