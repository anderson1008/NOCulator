`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/11/2016 10:44:04 AM
// Design Name: 
// Module Name: merge
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

module merge(
    clk,
    n_rst,
    data_in_0,
    data_in_1,
    data_in_2,
    data_in_3,
    data_in_4,
    pre_route_0,
    pre_route_1,
    pre_route_2,
    pre_route_3,
    pre_route_4,
    hs_master_buf_0,
    hs_master_buf_1,
    hs_master_buf_2,
    hs_master_buf_3,
    hs_master_buf_4,
    hs_m_buf_empty_new, // empty get set in this module
    hs_m_buf_empty_out, // it get clear in pc.v
    data_out_0,
    data_out_1,
    data_out_2,
    data_out_3,
    data_out_4,
    merged
    );
    
    input clk, n_rst;
    input [`DATA_WIDTH-1:0] data_in_0, data_in_1, data_in_2, data_in_3, data_in_4;
    input [`NUM_PORT-1:0] hs_m_buf_empty_new;
    input [`IR_WIDTH-1:0] pre_route_0, pre_route_1, pre_route_2, pre_route_3, pre_route_4;
    output [`NUM_PORT-1:0] hs_m_buf_empty_out;
    output [`IR_DATA_WIDTH-1:0] data_out_0, data_out_1, data_out_2, data_out_3, data_out_4;
    output [`IR_DATA_WIDTH-1:0] hs_master_buf_0, hs_master_buf_1, hs_master_buf_2, hs_master_buf_3, hs_master_buf_4;
    output [`NUM_PORT-1:0] merged;

    
    reg [`IR_DATA_WIDTH-1:0] hs_master_buf_0, hs_master_buf_1, hs_master_buf_2, hs_master_buf_3, hs_master_buf_4;
    wire [`IR_DATA_WIDTH-1:0] new_hs_master_buf_0, new_hs_master_buf_1, new_hs_master_buf_2, new_hs_master_buf_3, new_hs_master_buf_4;
    wire [`IR_DATA_WIDTH-1:0] new_new_hs_master_buf_0, new_new_hs_master_buf_1, new_new_hs_master_buf_2, new_new_hs_master_buf_3, new_new_hs_master_buf_4;
    wire [`IR_DATA_WIDTH-1:0] old_hs_master_buf_0, old_hs_master_buf_1, old_hs_master_buf_2, old_hs_master_buf_3, old_hs_master_buf_4;
    wire [`NUM_PORT-1:0] kill_0, kill_1, kill_2, kill_3, kill_4;
    reg [`NUM_PORT-1:0] hs_m_buf_empty_out;
    wire [`NUM_PORT-1:0] hs_m_buf_empty_old;
        
    merge_buf merge_buf_0(
    .type_in_0      (data_in_0[`HS_POS]),
    .type_in_1      (data_in_1[`HS_POS]),
    .type_in_2      (data_in_2[`HS_POS]),
    .type_in_3      (data_in_3[`HS_POS]),
    .type_in_4      (data_in_4[`HS_POS]),
    .addr_in_0      (data_in_0[`MEM_ADDR_POS]),
    .addr_in_1      (data_in_1[`MEM_ADDR_POS]),
    .addr_in_2      (data_in_2[`MEM_ADDR_POS]),
    .addr_in_3      (data_in_3[`MEM_ADDR_POS]),
    .addr_in_4      (data_in_4[`MEM_ADDR_POS]),
    .src_in_0       (data_in_0[`SRC_LIST_POS]),
    .src_in_1       (data_in_1[`SRC_LIST_POS]),
    .src_in_2       (data_in_2[`SRC_LIST_POS]),
    .src_in_3       (data_in_3[`SRC_LIST_POS]),
    .src_in_4       (data_in_4[`SRC_LIST_POS]),
    .dst_in_0       (data_in_0[`DST_POS]),
    .dst_in_1       (data_in_1[`DST_POS]),
    .dst_in_2       (data_in_2[`DST_POS]),
    .dst_in_3       (data_in_3[`DST_POS]),
    .dst_in_4       (data_in_4[`DST_POS]),
    .hs_master_buf  (old_hs_master_buf_0),
    .kill           (kill_0),
    .hs_master_buf_out (new_hs_master_buf_0)
    );
    
    merge_buf merge_buf_1(
    .type_in_0      (data_in_0[`HS_POS]),
    .type_in_1      (data_in_1[`HS_POS]),
    .type_in_2      (data_in_2[`HS_POS]),
    .type_in_3      (data_in_3[`HS_POS]),
    .type_in_4      (data_in_4[`HS_POS]),
    .addr_in_0      (data_in_0[`MEM_ADDR_POS]),
    .addr_in_1      (data_in_1[`MEM_ADDR_POS]),
    .addr_in_2      (data_in_2[`MEM_ADDR_POS]),
    .addr_in_3      (data_in_3[`MEM_ADDR_POS]),
    .addr_in_4      (data_in_4[`MEM_ADDR_POS]),
    .src_in_0       (data_in_0[`SRC_LIST_POS]),
    .src_in_1       (data_in_1[`SRC_LIST_POS]),
    .src_in_2       (data_in_2[`SRC_LIST_POS]),
    .src_in_3       (data_in_3[`SRC_LIST_POS]),
    .src_in_4       (data_in_4[`SRC_LIST_POS]),
    .dst_in_0       (data_in_0[`DST_POS]),
    .dst_in_1       (data_in_1[`DST_POS]),
    .dst_in_2       (data_in_2[`DST_POS]),
    .dst_in_3       (data_in_3[`DST_POS]),
    .dst_in_4       (data_in_4[`DST_POS]),
    .hs_master_buf  (old_hs_master_buf_1),
    .kill           (kill_1),
    .hs_master_buf_out (new_hs_master_buf_1)
    );
    
    merge_buf merge_buf_2(
    .type_in_0      (data_in_0[`HS_POS]),
    .type_in_1      (data_in_1[`HS_POS]),
    .type_in_2      (data_in_2[`HS_POS]),
    .type_in_3      (data_in_3[`HS_POS]),
    .type_in_4      (data_in_4[`HS_POS]),
    .addr_in_0      (data_in_0[`MEM_ADDR_POS]),
    .addr_in_1      (data_in_1[`MEM_ADDR_POS]),
    .addr_in_2      (data_in_2[`MEM_ADDR_POS]),
    .addr_in_3      (data_in_3[`MEM_ADDR_POS]),
    .addr_in_4      (data_in_4[`MEM_ADDR_POS]),
    .src_in_0       (data_in_0[`SRC_LIST_POS]),
    .src_in_1       (data_in_1[`SRC_LIST_POS]),
    .src_in_2       (data_in_2[`SRC_LIST_POS]),
    .src_in_3       (data_in_3[`SRC_LIST_POS]),
    .src_in_4       (data_in_4[`SRC_LIST_POS]),
    .dst_in_0       (data_in_0[`DST_POS]),
    .dst_in_1       (data_in_1[`DST_POS]),
    .dst_in_2       (data_in_2[`DST_POS]),
    .dst_in_3       (data_in_3[`DST_POS]),
    .dst_in_4       (data_in_4[`DST_POS]),
    .hs_master_buf  (old_hs_master_buf_2),
    .kill           (kill_2),
    .hs_master_buf_out (new_hs_master_buf_2)
    );
       
    merge_buf merge_buf_3(
    .type_in_0      (data_in_0[`HS_POS]),
    .type_in_1      (data_in_1[`HS_POS]),
    .type_in_2      (data_in_2[`HS_POS]),
    .type_in_3      (data_in_3[`HS_POS]),
    .type_in_4      (data_in_4[`HS_POS]),
    .addr_in_0      (data_in_0[`MEM_ADDR_POS]),
    .addr_in_1      (data_in_1[`MEM_ADDR_POS]),
    .addr_in_2      (data_in_2[`MEM_ADDR_POS]),
    .addr_in_3      (data_in_3[`MEM_ADDR_POS]),
    .addr_in_4      (data_in_4[`MEM_ADDR_POS]),
    .src_in_0       (data_in_0[`SRC_LIST_POS]),
    .src_in_1       (data_in_1[`SRC_LIST_POS]),
    .src_in_2       (data_in_2[`SRC_LIST_POS]),
    .src_in_3       (data_in_3[`SRC_LIST_POS]),
    .src_in_4       (data_in_4[`SRC_LIST_POS]),
    .dst_in_0       (data_in_0[`DST_POS]),
    .dst_in_1       (data_in_1[`DST_POS]),
    .dst_in_2       (data_in_2[`DST_POS]),
    .dst_in_3       (data_in_3[`DST_POS]),
    .dst_in_4       (data_in_4[`DST_POS]),
    .hs_master_buf  (old_hs_master_buf_3),
    .kill           (kill_3),
    .hs_master_buf_out (new_hs_master_buf_3)
    );
               
    merge_buf merge_buf_4(
    .type_in_0      (data_in_0[`HS_POS]),
    .type_in_1      (data_in_1[`HS_POS]),
    .type_in_2      (data_in_2[`HS_POS]),
    .type_in_3      (data_in_3[`HS_POS]),
    .type_in_4      (data_in_4[`HS_POS]),
    .addr_in_0      (data_in_0[`MEM_ADDR_POS]),
    .addr_in_1      (data_in_1[`MEM_ADDR_POS]),
    .addr_in_2      (data_in_2[`MEM_ADDR_POS]),
    .addr_in_3      (data_in_3[`MEM_ADDR_POS]),
    .addr_in_4      (data_in_4[`MEM_ADDR_POS]),
    .src_in_0       (data_in_0[`SRC_LIST_POS]),
    .src_in_1       (data_in_1[`SRC_LIST_POS]),
    .src_in_2       (data_in_2[`SRC_LIST_POS]),
    .src_in_3       (data_in_3[`SRC_LIST_POS]),
    .src_in_4       (data_in_4[`SRC_LIST_POS]),
    .dst_in_0       (data_in_0[`DST_POS]),
    .dst_in_1       (data_in_1[`DST_POS]),
    .dst_in_2       (data_in_2[`DST_POS]),
    .dst_in_3       (data_in_3[`DST_POS]),
    .dst_in_4       (data_in_4[`DST_POS]),
    .hs_master_buf  (old_hs_master_buf_4),
    .kill           (kill_4),
    .hs_master_buf_out (new_hs_master_buf_4)
    );
    
    // if this is a hs flit and not merged with any master hs buffer
    // try to locate an empty master hs_buffer
    wire hs_valid_0, hs_valid_1, hs_valid_2, hs_valid_3, hs_valid_4;
    // store hs_data_in_0
    assign hs_valid_0 = data_in_0 [`HS_POS] && ~kill_0[0] && ~kill_1[0] && ~kill_2[0] && kill_3[0] && kill_4[0];
    assign hs_valid_1 = data_in_1 [`HS_POS] && ~kill_0[1] && ~kill_1[1] && ~kill_2[1] && kill_3[1] && kill_4[1];
    assign hs_valid_2 = data_in_2 [`HS_POS] && ~kill_0[2] && ~kill_1[2] && ~kill_2[2] && kill_3[2] && kill_4[2];
    assign hs_valid_3 = data_in_3 [`HS_POS] && ~kill_0[3] && ~kill_1[3] && ~kill_2[3] && kill_3[3] && kill_4[3];
    assign hs_valid_4 = data_in_4 [`HS_POS] && ~kill_0[4] && ~kill_1[4] && ~kill_2[4] && kill_3[4] && kill_4[4];

    // if there is an empty hs master buf and un-merged hs flit, store in the hs master buf
    assign new_new_hs_master_buf_0 = hs_m_buf_empty_new[0] ? (hs_valid_0 ? new_data_0 : 
            hs_valid_1 ? new_data_1 :
            hs_valid_2 ? new_data_2 :
            hs_valid_3 ? new_data_3 :
            hs_valid_4 ? new_data_4 : 
            new_hs_master_buf_0) : new_hs_master_buf_0;
    assign new_new_hs_master_buf_1 = ~hs_m_buf_empty_new[0] && hs_m_buf_empty_new[1] ? (hs_valid_0 ? new_data_0 : 
            hs_valid_1 ? new_data_1 :
            hs_valid_2 ? new_data_2 :
            hs_valid_3 ? new_data_3 :
            hs_valid_4 ? new_data_4 :
            old_hs_master_buf_1) : old_hs_master_buf_1;
    assign new_new_hs_master_buf_2 = ~hs_m_buf_empty_new[0] && ~hs_m_buf_empty_new[1] && hs_m_buf_empty_new[2] ? (hs_valid_0 ? new_data_0 : 
            hs_valid_1 ? new_data_1 :
            hs_valid_2 ? new_data_2 :
            hs_valid_3 ? new_data_3 :
            hs_valid_4 ? new_data_4 :    
            new_hs_master_buf_2) : new_hs_master_buf_2;
    assign new_new_hs_master_buf_3 = ~hs_m_buf_empty_new[0] && ~hs_m_buf_empty_new[1] && ~hs_m_buf_empty_new[2] && hs_m_buf_empty_new[3] ? (hs_valid_0 ? new_data_0 : 
            hs_valid_1 ? new_data_1 :
            hs_valid_2 ? new_data_2 :
            hs_valid_3 ? new_data_3 :
            hs_valid_4 ? new_data_4 :    
            new_hs_master_buf_3) : new_hs_master_buf_3;
    assign new_new_hs_master_buf_4 = ~hs_m_buf_empty_new[0] && ~hs_m_buf_empty_new[1] && ~hs_m_buf_empty_new[2] && ~hs_m_buf_empty_new[3] && hs_m_buf_empty_new[4] ? (hs_valid_0 ? new_data_0 : 
            hs_valid_1 ? new_data_1 :
            hs_valid_2 ? new_data_2 :
            hs_valid_3 ? new_data_3 :
            hs_valid_4 ? new_data_4 :    
            new_hs_master_buf_4) : new_hs_master_buf_4;

    // update empty flag of hs buf according to the merge and store result.        

    assign hs_m_buf_empty_old[0] = hs_m_buf_empty_new[0] ? (hs_valid_0 ? 1'b0 : 
            hs_valid_1 ? 1'b0 :
            hs_valid_2 ? 1'b0 :
            hs_valid_3 ? 1'b0 :
            hs_valid_4 ? 1'b0 :
            hs_m_buf_empty_new[0]) : hs_m_buf_empty_new[0];           
    assign hs_m_buf_empty_old[1] = ~hs_m_buf_empty_new[0] && hs_m_buf_empty_new[1] ? (hs_valid_0 ? 1'b0 : 
            hs_valid_1 ? 1'b0 :
            hs_valid_2 ? 1'b0 :
            hs_valid_3 ? 1'b0 :
            hs_valid_4 ? 1'b0 :
            hs_m_buf_empty_new[1]) : hs_m_buf_empty_new[1];
    assign hs_m_buf_empty_old[2] = ~hs_m_buf_empty_new[0] && ~hs_m_buf_empty_new[1] && hs_m_buf_empty_new[2] ? (hs_valid_0 ? 1'b0 : 
            hs_valid_1 ? 1'b0 :
            hs_valid_2 ? 1'b0 :
            hs_valid_3 ? 1'b0 :
            hs_valid_4 ? 1'b0 :
            hs_m_buf_empty_new[2]) : hs_m_buf_empty_new[2];
    assign hs_m_buf_empty_old[3] = ~hs_m_buf_empty_new[0] && ~hs_m_buf_empty_new[1] && ~hs_m_buf_empty_new[2] && hs_m_buf_empty_new[3] ? (hs_valid_0 ? 1'b0 : 
            hs_valid_1 ? 1'b0 :
            hs_valid_2 ? 1'b0 :
            hs_valid_3 ? 1'b0 :
            hs_valid_4 ? 1'b0 :
            hs_m_buf_empty_new[3]) : hs_m_buf_empty_new[3];
    assign hs_m_buf_empty_old[4] = ~hs_m_buf_empty_new[0] && ~hs_m_buf_empty_new[1] && ~hs_m_buf_empty_new[2] && ~hs_m_buf_empty_new[3] && hs_m_buf_empty_new[4] ? (hs_valid_0 ? 1'b0 : 
            hs_valid_1 ? 1'b0 :
            hs_valid_2 ? 1'b0 :
            hs_valid_3 ? 1'b0 :
            hs_valid_4 ? 1'b0 :
            hs_m_buf_empty_new[4]) : hs_m_buf_empty_new[4];
   
   
    wire [2:0] num_empty_hs_buf;
    assign num_empty_hs_buf = hs_m_buf_empty_new[0] +  hs_m_buf_empty_new[1] +  hs_m_buf_empty_new[2] +  hs_m_buf_empty_new[3] +  hs_m_buf_empty_new[4];
    wire [`NUM_PORT-1:0] merge_result;
    // determine if input flit is merged
    assign merge_result [0] = hs_valid_0 && (num_empty_hs_buf > 0);
    assign merge_result [1] = hs_valid_1 && (num_empty_hs_buf > hs_valid_0);
    assign merge_result [2] = hs_valid_2 && (num_empty_hs_buf > (hs_valid_0 + hs_valid_1));
    assign merge_result [3] = hs_valid_3 && (num_empty_hs_buf > (hs_valid_0 + hs_valid_1 + hs_valid_2));
    assign merge_result [4] = hs_valid_4 && (num_empty_hs_buf > (hs_valid_0 + hs_valid_1 + hs_valid_2 + hs_valid_3));
   
    wire [`IR_DATA_WIDTH-1:0] new_data_0, new_data_1, new_data_2, new_data_3, new_data_4;
    
    assign new_data_0 = {pre_route_0, data_in_0[`DATA_WIDTH-1:0]};
    assign new_data_1 = {pre_route_1, data_in_1[`DATA_WIDTH-1:0]};
    assign new_data_2 = {pre_route_2, data_in_2[`DATA_WIDTH-1:0]};
    assign new_data_3 = {pre_route_3, data_in_3[`DATA_WIDTH-1:0]};
    assign new_data_4 = {pre_route_4, data_in_4[`DATA_WIDTH-1:0]};
    
    // determine the output after merging            
    assign data_out_0 = merge_result[0] ? `IR_DATA_WIDTH'd0 : new_data_0;
    assign data_out_1 = merge_result[1] ? `IR_DATA_WIDTH'd0 : new_data_1;         
    assign data_out_2 = merge_result[2] ? `IR_DATA_WIDTH'd0 : new_data_2;        
    assign data_out_3 = merge_result[3] ? `IR_DATA_WIDTH'd0 : new_data_3; 
    assign data_out_4 = merge_result[4] ? `IR_DATA_WIDTH'd0 : new_data_4; 
    assign merged = merge_result;
               
    always @ (posedge clk or negedge n_rst) begin
        if (~n_rst) begin
            hs_master_buf_0 <= `IR_DATA_WIDTH'd0;
            hs_master_buf_1 <= `IR_DATA_WIDTH'd0;
            hs_master_buf_2 <= `IR_DATA_WIDTH'd0;
            hs_master_buf_3 <= `IR_DATA_WIDTH'd0;
            hs_master_buf_4 <= `IR_DATA_WIDTH'd0;
            hs_m_buf_empty_out <= 5'b11111;
        end
        else begin
            hs_master_buf_0 <= new_new_hs_master_buf_0;
            hs_master_buf_1 <= new_new_hs_master_buf_1;
            hs_master_buf_2 <= new_new_hs_master_buf_2;
            hs_master_buf_3 <= new_new_hs_master_buf_3;
            hs_master_buf_4 <= new_new_hs_master_buf_4;  
            hs_m_buf_empty_out <= hs_m_buf_empty_old;          
        end
    end
    
    
    assign old_hs_master_buf_0 = hs_master_buf_0;
    assign old_hs_master_buf_1 = hs_master_buf_1;
    assign old_hs_master_buf_2 = hs_master_buf_2;
    assign old_hs_master_buf_3 = hs_master_buf_3;
    assign old_hs_master_buf_4 = hs_master_buf_4;
    
    
endmodule
