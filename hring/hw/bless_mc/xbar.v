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

`ifdef BLESS
module xbar(
    in_0,
    in_1,
    in_2,
    in_3,
    indir_rank0,
    indir_rank1,
    indir_rank2,
    indir_rank3,
    allocPV_0,
    allocPV_1,
    allocPV_2,
    allocPV_3,
    out_0,
    out_1,
    out_2,
    out_3
    );
    
    input [`DATA_WIDTH_XBAR-1:0] in_0, in_1, in_2, in_3;
    input [`PC_INDEX_WIDTH-2:0] indir_rank0, indir_rank1, indir_rank2, indir_rank3;
    input [`NUM_PORT-2:0] allocPV_0, allocPV_1, allocPV_2, allocPV_3; 
    output [`DATA_WIDTH_XBAR-1:0] out_0, out_1, out_2, out_3;
    
   wire [`PC_INDEX_WIDTH-2:0]  sel_out [3:0]; // select an input channel to channel to an output port, based on the sorted channel index and sorted APV
    
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

   genvar i;        
       
   generate
        for (i=0; i<`DATA_WIDTH_XBAR; i=i+1) begin : selectOutput
            mux4to1 mux_out_0 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[0], out_0[i] );
            mux4to1 mux_out_1 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[1], out_1[i] );
            mux4to1 mux_out_2 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[2], out_2[i] );
            mux4to1 mux_out_3 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[3], out_3[i] );
        end
   
   endgenerate 
    
    
endmodule

`endif // BLESS

`ifdef CARPOOL
module xbar(
    in_0,
    in_1,
    in_2,
    in_3,
    indir_rank0,
    indir_rank1,
    indir_rank2,
    indir_rank3,
    allocPV_0,
    allocPV_1,
    allocPV_2,
    allocPV_3,
    out_0,
    out_1,
    out_2,
    out_3,
    apv_on_out0,
    apv_on_out1,
    apv_on_out2,
    apv_on_out3
    );
    
    input [`DATA_WIDTH_XBAR-1:0] in_0, in_1, in_2, in_3;
    input [`PC_INDEX_WIDTH-2:0] indir_rank0, indir_rank1, indir_rank2, indir_rank3;
    input [`NUM_PORT-1:0] allocPV_0, allocPV_1, allocPV_2, allocPV_3; 
    output [`NUM_PORT-1:0] apv_on_out0, apv_on_out1, apv_on_out2, apv_on_out3;
    output [`DATA_WIDTH_XBAR-1:0] out_0, out_1, out_2, out_3;
    
   wire [`PC_INDEX_WIDTH-2:0]  sel_out [3:0]; // select an input channel to channel to an output port, based on the sorted channel index and sorted APV

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
   
   
   assign apv_on_out0 = allocPV_0[0] ? allocPV_0 :
                        allocPV_1[0] ? allocPV_1 :
                        allocPV_2[0] ? allocPV_2 :
                        allocPV_3[0] ? allocPV_3 : 4'b0;

   assign apv_on_out1 = allocPV_0[1] ? allocPV_0 :
                        allocPV_1[1] ? allocPV_1 :
                        allocPV_2[1] ? allocPV_2 :
                        allocPV_3[1] ? allocPV_3 : 4'b0;
                        
   assign apv_on_out2 = allocPV_0[2] ? allocPV_0 :
                         allocPV_1[2] ? allocPV_1 :
                         allocPV_2[2] ? allocPV_2 :
                         allocPV_3[2] ? allocPV_3 : 4'b0;
 
    assign apv_on_out3 = allocPV_0[3] ? allocPV_0 :
                          allocPV_1[3] ? allocPV_1 :
                          allocPV_2[3] ? allocPV_2 :
                          allocPV_3[3] ? allocPV_3 : 4'b0;                                                                    
                       
       
   generate
        for (i=0; i<`DATA_WIDTH_XBAR; i=i+1) begin : selectOutput
            mux4to1 mux_out_0 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[0], out_0[i] );
            mux4to1 mux_out_1 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[1], out_1[i] );
            mux4to1 mux_out_2 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[2], out_2[i] );
            mux4to1 mux_out_3 (in_0[i], in_1[i], in_2[i], in_3[i], sel_out[3], out_3[i] );
        end
   
   endgenerate 
    
    
endmodule
`endif //CARPOOL