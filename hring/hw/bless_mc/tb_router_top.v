`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 01/04/2017 01:13:05 AM
// Design Name: 
// Module Name: tb_router_top
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

module tb_router_top;
reg clk, reset;
reg [`DATA_WIDTH-1:0] data_in_0, data_in_1, data_in_2, data_in_3, data_in_4;
wire [`DATA_WIDTH-1:0] data_out_0, data_out_1, data_out_2, data_out_3, data_out_4;

router carpool(
clk,
n_rst,
data_in_0,
data_in_1,
data_in_2,
data_in_3,
data_in_4,
data_out_0,
data_out_1,
data_out_2,
data_out_3,
data_out_4
);

initial begin
   clk = 1'b0; reset = 1'b1;  
   #10;
   reset = 1'b0;
   #10;
   reset = 1'b1;
   
   `ifdef BLESS
   data_in_0 = {8'h0,3'd3,3'd7,6'b0,3'd1,3'd0,3'd3,3'd3,128'hDEAD_BEEF_0000_0001};
   data_in_1 = {8'h1,3'd7,3'd7,6'b0,3'd1,3'd0,3'd0,3'd0,128'hDEAD_BEEF_0000_0003};
   data_in_2 = {8'h2,3'd0,3'd0,6'b0,3'd1,3'd0,3'd3,3'd4,128'hDEAD_BEEF_0000_0007};
   data_in_3 = {8'h3,3'd1,3'd0,6'b0,3'd1,3'd0,3'd3,3'd4,128'hDEAD_BEEF_0000_000F};
   data_in_4 = {8'h4,3'd0,3'd0,6'b0,3'd1,3'd0,3'd3,3'd4,128'hDEAD_BEEF_0000_0011};      
   `endif
   
   `ifdef CARPOOL
   data_in_0 = {8'h0,1'b0,1'b0,3'd3,3'd7,6'b0,4'd1,4'd0,3'd3,3'd3,128'hFFFF_FFFF_FFFF_FFFF_FFFF_FFFF_FFFF_FFFF};
   data_in_1 = {8'h1,1'b0,1'b1,3'd7,3'd7,6'b0,4'd1,4'd0,3'd0,3'd0,64'h800000E3,64'hDEAD_BEEF_0000_0001};
   data_in_2 = {8'h2,1'b1,1'b0,3'd0,3'd0,6'b0,4'd1,4'd0,3'd3,3'd4,64'h8,64'hDEAD_BEEF_0000_0003};
   data_in_3 = {8'h3,1'b0,1'b0,3'd1,3'd0,6'b0,4'd1,4'd0,3'd3,3'd4,128'hFFFF_FFFF_FFFF_FFFF_FFFF_FFFF_FFFF_0001};
   data_in_4 = {8'h4,1'b1,1'b0,3'd0,3'd0,6'b0,4'd1,4'd0,3'd3,3'd4,64'h8000000,64'hDEAD_BEEF_0000_0007};
   `endif //CARPOOL
   
end


always @ *
   #5 clk <= ~clk;
endmodule
