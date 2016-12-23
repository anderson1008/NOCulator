`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/08/2016 11:02:10 PM
// Design Name: 
// Module Name: vcArb
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

// VC arbiter (per port)
// Assuming 4 VCs/port

module vcArb4to1(
    time_in_0,
    time_in_1,
    time_in_2,
    time_in_3,
    winner_time_out,
    winner_vc_out // the winner vc index
    );

    input [`TIME_WIDTH-1:0] time_in_0, time_in_1, time_in_2, time_in_3;
    output [`VC_INDEX_WIDTH-1:0] winner_vc_out;
    output [`TIME_WIDTH-1:0] winner_time_out;
      
    wire [`TIME_WIDTH-1:0] w_time_0, w_time_1;
    wire [`VC_INDEX_WIDTH-1:0] w_win_0, w_win_1; 
      
    arb # (
    .WIDTH_INDEX (`VC_INDEX_WIDTH)) 
    arb_0_0(
    .val_in_0   (time_in_0),
    .index_in_0 (`VC_INDEX_WIDTH'd0),
    .val_in_1   (time_in_1),
    .index_in_1 (`VC_INDEX_WIDTH'd1),
    .en_0       (1'b1),
    .en_1       (1'b1),
    .val_out    (w_time_0),
    .index_out  (w_win_0)
    );
    
    arb # (
    .WIDTH_INDEX (`VC_INDEX_WIDTH)) 
    arb_0_1(
    .val_in_0   (time_in_2),
    .index_in_0 (`VC_INDEX_WIDTH'd2),
    .val_in_1   (time_in_3),
    .index_in_1 (`VC_INDEX_WIDTH'd3),
    .en_0       (1'b1),
    .en_1       (1'b1),
    .val_out    (w_time_1),
    .index_out  (w_win_1)
    );    
    
    arb # (
    .WIDTH_INDEX (`VC_INDEX_WIDTH)) 
    arb_1_0(
    .val_in_0   (w_time_0),
    .index_in_0 (w_win_0),
    .val_in_1   (w_time_1),
    .index_in_1 (w_win_1),
    .en_0       (1'b1),
    .en_1       (1'b1),
    .val_out    (winner_time_out),
    .index_out  (winner_vc_out)
    );      
    
endmodule
