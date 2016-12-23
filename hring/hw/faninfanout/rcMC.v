`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/10/2016 02:24:41 PM
// Design Name: 
// Module Name: rcMC
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

module rcMC(
    dstList,
    indir,  // arrived inport
    outdir, // targeted output
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
    
    input [`DST_LIST_WIDTH-1:0] dstList;
    input [`PC_INDEX_WIDTH-2:0] outdir;
    input [`PC_INDEX_WIDTH-1:0] indir;
    input [`NETWORK_SIZE-1:0]   nodeLocalN, nodeLocalE, nodeLocalS, nodeLocalW;
    input [`NETWORK_SIZE-1:0]   nodeStraightN, nodeStraightE, nodeStraightS, nodeStraightW;
    input [`NETWORK_SIZE-1:0]   nodeLeftN, nodeLeftE, nodeLeftS, nodeLeftW;
    input [`NETWORK_SIZE-1:0]   nodeRightN, nodeRightE, nodeRightS, nodeRightW;
    input flitLTBOld, flitRTBOld;
    output flitLTBNew, flitRTBNew;
    output [`NUM_PORT-1:0]      preferPortVector;
    

    assign flitLTBNew = (outdir==(indir+2)%4) ? flitLTBOld : 1'b0;
    assign flitRTBNew = (outdir==(indir+2)%4) ? flitRTBOld : 1'b0;

    assign preferPortVector [0] = (
    (indir==2 && (dstList & nodeStraightN) || (dstList & nodeLeftN && flitLTBOld) || (dstList & nodeRightN && flitRTBOld)) ||
    (indir==3 && flitLTBOld & nodeStraightN) ||
    (indir==1 && flitRTBOld & nodeStraightN) 
 //   ||    (indir==4 && ((dstList & nodeStraightN) || (dstList & nodeLeftN && nodeLTB) || (dstList & nodeRightN && nodeRTB)))  
    ) ? 1'b1 : 1'b0;
    
    assign preferPortVector [1] = (
    (indir==3 && (dstList & nodeStraightE) || (dstList & nodeLeftE && flitLTBOld) || (dstList & nodeRightE && flitRTBOld)) ||
    (indir==0 && flitLTBOld & nodeStraightE) ||
    (indir==2 && flitRTBOld & nodeStraightE) 
//  ||  (indir==4 && ((dstList & nodeStraightE) || (dstList & nodeLeftE && nodeLTB) || (dstList & nodeRightE && nodeRTB)))  
    ) ? 1'b1 : 1'b0;
        
    assign preferPortVector [2] = (
    (indir==0 && (dstList & nodeStraightS) || (dstList & nodeLeftS && flitLTBOld) || (dstList & nodeRightS && flitRTBOld)) ||
    (indir==1 && flitLTBOld & nodeStraightS) ||
    (indir==3 && flitRTBOld & nodeStraightS) 
//    ||  (indir==4 && ((dstList & nodeStraightS) || (dstList & nodeLeftS && nodeLTB) || (dstList & nodeRightS && nodeRTB)))  
    ) ? 1'b1 : 1'b0;        


    assign preferPortVector [3] = (
    (indir==0 && (dstList & nodeStraightW) || (dstList & nodeLeftW && flitLTBOld) || (dstList & nodeRightW && flitRTBOld)) ||
    (indir==1 && flitLTBOld & nodeStraightW) ||
    (indir==2 && flitRTBOld & nodeStraightW) 
 //   ||  (indir==4 && ((dstList & nodeStraightW) || (dstList & nodeLeftW && nodeLTB) || (dstList & nodeRightW && nodeRTB)))  
    ) ? 1'b1 : 1'b0;

    assign preferPortVector [4] = (indir==0 && (dstList & nodeLocalS)) || (indir==1 && (dstList & nodeLocalW)) ||
    (indir == 2 && (dstList & nodeLocalN)) || (indir==3 && (dstList & nodeLocalE)) 
//    || (indir==4 && (dstList & (nodeLocalN | nodeLocalE | nodeLocalS | nodeLocalW)))
    ? 1'b1 : 1'b0; 
    

    
endmodule
