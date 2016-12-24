`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/20/2016 10:52:34 PM
// Design Name: 
// Module Name: selNextPPV
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


module selNextPPV(
    pre_nppv,
    outdir,
    nppv
    );
    
    input [`NUM_PORT*4-1:0] pre_nppv;
    input [1:0] outdir;
    output [`NUM_PORT-1:0] nppv;
    wire [`NUM_PORT-1:0] w_nppv [0:3];

    genvar i;
    generate 
        for (i=0; i<4; i=i+1) begin : split_nppv
            assign w_nppv[i] = pre_nppv[i*`NUM_PORT +: `NUM_PORT];
        end    
    endgenerate

    assign nppv = outdir == 0 ? w_nppv[0] :
                  outdir == 1 ? w_nppv[1] :
                  outdir == 2 ? w_nppv[2] :
                  outdir == 3 ? w_nppv[3] :
                  `NUM_PORT'h0;
    
endmodule
