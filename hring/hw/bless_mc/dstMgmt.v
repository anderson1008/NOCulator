`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/20/2016 09:26:23 PM
// Design Name: 
// Module Name: dstMgmt
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


module dstMgmt(
    allocPV,
    dstList_in,
    dstList_out
    );
    
    // TODO: Should consider if local ejection fails
    // need to preserve the destinatio
    input [`NUM_PORT-1:0] allocPV;
    input [`DST_LIST_WIDTH-1:0] dstList_in;
    output [`DST_LIST_WIDTH-1:0] dstList_out;
    
    parameter OUTDIR = 0;
    
    wire [`DST_LIST_WIDTH-1:0] mask_out_port;
    wire replica;
    wire w_replica [0:OUTDIR+1];
    
    genvar i;
    generate
        // construct the mask for mc flit
        assign mask_out_port =~((allocPV[0] ? `N_MASK : 'h0) | 
                                (allocPV[1] ? `E_MASK : 'h0) |
                                (allocPV[2] ? `S_MASK : 'h0) |
                                (allocPV[3] ? `W_MASK : 'h0) |
                                (allocPV[4] ? `L_MASK : 'h0));
        
        assign w_replica[0] = 1'b0;

        for (i=0; i<OUTDIR+1; i=i+1) begin: flit_is_replica
            // determine if the flit is a replica
            assign w_replica[i+1] = w_replica [i] || allocPV[i];
        end
        
        assign replica = w_replica[OUTDIR+1];
        
        if (OUTDIR == 0)
            assign dstList_out = dstList_in & (replica ? ~(`N_MASK) : mask_out_port);
        else if (OUTDIR == 1)
            assign dstList_out = dstList_in & (replica ? ~(`E_MASK) : mask_out_port);
        else if (OUTDIR == 2)
            assign dstList_out = dstList_in & (replica ? ~(`S_MASK) : mask_out_port);
        else if (OUTDIR == 3)
            assign dstList_out = dstList_in & (replica ? ~(`W_MASK) : mask_out_port);
    
    endgenerate
    
    
    
endmodule
