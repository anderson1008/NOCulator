`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/11/2016 10:30:42 PM
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


// need to select the resulting PPV (one or more) of the next node based on the allocated ports

module nextRC(
    dst,
    dstList,
    mc,
    indir,
    flitLTBOld,
    flitRTBOld,
    flitLTBNew_0_plus1,
    flitLTBNew_1_plus2,
    flitLTBNew_2_plus3,
    flitLTBNew_3,
    flitRTBNew_0_plus1,
    flitRTBNew_1_plus2,
    flitRTBNew_2_plus3,
    flitRTBNew_3,
    localFlitLTB,
    localFlitRTB,
    
    preferPortVector_0_plus1,
    preferPortVector_1_plus2,
    preferPortVector_2_plus3,
    preferPortVector_3
    );
    
    
    input [`DST_WIDTH-1:0] dst;
    input [`DST_LIST_WIDTH-1:0] dstList;
    input [`PC_INDEX_WIDTH-1:0] indir;
    input mc, flitLTBOld, flitRTBOld; 
    output localFlitLTB, localFlitRTB;
    output     flitLTBNew_0_plus1, flitLTBNew_1_plus2, flitLTBNew_2_plus3, flitLTBNew_3, flitRTBNew_0_plus1, flitRTBNew_1_plus2, flitRTBNew_2_plus3, flitRTBNew_3;
    output [`NUM_PORT-1:0] preferPortVector_0_plus1, preferPortVector_1_plus2, preferPortVector_2_plus3, preferPortVector_3;
    
    wire [`PC_INDEX_WIDTH - 2:0] target_outdir_0_plus1, target_outdir_1_plus2, target_outdir_2_plus3, target_outdir_3;
    // Notation: 
    // the target outdir is either 0/1/2/3 for local flit or +1/+2/+3 for non-local flit
    assign target_outdir_0_plus1 = (indir == 4) ? 2'd0 : ((indir + 2'd1) % 4);
    assign target_outdir_1_plus2 = (indir == 4) ? 2'd1 : ((indir + 2'd2) % 4);
    assign target_outdir_2_plus3 = (indir == 4) ? 2'd2 : ((indir + 2'd3) % 4);
    assign target_outdir_3 = (indir == 4) ? 2'd3 : indir; // TODO: not sure what will result
    
        // assign LTB and RTB for local flit
    // not sure if it is correct
    assign localFlitLTB = (indir==4 && `NODE_LTB) ? 1'b1 : 1'b0;
    assign localFlitRTB = (indir==4 && `NODE_RTB) ? 1'b1 : 1'b0;
    
    rc rc0(
    .dst                (dst),
    .dstList            (dstList),
    .indir              (indir),
    .outdir             (target_outdir_0_plus1),
    .mc                 (mc),
    .nodeStraightN      (`STRAIGHT_N), 
    .nodeStraightE      (`STRAIGHT_E), 
    .nodeStraightS      (`STRAIGHT_S), 
    .nodeStraightW      (`STRAIGHT_W),   
    .nodeLeftN          (`LEFT_N), 
    .nodeLeftE          (`LEFT_E), 
    .nodeLeftS          (`LEFT_S), 
    .nodeLeftW          (`LEFT_W),
    .nodeRightN         (`RIGHT_N), 
    .nodeRightE         (`RIGHT_E),
    .nodeRightS         (`RIGHT_S), 
    .nodeRightW         (`RIGHT_W),    
    .nodeLocalN         (`LOCAL_N),     
    .nodeLocalE         (`LOCAL_E),     
    .nodeLocalS         (`LOCAL_S),     
    .nodeLocalW         (`LOCAL_W),            
    .flitLTBOld         (flitLTBOld),    
    .flitRTBOld         (flitRTBOld),
    .flitLTBNew         (flitLTBNew_0_plus1),
    .flitRTBNew         (flitRTBNew_0_plus1),     
    .preferPortVector   (preferPortVector_0_plus1)
    );    

    rc rc1(
    .dst                (dst),
    .dstList            (dstList),
    .indir              (indir),
    .outdir             (target_outdir_1_plus2),
    .mc                 (mc),
    .nodeStraightN      (`STRAIGHT_N), 
    .nodeStraightE      (`STRAIGHT_E), 
    .nodeStraightS      (`STRAIGHT_S), 
    .nodeStraightW      (`STRAIGHT_W),   
    .nodeLeftN          (`LEFT_N), 
    .nodeLeftE          (`LEFT_E), 
    .nodeLeftS          (`LEFT_S), 
    .nodeLeftW          (`LEFT_W),
    .nodeRightN         (`RIGHT_N), 
    .nodeRightE         (`RIGHT_E),
    .nodeRightS         (`RIGHT_S), 
    .nodeRightW         (`RIGHT_W),    
    .nodeLocalN         (`LOCAL_N),     
    .nodeLocalE         (`LOCAL_E),     
    .nodeLocalS         (`LOCAL_S),     
    .nodeLocalW         (`LOCAL_W),          
    .flitLTBOld         (flitLTBOld),    
    .flitRTBOld         (flitRTBOld),       
    .flitLTBNew         (flitLTBNew_1_plus2),
    .flitRTBNew         (flitRTBNew_1_plus2),   
    .preferPortVector   (preferPortVector_1_plus2)
    );   
    
     rc rc2(
    .dst                (dst),
    .dstList            (dstList),
    .indir              (indir),
    .outdir              (target_outdir_2_plus3),
    .mc                 (mc),
    .nodeStraightN      (`STRAIGHT_N), 
    .nodeStraightE      (`STRAIGHT_E), 
    .nodeStraightS      (`STRAIGHT_S), 
    .nodeStraightW      (`STRAIGHT_W),   
    .nodeLeftN          (`LEFT_N), 
    .nodeLeftE          (`LEFT_E), 
    .nodeLeftS          (`LEFT_S), 
    .nodeLeftW          (`LEFT_W),
    .nodeRightN         (`RIGHT_N), 
    .nodeRightE         (`RIGHT_E),
    .nodeRightS         (`RIGHT_S), 
    .nodeRightW         (`RIGHT_W),    
    .nodeLocalN         (`LOCAL_N),     
    .nodeLocalE         (`LOCAL_E),     
    .nodeLocalS         (`LOCAL_S),     
    .nodeLocalW         (`LOCAL_W),       
    .flitLTBOld         (flitLTBOld),    
    .flitRTBOld         (flitRTBOld),  
    .flitLTBNew         (flitLTBNew_2_plus3),
    .flitRTBNew         (flitRTBNew_2_plus3),     
    .preferPortVector   (preferPortVector_2_plus3)
    );    
 
    rc rc3(
   .dst                (dst),
   .dstList            (dstList),
   .indir              (indir),
   .outdir              (target_outdir_3),
   .mc                 (mc),
   .nodeStraightN      (`STRAIGHT_N), 
   .nodeStraightE      (`STRAIGHT_E), 
   .nodeStraightS      (`STRAIGHT_S), 
   .nodeStraightW      (`STRAIGHT_W),   
   .nodeLeftN          (`LEFT_N), 
   .nodeLeftE          (`LEFT_E), 
   .nodeLeftS          (`LEFT_S), 
   .nodeLeftW          (`LEFT_W),
   .nodeRightN         (`RIGHT_N), 
   .nodeRightE         (`RIGHT_E),
   .nodeRightS         (`RIGHT_S), 
   .nodeRightW         (`RIGHT_W),    
   .nodeLocalN         (`LOCAL_N),     
   .nodeLocalE         (`LOCAL_E),     
   .nodeLocalS         (`LOCAL_S),     
   .nodeLocalW         (`LOCAL_W),          
   .flitLTBOld         (flitLTBOld),    
   .flitRTBOld         (flitRTBOld), 
   .flitLTBNew         (flitLTBNew_3),
   .flitRTBNew         (flitRTBNew_3),       
   .preferPortVector   (preferPortVector_3)
   ); 
       
endmodule
