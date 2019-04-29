// Permuter Block

`include "global.vh"

module permuterBlock (
    inFlit0, 
    inFlit1, 
    swap, 
    outFlit0, 
    outFlit1
    );

parameter PERM_WIDTH = 8;

input                            swap;
input   [PERM_WIDTH-1:0] 	inFlit0,inFlit1;
output 	[PERM_WIDTH-1:0]	outFlit0, outFlit1;

wire	[PERM_WIDTH-1:0] swapFlit [1:0];
wire	[PERM_WIDTH-1:0] straightFlit [1:0];

genvar i;
generate
   for (i=0; i<PERM_WIDTH; i=i+1) begin : PermutNet
      demux1to2 demux0(inFlit0[i], swap, straightFlit[0][i], swapFlit[0][i]);
      demux1to2 demux1(inFlit1[i], swap, straightFlit[1][i], swapFlit[1][i]);
      mux2to1 mux0(straightFlit[0][i], swapFlit[1][i], swap, outFlit0[i]);
      mux2to1 mux1(straightFlit[1][i], swapFlit[0][i], swap, outFlit1[i]);
   end
endgenerate
	
endmodule
