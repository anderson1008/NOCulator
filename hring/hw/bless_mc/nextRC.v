`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/18/2016 10:23:41 PM
// Design Name: 
// Module Name: nextRC
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

module nextRC(
    dst,
    dstList,
    mc,
    indir,
    nextPPV   
    );
    
    input mc;
    input [`DST_WIDTH-1:0] dst;
    input [`DST_LIST_WIDTH-1:0] dstList;
    input [`PC_INDEX_WIDTH-1:0] indir;
    output [`NUM_PORT * 4 -1:0] nextPPV;

    wire [`PC_INDEX_WIDTH - 2:0] target_outdir_0_plus1, target_outdir_1_plus2, target_outdir_2_plus3, target_outdir_3;
    // Notation: 
    // the target outdir is either 0/1/2/3 for local flit or +1/+2/+3 for non-local flit
    assign target_outdir_0_plus1 = (indir == 4) ? 2'd0 : ((indir + 2'd1) % 4);
    assign target_outdir_1_plus2 = (indir == 4) ? 2'd1 : ((indir + 2'd2) % 4);
    assign target_outdir_2_plus3 = (indir == 4) ? 2'd2 : ((indir + 2'd3) % 4);
    assign target_outdir_3 = (indir == 4) ? 2'd3 : indir; // TODO: not sure what will result
    
    wire [`NUM_PORT-1:0] w_ppv_0, w_ppv_1, w_ppv_2, w_ppv_3;
    rc rc0(
    .dst                    (dst), 
    .dstList                (dstList),
    .outdir                 (target_outdir_0_plus1),
    .mc                     (mc),
    .preferPortVector       (w_ppv_0)
    );
    
    rc rc1(
    .dst                    (dst), 
    .dstList                (dstList),
    .outdir                 (target_outdir_1_plus2),
    .mc                     (mc),
    .preferPortVector       (w_ppv_1)
    );
    
    rc rc2(
    .dst                    (dst), 
    .dstList                (dstList),
    .outdir                 (target_outdir_2_plus3),
    .mc                     (mc),
    .preferPortVector       (w_ppv_2)
    );
    
    rc rc3(
    .dst                    (dst), 
    .dstList                (dstList),
    .outdir                 (target_outdir_3),
    .mc                     (mc),
    .preferPortVector       (w_ppv_3)
    );
           
    assign nextPPV = (indir == 4) ? {w_ppv_0, w_ppv_1, w_ppv_2, w_ppv_3} :
                     (indir == 3) ? {w_ppv_0, w_ppv_1, w_ppv_2, `NUM_PORT'h0} :
                     (indir == 2) ? {w_ppv_0, w_ppv_1, `NUM_PORT'h0, w_ppv_3} :
                     (indir == 1) ? {w_ppv_0, `NUM_PORT'h0, w_ppv_2, w_ppv_3} :
                     {`NUM_PORT'h0, w_ppv_1, w_ppv_2, w_ppv_3};
           
endmodule
