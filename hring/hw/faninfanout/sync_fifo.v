`timescale 1ns / 1ps
`include "global.vh"
//-----------------------------------------------------
// Design Name : syn_fifo
// File Name   : syn_fifo.v
// Function    : Synchronous (single clock) FIFO
// Coder       : Deepak Kumar Tala
//-----------------------------------------------------
module syn_fifo (
clk      , // Clock input
rst      , // Active high reset
wr_cs    , // Write chip select
rd_cs    , // Read chipe select
uppv     ,
data_in  , // Data input
rd_en    , // Read enable
wr_en    , // Write Enable
data_out , // Data Output
empty     // FIFO empty
//full       // FIFO full
);    
 
 

// Port Declarations
input clk ;
input rst ;
input wr_cs ;
input rd_cs ;
input rd_en ;
input wr_en ;
input [`NUM_PORT-1:0] uppv;
input [`IR_DATA_WIDTH-1:0] data_in ;
//output full ;
output empty ;
(*keep="true"*) output [`IR_DATA_WIDTH-1:0] data_out ;

//-----------Internal variables-------------------
reg [`ADDR_WIDTH-1:0] wr_pointer;
reg [`ADDR_WIDTH-1:0] rd_pointer;
reg [`ADDR_WIDTH :0] status_cnt;
reg [`IR_DATA_WIDTH-1:0] data_out ;
wire [`IR_DATA_WIDTH-1:0] data_ram ;
wire rd_done = !(|uppv); // there is no unclaimed ppv (which is fed back from SA)

//-----------Variable assignments---------------
//assign full = (status_cnt == (`RAM_DEPTH-1));
assign empty = (status_cnt == 0);

//-----------Code Start---------------------------
always @ (posedge clk)
begin : WRITE_POINTER
  if (~rst) begin
    wr_pointer <= 0;
  end else if (wr_cs && wr_en ) begin
    wr_pointer <= wr_pointer + 1;
  end
end

always @ (posedge clk)
begin : READ_POINTER
  if (~rst) begin
    rd_pointer <= 0;
  end else if (rd_cs && rd_en && rd_done)
    rd_pointer <= rd_pointer + 1;
end

always  @ (posedge clk)
begin : READ_DATA
  if (~rst) begin
    data_out <= `IR_DATA_WIDTH'd0;
//  end else if (rd_cs && rd_en ) begin
  end else if (rd_cs) begin 
    if (rd_en && !rd_done) // update PPV in case flit fails to claim all the requested port.
      data_out <= {data_out [`IR_DATA_WIDTH-1: `PPV_END], uppv, data_out [`PPV_START-1:0]}; 
    else // doesn't have to be selected, so we can always probe the flit at the head
      data_out <= data_ram;
  end
end

always @ (posedge clk)
begin : STATUS_COUNTER
  if (~rst) begin
    status_cnt <= 0;
  // Read but no write.
  end else if ((rd_cs && rd_en) && !(wr_cs && wr_en) 
                && (status_cnt != 0)) begin
    status_cnt <= status_cnt - 1;
  // Write but no read.
  end else if ((wr_cs && wr_en) && !(rd_cs && rd_en) 
               && (status_cnt != `RAM_DEPTH)) begin
    status_cnt <= status_cnt + 1;
  end
end 
   
mem_dp_sr_sw DP_RAM (
.clk       (clk),
.address_0 (wr_pointer) , // address_0 input 
.data_0    (data_in)    , // data_0 bi-directional
.cs_0      (wr_cs)      , // chip select
.we_0      (wr_en)      , // write enable
.address_1 (rd_pointer) , // address_q input
.data_1    (data_ram)  // data_1 bi-directional
);     

endmodule
