`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/10/2016 12:09:29 AM
// Design Name: 
// Module Name: mux6to1
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

module mux6to1(
    in_0,
    in_1,
    in_2,
    in_3,
    in_4,
    in_5,
    sel,
    out
    );
    
    input in_0, in_1, in_2, in_3, in_4, in_5;
    input [`PC_INDEX_WIDTH-1:0] sel;
    output out;
    
    wire w_data_0_1, w_data_2_3, w_data_4_5, w_data_01_23;

    // stage 1
    mux mux_0_1 (
    .in_0 (in_0),
    .in_1 (in_1),
    .sel  (sel[0]),
    .out  (w_data_0_1)
    );
    
    mux mux_2_3 (
    .in_0 (in_2),
    .in_1 (in_3),
    .sel  (sel[0]),
    .out  (w_data_2_3)   
    );
    
    mux mux_4_5 (
    .in_0 (in_4),
    .in_1 (in_5),
    .sel  (sel[0]),
    .out  (w_data_4_5)  
    );
    
    // stage 2
    mux mux_01_23 (
    .in_0 (w_data_0_1),
    .in_1 (w_data_2_3),
    .sel  (sel[1]),
    .out  (w_data_01_23)
    );
    
    // stage 3
    mux mux_0123_45 (
    .in_0 (w_data_01_23),
    .in_1 (w_data_4_5),
    .sel  (sel[2]),
    .out  (out)
    );  
    
endmodule
