// Permutation Network

`include "global.vh"

module permutationNetwork ( 
    time0, 
    time1, 
    time2, 
    time3,
    ppv0, 
    ppv1, 
    ppv2, 
    ppv3, 
    rank0_dir, 
    rank1_dir, 
    rank2_dir, 
    rank3_dir,
    rank0_ppv,
    rank1_ppv,
    rank2_ppv,
    rank3_ppv,
    eject,
    v_mc,
    sorted_eject,
    sorted_mc
    );

output [1:0] rank0_dir, rank1_dir, rank2_dir, rank3_dir;
input [`TIME_WIDTH-1:0] time0, time1, time2, time3;
input [`NUM_PORT-2:0] ppv0, ppv1, ppv2, ppv3;
output [`NUM_PORT-2:0] rank0_ppv, rank1_ppv, rank2_ppv, rank3_ppv;
input [3:0] eject, v_mc;
output [3:0] sorted_eject, sorted_mc;

wire  [`PERM_WIDTH-1:0] swapFlit [1:0];
wire  [`PERM_WIDTH-1:0] straightFlit [1:0];
wire  swap [0:3];
wire  [`PERM_WIDTH-1:0] w_out [3:0];

// (1: downward sort; 0: upward sort)
arbiterPN arbiterPN00 (time0, time1, 1'b0, swap[0]);
arbiterPN arbiterPN01 (time2, time3, 1'b1, swap[1]);
arbiterPN arbiterPN10 (straightFlit[0][`PERM_WIDTH-2-`TIME_WIDTH+:`TIME_WIDTH], swapFlit[1][`PERM_WIDTH-2-`TIME_WIDTH+:`TIME_WIDTH], 1'b0, swap[2]);
arbiterPN arbiterPN11 (swapFlit[0][`PERM_WIDTH-2-`TIME_WIDTH+:`TIME_WIDTH], straightFlit[1][`PERM_WIDTH-2-`TIME_WIDTH+:`TIME_WIDTH], 1'b0, swap[3]);

permuterBlock # (`PERM_WIDTH) PN00({2'd0,time0,v_mc[0],eject[0],ppv0}, {2'd1,time1,v_mc[1],eject[1],ppv1}, swap[0], straightFlit[0], swapFlit[0]);
permuterBlock # (`PERM_WIDTH) PN01({2'd2,time2,v_mc[2],eject[2],ppv2}, {2'd3,time3,v_mc[2],eject[3],ppv3}, swap[1], swapFlit[1], straightFlit[1]);
permuterBlock # (`PERM_WIDTH) PN10(straightFlit[0], swapFlit[1], swap[2], w_out[0], w_out[1]);
permuterBlock # (`PERM_WIDTH) PN11 (swapFlit[0], straightFlit[1], swap[3], w_out[2], w_out[3]);

assign rank0_dir = w_out[0][`PERM_WIDTH-1:`PERM_WIDTH-2];
assign rank1_dir = w_out[1][`PERM_WIDTH-1:`PERM_WIDTH-2];
assign rank2_dir = w_out[2][`PERM_WIDTH-1:`PERM_WIDTH-2];
assign rank3_dir = w_out[3][`PERM_WIDTH-1:`PERM_WIDTH-2];
assign rank0_ppv = w_out[0][`NUM_PORT-2:0];
assign rank1_ppv = w_out[1][`NUM_PORT-2:0];
assign rank2_ppv = w_out[2][`NUM_PORT-2:0];
assign rank3_ppv = w_out[3][`NUM_PORT-2:0];
assign sorted_eject = {w_out[3][`NUM_PORT-1],w_out[2][`NUM_PORT-1],w_out[1][`NUM_PORT-1],w_out[0][`NUM_PORT-1]};
assign sorted_mc = {w_out[3][`NUM_PORT],w_out[2][`NUM_PORT],w_out[1][`NUM_PORT],w_out[0][`NUM_PORT]};

endmodule