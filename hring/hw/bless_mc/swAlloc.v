`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/19/2016 12:22:57 AM
// Design Name: 
// Module Name: swAlloc
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


// this is a naive port allocator

module swAlloc(
    mc_0,
    mc_1,
    mc_2,
    mc_3,
    numFlit_in,
    ppv_0,
    ppv_1,
    ppv_2,
    ppv_3,
    allocPV_0,
    allocPV_1,
    allocPV_2,
    allocPV_3,
    unallocPV_0,
    unallocPV_1,
    unallocPV_2,
    unallocPV_3
    );
    
    input mc_0, mc_1, mc_2, mc_3;
    input [`PC_INDEX_WIDTH-1:0] numFlit_in;
    input [`NUM_PORT-2:0] ppv_0, ppv_1, ppv_2, ppv_3;
    output [`NUM_PORT-2:0] allocPV_0, allocPV_1, allocPV_2, allocPV_3; 
    output [`NUM_PORT-2:0] unallocPV_0, unallocPV_1, unallocPV_2, unallocPV_3; 

    wire [`NUM_PORT-2:0] w_availPort [1:3];
    wire [`PC_INDEX_WIDTH-1:0] w_numFlit [1:3];

    
    seqPortAlloc rank0PortAlloc(
    .mc                     (mc_0),
    .numFlit_in             (numFlit_in),
    .availPortVector_in     (4'b1111),
    .ppv                    (ppv_0),
    .allocatedPortVector    (allocPV_0),
    .unallocPortVector      (unallocPV_0),
    .availPortVector_out    (w_availPort[1]),
    .numFlit_out            (w_numFlit[1])
    );
  
                         
    seqPortAlloc rank1PortAlloc(
    .mc                     (mc_1),
    .numFlit_in             (w_numFlit[1]),
    .availPortVector_in     (w_availPort[1]),
    .ppv                    (ppv_1),
    .allocatedPortVector    (allocPV_1),
    .unallocPortVector      (unallocPV_1),
    .availPortVector_out    (w_availPort[2]),
    .numFlit_out            (w_numFlit[2])
    );    
    

    seqPortAlloc rank02ortAlloc(
    .mc                     (mc_2),
    .numFlit_in             (w_numFlit[2]),
    .availPortVector_in     (w_availPort[2]),
    .ppv                    (ppv_2),
    .allocatedPortVector    (allocPV_2),
    .unallocPortVector      (unallocPV_2),
    .availPortVector_out    (w_availPort[3]),
    .numFlit_out            (w_numFlit[3])
    );

    seqPortAlloc rank3PortAlloc(
    .mc                     (mc_3),
    .numFlit_in             (w_numFlit[3]),
    .availPortVector_in     (w_availPort[3]),
    .ppv                    (ppv_3),
    .allocatedPortVector    (allocPV_3),
    .unallocPortVector      (unallocPV_3),
    .availPortVector_out    (),
    .numFlit_out            ()
    );

        
endmodule
