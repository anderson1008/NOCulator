`timescale 1ns / 1ps
`include "global.vh"

//-----------------------------------------------------
// Design Name : ram_dp_sr_sw
// File Name   : ram_dp_sr_sw.v
// Function    : Synchronous read write RAM
// Coder       : Deepak Kumar Tala
//-----------------------------------------------------
module mem_dp_sr_sw (
clk       , // Clock Input
address_0 , // address_0 Input
data_0    , // data_0 bi-directional
cs_0      , // Chip Select
we_0      , // Write Enable/Read Enable
address_1 , // address_1 Input
data_1     // data_1 bi-directional
); 

//--------------Input Ports----------------------- 
input [`ADDR_WIDTH-1:0] address_0 ;
input cs_0 ;
input we_0 ;
input [`ADDR_WIDTH-1:0] address_1 ;
input clk;

//--------------Inout Ports----------------------- 
input [`IR_DATA_WIDTH-1:0] data_0 ; 
output [`IR_DATA_WIDTH-1:0] data_1 ;

//--------------Internal variables---------------- 
//wire [`IR_DATA_WIDTH-1:0] data_0_out ; 
//wire [`IR_DATA_WIDTH-1:0] data_1_out ;
reg [`IR_DATA_WIDTH-1:0] mem [0:`RAM_DEPTH-1];

//--------------Code Starts Here------------------ 
// Memory Write Block 
// Write Operation : When we_0 = 1, cs_0 = 1
always @ (posedge clk)
begin : MEM_WRITE
  if ( cs_0 && we_0 ) 
     mem[address_0] <= data_0;
end

assign data_1 =  mem[address_1] ; 

endmodule // End of Module ram_dp_sr_sw
