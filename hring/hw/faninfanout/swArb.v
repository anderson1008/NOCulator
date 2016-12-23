`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/08/2016 11:46:25 PM
// Design Name: 
// Module Name: swArb
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

module swArb( 
    time_in_0,
    time_in_1,
    time_in_2,
    time_in_3,
    time_in_4,
    req_in_0,
    req_in_1,
    req_in_2,
    req_in_3,
    req_in_4,
    vc_pc_0,
    vc_pc_1,
    vc_pc_2,
    vc_pc_3,
    vc_pc_4,
    vc0_credit, // downstream VC credit
    vc1_credit,
    vc2_credit,
    vc3_credit,
    vcNew, // updated vc
    winner_pc_out // the winner vc index,
    );
    
    input [`TIME_WIDTH-1:0] time_in_0, time_in_1, time_in_2, time_in_3, time_in_4;
    input req_in_0, req_in_1, req_in_2, req_in_3, req_in_4;
    input [`VC_INDEX_WIDTH-1:0] vc_pc_0, vc_pc_1, vc_pc_2, vc_pc_3, vc_pc_4;
    input [`ADDR_WIDTH-1:0] vc0_credit, vc1_credit, vc2_credit, vc3_credit;
    output [`VC_INDEX_WIDTH-2:0] vcNew;
    output [`PC_INDEX_WIDTH-1:0] winner_pc_out;
  
    wire [`TIME_WIDTH-1:0] w_time_0, w_time_1, w_time_2;
    wire [`PC_INDEX_WIDTH-1:0] w_win_0, w_win_1, w_win_2, w_win_out; 
          
     arb # (
    .WIDTH_INDEX (`PC_INDEX_WIDTH)) 
    arb_0_0(
    .val_in_0   (time_in_0),
    .index_in_0 (`PC_INDEX_WIDTH'd0),
    .val_in_1   (time_in_1),
    .index_in_1 (`PC_INDEX_WIDTH'd1),
    .en_0       (req_in_0),
    .en_1       (req_in_1),
    .val_out    (w_time_0),
    .index_out  (w_win_0)
    );
    
    arb # (
   .WIDTH_INDEX (`PC_INDEX_WIDTH)) 
   arb_0_1(
   .val_in_0   (time_in_2),
   .index_in_0 (`PC_INDEX_WIDTH'd2),
   .val_in_1   (time_in_3),
   .index_in_1 (`PC_INDEX_WIDTH'd3),
   .en_0       (req_in_2),
   .en_1       (req_in_3),
   .val_out    (w_time_1),
   .index_out  (w_win_1)
   ); 
   
  arb # (
  .WIDTH_INDEX (`PC_INDEX_WIDTH)) 
  arb_1_0(
  .val_in_0   (w_time_1),
  .index_in_0 (w_win_1),
  .val_in_1   (time_in_4),
  .index_in_1 (`PC_INDEX_WIDTH'd4),
  .en_0       (req_in_2 | req_in_3),
  .en_1       (req_in_4),
  .val_out    (w_time_2),
  .index_out  (w_win_2)
  );   
     
  arb # (
  .WIDTH_INDEX (`PC_INDEX_WIDTH)) 
  arb_2_0(
  .val_in_0   (w_time_0),
  .index_in_0 (w_win_0),
  .val_in_1   (w_time_2),
  .index_in_1 (w_win_2),
  .en_0       (req_in_0 | req_in_1),
  .en_1       (req_in_2 | req_in_3 | req_in_4),
  .val_out    (),
  .index_out  (w_win_out)   // w_win_out is the index of input port
  );     
  
  
  // get the vc index associated with the winner input port
  wire [`VC_INDEX_WIDTH-1:0] vc = (w_win_out == 0) ? vc_pc_0 : 
            (w_win_out == 1) ? vc_pc_1 :
            (w_win_out == 2) ? vc_pc_2 :
            (w_win_out == 3) ? vc_pc_3 :
            (w_win_out == 4) ? vc_pc_4 : vc_pc_0;
               
  // check VC credit in the downstream router and change VC if necessary
  wire thisVCHasCredit = (vc == 0) ? (vc0_credit > 0) : 
                   (vc == 1) ? (vc1_credit > 0) :
                   (vc == 2) ? (vc2_credit > 0) :
                   (vc == 3) ? (vc3_credit > 0) : 1'b0;
                   
  wire otherVCHasCredit = thisVCHasCredit ? 1'b0 :
                    (vc0_credit > 0) ? 1'b1 : (
                    (vc1_credit > 0) ? 1'b1 : (
                    (vc2_credit > 0) ? 1'b1 : (
                    (vc3_credit > 0) ? 1'b1 : 1'b0)));
                    
  assign vcNew = thisVCHasCredit ? vc : 
              (vc0_credit > 0) ? `VC_INDEX_WIDTH'd0 : (
              (vc1_credit > 0) ? `VC_INDEX_WIDTH'd1 : (
              (vc2_credit > 0) ? `VC_INDEX_WIDTH'd2 : (
              (vc3_credit > 0) ? `VC_INDEX_WIDTH'd3 : vc)));  
   
  assign winner_pc_out = ((req_in_0 | req_in_1 | req_in_2 | req_in_3 | req_in_4) && (thisVCHasCredit || otherVCHasCredit)) ? w_win_out  : `NULL_PC; // 5 means nothing. output 0
endmodule
