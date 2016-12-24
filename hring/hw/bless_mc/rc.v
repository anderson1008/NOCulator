`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/18/2016 10:31:54 PM
// Design Name: 
// Module Name: rc
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

module rc(
    dst,
    dstList,
    outdir,
    mc,
    preferPortVector
    );
    
    input [`DST_WIDTH-1:0] dst;
    input [`DST_LIST_WIDTH-1:0] dstList;
    input [`PC_INDEX_WIDTH-2:0] outdir;
    input mc;
    output [`NUM_PORT-1:0] preferPortVector;
    
    wire [`NUM_PORT-1:0] ppv_uc, ppv_mc;

    rcMC rcMC(
    .dstList (dstList),
    .outdir  (outdir),       // target output
    .preferPortVector (ppv_mc)
    );
    
    rcUC rcUC(
    .dst     (dst),
    .outdir  (outdir),
    .preferPortVector (ppv_uc)
    );
    
    assign preferPortVector = mc ? ppv_mc : ppv_uc;
    
endmodule
