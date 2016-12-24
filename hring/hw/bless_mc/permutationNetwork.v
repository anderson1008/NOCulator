// Permutation Network

`include "global.vh"

module permutationNetwork ( 
    time0, 
    time1, 
    time2, 
    time3, 
    dout0, 
    dout1, 
    dout2, 
    dout3
    );

output [1:0] dout0, dout1, dout2, dout3; // dout0 has highest priority (oldest); dout3 has lowest priority (lastest)
input [`TIME_WIDTH-1:0] time0, time1, time2, time3;

wire  [`PERM_WIDTH-1:0] swapFlit [1:0];
wire  [`PERM_WIDTH-1:0] straightFlit [1:0];
wire  swap [0:3];
wire  [`PERM_WIDTH-1:0] w_out [3:0];

// (1: downward sort; 0: upward sort)
arbiterPN arbiterPN00 (time3, time2, 1'b0, swap[0]);
arbiterPN arbiterPN01 (time1, time0, 1'b0, swap[1]);
arbiterPN arbiterPN10 (straightFlit[0][`TIME_WIDTH-1:0], swapFlit[1][`TIME_WIDTH-1:0], 1'b0, swap[2]);
arbiterPN arbiterPN11 (swapFlit[0][`TIME_WIDTH-1:0], straightFlit[1][`TIME_WIDTH-1:0], 1'b0, swap[3]);

permuterBlock # (`PERM_WIDTH) PN00({2'd3,time3}, {2'd2,time2}, swap[0], straightFlit[0], swapFlit[0]);
permuterBlock # (`PERM_WIDTH) PN01({2'd1,time1}, {2'd0,time0}, swap[1], swapFlit[1], straightFlit[1]);
permuterBlock # (`PERM_WIDTH) PN10(straightFlit[0], swapFlit[1], swap[2], w_out[0], w_out[1]);
permuterBlock # (`PERM_WIDTH) PN11 (swapFlit[0], straightFlit[1], swap[3], w_out[2], w_out[3]);

assign dout0 = w_out[0][`PERM_WIDTH-1:`PERM_WIDTH-2];
assign dout1 = w_out[1][`PERM_WIDTH-1:`PERM_WIDTH-2];
assign dout2 = w_out[2][`PERM_WIDTH-1:`PERM_WIDTH-2];
assign dout3 = w_out[3][`PERM_WIDTH-1:`PERM_WIDTH-2];

endmodule