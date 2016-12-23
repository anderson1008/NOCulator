`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/08/2016 10:45:22 PM
// Design Name: 
// Module Name: arb
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

// We assume input 0 has higher priority by default, unless port 1 is older than port 0
`include "global.vh"

module arb (
    val_in_0,
    index_in_0,
    val_in_1,
    index_in_1,
    en_0,
    en_1,
    val_out,
    index_out
    );
    
    parameter WIDTH_INDEX = 3;
    
    input [`TIME_WIDTH-1:0] val_in_0, val_in_1;
    input [WIDTH_INDEX-1:0] index_in_0, index_in_1;
    input en_0, en_1;
    output [`TIME_WIDTH-1:0] val_out;
    output [WIDTH_INDEX-1:0] index_out;
    
    wire older;
    assign older = (val_in_0 < val_in_1) ? 1'b1 : 1'b0;
    

    assign val_out = ((en_0 & older) | !en_1) ? val_in_0 : val_in_1;
    assign index_out = ((en_0 & older) | !en_1) ? index_in_0 : index_in_1;
       
endmodule
