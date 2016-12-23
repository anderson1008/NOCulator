`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/10/2016 07:42:10 PM
// Design Name: 
// Module Name: vcArb5to1
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


module vcArb5to1(
    time_in_0,
    time_in_1,
    time_in_2,
    time_in_3,
    time_in_4,
    winner_time_out,
    winner_vc_out // the winner vc index
    );
    
    input [`TIME_WIDTH-1:0] time_in_0, time_in_1, time_in_2, time_in_3, time_in_4;
    output [`VC_INDEX_WIDTH-1:0] winner_vc_out;
    output [`TIME_WIDTH-1:0] winner_time_out;
      
    wire [`TIME_WIDTH-1:0] w_time_0, w_time_1, w_time_2;
    wire [`VC_INDEX_WIDTH-1:0] w_win_0, w_win_1, w_win_2; 
      
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
    .val_out    (w_time_2),
    .index_out  (w_win_2)
    );      

    arb # (
    .WIDTH_INDEX (`VC_INDEX_WIDTH)) 
    arb_2_0(
    .val_in_0   (w_time_2),
    .index_in_0 (w_win_2),
    .val_in_1   (time_in_4),
    .index_in_1 (`VC_INDEX_WIDTH'd4),
    .en_0       (1'b1),
    .en_1       (1'b1),
    .val_out    (winner_time_out),
    .index_out  (winner_vc_out)
    ); 
   
endmodule
