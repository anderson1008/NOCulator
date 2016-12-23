`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/10/2016 05:28:19 PM
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
    indir,
    outdir,
    mc,
    nodeStraightN, nodeStraightE, nodeStraightS, nodeStraightW,   // mask
    nodeLeftN, nodeLeftE, nodeLeftS, nodeLeftW,       // mask
    nodeRightN, nodeRightE, nodeRightS, nodeRightW,      // mask
    nodeLocalN, nodeLocalE, nodeLocalS, nodeLocalW,      // mask
    flitLTBOld,     // LTB of flit
    flitRTBOld,     // RTB of flit
    flitLTBNew,
    flitRTBNew,
    preferPortVector
    );
    
    input [`DST_WIDTH-1:0] dst;
    input [`DST_LIST_WIDTH-1:0] dstList;
    input [`PC_INDEX_WIDTH-1:0] indir;
    input [`PC_INDEX_WIDTH-2:0] outdir;
    input mc;
    input [`NETWORK_SIZE-1:0]   nodeLocalN, nodeLocalE, nodeLocalS, nodeLocalW;
    input [`NETWORK_SIZE-1:0]   nodeStraightN, nodeStraightE, nodeStraightS, nodeStraightW;
    input [`NETWORK_SIZE-1:0]   nodeLeftN, nodeLeftE, nodeLeftS, nodeLeftW;
    input [`NETWORK_SIZE-1:0]   nodeRightN, nodeRightE, nodeRightS, nodeRightW;
    input flitLTBOld, flitRTBOld;
    output flitLTBNew, flitRTBNew;
    output [`NUM_PORT-1:0]      preferPortVector;
    
    wire [`NUM_PORT-1:0] ppv_uc, ppv_mc;
 
    rcMC rcMC(
    .dstList        (dstList),
    .outdir          (outdir),
    .indir           (indir),
    .nodeStraightN  (nodeStraightN), 
    .nodeStraightE  (nodeStraightE), 
    .nodeStraightS  (nodeStraightS), 
    .nodeStraightW  (nodeStraightW),  
    .nodeLeftN      (nodeLeftN), 
    .nodeLeftE      (nodeLeftE), 
    .nodeLeftS      (nodeLeftS), 
    .nodeLeftW      (nodeLeftW),       
    .nodeRightN     (nodeRightN),
    .nodeRightE     (nodeRightE), 
    .nodeRightS     (nodeRightS), 
    .nodeRightW     (nodeRightW),     
    .nodeLocalN     (nodeLocalN),      
    .nodeLocalE     (nodeLocalE),      
    .nodeLocalS     (nodeLocalS),      
    .nodeLocalW     (nodeLocalW),      
    .flitLTBOld     (flitLTBOld),     // LTB of flit
    .flitRTBOld     (flitRTBOld),     // RTB of flit
    .flitLTBNew     (flitLTBNew),
    .flitRTBNew     (flitRTBNew),
    .preferPortVector (ppv_mc)
    );
    
    rcUC rcUC(
    .dst             (dst),
    .outdir          (outdir),
    .preferPortVector (ppv_uc)
    );
    
    assign preferPortVector = mc ? ppv_mc : ppv_uc;
    
endmodule
