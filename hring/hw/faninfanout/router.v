`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/11/2016 06:48:54 PM
// Design Name: 
// Module Name: router
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

module router(
    clk,
    n_rst,
    data_in_0,
    data_in_1,
    data_in_2,
    data_in_3,
    data_in_4,
    data_out_0,
    data_out_1,
    data_out_2,
    data_out_3,
    data_out_4,
    credit_return_in_0,    
    credit_return_in_1,    
    credit_return_in_2,    
    credit_return_in_3,
    credit_return_out_0,
    credit_return_out_1,
    credit_return_out_2,
    credit_return_out_3    
    );
    
    // missing fuction:
    // Credit management
    // update PPV, in case incomplete allocation.
    // updata rd ptr in PC
    // choose ppv
    // update turn bit
     
    
    input clk, n_rst;
    // Assume data_in_* will maintain unchanged for 1 cycle.
    input [`DATA_WIDTH-1:0] data_in_0, data_in_1, data_in_2, data_in_3, data_in_4;
    output [`DATA_WIDTH-1:0] data_out_0, data_out_1, data_out_2, data_out_3, data_out_4;
    // Each downstream VC has a dedicated wire for returning credit for simplicity
    input  [`NUM_VC-1-2:0]   credit_return_in_0, credit_return_in_1, credit_return_in_2, credit_return_in_3;     
    output  [`NUM_VC-1-2:0]   credit_return_out_0, credit_return_out_1, credit_return_out_2, credit_return_out_3;     
    
    // seperate the field
    wire [`SRC_LIST_WIDTH-1:0] dst_src_list_0 = data_in_0 [`SRC_LIST_POS];
    wire [`SRC_LIST_WIDTH-1:0] dst_src_list_1 = data_in_1 [`SRC_LIST_POS];
    wire [`SRC_LIST_WIDTH-1:0] dst_src_list_2 = data_in_2 [`SRC_LIST_POS];
    wire [`SRC_LIST_WIDTH-1:0] dst_src_list_3 = data_in_3 [`SRC_LIST_POS];
    wire [`SRC_LIST_WIDTH-1:0] dst_src_list_4 = data_in_4 [`SRC_LIST_POS];
    wire [`DST_WIDTH-1:0] dst_0 = data_in_0 [`DST_POS];
    wire [`DST_WIDTH-1:0] dst_1 = data_in_1 [`DST_POS];
    wire [`DST_WIDTH-1:0] dst_2 = data_in_2 [`DST_POS];
    wire [`DST_WIDTH-1:0] dst_3 = data_in_3 [`DST_POS];
    wire [`DST_WIDTH-1:0] dst_4 = data_in_4 [`DST_POS];
    wire mc_0 = data_in_0 [`MC_POS];
    wire mc_1 = data_in_1 [`MC_POS];
    wire mc_2 = data_in_2 [`MC_POS];
    wire mc_3 = data_in_3 [`MC_POS];
    wire mc_4 = data_in_4 [`MC_POS];
	wire hs_0 = data_in_0 [`HS_POS];
	wire hs_1 = data_in_1 [`HS_POS];
	wire hs_2 = data_in_2 [`HS_POS];
	wire hs_3 = data_in_3 [`HS_POS];
	wire hs_4 = data_in_4 [`HS_POS];
    wire ltb_0 = data_in_0 [`AD_LTB];
    wire ltb_1 = data_in_1 [`AD_LTB];
    wire ltb_2 = data_in_2 [`AD_LTB];
    wire ltb_3 = data_in_3 [`AD_LTB];
    wire ltb_4;
    wire rtb_0 = data_in_0 [`AD_RTB];
    wire rtb_1 = data_in_1 [`AD_RTB];
    wire rtb_2 = data_in_2 [`AD_RTB];
    wire rtb_3 = data_in_3 [`AD_RTB];
    wire rtb_4;
	wire [`NUM_PORT-1:0] ppv_0 = data_in_0 [`PPV_POS];
	wire [`NUM_PORT-1:0] ppv_1 = data_in_1 [`PPV_POS];
	wire [`NUM_PORT-1:0] ppv_2 = data_in_2 [`PPV_POS];
	wire [`NUM_PORT-1:0] ppv_3 = data_in_3 [`PPV_POS];
	wire [`NUM_PORT-1:0] ppv_4 = data_in_4 [`PPV_POS];
    wire [`TIME_WIDTH-1:0] time_0 = data_in_0 [`TIME_POS];
    wire [`TIME_WIDTH-1:0] time_1 = data_in_1 [`TIME_POS];
    wire [`TIME_WIDTH-1:0] time_2 = data_in_2 [`TIME_POS];
    wire [`TIME_WIDTH-1:0] time_3 = data_in_3 [`TIME_POS];
    wire [`TIME_WIDTH-1:0] time_4 = data_in_4 [`TIME_POS];
   
    wire [`NUM_PORT-1:0] ppv_in0_out1, ppv_in0_out2, ppv_in0_out3;
    wire [`NUM_PORT-1:0] ppv_in1_out2, ppv_in1_out3, ppv_in1_out0;
    wire [`NUM_PORT-1:0] ppv_in2_out3, ppv_in2_out0, ppv_in2_out1;
    wire [`NUM_PORT-1:0] ppv_in3_out0, ppv_in3_out1, ppv_in3_out2;
    wire [`NUM_PORT-1:0] ppv_in4_out0, ppv_in4_out1, ppv_in4_out2, ppv_in4_out3;
    wire [2:0] vc_sel_0, vc_sel_1, vc_sel_2, vc_sel_3, vc_sel_4;
    wire [2:0] pc_sel_0, pc_sel_1, pc_sel_2, pc_sel_3, pc_sel_4;
    wire [1:0] vc_new_0, vc_new_1, vc_new_2, vc_new_3, vc_new_4;
    wire bypass_0, bypass_1, bypass_2, bypass_3;
    wire [`IR_DATA_WIDTH-1:0] data_merged_0, data_merged_1, data_merged_2, data_merged_3, data_merged_4;
    wire [`IR_DATA_WIDTH-1:0] data_bw_0, data_bw_1, data_bw_2, data_bw_3, data_bw_4;
    wire [`IR_DATA_WIDTH-1:0] hs_master_buf_0, hs_master_buf_1, hs_master_buf_2, hs_master_buf_3, hs_master_buf_4;
    wire [`IR_DATA_WIDTH-1:0] pc0_vc0, pc0_vc1, pc0_vc2, pc0_vc3, pc1_vc0, pc1_vc1, pc1_vc2, pc1_vc3, 
                            pc2_vc0, pc2_vc1, pc2_vc2, pc2_vc3, pc3_vc0, pc3_vc1, pc3_vc2, pc3_vc3,
                            pc4_vc0, pc4_vc1, pc4_vc2, pc4_vc3;
    wire [1:0]  vc_0 = data_in_0 [`VC_POS];
    wire [1:0]  vc_1 = data_in_1 [`VC_POS];
    wire [1:0]  vc_2 = data_in_2 [`VC_POS];
    wire [1:0]  vc_3 = data_in_3 [`VC_POS];
    wire [1:0]  vc_4 = data_in_4 [`VC_POS];
    wire [`NUM_PORT-1:0] uppv_0, uppv_1, uppv_2, uppv_3, uppv_4;

    // router computation
    
    wire ltb_in0_out1, ltb_in0_out2, ltb_in0_out3, rtb_in0_out1, rtb_in0_out2, rtb_in0_out3;
    wire ltb_in1_out0, ltb_in1_out2, ltb_in1_out3, rtb_in1_out0, rtb_in1_out2, rtb_in1_out3;
    wire ltb_in2_out0, ltb_in2_out1, ltb_in2_out3, rtb_in2_out0, rtb_in2_out1, rtb_in2_out3;
    wire ltb_in3_out0, ltb_in3_out1, ltb_in3_out2, rtb_in3_out0, rtb_in3_out1, rtb_in3_out2;
    wire ltb_in4_out0, ltb_in4_out1, ltb_in4_out2, ltb_in4_out3, rtb_in4_out0, rtb_in4_out1, rtb_in4_out2, rtb_in4_out3;
    nextRC nrc_in0(
    .dst            (dst_0),
    .dstList        (dst_src_list_0),
    .mc             (mc_0),
    .indir          (3'd0),
    .flitLTBOld     (ltb_0),
    .flitRTBOld     (rtb_0),
    
    .localFlitLTB   (),
    .localFlitRTB   (),
    .flitLTBNew_0_plus1 (ltb_in0_out1),
    .flitLTBNew_1_plus2 (ltb_in0_out2),
    .flitLTBNew_2_plus3 (ltb_in0_out3),
    .flitLTBNew_3       (),
    .flitRTBNew_0_plus1 (rtb_in0_out1),
    .flitRTBNew_1_plus2 (rtb_in0_out2),
    .flitRTBNew_2_plus3 (rtb_in0_out3),
    .flitRTBNew_3       (),
    .preferPortVector_0_plus1 (ppv_in0_out1),
    .preferPortVector_1_plus2 (ppv_in0_out2),
    .preferPortVector_2_plus3 (ppv_in0_out3),
    .preferPortVector_3       ()
    );
    

    nextRC nrc_in1(
    .dst            (dst_1),
    .dstList        (dst_src_list_1),
    .mc             (mc_1),
    .indir          (3'd1),
    .flitLTBOld     (ltb_1),
    .flitRTBOld     (rtb_1),
    
    .localFlitLTB   (),
    .localFlitRTB   (),
    .flitLTBNew_0_plus1 (ltb_in1_out2),
    .flitLTBNew_1_plus2 (ltb_in1_out3),
    .flitLTBNew_2_plus3 (ltb_in1_out0),
    .flitLTBNew_3       (),
    .flitRTBNew_0_plus1 (rtb_in1_out2),
    .flitRTBNew_1_plus2 (rtb_in1_out3),
    .flitRTBNew_2_plus3 (rtb_in1_out0),
    .flitRTBNew_3       (),    
    .preferPortVector_0_plus1 (ppv_in1_out2),
    .preferPortVector_1_plus2 (ppv_in1_out3),
    .preferPortVector_2_plus3 (ppv_in1_out0),
    .preferPortVector_3       ()
    );

    nextRC nrc_in2(
    .dst            (dst_2),
    .dstList        (dst_src_list_2),
    .mc             (mc_2),
    .indir          (3'd2),
    .flitLTBOld     (ltb_2),
    .flitRTBOld     (rtb_2),
    
    .localFlitLTB   (),
    .localFlitRTB   (),
    .flitLTBNew_0_plus1 (ltb_in2_out3),
    .flitLTBNew_1_plus2 (ltb_in2_out0),
    .flitLTBNew_2_plus3 (ltb_in2_out1),
    .flitLTBNew_3       (),
    .flitRTBNew_0_plus1 (rtb_in2_out3),
    .flitRTBNew_1_plus2 (rtb_in2_out0),
    .flitRTBNew_2_plus3 (rtb_in2_out1),
    .flitRTBNew_3       (),    
    .preferPortVector_0_plus1 (ppv_in2_out3),
    .preferPortVector_1_plus2 (ppv_in2_out0),
    .preferPortVector_2_plus3 (ppv_in2_out1),
    .preferPortVector_3       ()
    );

    nextRC nrc_in3(
    .dst            (dst_3),
    .dstList        (dst_src_list_3),
    .mc             (mc_0),
    .indir          (3'd3),
    .flitLTBOld     (ltb_3),
    .flitRTBOld     (rtb_3),
    
    .localFlitLTB   (),
    .localFlitRTB   (),
    .flitLTBNew_0_plus1 (ltb_in3_out0),
    .flitLTBNew_1_plus2 (ltb_in3_out1),
    .flitLTBNew_2_plus3 (ltb_in3_out2),
    .flitLTBNew_3       (),
    .flitRTBNew_0_plus1 (rtb_in3_out0),
    .flitRTBNew_1_plus2 (rtb_in3_out1),
    .flitRTBNew_2_plus3 (rtb_in3_out2),
    .flitRTBNew_3       (),   
    .preferPortVector_0_plus1 (ppv_in3_out0),
    .preferPortVector_1_plus2 (ppv_in3_out1),
    .preferPortVector_2_plus3 (ppv_in3_out2),
    .preferPortVector_3       ()
    );

    nextRC nrc_in4(
    .dst            (dst_4),
    .dstList        (dst_src_list_4),
    .mc             (mc_4),
    .indir          (3'd4),
    .flitLTBOld     (),
    .flitRTBOld     (),
    
    .localFlitLTB   (ltb_4),
    .localFlitRTB   (rtb_4),
    .flitLTBNew_0_plus1 (ltb_in4_out0),
    .flitLTBNew_1_plus2 (ltb_in4_out1),
    .flitLTBNew_2_plus3 (ltb_in4_out2),
    .flitLTBNew_3       (ltb_in4_out3),
    .flitRTBNew_0_plus1 (rtb_in4_out0),
    .flitRTBNew_1_plus2 (rtb_in4_out1),
    .flitRTBNew_2_plus3 (rtb_in4_out2),
    .flitRTBNew_3       (rtb_in4_out3),
    .preferPortVector_0_plus1 (ppv_in4_out0),
    .preferPortVector_1_plus2 (ppv_in4_out1),
    .preferPortVector_2_plus3 (ppv_in4_out2),
    .preferPortVector_3       (ppv_in4_out3)
    );
    
    wire [`IR_WIDTH-1:0]  pre_route_0, pre_route_1, pre_route_2, pre_route_3, pre_route_4;
    assign pre_route_0 = {7'b0, ltb_in0_out1, rtb_in0_out1, ppv_in0_out1, ltb_in0_out2, rtb_in0_out2, ppv_in0_out2, ltb_in0_out3, rtb_in0_out3, ppv_in0_out3};
    assign pre_route_1 = {ltb_in1_out0, rtb_in1_out0, ppv_in1_out0, 7'b0, ltb_in1_out2, rtb_in1_out2, ppv_in1_out2, ltb_in1_out3, rtb_in1_out3, ppv_in1_out3};
    assign pre_route_2 = {ltb_in2_out0, rtb_in2_out0, ppv_in2_out0, ltb_in2_out1, rtb_in2_out1, ppv_in2_out1, 7'b0, ltb_in2_out3, rtb_in2_out3, ppv_in2_out3};
    assign pre_route_3 = {ltb_in3_out0, rtb_in3_out0, ppv_in3_out0, ltb_in3_out1, rtb_in3_out1, ppv_in3_out1, ltb_in3_out2, rtb_in3_out2, ppv_in3_out2, 7'b0};
    assign pre_route_4 = {ltb_in4_out0, rtb_in4_out0, ppv_in4_out0, ltb_in4_out1, rtb_in4_out1, ppv_in4_out1, ltb_in4_out2, rtb_in4_out2, ppv_in4_out2, ltb_in4_out3, rtb_in4_out3, ppv_in4_out3};
    
    wire [`NUM_PORT-1:0] merged, hs_buf_empty, hs_buf_empty_next;
    // TODO: need to add flowID comparison
	merge merge(
    .clk		(clk),
	.n_rst		(n_rst),
	.data_in_0	(data_in_0),
	.data_in_1	(data_in_1),
    .data_in_2	(data_in_2),
    .data_in_3  (data_in_3),
    .data_in_4	(data_in_4),
    .pre_route_0  (pre_route_0),
    .pre_route_1  (pre_route_1),
    .pre_route_2  (pre_route_2),
    .pre_route_3  (pre_route_3),
    .pre_route_4  (pre_route_4),
    .hs_master_buf_0	(hs_master_buf_0),
    .hs_master_buf_1	(hs_master_buf_1),
    .hs_master_buf_2	(hs_master_buf_2),
    .hs_master_buf_3	(hs_master_buf_3),
    .hs_master_buf_4	(hs_master_buf_4),
    .hs_m_buf_empty_new	(hs_buf_empty),   // from  pc
    .hs_m_buf_empty_out	(hs_buf_empty_next), // to pc
    .data_out_0 (data_merged_0),
    .data_out_1	(data_merged_1),
    .data_out_2	(data_merged_2),
    .data_out_3	(data_merged_3),
    .data_out_4	(data_merged_4),
    .merged (merged)
    );
      
    lookaheadSWAlloc   lookaheadSWAlloc (
        .req_in0_vc0       (pc0_vc0[`PPV_POS]),
       .time_in0_vc0      (pc0_vc0[`TIME_POS]),
       .req_in0_vc1       (pc0_vc1[`PPV_POS]),
       .time_in0_vc1      (pc0_vc1[`TIME_POS]),    
       .req_in0_vc2      (pc0_vc2[`PPV_POS]),
       .time_in0_vc2     (pc0_vc2[`TIME_POS]), 
       .req_in0_vc3      (pc0_vc3[`PPV_POS]),
       .time_in0_vc3     (pc0_vc3[`TIME_POS]), 
       .req_in0_vc4      (hs_master_buf_0[`PPV_POS]),
       .time_in0_vc4     (hs_master_buf_0[`TIME_POS]), 
       .req_in0_vc5        (ppv_0),
       .time_in0_vc5        (time_0),
       .req_in1_vc0      (pc1_vc0[`PPV_POS]),
       .time_in1_vc0     (pc1_vc0[`TIME_POS]), 
       .req_in1_vc1      (pc1_vc1[`PPV_POS]),
       .time_in1_vc1     (pc1_vc1[`TIME_POS]), 
       .req_in1_vc2      (pc1_vc2[`PPV_POS]),
       .time_in1_vc2     (pc1_vc2[`TIME_POS]), 
       .req_in1_vc3      (pc1_vc3[`PPV_POS]),
       .time_in1_vc3     (pc1_vc3[`TIME_POS]), 
       .req_in1_vc4      (hs_master_buf_1[`PPV_POS]),
       .time_in1_vc4    (hs_master_buf_1[`TIME_POS]), 
       .req_in1_vc5        (ppv_1),
       .time_in1_vc5        (time_1),
       .req_in2_vc0      (pc2_vc0[`PPV_POS]),
       .time_in2_vc0     (pc2_vc0[`TIME_POS]), 
       .req_in2_vc1      (pc2_vc1[`PPV_POS]),
       .time_in2_vc1     (pc2_vc1[`TIME_POS]), 
       .req_in2_vc2      (pc2_vc2[`PPV_POS]),
       .time_in2_vc2     (pc2_vc2[`TIME_POS]), 
       .req_in2_vc3      (pc2_vc3[`PPV_POS]),
       .time_in2_vc3     (pc2_vc3[`TIME_POS]), 
       .req_in2_vc4      (hs_master_buf_2[`PPV_POS]),
       .time_in2_vc4     (hs_master_buf_2[`TIME_POS]), 
       .req_in2_vc5        (ppv_2),
       .time_in2_vc5        (time_2),
       .req_in3_vc0      (pc3_vc0[`PPV_POS]),
       .time_in3_vc0     (pc3_vc0[`TIME_POS]), 
       .req_in3_vc1      (pc3_vc1[`PPV_POS]),
       .time_in3_vc1     (pc3_vc1[`TIME_POS]), 
       .req_in3_vc2      (pc3_vc2[`PPV_POS]),
       .time_in3_vc2     (pc3_vc2[`TIME_POS]), 
       .req_in3_vc3      (pc3_vc3[`PPV_POS]),
       .time_in3_vc3     (pc3_vc3[`TIME_POS]), 
       .req_in3_vc4      (hs_master_buf_3[`PPV_POS]),
       .time_in3_vc4     (hs_master_buf_3[`TIME_POS]), 
       .req_in3_vc5        (ppv_3),
       .time_in3_vc5        (time_3),
       .req_in4_vc0      (pc4_vc0[`PPV_POS]),
       .time_in4_vc0     (pc4_vc0[`TIME_POS]), 
       .req_in4_vc1      (pc4_vc1[`PPV_POS]),
       .time_in4_vc1     (pc4_vc1[`TIME_POS]), 
       .req_in4_vc2      (pc4_vc2[`PPV_POS]),
       .time_in4_vc2     (pc4_vc2[`TIME_POS]), 
       .req_in4_vc3      (pc4_vc3[`PPV_POS]),
       .time_in4_vc3     (pc4_vc3[`TIME_POS]), 
       .req_in4_vc4      (hs_master_buf_4[`PPV_POS]),
       .time_in4_vc4     (hs_master_buf_4[`TIME_POS]),
       .credit_return_in_0  (credit_return_in_0),
       .credit_return_in_1  (credit_return_in_1),
       .credit_return_in_2  (credit_return_in_2),
       .credit_return_in_3  (credit_return_in_3),
       .credit_return_out_0  (credit_return_out_0),
       .credit_return_out_1  (credit_return_out_1),
       .credit_return_out_2  (credit_return_out_2),
       .credit_return_out_3  (credit_return_out_3),       
       .uppv_0           (uppv_0),
       .uppv_1           (uppv_1),
       .uppv_2           (uppv_2),
       .uppv_3           (uppv_3),
       .uppv_4           (uppv_4),
       .vc_new_out_0      (vc_new_0),
       .vc_new_out_1      (vc_new_1),
       .vc_new_out_2      (vc_new_2),
       .vc_new_out_3      (vc_new_3),
       .vc_new_out_4      (vc_new_4),                      
       .sel_pc_out_0      (pc_sel_0),
       .sel_vc_out_0      (vc_sel_0),
       .sel_pc_out_1      (pc_sel_1),
       .sel_vc_out_1      (vc_sel_1),
       .sel_pc_out_2      (pc_sel_2),
       .sel_vc_out_2      (vc_sel_2),
       .sel_pc_out_3      (pc_sel_3),
       .sel_vc_out_3      (vc_sel_3),
       .sel_pc_out_4      (pc_sel_4),
       .sel_vc_out_4      (vc_sel_4),
       .grant_new_0       (bypass_0), // grant the newly incoming flit on port 0
       .grant_new_1       (bypass_1), // grant the newly incoming flit on port 1
       .grant_new_2       (bypass_2), // grant the newly incoming flit on port 2
       .grant_new_3       (bypass_3)  // grant the newly incoming flit on port 3
    );
    
	
	pc pc_in0 (
    .clk       (clk), 
    .rst       (n_rst),
    .vc        (vc_0),
    .sel_vc_out(vc_sel_0), 
    .pc_en     (1'b1),
    .bypass    (bypass_0),
    .uppv      (uppv_0),
    .merged     (merged[0]),
    .hs_buf_empty   (hs_buf_empty_next[0]),
    .data_in   (data_merged_0),
    .vc_data_out_0  (pc0_vc0), 
    .vc_data_out_1  (pc0_vc1), 
    .vc_data_out_2  (pc0_vc2), 
    .vc_data_out_3  (pc0_vc3),
    .data_out  (data_bw_0),
    .master_hs_buffer (hs_master_buf_0),
    .hs_buf_empty_new (hs_buf_empty[0])
    );

 	pc pc_in1 (
    .clk       (clk), 
    .rst       (n_rst),
    .vc        (vc_1),
    .sel_vc_out(vc_sel_1), 
    .pc_en     (1'b1),
    .bypass    (bypass_1),
    .uppv      (uppv_1),
    .merged    (merged[1]),
    .hs_buf_empty   (hs_buf_empty_next[1]),    
    .data_in   (data_merged_1),
    .vc_data_out_0  (pc1_vc0), 
    .vc_data_out_1  (pc1_vc1), 
    .vc_data_out_2  (pc1_vc2), 
    .vc_data_out_3  (pc1_vc3),    
    .data_out  (data_bw_1),
    .master_hs_buffer (hs_master_buf_1),
    .hs_buf_empty_new (hs_buf_empty[1])
    );
    
    pc pc_in2 (
    .clk       (clk), 
    .rst       (n_rst),
    .vc        (vc_2),
    .sel_vc_out(vc_sel_2), 
    .pc_en     (1'b1),
    .bypass    (bypass_2),
    .uppv      (uppv_2),
    .merged    (merged[2]),
    .hs_buf_empty   (hs_buf_empty_next[2]),            
    .data_in   (data_merged_2),
    .vc_data_out_0  (pc2_vc0), 
    .vc_data_out_1  (pc2_vc1), 
    .vc_data_out_2  (pc2_vc2), 
    .vc_data_out_3  (pc2_vc3),
    .data_out  (data_bw_2),
    .master_hs_buffer (hs_master_buf_2),
    .hs_buf_empty_new (hs_buf_empty[2])    
    ); 
    
    pc pc_in3 (
    .clk       (clk), 
    .rst       (n_rst),
    .vc        (vc_3),
    .sel_vc_out(vc_sel_3), 
    .pc_en     (1'b1),
    .bypass    (bypass_3),
    .uppv      (uppv_3),
    .merged    (merged[3]),
    .hs_buf_empty   (hs_buf_empty_next[3]),            
    .data_in   (data_merged_3),
    .vc_data_out_0  (pc3_vc0), 
    .vc_data_out_1  (pc3_vc1), 
    .vc_data_out_2  (pc3_vc2), 
    .vc_data_out_3  (pc3_vc3),
    .data_out  (data_bw_3),
    .master_hs_buffer (hs_master_buf_3),
    .hs_buf_empty_new (hs_buf_empty[3])    
    );
    
    
    pc pc_in4 (
    .clk       (clk), 
    .rst       (n_rst),
    .vc        (vc_4),
    .sel_vc_out(vc_sel_4), 
    .pc_en     (1'b1),
    .bypass    (1'b0),
    .uppv      (uppv_4),
    .merged    (merged[4]),
    .hs_buf_empty   (hs_buf_empty_next[4]),            
    .data_in   (data_merged_4),
    .vc_data_out_0  (pc4_vc0), 
    .vc_data_out_1  (pc4_vc1), 
    .vc_data_out_2  (pc4_vc2), 
    .vc_data_out_3  (pc4_vc3),
    .data_out  (data_bw_4),
    .master_hs_buffer (hs_master_buf_4),
    .hs_buf_empty_new (hs_buf_empty[4])      
    );       
    

    xbar xbar(
    .in_0       ({data_bw_0[`IR_DATA_WIDTH-1:`VC_LEFT_POS], vc_new_0, data_bw_0[`VC_RIGHT_POS:0]}),
    .in_1       ({data_bw_1[`IR_DATA_WIDTH-1:`VC_LEFT_POS], vc_new_1, data_bw_1[`VC_RIGHT_POS:0]}),
    .in_2       ({data_bw_2[`IR_DATA_WIDTH-1:`VC_LEFT_POS], vc_new_2, data_bw_0[`VC_RIGHT_POS:0]}),
    .in_3       ({data_bw_3[`IR_DATA_WIDTH-1:`VC_LEFT_POS], vc_new_3, data_bw_0[`VC_RIGHT_POS:0]}),
    .in_4       ({data_bw_4[`IR_DATA_WIDTH-1:`VC_LEFT_POS], vc_new_4, data_bw_0[`VC_RIGHT_POS:0]}),
    .out_0      (data_out_0),
    .out_1      (data_out_1),
    .out_2      (data_out_2),
    .out_3      (data_out_3),
    .out_4      (data_out_4),
    .sel_out_0  (pc_sel_0),
    .sel_out_1  (pc_sel_1),
    .sel_out_2  (pc_sel_2),
    .sel_out_3  (pc_sel_3),
    .sel_out_4  (pc_sel_4)
    );
     
endmodule
