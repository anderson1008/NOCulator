`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/09/2016 12:36:32 AM
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

// TODO: Need to check the downstream credit and alter VC field, which can be done in read stage of BW

`include "global.vh"

module swAlloc(
    req_in0_vc0,
    time_in0_vc0,
    req_in0_vc1,
    time_in0_vc1,    
    req_in0_vc2,
    time_in0_vc2,    
    req_in0_vc3,
    time_in0_vc3,
    req_in1_vc0,
    time_in1_vc0,
    req_in1_vc1,
    time_in1_vc1,
    req_in1_vc2,
    time_in1_vc2,
    req_in1_vc3,
    time_in1_vc3,
    req_in2_vc0,
    time_in2_vc0,
    req_in2_vc1,
    time_in2_vc1,
    req_in2_vc2,
    time_in2_vc2,
    req_in2_vc3,
    time_in2_vc3,
    req_in3_vc0,
    time_in3_vc0,
    req_in3_vc1,
    time_in3_vc1,
    req_in3_vc2,
    time_in3_vc2,
    req_in3_vc3,
    time_in3_vc3,
    req_in4_vc0,
    time_in4_vc0,
    req_in4_vc1,
    time_in4_vc1,            
    req_in4_vc2,
    time_in4_vc2,
    req_in4_vc3,
    time_in4_vc3,
    vc0_credit_out_0,
    vc1_credit_out_0,
    vc2_credit_out_0,
    vc3_credit_out_0,
    vc0_credit_out_1,
    vc1_credit_out_1,
    vc2_credit_out_1,
    vc3_credit_out_1,
    vc0_credit_out_2,
    vc1_credit_out_2,
    vc2_credit_out_2,
    vc3_credit_out_2,
    vc0_credit_out_3,
    vc1_credit_out_3,
    vc2_credit_out_3,
    vc3_credit_out_3,
    vc0_credit_out_4,
    vc1_credit_out_4,
    vc2_credit_out_4,
    vc3_credit_out_4,
    vc_new_out_0,
    vc_new_out_1,
    vc_new_out_2,
    vc_new_out_3,
    vc_new_out_4,                      
    sel_pc_out_0,
    sel_vc_out_0,
    sel_pc_out_1,
    sel_vc_out_1,
    sel_pc_out_2,
    sel_vc_out_2,
    sel_pc_out_3,
    sel_vc_out_3,
    sel_pc_out_4,
    sel_vc_out_4                                
    );
    
    input [`NUM_PORT-1:0] req_in0_vc0, req_in0_vc1, req_in0_vc2, req_in0_vc3, req_in1_vc0, req_in1_vc1, req_in1_vc2, req_in1_vc3, req_in2_vc0, req_in2_vc1, req_in2_vc2, req_in2_vc3, 
                          req_in3_vc0, req_in3_vc1, req_in3_vc2, req_in3_vc3, req_in4_vc0, req_in4_vc1, req_in4_vc2, req_in4_vc3;
    input [`TIME_WIDTH-1:0] time_in0_vc0, time_in0_vc1, time_in0_vc2, time_in0_vc3, time_in1_vc0, time_in1_vc1, time_in1_vc2, time_in1_vc3, time_in2_vc0, time_in2_vc1, time_in2_vc2, time_in2_vc3, 
                          time_in3_vc0, time_in3_vc1, time_in3_vc2, time_in3_vc3, time_in4_vc0, time_in4_vc1, time_in4_vc2, time_in4_vc3;
    input [`ADDR_WIDTH-1:0] vc0_credit_out_0, vc1_credit_out_0, vc2_credit_out_0, vc3_credit_out_0, vc0_credit_out_1, vc1_credit_out_1, vc2_credit_out_1, vc3_credit_out_1, vc0_credit_out_2,
                          vc1_credit_out_2, vc2_credit_out_2, vc3_credit_out_2, vc0_credit_out_3, vc1_credit_out_3, vc2_credit_out_3, vc3_credit_out_3, vc0_credit_out_4, vc1_credit_out_4,
                          vc2_credit_out_4, vc3_credit_out_4;
    output [`VC_INDEX_WIDTH-1:0] vc_new_out_0, vc_new_out_1, vc_new_out_2, vc_new_out_3, vc_new_out_4;
    output [`PC_INDEX_WIDTH-1:0]  sel_pc_out_0, sel_pc_out_1, sel_pc_out_2, sel_pc_out_3, sel_pc_out_4;
    output [`VC_INDEX_WIDTH-1:0]  sel_vc_out_0, sel_vc_out_1, sel_vc_out_2, sel_vc_out_3, sel_vc_out_4; // select a VC from each PC
    
    wire [`TIME_WIDTH-1:0] w_time_0, w_time_1, w_time_2, w_time_3, w_time_4;
    wire [`NUM_PORT-1:0] w_req_pc0, w_req_pc1, w_req_pc2, w_req_pc3, w_req_pc4; 
    
    // VC Allocation 
    vcArb4to1 pc0_vcArb(
    .time_in_0     (time_in0_vc0),
    .time_in_1     (time_in0_vc1),
    .time_in_2     (time_in0_vc2),
    .time_in_3     (time_in0_vc3),
    .winner_time_out (w_time_0),
    .winner_vc_out (sel_vc_out_0) 
    );

    vcArb4to1 pc1_vcArb(
    .time_in_0     (time_in1_vc0),
    .time_in_1     (time_in1_vc1),
    .time_in_2     (time_in1_vc2),
    .time_in_3     (time_in1_vc3),
    .winner_time_out (w_time_1),
    .winner_vc_out (sel_vc_out_1) 
    );
    
    vcArb4to1 pc2_vcArb(
    .time_in_0     (time_in2_vc0),
    .time_in_1     (time_in2_vc1),
    .time_in_2     (time_in2_vc2),
    .time_in_3     (time_in2_vc3),
    .winner_time_out (w_time_2),
    .winner_vc_out (sel_vc_out_2) 
    );
    
    vcArb4to1 pc3_vcArb(
    .time_in_0     (time_in3_vc0),
    .time_in_1     (time_in3_vc1),
    .time_in_2     (time_in3_vc2),
    .time_in_3     (time_in3_vc3),
    .winner_time_out (w_time_3),
    .winner_vc_out (sel_vc_out_3) 
    );
        
    vcArb4to1 pc4_vcArb(
    .time_in_0     (time_in4_vc0),
    .time_in_1     (time_in4_vc1),
    .time_in_2     (time_in4_vc2),
    .time_in_3     (time_in4_vc3),
    .winner_time_out (w_time_4),
    .winner_vc_out (sel_vc_out_4) 
    );
    
    // Select a request among all VCs in a PC
    
    reqSelPC4to1 selPC0(
    .req_in_0       (req_in0_vc0),
    .req_in_1       (req_in0_vc1),
    .req_in_2       (req_in0_vc2),
    .req_in_3       (req_in0_vc3),
    .sel            (sel_vc_out_0),
    .req_out        (w_req_pc0)
    );

    reqSelPC4to1 selPC1(
    .req_in_0       (req_in1_vc0),
    .req_in_1       (req_in1_vc1),
    .req_in_2       (req_in1_vc2),
    .req_in_3       (req_in1_vc3),
    .sel            (sel_vc_out_1),
    .req_out        (w_req_pc1)
    );
    
    reqSelPC4to1 selPC2(
    .req_in_0       (req_in2_vc0),
    .req_in_1       (req_in2_vc1),
    .req_in_2       (req_in2_vc2),
    .req_in_3       (req_in2_vc3),
    .sel            (sel_vc_out_2),
    .req_out        (w_req_pc2)
    );
    
    reqSelPC4to1 selPC3(
    .req_in_0       (req_in3_vc0),
    .req_in_1       (req_in3_vc1),
    .req_in_2       (req_in3_vc2),
    .req_in_3       (req_in3_vc3),
    .sel            (sel_vc_out_3),
    .req_out        (w_req_pc3)
    );
    
    reqSelPC4to1 selPC4(
    .req_in_0       (req_in4_vc0),
    .req_in_1       (req_in4_vc1),
    .req_in_2       (req_in4_vc2),
    .req_in_3       (req_in4_vc3),
    .sel            (sel_vc_out_4),
    .req_out        (w_req_pc4)
    );    
            
    // Switch Allocation
    // Stall if downstream router running out of credit
    swArb out0Arb( 
    .time_in_0       (w_time_0),
    .time_in_1       (w_time_1),
    .time_in_2       (w_time_2),
    .time_in_3       (w_time_3),
    .time_in_4       (w_time_4),
    .req_in_0        (w_req_pc0[0]),
    .req_in_1        (w_req_pc1[0]),
    .req_in_2        (w_req_pc2[0]),
    .req_in_3        (w_req_pc3[0]),
    .req_in_4        (w_req_pc4[0]),
    .vc_pc_0         (sel_vc_out_0),         
    .vc_pc_1         (sel_vc_out_1),
    .vc_pc_2         (sel_vc_out_2),
    .vc_pc_3         (sel_vc_out_3),
    .vc_pc_4         (sel_vc_out_4),
    .vc0_credit      (vc0_credit_out_0), // downstream VC credit
    .vc1_credit      (vc1_credit_out_0),
    .vc2_credit      (vc2_credit_out_0),
    .vc3_credit      (vc3_credit_out_0),
    .vcNew           (vc_new_out_0),
    .winner_pc_out   (sel_pc_out_0)
    );

    swArb out1Arb( 
    .time_in_0       (w_time_0),
    .time_in_1       (w_time_1),
    .time_in_2       (w_time_2),
    .time_in_3       (w_time_3),
    .time_in_4       (w_time_4),
    .req_in_0        (w_req_pc0[1]),
    .req_in_1        (w_req_pc1[1]),
    .req_in_2        (w_req_pc2[1]),
    .req_in_3        (w_req_pc3[1]),
    .req_in_4        (w_req_pc4[1]),
    .vc_pc_0         (sel_vc_out_0),         
    .vc_pc_1         (sel_vc_out_1),
    .vc_pc_2         (sel_vc_out_2),
    .vc_pc_3         (sel_vc_out_3),
    .vc_pc_4         (sel_vc_out_4),
    .vc0_credit      (vc0_credit_out_1), // downstream VC credit
    .vc1_credit      (vc1_credit_out_1),
    .vc2_credit      (vc2_credit_out_1),
    .vc3_credit      (vc3_credit_out_1),
    .vcNew           (vc_new_out_1),    
    .winner_pc_out   (sel_pc_out_1)
    );

    swArb out2Arb( 
    .time_in_0       (w_time_0),
    .time_in_1       (w_time_1),
    .time_in_2       (w_time_2),
    .time_in_3       (w_time_3),
    .time_in_4       (w_time_4),
    .req_in_0        (w_req_pc0[2]),
    .req_in_1        (w_req_pc1[2]),
    .req_in_2        (w_req_pc2[2]),
    .req_in_3        (w_req_pc3[2]),
    .req_in_4        (w_req_pc4[2]),
    .vc_pc_0         (sel_vc_out_0),         
    .vc_pc_1         (sel_vc_out_1),
    .vc_pc_2         (sel_vc_out_2),
    .vc_pc_3         (sel_vc_out_3),
    .vc_pc_4         (sel_vc_out_4),
    .vc0_credit      (vc0_credit_out_2), // downstream VC credit
    .vc1_credit      (vc1_credit_out_2),
    .vc2_credit      (vc2_credit_out_2),
    .vc3_credit      (vc3_credit_out_2),
    .vcNew           (vc_new_out_2),    
    .winner_pc_out   (sel_pc_out_2)
    );
    
     swArb out3Arb( 
    .time_in_0       (w_time_0),
    .time_in_1       (w_time_1),
    .time_in_2       (w_time_2),
    .time_in_3       (w_time_3),
    .time_in_4       (w_time_4),
    .req_in_0        (w_req_pc0[3]),
    .req_in_1        (w_req_pc1[3]),
    .req_in_2        (w_req_pc2[3]),
    .req_in_3        (w_req_pc3[3]),
    .req_in_4        (w_req_pc4[3]),
    .vc_pc_0         (sel_vc_out_0),         
    .vc_pc_1         (sel_vc_out_1),
    .vc_pc_2         (sel_vc_out_2),
    .vc_pc_3         (sel_vc_out_3),
    .vc_pc_4         (sel_vc_out_4),
    .vc0_credit      (vc0_credit_out_3), // downstream VC credit
    .vc1_credit      (vc1_credit_out_3),
    .vc2_credit      (vc2_credit_out_3),
    .vc3_credit      (vc3_credit_out_3),
    .vcNew           (vc_new_out_3),    
    .winner_pc_out   (sel_pc_out_3)
    );
    
     swArb out4Arb( 
    .time_in_0       (w_time_0),
    .time_in_1       (w_time_1),
    .time_in_2       (w_time_2),
    .time_in_3       (w_time_3),
    .time_in_4       (w_time_4),
    .req_in_0        (w_req_pc0[4]),
    .req_in_1        (w_req_pc1[4]),
    .req_in_2        (w_req_pc2[4]),
    .req_in_3        (w_req_pc3[4]),
    .req_in_4        (w_req_pc4[4]),
    .vc_pc_0         (sel_vc_out_0),         
    .vc_pc_1         (sel_vc_out_1),
    .vc_pc_2         (sel_vc_out_2),
    .vc_pc_3         (sel_vc_out_3),
    .vc_pc_4         (sel_vc_out_4),
    .vc0_credit      (vc0_credit_out_4), // downstream VC credit
    .vc1_credit      (vc1_credit_out_4),
    .vc2_credit      (vc2_credit_out_4),
    .vc3_credit      (vc3_credit_out_4),
    .vcNew           (vc_new_out_4),    
    .winner_pc_out   (sel_pc_out_4)
    );
                                   
endmodule
