`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/20/2016 07:27:29 AM
// Design Name: 
// Module Name: paraPortAlloc_st1
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

module paraPortAlloc_st1(
    mc,
    treat_as_uc,
    ppv,
    ppv_others,
    numFlit_in,
    apv_p,
    ppv_p
    );
    
    input [`NUM_PORT-2:0] ppv;
    input [(`NUM_PORT-1)*3-1:0] ppv_others;
    input mc, treat_as_uc;
    input [`PC_INDEX_WIDTH-1:0] numFlit_in;

    output [`NUM_PORT-2:0] apv_p, ppv_p;
    
    wire [`NUM_PORT-2:0] ppv_o [0:2];
    wire [`NUM_PORT-2:0] m_apv_p, m_apv_p_mc, m_treat_as_uc;
    wire [`PC_INDEX_WIDTH-1:0] numFlit_fork;

    assign ppv_o [0] = ppv_others [0+:4];
    assign ppv_o [1] = ppv_others [4+:4];
    assign ppv_o [2] = ppv_others [8+:4];
    
    genvar i;
    generate  
        for (i=0; i<`NUM_PORT-1; i=i+1) begin : non_conflict_productive   
            assign m_apv_p [i] = ppv[i] && ~ppv_o[0][i] && ~ppv_o[1][i] && ~ppv_o[2][i];
        end    
    endgenerate
    
    assign numFlit_fork = 5 - numFlit_in;
    assign m_apv_p_mc [0] = m_apv_p[0];
    assign m_apv_p_mc [1] = numFlit_fork - m_apv_p[0] > 0 ? m_apv_p[1] : 1'b0;
    assign m_apv_p_mc [2] = numFlit_fork - m_apv_p[0] - m_apv_p[1] > 0 ? m_apv_p[2] : 1'b0;
    assign m_apv_p_mc [3] = numFlit_fork - m_apv_p[0] - m_apv_p[1] - m_apv_p[2] > 0 ? m_apv_p[3] : 1'b0;
    
    assign m_treat_as_uc = m_apv_p [0] ? 4'b0001 : m_apv_p [1] ? 4'b0010 : m_apv_p [2] ? 4'b0100 : m_apv_p [3] ? 4'b1000 : 4'b0;
    
    assign apv_p = (treat_as_uc | ~mc) ? m_treat_as_uc : m_apv_p_mc;
    assign ppv_p = mc ? (|apv_p ? 4'h0: apv_p ^ ppv) : m_treat_as_uc ^ ppv;
    
endmodule
