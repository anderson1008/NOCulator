`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 01/03/2017 05:07:13 PM
// Design Name: 
// Module Name: dff
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

module dff_async_reset # (parameter WIDTH=1) (
data  , // Data Input
clk    , // Clock Input
reset , // Reset input 
q         // Q output
);
//-----------Input Ports---------------
input [WIDTH-1:0] data;
input clk, reset ; 

//-----------Output Ports---------------
output [WIDTH-1:0] q;

//------------Internal Variables--------
reg [WIDTH-1:0] q;

//-------------Code Starts Here---------
always @ ( posedge clk or negedge reset)
if (~reset) begin
  q <= 1'b0;
end  else begin
  q <= data;
end

endmodule //End Of Module dff_async_reset
