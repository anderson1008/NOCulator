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
    unallocPortVector,
    availPortVector_out,
    numFlit_out
    );
    
    input mc;
    input [`PC_INDEX_WIDTH-1:0] numFlit_in;
    input [`NUM_PORT-2:0] availPortVector_in, ppv;
    output [`NUM_PORT-2:0] allocatedPortVector, availPortVector_out, unallocPortVector;
    output [`PC_INDEX_WIDTH-1:0] numFlit_out;  
    
    wire [`PC_INDEX_WIDTH-1:0] numFlit [`NUM_PORT-1:0];
    assign numFlit[0] = numFlit_in;
    
    genvar i;
    
    generate 
    for (i=0; i<`NUM_PORT-1; i=i+1) begin : portAllocation   
        assign allocatedPortVector[i] = ppv[i] && availPortVector_in[i] && (~mc || (mc && (numFlit[i] <= `NUM_PORT-1)));
        assign numFlit[i+1] = numFlit[i] + allocatedPortVector[i]; 
        assign availPortVector_out[i] =  availPortVector_in[i] && ~allocatedPortVector[i];
        assign unallocPortVector[i] = ppv[i] && ~allocatedPortVector[i];
    end
    endgenerate
    
    assign numFlit_out = numFlit[`NUM_PORT-1];
 
endmodule
