`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/20/2016 06:12:32 AM
// Design Name: 
// Module Name: seqPortAlloc
// Project Name: 
// Target Devices: 
// Tool Versions: 
// Description: 
// 
// Dependencies: 
// 
// Revision:
// Revision 0.01 - File Created
// Additional Comments:
// 
//////////////////////////////////////////////////////////////////////////////////

`include "global.vh"

//   sequential port allocaor
module seqPortAlloc(
    mc,
    numFlit_in,
    availPortVector_in,
    ppv,
    allocatedPortVector,
    availPortVector_out,
    numFlit_out
    );
    
    input mc;
    input [`PC_INDEX_WIDTH-1:0] numFlit_in;
    input [`NUM_PORT-2:0] availPortVector_in, ppv;
    output [`NUM_PORT-2:0] allocatedPortVector, availPortVector_out;
    output [`PC_INDEX_WIDTH-1:0] numFlit_out;  
    
    wire [`PC_INDEX_WIDTH-1:0] numFlit [`NUM_PORT-1:0];
    wire [`NUM_PORT-2:0] allocatedPortVector_st1, availPortVector_out_st1;
    wire [`NUM_PORT-2:0] allocatedPortVector_st2, availPortVector_out_st2;
    
    assign numFlit[0] = numFlit_in;
    
    genvar i;
    generate   
    // Stage 1: allocate the productive port
    for (i=0; i<`NUM_PORT-1; i=i+1) begin : productiveAlloc   
        assign allocatedPortVector_st1[i] = ppv[i] && availPortVector_in[i] && (~mc || (mc && (numFlit[i] <= `NUM_PORT-1)));
        assign numFlit[i+1] = numFlit[i] + allocatedPortVector_st1[i]; 
        assign availPortVector_out_st1[i] =  availPortVector_in[i] && ~allocatedPortVector_st1[i];
    end
    endgenerate

    // Stage 2: deflection: find the first available port in the order of N, E, S, W
    assign allocatedPortVector_st2[0] = availPortVector_out_st1[0];
    assign allocatedPortVector_st2[1] = availPortVector_out_st1[1] && ~availPortVector_out_st1[0];
    assign allocatedPortVector_st2[2] = availPortVector_out_st1[2] && ~availPortVector_out_st1[1] && ~availPortVector_out_st1[0];
    assign allocatedPortVector_st2[3] = availPortVector_out_st1[3] && ~availPortVector_out_st1[2] && ~availPortVector_out_st1[1] && ~availPortVector_out_st1[0];
    assign availPortVector_out_st2 [0] = 1'b0;
    assign availPortVector_out_st2 [1] = availPortVector_out_st1[0] && availPortVector_out_st1[1];
    assign availPortVector_out_st2 [2] = |availPortVector_out_st1[1:0] && availPortVector_out_st1[2];
    assign availPortVector_out_st2 [3] = |availPortVector_out_st1[2:0] && availPortVector_out_st1[3];
    

    wire get_port_st1;
    assign get_port_st1 = |allocatedPortVector_st1;
    assign allocatedPortVector = (~|ppv || get_port_st1) ? allocatedPortVector_st1 : allocatedPortVector_st2;
    assign availPortVector_out = (~|ppv || get_port_st1) ? availPortVector_out_st1 : availPortVector_out_st2;          
    assign numFlit_out = (~|ppv || get_port_st1) ? numFlit[`NUM_PORT-1] : numFlit[`NUM_PORT-1] + 1'b1;
 
endmodule
