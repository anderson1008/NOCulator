`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/20/2016 07:03:40 PM
// Design Name: 
// Module Name: xbar
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


module xbar(
    in_0,
    in_1,
    in_2,
    in_3,
    out_0,
    out_1,
    out_2,
    out_3,
    indir_rank0,
    indir_rank1,
    indir_rank2,
    indir_rank3
//    allocPV_0,
//    allocPV_1,
//    allocPV_2,
//    allocPV_3
    );
    
    input [`DATA_WIDTH_XBAR-1:0] in_0, in_1, in_2, in_3;
    input [`PC_INDEX_WIDTH-2:0] indir_rank0, indir_rank1, indir_rank2, indir_rank3;
    wire [`NUM_PORT-2:0] allocPV_0, allocPV_1, allocPV_2, allocPV_3; // only checking neighoring nodes
    output [`DATA_WIDTH_XBAR-1:0] out_0, out_1, out_2, out_3;
    
   wire [`PC_INDEX_WIDTH-2:0]  sel_out [3:0];
   assign allocPV_0 = in_0[`NL_PPV_POS];
   assign allocPV_1 = in_1[`NL_PPV_POS];
   assign allocPV_2 = in_2[`NL_PPV_POS];
   assign allocPV_3 = in_3[`NL_PPV_POS];
   
   genvar i;
   
   generate
      assign sel_out[0] = allocPV_0 [0] ? indir_rank0 : 
                      allocPV_1 [0] ? indir_rank1 :
                      allocPV_2 [0] ? indir_rank2 :
                      allocPV_3 [0] ? indir_rank3 :
                      `NULL_PC;
   
      assign sel_out[1] = allocPV_0 [1] ? indir_rank0 : 
                    allocPV_1 [1] ? indir_rank1 :
                    allocPV_2 [1] ? indir_rank2 :
                    allocPV_3 [1] ? indir_rank3 :
                    `NULL_PC;
                    
     assign sel_out[2] = allocPV_0 [2] ? indir_rank0 : 
                   allocPV_1 [2] ? indir_rank1 :
                   allocPV_2 [2] ? indir_rank2 :
                   allocPV_3 [2] ? indir_rank3 :
                   `NULL_PC;
                   
      assign sel_out[3]= allocPV_0 [3] ? indir_rank0 : 
                     allocPV_1 [3] ? indir_rank1 :
                     allocPV_2 [3] ? indir_rank2 :
                     allocPV_3 [3] ? indir_rank3 :
                     `NULL_PC;               
   endgenerate
       
   generate
        for (i=0; i<`DATA_WIDTH_XBAR; i=i+1) begin : selectOutput
            mux4to1 mux_out_0 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[0], out_0[i] );
            mux4to1 mux_out_1 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[1], out_1[i] );
            mux4to1 mux_out_2 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[2], out_2[i] );
            mux4to1 mux_out_3 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[3], out_3[i] );
        end
   
   endgenerate 
    
    
endmodule
