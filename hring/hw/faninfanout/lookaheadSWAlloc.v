`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/10/2016 07:18:22 PM
// Design Name: 
// Module Name: lookaheadSWAlloc
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

module lookaheadSWAlloc(
    req_in0_vc0,
    time_in0_vc0,
    req_in0_vc1,
    time_in0_vc1,    
    req_in0_vc2,
    time_in0_vc2,    
    req_in0_vc3,
    time_in0_vc3,
    req_in0_vc4,
    time_in0_vc4,
    req_in0_vc5,
    time_in0_vc5,
    req_in1_vc0,
    time_in1_vc0,
    req_in1_vc1,
    time_in1_vc1,
    req_in1_vc2,
    time_in1_vc2,
    req_in1_vc3,
    time_in1_vc3,
    req_in1_vc4,
    time_in1_vc4,
    req_in1_vc5,
    time_in1_vc5,
    req_in2_vc0,
    time_in2_vc0,
    req_in2_vc1,
    time_in2_vc1,
    req_in2_vc2,
    time_in2_vc2,
    req_in2_vc3,
    time_in2_vc3,
    req_in2_vc4,
    time_in2_vc4,
    req_in2_vc5,
    time_in2_vc5,
    req_in3_vc0,
    time_in3_vc0,
    req_in3_vc1,
    time_in3_vc1,
    req_in3_vc2,
    time_in3_vc2,
    req_in3_vc3,
    time_in3_vc3,
    req_in3_vc4,
    time_in3_vc4,
    req_in3_vc5,
    time_in3_vc5,
    req_in4_vc0,
    time_in4_vc0,
    req_in4_vc1,
    time_in4_vc1,            
    req_in4_vc2,
    time_in4_vc2,
    req_in4_vc3,
    time_in4_vc3,
    req_in4_vc4,
    time_in4_vc4,
    credit_return_in_0,
    credit_return_in_1,
    credit_return_in_2,
    credit_return_in_3,
    credit_return_out_0,
    credit_return_out_1,
    credit_return_out_2,
    credit_return_out_3,   
    uppv_0,
    uppv_1,
    uppv_2,
    uppv_3,
    uppv_4,    
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
    sel_vc_out_4,
    grant_new_0, // grant the newly incoming flit on port 0
    grant_new_1, // grant the newly incoming flit on port 1
    grant_new_2, // grant the newly incoming flit on port 2
    grant_new_3  // grant the newly incoming flit on port 3
    );
    
    // For hs_master_buf, check timer.
    
    input [`NUM_PORT-1:0] req_in0_vc0, req_in0_vc1, req_in0_vc2, req_in0_vc3, req_in0_vc4, req_in0_vc5, req_in1_vc0, req_in1_vc1, req_in1_vc2, req_in1_vc3, req_in1_vc4, req_in1_vc5,
                          req_in2_vc0, req_in2_vc1, req_in2_vc2, req_in2_vc3, req_in2_vc4, req_in2_vc5, req_in3_vc0, req_in3_vc1, req_in3_vc2, req_in3_vc3, req_in3_vc4, req_in3_vc5, 
                          req_in4_vc0, req_in4_vc1, req_in4_vc2, req_in4_vc3, req_in4_vc4;
    input [`TIME_WIDTH-1:0] time_in0_vc0, time_in0_vc1, time_in0_vc2, time_in0_vc3, time_in0_vc4, time_in0_vc5, time_in1_vc0, time_in1_vc1, time_in1_vc2, time_in1_vc3, time_in1_vc4, time_in1_vc5, 
                            time_in2_vc0, time_in2_vc1, time_in2_vc2, time_in2_vc3, time_in2_vc4, time_in2_vc5, time_in3_vc0, time_in3_vc1, time_in3_vc2, time_in3_vc3, time_in3_vc4, time_in3_vc5,
                            time_in4_vc0, time_in4_vc1, time_in4_vc2, time_in4_vc3, time_in4_vc4;
    input [`NUM_VC-1-2:0] credit_return_in_0, credit_return_in_1, credit_return_in_2, credit_return_in_3;
    output [`NUM_VC-1-2:0] credit_return_out_0, credit_return_out_1, credit_return_out_2, credit_return_out_3;
//    input [`ADDR_WIDTH-1:0] vc0_credit_out_0, vc1_credit_out_0, vc2_credit_out_0, vc3_credit_out_0, vc0_credit_out_1, vc1_credit_out_1, vc2_credit_out_1, vc3_credit_out_1, vc0_credit_out_2,
//                          vc1_credit_out_2, vc2_credit_out_2, vc3_credit_out_2, vc0_credit_out_3, vc1_credit_out_3, vc2_credit_out_3, vc3_credit_out_3, vc0_credit_out_4, vc1_credit_out_4,
//                          vc2_credit_out_4, vc3_credit_out_4;
    output [`NUM_PORT-1:0] uppv_0, uppv_1, uppv_2, uppv_3, uppv_4;
    output [`VC_INDEX_WIDTH-2:0] vc_new_out_0, vc_new_out_1, vc_new_out_2, vc_new_out_3, vc_new_out_4;
    output [`PC_INDEX_WIDTH-1:0]  sel_pc_out_0, sel_pc_out_1, sel_pc_out_2, sel_pc_out_3, sel_pc_out_4;
    output [`VC_INDEX_WIDTH-1:0]  sel_vc_out_0, sel_vc_out_1, sel_vc_out_2, sel_vc_out_3, sel_vc_out_4; // select a VC from each PC
    output grant_new_0, grant_new_1, grant_new_2, grant_new_3;    
    
    wire [`TIME_WIDTH-1:0] w_time_0, w_time_1, w_time_2, w_time_3, w_time_4;
    wire [`NUM_PORT-1:0] w_req_pc0, w_req_pc1, w_req_pc2, w_req_pc3, w_req_pc4; 
    reg [`ADDR_WIDTH-1:0] vc0_credit_out_0, vc1_credit_out_0, vc2_credit_out_0, vc3_credit_out_0, vc0_credit_out_1, vc1_credit_out_1, vc2_credit_out_1, vc3_credit_out_1, vc0_credit_out_2,
                          vc1_credit_out_2, vc2_credit_out_2, vc3_credit_out_2, vc0_credit_out_3, vc1_credit_out_3, vc2_credit_out_3, vc3_credit_out_3, vc0_credit_out_4, vc1_credit_out_4,
                          vc2_credit_out_4, vc3_credit_out_4;       
         
    
    // VC Allocation 
    // Select a VC from each PC, including the newly incoming flit on VC4
    vcArb6to1 pc0_vcArb(
    .time_in_0     (time_in0_vc0),
    .time_in_1     (time_in0_vc1),
    .time_in_2     (time_in0_vc2),
    .time_in_3     (time_in0_vc3),
    .time_in_4     (time_in0_vc4),
    .time_in_5     (time_in0_vc5),
    .winner_time_out (w_time_0),
    .winner_vc_out (sel_vc_out_0) 
    );
    
    vcArb6to1 pc1_vcArb(
    .time_in_0     (time_in1_vc0),
    .time_in_1     (time_in1_vc1),
    .time_in_2     (time_in1_vc2),
    .time_in_3     (time_in1_vc3),
    .time_in_4     (time_in1_vc4),
    .time_in_5     (time_in1_vc5),
    .winner_time_out (w_time_1),
    .winner_vc_out (sel_vc_out_1) 
    );
    
    vcArb6to1 pc2_vcArb(
    .time_in_0     (time_in2_vc0),
    .time_in_1     (time_in2_vc1),
    .time_in_2     (time_in2_vc2),
    .time_in_3     (time_in2_vc3),
    .time_in_4     (time_in2_vc4),
    .time_in_5     (time_in2_vc5),
    .winner_time_out (w_time_2),
    .winner_vc_out (sel_vc_out_2) 
    );
    
    vcArb6to1 pc3_vcArb(
    .time_in_0     (time_in3_vc0),
    .time_in_1     (time_in3_vc1),
    .time_in_2     (time_in3_vc2),
    .time_in_3     (time_in3_vc3),
    .time_in_4     (time_in3_vc4),
    .time_in_5     (time_in3_vc5),
    .winner_time_out (w_time_3),
    .winner_vc_out (sel_vc_out_3) 
    );
       
    vcArb5to1 pc4_vcArb(
    .time_in_0     (time_in4_vc0),
    .time_in_1     (time_in4_vc1),
    .time_in_2     (time_in4_vc2),
    .time_in_3     (time_in4_vc3),
    .time_in_4     (time_in4_vc4),
    .winner_time_out (w_time_4),
    .winner_vc_out (sel_vc_out_4) 
    );
    
    // Select a request among all VCs in a PC
    
    reqSelPC6to1 selPC0(
    .req_in_0       (req_in0_vc0),
    .req_in_1       (req_in0_vc1),
    .req_in_2       (req_in0_vc2),
    .req_in_3       (req_in0_vc3),
    .req_in_4       (req_in0_vc4),
    .req_in_5       (req_in0_vc5),
    .sel            (sel_vc_out_0),
    .req_out        (w_req_pc0)
    );
    
    reqSelPC6to1 selPC1(
    .req_in_0       (req_in1_vc0),
    .req_in_1       (req_in1_vc1),
    .req_in_2       (req_in1_vc2),
    .req_in_3       (req_in1_vc3),
    .req_in_4       (req_in1_vc4),
    .req_in_5       (req_in1_vc5),
    .sel            (sel_vc_out_1),
    .req_out        (w_req_pc1)
    );
    
    reqSelPC6to1 selPC2(
    .req_in_0       (req_in2_vc0),
    .req_in_1       (req_in2_vc1),
    .req_in_2       (req_in2_vc2),
    .req_in_3       (req_in2_vc3),
    .req_in_4       (req_in2_vc4),
    .req_in_5       (req_in2_vc5),
    .sel            (sel_vc_out_2),
    .req_out        (w_req_pc2)
    );
    
    reqSelPC6to1 selPC3(
    .req_in_0       (req_in3_vc0),
    .req_in_1       (req_in3_vc1),
    .req_in_2       (req_in3_vc2),
    .req_in_3       (req_in3_vc3),
    .req_in_4       (req_in3_vc4),
    .req_in_5       (req_in3_vc5),
    .sel            (sel_vc_out_3),
    .req_out        (w_req_pc3)
    );
    
    reqSelPC5to1 selPC4(
    .req_in_0       (req_in4_vc0),
    .req_in_1       (req_in4_vc1),
    .req_in_2       (req_in4_vc2),
    .req_in_3       (req_in4_vc3),
    .req_in_4       (req_in4_vc4),
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
    .vc0_credit      (1'b1), // for local destined port, we assume infinite ejection buffer
    .vc1_credit      (1'b1), // for local destined port, we assume infinite ejection buffer
    .vc2_credit      (1'b1), // for local destined port, we assume infinite ejection buffer
    .vc3_credit      (1'b1), // for local destined port, we assume infinite ejection buffer
    .vcNew           (vc_new_out_4),    
    .winner_pc_out   (sel_pc_out_4)
    );

    // check if incoming flit can be bypass
    
    assign grant_new_0 = |((sel_vc_out_0 == `VC_INDEX_WIDTH'd4) ? 
    (sel_pc_out_0 == `PC_INDEX_WIDTH'd0 | sel_pc_out_1 == `PC_INDEX_WIDTH'd0 | sel_pc_out_2 == `PC_INDEX_WIDTH'd0 | 
    sel_pc_out_3 == `PC_INDEX_WIDTH'd0 | sel_pc_out_4 == `PC_INDEX_WIDTH'd0 ? `NUM_PORT'd0 : req_in0_vc4) 
    : req_in0_vc4);

    assign grant_new_1 = |((sel_vc_out_1 == `VC_INDEX_WIDTH'd4) ? 
    (sel_pc_out_0 == `PC_INDEX_WIDTH'd1 | sel_pc_out_1 == `PC_INDEX_WIDTH'd1 | sel_pc_out_2 == `PC_INDEX_WIDTH'd1 | 
    sel_pc_out_3 == `PC_INDEX_WIDTH'd1 | sel_pc_out_4 == `PC_INDEX_WIDTH'd1 ? `NUM_PORT'd0 : req_in0_vc4) 
    : req_in0_vc4);
       
     assign grant_new_2 = |((sel_vc_out_0 == `VC_INDEX_WIDTH'd4) ? 
    (sel_pc_out_0 == `PC_INDEX_WIDTH'd2 | sel_pc_out_1 == `PC_INDEX_WIDTH'd2 | sel_pc_out_2 == `PC_INDEX_WIDTH'd2 | 
    sel_pc_out_3 == `PC_INDEX_WIDTH'd2 | sel_pc_out_4 == `PC_INDEX_WIDTH'd2 ? `NUM_PORT'd0 : req_in0_vc4) 
    : req_in0_vc4);

    assign grant_new_3 = |((sel_vc_out_0 == `VC_INDEX_WIDTH'd4) ? 
    (sel_pc_out_0 == `PC_INDEX_WIDTH'd3 | sel_pc_out_1 == `PC_INDEX_WIDTH'd3 | sel_pc_out_2 == `PC_INDEX_WIDTH'd3 | 
    sel_pc_out_3 == `PC_INDEX_WIDTH'd3 | sel_pc_out_4 == `PC_INDEX_WIDTH'd3 ? `NUM_PORT'd0 : req_in0_vc4) 
    : req_in0_vc4);
    
    
    // Managing Credit
    assign credit_return_out_0[0] = sel_pc_out_0!=`NULL_PC && vc_new_out_0 == 0;
    assign credit_return_out_0[1] = sel_pc_out_0!=`NULL_PC && vc_new_out_0 == 1;
    assign credit_return_out_0[2] = sel_pc_out_0!=`NULL_PC && vc_new_out_0 == 2;
    assign credit_return_out_0[3] = sel_pc_out_0!=`NULL_PC && vc_new_out_0 == 3;
    
    assign credit_return_out_1[0] = sel_pc_out_1!=`NULL_PC && vc_new_out_1 == 0;
    assign credit_return_out_1[1] = sel_pc_out_1!=`NULL_PC && vc_new_out_1 == 1;
    assign credit_return_out_1[2] = sel_pc_out_1!=`NULL_PC && vc_new_out_1 == 2;
    assign credit_return_out_1[3] = sel_pc_out_1!=`NULL_PC && vc_new_out_1 == 3;
    
    assign credit_return_out_2[0] = sel_pc_out_2!=`NULL_PC && vc_new_out_2 == 0;
    assign credit_return_out_2[1] = sel_pc_out_2!=`NULL_PC && vc_new_out_2 == 1;
    assign credit_return_out_2[2] = sel_pc_out_2!=`NULL_PC && vc_new_out_2 == 2;
    assign credit_return_out_2[3] = sel_pc_out_2!=`NULL_PC && vc_new_out_2 == 3;
    
    assign credit_return_out_3[0] = sel_pc_out_3!=`NULL_PC && vc_new_out_3 == 0;
    assign credit_return_out_3[1] = sel_pc_out_3!=`NULL_PC && vc_new_out_3 == 1;
    assign credit_return_out_3[2] = sel_pc_out_3!=`NULL_PC && vc_new_out_3 == 2;
    assign credit_return_out_3[3] = sel_pc_out_3!=`NULL_PC && vc_new_out_3 == 3;
    
    always @ * begin
       vc0_credit_out_0 = `RAM_DEPTH + credit_return_in_0[0] - (sel_pc_out_0!=`NULL_PC && vc_new_out_0 == 0);
       vc1_credit_out_0 = `RAM_DEPTH + credit_return_in_0[1] - (sel_pc_out_0!=`NULL_PC && vc_new_out_0 == 1);
       vc2_credit_out_0 = `RAM_DEPTH + credit_return_in_0[2] - (sel_pc_out_0!=`NULL_PC && vc_new_out_0 == 2);
       vc3_credit_out_0 = `RAM_DEPTH + credit_return_in_0[3] - (sel_pc_out_0!=`NULL_PC && vc_new_out_0 == 3);

       vc0_credit_out_1 = `RAM_DEPTH + credit_return_in_1[0] - (sel_pc_out_1!=`NULL_PC && vc_new_out_1 == 0);
       vc1_credit_out_1 = `RAM_DEPTH + credit_return_in_1[1] - (sel_pc_out_1!=`NULL_PC && vc_new_out_1 == 1);
       vc2_credit_out_1 = `RAM_DEPTH + credit_return_in_1[2] - (sel_pc_out_1!=`NULL_PC && vc_new_out_1 == 2);
       vc3_credit_out_1 = `RAM_DEPTH + credit_return_in_1[3] - (sel_pc_out_1!=`NULL_PC && vc_new_out_1 == 3);
       
       vc0_credit_out_2 = `RAM_DEPTH + credit_return_in_2[0] - (sel_pc_out_2!=`NULL_PC && vc_new_out_2 == 0);
       vc1_credit_out_2 = `RAM_DEPTH + credit_return_in_2[1] - (sel_pc_out_2!=`NULL_PC && vc_new_out_2 == 1);
       vc2_credit_out_2 = `RAM_DEPTH + credit_return_in_2[2] - (sel_pc_out_2!=`NULL_PC && vc_new_out_2 == 2);
       vc3_credit_out_2 = `RAM_DEPTH + credit_return_in_2[3] - (sel_pc_out_2!=`NULL_PC && vc_new_out_2 == 3);
      
       vc0_credit_out_3 = `RAM_DEPTH + credit_return_in_3[0] - (sel_pc_out_3!=`NULL_PC && vc_new_out_3 == 0);
       vc1_credit_out_3 = `RAM_DEPTH + credit_return_in_3[1] - (sel_pc_out_3!=`NULL_PC && vc_new_out_3 == 1);
       vc2_credit_out_3 = `RAM_DEPTH + credit_return_in_3[2] - (sel_pc_out_3!=`NULL_PC && vc_new_out_3 == 2);
       vc3_credit_out_3 = `RAM_DEPTH + credit_return_in_3[3] - (sel_pc_out_3!=`NULL_PC && vc_new_out_3 == 3);                 
    end 

    // record the unsuccessful allocation
    // use to update the ppv field stored in PC
    // so that they can continue requesting these ports
    assign uppv_0 [0] = w_req_pc0[0] && (sel_pc_out_0 != 0);       
    assign uppv_0 [1] = w_req_pc0[1] && (sel_pc_out_1 != 0);       
    assign uppv_0 [2] = w_req_pc0[2] && (sel_pc_out_2 != 0);       
    assign uppv_0 [3] = w_req_pc0[3] && (sel_pc_out_3 != 0);       
    assign uppv_0 [4] = w_req_pc0[4] && (sel_pc_out_4 != 0);
    assign uppv_1 [0] = w_req_pc1[0] && (sel_pc_out_0 != 1);       
    assign uppv_1 [1] = w_req_pc1[1] && (sel_pc_out_1 != 1);       
    assign uppv_1 [2] = w_req_pc1[2] && (sel_pc_out_2 != 1);       
    assign uppv_1 [3] = w_req_pc1[3] && (sel_pc_out_3 != 1);       
    assign uppv_1 [4] = w_req_pc1[4] && (sel_pc_out_4 != 1);
    assign uppv_2 [0] = w_req_pc2[0] && (sel_pc_out_0 != 2);       
    assign uppv_2 [1] = w_req_pc2[1] && (sel_pc_out_1 != 2);       
    assign uppv_2 [2] = w_req_pc2[2] && (sel_pc_out_2 != 2);       
    assign uppv_2 [3] = w_req_pc2[3] && (sel_pc_out_3 != 2);       
    assign uppv_2 [4] = w_req_pc2[4] && (sel_pc_out_4 != 2);
    assign uppv_3 [0] = w_req_pc3[0] && (sel_pc_out_0 != 3);       
    assign uppv_3 [1] = w_req_pc3[1] && (sel_pc_out_1 != 3);       
    assign uppv_3 [2] = w_req_pc3[2] && (sel_pc_out_2 != 3);       
    assign uppv_3 [3] = w_req_pc3[3] && (sel_pc_out_3 != 3);       
    assign uppv_3 [4] = w_req_pc3[4] && (sel_pc_out_4 != 3);
    assign uppv_4 [0] = w_req_pc4[0] && (sel_pc_out_0 != 4);       
    assign uppv_4 [1] = w_req_pc4[1] && (sel_pc_out_1 != 4);       
    assign uppv_4 [2] = w_req_pc4[2] && (sel_pc_out_2 != 4);       
    assign uppv_4 [3] = w_req_pc4[3] && (sel_pc_out_3 != 4);       
    assign uppv_4 [4] = w_req_pc4[4] && (sel_pc_out_4 != 4); 
   
endmodule
