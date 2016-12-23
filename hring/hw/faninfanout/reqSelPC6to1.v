`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/11/2016 12:12:40 AM
// Design Name: 
// Module Name: reqSelPC6to1
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

module reqSelPC6to1(
  req_in_0,
  req_in_1,
  req_in_2,
  req_in_3,
  req_in_4,
  req_in_5,
  sel,
  req_out
  );
  
  input [`NUM_PORT-1:0] req_in_0, req_in_1, req_in_2, req_in_3, req_in_4, req_in_5;
  input [`VC_INDEX_WIDTH-1:0] sel;
  output [`NUM_PORT-1:0] req_out;
  
  reg [`NUM_PORT-1:0] req_out;
  
  always @ * begin
      case (sel)
          0: req_out = req_in_0;
          1: req_out = req_in_1;
          2: req_out = req_in_2;
          3: req_out = req_in_3;
          4: req_out = req_in_4;
          5: req_out = req_in_5;
          default: req_out = `NUM_PORT'd0;
      endcase
  end
  
endmodule
