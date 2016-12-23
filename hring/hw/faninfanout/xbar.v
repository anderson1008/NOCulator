`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/09/2016 11:09:41 PM
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
    in_4,
    out_0,
    out_1,
    out_2,
    out_3,
    out_4,
    sel_out_0,
    sel_out_1,
    sel_out_2,
    sel_out_3,
    sel_out_4
    );
    
    input [`IR_DATA_WIDTH-1:0] in_0, in_1, in_2, in_3, in_4;
    input [`PC_INDEX_WIDTH-1:0]  sel_out_0, sel_out_1, sel_out_2, sel_out_3, sel_out_4;
    output [`DATA_WIDTH-1:0] out_0, out_1, out_2, out_3, out_4;
    
    wire [`DATA_WIDTH-1:0] xbar_in_0 [0:`NUM_PORT-1]; // input 0 of crossbar in terms of each output 
    wire [`DATA_WIDTH-1:0] xbar_in_1 [0:`NUM_PORT-1]; // input 0 of crossbar in terms of each output 
    wire [`DATA_WIDTH-1:0] xbar_in_2 [0:`NUM_PORT-1]; // input 0 of crossbar in terms of each output 
    wire [`DATA_WIDTH-1:0] xbar_in_3 [0:`NUM_PORT-1]; // input 0 of crossbar in terms of each output 
    wire [`DATA_WIDTH-1:0] xbar_in_4 [0:`NUM_PORT-1]; // input 0 of crossbar in terms of each output 
    
    genvar j;
    
    generate
        for (j=0; j<4; j=j+1) begin : assign_ppv_turn_bit
            assign xbar_in_0 [j] ={in_0[`IR_DATA_WIDTH-(3-j)*7-1:`DATA_WIDTH+j*7], in_0[`DATA_WIDTH-1-`NUM_PORT-2:0]};
            assign xbar_in_1 [j] ={in_1[`IR_DATA_WIDTH-(3-j)*7-1:`DATA_WIDTH+j*7], in_1[`DATA_WIDTH-1-`NUM_PORT-2:0]};
            assign xbar_in_2 [j] ={in_2[`IR_DATA_WIDTH-(3-j)*7-1:`DATA_WIDTH+j*7], in_2[`DATA_WIDTH-1-`NUM_PORT-2:0]};
            assign xbar_in_3 [j] ={in_3[`IR_DATA_WIDTH-(3-j)*7-1:`DATA_WIDTH+j*7], in_3[`DATA_WIDTH-1-`NUM_PORT-2:0]};
            assign xbar_in_4 [j] ={in_4[`IR_DATA_WIDTH-(3-j)*7-1:`DATA_WIDTH+j*7], in_4[`DATA_WIDTH-1-`NUM_PORT-2:0]};
        end
    endgenerate    
        
        
        
    // select ppv, ltb, rtb 
  genvar i;
        generate
            for (i=0; i<`DATA_WIDTH; i=i+1) begin : sel_output
                mux6to1 mux_out0 (
                .in_0 (xbar_in_0[0][i]),
                .in_1 (xbar_in_1[0][i]),
                .in_2 (xbar_in_2[0][i]),
                .in_3 (xbar_in_3[0][i]),
                .in_4 (xbar_in_4[0][i]),
                .in_5 (1'b0),
                .sel  (sel_out_0),
                .out  (out_0[i])
                );
    
                mux6to1 mux_out1 (
                .in_0 (xbar_in_0[1][i]),
                .in_1 (xbar_in_1[1][i]),
                .in_2 (xbar_in_2[1][i]),
                .in_3 (xbar_in_3[1][i]),
                .in_4 (xbar_in_4[1][i]),
                .in_5 (1'b0),
                .sel  (sel_out_1),
                .out  (out_1[i])
                );            
    
                mux6to1 mux_out2 (
                .in_0 (xbar_in_0[2][i]),
                .in_1 (xbar_in_1[2][i]),
                .in_2 (xbar_in_2[2][i]),
                .in_3 (xbar_in_3[2][i]),
                .in_4 (xbar_in_4[2][i]),
                .in_5 (1'b0),
                .sel  (sel_out_2),
                .out  (out_2[i])
                );
                
                mux6to1 mux_out3 (
                .in_0 (xbar_in_0[3][i]),
                .in_1 (xbar_in_1[3][i]),
                .in_2 (xbar_in_2[3][i]),
                .in_3 (xbar_in_3[3][i]),
                .in_4 (xbar_in_4[3][i]),
                .in_5 (1'b0),
                .sel  (sel_out_3),
                .out  (out_3[i])
                );
                
                // for local destined flit, ppv doesn't affect anything
                mux6to1 mux_out4 (
                .in_0 (in_0[i]),
                .in_1 (in_1[i]),
                .in_2 (in_2[i]),
                .in_3 (in_3[i]),
                .in_4 (in_4[i]),
                .in_5 (1'b0),
                .sel  (sel_out_4),
                .out  (out_4[i])
                );                                
            end
        endgenerate
    
  /*      
    genvar i;
    
    generate
        for (i=0; i<`DATA_WIDTH_XBAR; i=i+1) begin : sel_output
            mux6to1 mux_out0 (
            .in_0 (in_0[i]),
            .in_1 (in_1[i]),
            .in_2 (in_2[i]),
            .in_3 (in_3[i]),
            .in_4 (in_4[i]),
            .in_5 (1'b0),
            .sel  (sel_out_0),
            .out  (out_0[i])
            );

            mux6to1 mux_out1 (
            .in_0 (in_0[i]),
            .in_1 (in_1[i]),
            .in_2 (in_2[i]),
            .in_3 (in_3[i]),
            .in_4 (in_4[i]),
            .in_5 (1'b0),
            .sel  (sel_out_1),
            .out  (out_1[i])
            );            

            mux6to1 mux_out2 (
            .in_0 (in_0[i]),
            .in_1 (in_1[i]),
            .in_2 (in_2[i]),
            .in_3 (in_3[i]),
            .in_4 (in_4[i]),
            .in_5 (1'b0),
            .sel  (sel_out_2),
            .out  (out_2[i])
            );
            
            mux6to1 mux_out3 (
            .in_0 (in_0[i]),
            .in_1 (in_1[i]),
            .in_2 (in_2[i]),
            .in_3 (in_3[i]),
            .in_4 (in_4[i]),
            .in_5 (1'b0),
            .sel  (sel_out_3),
            .out  (out_3[i])
            );
            
             mux6to1 mux_out4 (
            .in_0 (in_0[i]),
            .in_1 (in_1[i]),
            .in_2 (in_2[i]),
            .in_3 (in_3[i]),
            .in_4 (in_4[i]),
            .in_5 (1'b0),
            .sel  (sel_out_4),
            .out  (out_4[i])
            );                                
        end
    endgenerate
    */
endmodule
