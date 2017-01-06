`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/20/2016 10:53:31 PM
// Design Name: 
// Module Name: local
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

`ifdef CARPOOL
module local(
    in_0,
    in_1,
    in_2,
    in_3,
    in_4,
    merge,
    num_flit_i,
    ppv_0,
    ppv_1,
    ppv_2,
    ppv_3,
    out_0,
    out_1,
    out_2,
    out_3,
    out_4,
    ppv_out_0,
    ppv_out_1,
    ppv_out_2,
    ppv_out_3,
    num_flit_o,
    eject
    );
    
    input [`DATA_WIDTH-1:0] in_0, in_1, in_2, in_3, in_4;
    input [`NUM_PORT-1:0] ppv_0, ppv_1, ppv_2, ppv_3;
    output [`NUM_PORT-2:0] ppv_out_0, ppv_out_1, ppv_out_2, ppv_out_3;
    input [`NUM_PORT-1:0] merge;
    input [`PC_INDEX_WIDTH-1:0] num_flit_i;
    output [`PC_INDEX_WIDTH-1:0] num_flit_o;
    output [`DATA_WIDTH-1:0] out_0, out_1, out_2, out_3, out_4;
    output [3:0] eject;
    
    wire [`TIME_WIDTH-1:0] time_0, time_1, time_2, time_3, eject_time_1, eject_time_2;
    wire cmp_0_1, cmp_2_3, cmp_last;
    wire [`PC_INDEX_WIDTH-1:0] eject_sel_1, eject_sel_2, eject_sel_3;
    wire inj_en_0, inj_en_1, inj_en_2, inj_en_3;
    wire [`DATA_WIDTH-1:0] w_out_0, w_out_1, w_out_2, w_out_3;
    
    
    // eject flit
    assign time_0 = ppv_0[4] && ~merge [0] ? in_0[`TIME_POS] : `MAX_TIME;
    assign time_1 = ppv_1[4] && ~merge [1] ? in_1[`TIME_POS] : `MAX_TIME;
    assign time_2 = ppv_2[4] && ~merge [2] ? in_2[`TIME_POS] : `MAX_TIME;
    assign time_3 = ppv_3[4] && ~merge [3] ? in_3[`TIME_POS] : `MAX_TIME;
    
    assign cmp_0_1 = time_0 <= time_1;    
    assign cmp_2_3 = time_2 <= time_3;    
    
    assign eject_sel_1 = cmp_0_1 ? 3'd0 : 3'd1;
    assign eject_time_1 = cmp_0_1 ? time_0 : time_1;
    assign eject_sel_2 = cmp_2_3 ? 3'd2 : 3'd3;
    assign eject_time_2 = cmp_2_3 ? time_2 : time_3;
    
    assign cmp_last = eject_time_1 <= eject_time_2;
    assign eject_sel_3 = ((eject_time_1 & eject_time_2) == `MAX_TIME) ? `NULL_PC : 
                         cmp_last ? eject_sel_1 : eject_sel_2;
    
    assign out_4 = (eject_sel_3 == 0) ? in_0 : 
                   (eject_sel_3 == 1) ? in_1 :
                   (eject_sel_3 == 2) ? in_2 :
                   (eject_sel_3 == 3) ? in_3 :
                   'h0;
    assign eject [0] = eject_sel_3 == 0;
    assign eject [1] = eject_sel_3 == 1;
    assign eject [2] = eject_sel_3 == 2;
    assign eject [3] = eject_sel_3 == 3;

    // kill   
    // Ejection doesn't mean the flit can be nullified. 
    // It may request for several ports
    wire [`DATA_WIDTH-1:0] w_kill_out0, w_kill_out1, w_kill_out2, w_kill_out3;
    wire [`NUM_PORT-2:0] w_kill_ppv0, w_kill_ppv1, w_kill_ppv2, w_kill_ppv3;
    assign w_kill_out0 = (merge[0] || (eject_sel_3 == 0 && ~|ppv_0[3:0])) ? 'h0 : in_0;
    assign w_kill_out1 = (merge[1] || (eject_sel_3 == 1 && ~|ppv_0[3:0])) ? 'h0 : in_1;
    assign w_kill_out2 = (merge[2] || (eject_sel_3 == 2 && ~|ppv_0[3:0])) ? 'h0 : in_2;
    assign w_kill_out3 = (merge[3] || (eject_sel_3 == 3 && ~|ppv_0[3:0])) ? 'h0 : in_3;
    assign w_kill_ppv0 = merge[0] ? 'h0 : ppv_0[3:0];
    assign w_kill_ppv1 = merge[1] ? 'h0 : ppv_1[3:0];
    assign w_kill_ppv2 = merge[2] ? 'h0 : ppv_2[3:0];
    assign w_kill_ppv3 = merge[3] ? 'h0 : ppv_3[3:0];
    
    // inject
    // note: ejection before injection can boost up throughput
    // Provided that a injection is not granted at lower channel, it is granted to a channel if either 
    // 1) channel has no flit 2) channel has flit which is merged 3) channel has flit which is ejected.
       
    assign inj_en_0 = ~w_kill_out0[`VALID_POS] && in_4[`VALID_POS] && ~merge[4]; // local injection is granted provided it is not merged.
    assign inj_en_1 = ~w_kill_out1[`VALID_POS] && ~inj_en_0 && in_4[`VALID_POS] && ~merge[4];
    assign inj_en_2 = ~w_kill_out2[`VALID_POS] && ~inj_en_1 && ~inj_en_0 && in_4[`VALID_POS] && ~merge[4];
    assign inj_en_3 = ~w_kill_out3[`VALID_POS] && ~inj_en_2 && ~inj_en_1 && ~inj_en_0 && in_4[`VALID_POS] && ~merge[4];
    
    assign out_0 = inj_en_0 ? in_4 : w_kill_out0;
    assign out_1 = inj_en_1 ? in_4 : w_kill_out1;
    assign out_2 = inj_en_2 ? in_4 : w_kill_out2;
    assign out_3 = inj_en_3 ? in_4 : w_kill_out3;
    
    // compute the ppv of local flits
    wire [`NUM_PORT-1:0] m_ppv4;
    rc localrc(
    .dst                (in_4[`DST_POS]),
    .dstList            (in_4[`DST_LIST_POS]),
    .mc                 (in_4[`MC_POS]),
    .preferPortVector   (m_ppv4)
    );
    
    assign ppv_out_0 = inj_en_0 ? m_ppv4[3:0] : w_kill_ppv0;
    assign ppv_out_1 = inj_en_1 ? m_ppv4[3:0] : w_kill_ppv1;
    assign ppv_out_2 = inj_en_2 ? m_ppv4[3:0] : w_kill_ppv2;
    assign ppv_out_3 = inj_en_3 ? m_ppv4[3:0] : w_kill_ppv3;
     
    assign num_flit_o = num_flit_i + inj_en_0 + inj_en_1 + inj_en_2 + inj_en_3 - out_4 [`VALID_POS] - merge[0] - merge[1] - merge[2] - merge [3]; // do not consider merged local flit
 
endmodule
`endif

`ifdef CARPOOL_LK_AHEAD_RC_PS
module local(
    in_0,
    in_1,
    in_2,
    in_3,
    in_4,
    merge,
    num_flit_i,
    ppv_0,
    ppv_1,
    ppv_2,
    ppv_3,
    out_0,
    out_1,
    out_2,
    out_3,
    out_4,
    num_flit_o
    );
    
    input [`DATA_WIDTH-1:0] in_0, in_1, in_2, in_3, in_4;
    input [`NUM_PORT-1:0] ppv_0, ppv_1, ppv_2, ppv_3;
    input [`NUM_PORT-1:0] merge;
    input [`PC_INDEX_WIDTH-1:0] num_flit_i;
    output [`PC_INDEX_WIDTH-1:0] num_flit_o;
    output [`DATA_WIDTH-1:0] out_0, out_1, out_2, out_3, out_4;
    
    wire [`TIME_WIDTH-1:0] time_0, time_1, time_2, time_3, eject_time_1, eject_time_2;
    wire cmp_0_1, cmp_2_3, cmp_last;
    wire [`PC_INDEX_WIDTH-1:0] eject_sel_1, eject_sel_2, eject_sel_3;
    wire inj_en_0, inj_en_1, inj_en_2, inj_en_3;
    wire [`DATA_WIDTH-1:0] w_out_0, w_out_1, w_out_2, w_out_3;
    
    
    // eject flit
    assign time_0 = ppv_0[4] && ~merge [0] ? in_0[`TIME_POS] : `MAX_TIME;
    assign time_1 = ppv_1[4] && ~merge [1] ? in_1[`TIME_POS] : `MAX_TIME;
    assign time_2 = ppv_2[4] && ~merge [2] ? in_2[`TIME_POS] : `MAX_TIME;
    assign time_3 = ppv_3[4] && ~merge [3] ? in_3[`TIME_POS] : `MAX_TIME;
    
    assign cmp_0_1 = time_0 <= time_1;    
    assign cmp_2_3 = time_2 <= time_3;    
    
    assign eject_sel_1 = cmp_0_1 ? 3'd0 : 3'd1;
    assign eject_time_1 = cmp_0_1 ? time_0 : time_1;
    assign eject_sel_2 = cmp_2_3 ? 3'd2 : 3'd3;
    assign eject_time_2 = cmp_2_3 ? time_2 : time_3;
    
    assign cmp_last = eject_time_1 <= eject_time_2;
    assign eject_sel_3 = ((eject_time_1 & eject_time_2) == `MAX_TIME) ? `NULL_PC : 
                         cmp_last ? eject_sel_1 : eject_sel_2;
    
    assign out_4 = (eject_sel_3 == 0) ? in_0 : 
                   (eject_sel_3 == 1) ? in_1 :
                   (eject_sel_3 == 2) ? in_2 :
                   (eject_sel_3 == 3) ? in_3 :
                   'h0;
    
    // inject
    // note: ejection before injection can boost up throughput
    // Provided that a injection is not granted at lower channel, it is granted to a channel if either 
    // 1) channel has no flit 2) channel has flit which is merged 3) channel has flit which is ejected.
    assign inj_en_0 = (~in_0[`VALID_POS] || merge [0] || eject_sel_3 == 0) && in_4[`VALID_POS] && ~merge[4]; // local injection is granted provided it is not merged.
    assign inj_en_1 = ((~in_1[`VALID_POS] || merge [1] || eject_sel_3 == 1) && ~inj_en_0) && in_4[`VALID_POS] && ~merge[4];
    assign inj_en_2 = ((~in_2[`VALID_POS] || merge [2] || eject_sel_3 == 2) && ~inj_en_1 && ~inj_en_0) && in_4[`VALID_POS] && ~merge[4];
    assign inj_en_3 = ((~in_3[`VALID_POS] || merge [3] || eject_sel_3 == 3) && ~inj_en_2 && ~inj_en_1 && ~inj_en_0) && in_4[`VALID_POS] && ~merge[4];
    
    assign w_out_0 = inj_en_0 ? in_4 : in_0;
    assign w_out_1 = inj_en_1 ? in_4 : in_1;
    assign w_out_2 = inj_en_2 ? in_4 : in_2;
    assign w_out_3 = inj_en_3 ? in_4 : in_3;
    
    
    // Internally, we use ppv filed as allocatedPPV.  
    // Ejection doesn't mean the flit can be nullified. 
    // It may request for several port          
    assign out_0 = ((eject_sel_3 == 0 && ~|ppv_0[3:0])|| merge[0] && ~inj_en_0) ? 'h0 : {w_out_0[`DATA_WIDTH-1:`PPV_END+1], eject_sel_3 == 0, w_out_0 [`PPV_START+4-1:0]}; // only update the local ejecjt bit of ppv
    assign out_1 = ((eject_sel_3 == 1 && ~|ppv_1[3:0]) || merge[1] && ~inj_en_1) ? 'h0 : {w_out_1[`DATA_WIDTH-1:`PPV_END+1], eject_sel_3 == 1, w_out_1 [`PPV_START+4-1:0]};
    assign out_2 = ((eject_sel_3 == 2 && ~|ppv_2[3:0]) || merge[2] && ~inj_en_2) ? 'h0 : {w_out_2[`DATA_WIDTH-1:`PPV_END+1], eject_sel_3 == 2, w_out_2 [`PPV_START+4-1:0]};
    assign out_3 = ((eject_sel_3 == 3 && ~|ppv_3[3:0])|| merge[3] && ~inj_en_3) ? 'h0 : {w_out_3[`DATA_WIDTH-1:`PPV_END+1], eject_sel_3 == 3, w_out_3 [`PPV_START+4-1:0]};
    
    assign num_flit_o = num_flit_i + inj_en_0 + inj_en_1 + inj_en_2 + inj_en_3 - out_4 [`VALID_POS] - merge[0] - merge[1] - merge[2] - merge [3]; // do not consider merged local flit
 
endmodule
`endif // CARPOOL_LK_AHEAD_RC_PS 
