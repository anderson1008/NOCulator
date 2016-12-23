`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/10/2016 10:02:04 PM
// Design Name: 
// Module Name: bw
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

module pc(
    clk       , 
    rst       ,
    vc        ,
    merged    ,
    sel_vc_out,
    pc_en     ,
    hs_buf_empty  ,
    bypass    ,
    uppv      , //unallocated ppv
    data_in   ,
    data_out  ,
    master_hs_buffer,
    vc_data_out_0, vc_data_out_1, vc_data_out_2, vc_data_out_3,
    hs_buf_empty_new
    );
    
    input clk, rst, bypass, pc_en, merged, hs_buf_empty;
    input [`VC_INDEX_WIDTH-1:0] sel_vc_out;
    input [`VC_INDEX_WIDTH-2:0] vc;
    input [`IR_DATA_WIDTH-1:0] data_in, master_hs_buffer;
    input [`NUM_PORT-1:0] uppv;
    output [`IR_DATA_WIDTH-1:0] data_out;
    output [`IR_DATA_WIDTH-1:0] vc_data_out_0, vc_data_out_1, vc_data_out_2, vc_data_out_3;
    output hs_buf_empty_new;

    wire [`NUM_VC-1:0] vc_wr_enable, vc_rd_enable;
    reg  [`IR_DATA_WIDTH-1:0] r_data_in; 
    wire valid = (data_in[`PPV_POS]!=5'b0) && ~bypass && ~merged;
    wire vc_empty_0, vc_empty_1, vc_empty_2, vc_empty_3;

    assign vc_wr_enable[3] = vc==2'd3 && valid;
    assign vc_wr_enable[2] = vc==2'd2 && valid;
    assign vc_wr_enable[1] = vc==2'd1 && valid;
    assign vc_wr_enable[0] = vc==2'd0 && valid;
  
    assign vc_rd_enable[5] = pc_en && (sel_vc_out == 3'd5);
    assign vc_rd_enable[4] = pc_en && (sel_vc_out == 3'd4) && ~hs_buf_empty;
    assign vc_rd_enable[3] = pc_en && (sel_vc_out == 3'd3) && ~vc_empty_3;
    assign vc_rd_enable[2] = pc_en && (sel_vc_out == 3'd2) && ~vc_empty_2;
    assign vc_rd_enable[1] = pc_en && (sel_vc_out == 3'd1) && ~vc_empty_1;
    assign vc_rd_enable[0] = pc_en && (sel_vc_out == 3'd0) && ~vc_empty_0;                 
    
    
    
    syn_fifo vc_0(
    .clk      (clk), // Clock input
    .rst      (rst), // Active high reset
    .wr_cs    (valid), // Write chip select
    .rd_cs    (1'b1), // Read chipe select
    .uppv     (uppv),
    .data_in  (data_in), // Data input
    .rd_en    (vc_rd_enable[0]), // Read enable
    .wr_en    (vc_wr_enable[0]), // Write Enable
    .data_out (vc_data_out_0), // Data Output
    .empty    (vc_empty_0) // FIFO empty
    );
    
    syn_fifo vc_1(
    .clk      (clk), // Clock input
    .rst      (rst), // Active high reset
    .wr_cs    (valid), // Write chip select
    .rd_cs    (1'b1), // Read chipe select
    .uppv     (uppv),
    .data_in  (data_in), // Data input
    .rd_en    (vc_rd_enable[1]), // Read enable
    .wr_en    (vc_wr_enable[1]), // Write Enable
    .data_out (vc_data_out_1), // Data Output
    .empty    (vc_empty_1) // FIFO empty
    );  
    
    syn_fifo vc_2(
    .clk      (clk), // Clock input
    .rst      (rst), // Active high reset
    .wr_cs    (valid), // Write chip select
    .rd_cs    (1'b1), // Read chipe select
    .uppv     (uppv),
    .data_in  (data_in), // Data input
    .rd_en    (vc_rd_enable[2]), // Read enable
    .wr_en    (vc_wr_enable[2]), // Write Enable
    .data_out (vc_data_out_2), // Data Output
    .empty    (vc_empty_2) // FIFO empty
    ); 
    
    syn_fifo vc_3(
    .clk      (clk), // Clock input
    .rst      (rst), // Active high reset
    .wr_cs    (valid), // Write chip select
    .rd_cs    (1'b1), // Read chipe select
    .uppv     (uppv),
    .data_in  (data_in), // Data input
    .rd_en    (vc_rd_enable[3]), // Read enable
    .wr_en    (vc_wr_enable[3]), // Write Enable
    .data_out (vc_data_out_3), // Data Output
    .empty    (vc_empty_3) // FIFO empty
    );
    
    assign hs_buf_empty_new = vc_rd_enable[4] ? 1'b1 : hs_buf_empty;
  
    always @ (posedge clk) begin
        r_data_in <= data_in; // just store the incoming flit in the buffer, for bypassing
    end
       

    assign data_out = vc_rd_enable[5] ? r_data_in :
                     (vc_rd_enable[4] ? master_hs_buffer : 
                     (vc_rd_enable[3] ? vc_data_out_3 : 
                     (vc_rd_enable[2] ? vc_data_out_2 :
                     (vc_rd_enable[1] ? vc_data_out_1 :
                     (vc_rd_enable[0] ? vc_data_out_0 : `IR_DATA_WIDTH'd0
                     )))));
     
endmodule
