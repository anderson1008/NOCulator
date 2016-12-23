`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/11/2016 11:27:03 AM
// Design Name: 
// Module Name: merge_buf
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

/*
   Merged into hs buf
*/

module merge_buf(
    type_in_0,
    type_in_1,
    type_in_2,
    type_in_3,
    type_in_4,
    addr_in_0,
    addr_in_1,
    addr_in_2,
    addr_in_3,
    addr_in_4,
    src_in_0,
    src_in_1,
    src_in_2,
    src_in_3,
    src_in_4,
    dst_in_0,
    dst_in_1,
    dst_in_2,
    dst_in_3,
    dst_in_4,
    hs_master_buf,
    kill,
    hs_master_buf_out
    );
    
    input type_in_0, type_in_1, type_in_2, type_in_3, type_in_4;
    input [`MEM_ADDR_WIDTH-1:0] addr_in_0, addr_in_1, addr_in_2, addr_in_3, addr_in_4;
    input [`DST_WIDTH-1:0] dst_in_0, dst_in_1, dst_in_2, dst_in_3, dst_in_4;
    input [`SRC_LIST_WIDTH-1:0] src_in_0, src_in_1, src_in_2, src_in_3, src_in_4;
    
    input [`IR_DATA_WIDTH-1:0] hs_master_buf;
    output [`NUM_PORT-1:0] kill;
    output [`IR_DATA_WIDTH-1:0] hs_master_buf_out;
    
    assign kill[0] = hs_master_buf[`HS_POS] == type_in_0 && hs_master_buf[`MEM_ADDR_POS] == addr_in_0 && hs_master_buf[`DST_POS] == dst_in_0;
    assign kill[1] = hs_master_buf[`HS_POS] == type_in_1 && hs_master_buf[`MEM_ADDR_POS] == addr_in_1 && hs_master_buf[`DST_POS] == dst_in_1;
    assign kill[2] = hs_master_buf[`HS_POS] == type_in_2 && hs_master_buf[`MEM_ADDR_POS] == addr_in_2 && hs_master_buf[`DST_POS] == dst_in_2;
    assign kill[3] = hs_master_buf[`HS_POS] == type_in_3 && hs_master_buf[`MEM_ADDR_POS] == addr_in_3 && hs_master_buf[`DST_POS] == dst_in_3;
    assign kill[4] = hs_master_buf[`HS_POS] == type_in_4 && hs_master_buf[`MEM_ADDR_POS] == addr_in_4 && hs_master_buf[`DST_POS] == dst_in_4;
    
    
    assign hs_master_buf_out [`SRC_LIST_POS] =  kill[0] ? (hs_master_buf[`SRC_LIST_POS] | src_in_0) :
                                        (kill[1] ? (hs_master_buf[`SRC_LIST_POS] | src_in_1) :
                                        (kill[2] ? (hs_master_buf[`SRC_LIST_POS] | src_in_2) :
                                        (kill[3] ? (hs_master_buf[`SRC_LIST_POS] | src_in_3) :
                                        (kill[4] ? (hs_master_buf[`SRC_LIST_POS] | src_in_4) : hs_master_buf[`SRC_LIST_POS]
    ))));
    
    assign hs_master_buf_out [`IR_DATA_WIDTH-1:`SRC_END] = hs_master_buf [`IR_DATA_WIDTH-1:`SRC_END];
    assign hs_master_buf_out [`LO_PAYLOAD_POS] = hs_master_buf[`LO_PAYLOAD_POS];
   // assign hs_master_buf_out [`MEM_ADDR_POS] = hs_master_buf[`MEM_ADDR_POS];
   // assign hs_master_buf_out [`HS_POS] = hs_master_buf[`HS_POS];
    
     
endmodule
