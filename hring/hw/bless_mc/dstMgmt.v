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

`ifdef CARPOOL_LK_AHEAD_RC_PS
    `define DST_MGMT
`elsif CARPOOL
    `define DST_MGMT
`endif

`ifdef DST_MGMT
module dstMgmt(
    allocPV,
    dstList_in,
    dstList_out
    );
    
    input [`NUM_PORT-1:0] allocPV;
    input [`DST_LIST_WIDTH-1:0] dstList_in;
    output [`DST_LIST_WIDTH-1:0] dstList_out;
    
    parameter OUTDIR = 0;
    
    wire [`DST_LIST_WIDTH-1:0] mask_out_port;
    wire replica;
    wire [`NUM_PORT-1:0] w_replica;
    wire [`NUM_PORT-1:0] first;
  
  
    assign w_replica[0] = 1'b0;
    assign first[0] = 1'b1;
    //assign forked[0] = 1'b0;  
    genvar i;
    generate

        for (i=0; i<`NUM_PORT-1; i=i+1) begin: flit_is_replica
            // determine if the flit is a replica
            // the flit on the first allocated port is not a replica
            assign first [i+1] = first [i] ? ~allocPV[i] : 1'b0;
            assign w_replica[i+1] = first [i+1] ? 1'b0 : (w_replica [i] || allocPV[i]);
        end
        
    endgenerate
    
    assign replica = w_replica[OUTDIR];

    // construct the mask for mc flit
    assign mask_out_port =~((w_replica[0] ? `N_MASK : 'h0) | 
                          (w_replica[1] ? `E_MASK : 'h0) |
                          (w_replica[2] ? `S_MASK : 'h0) |
                          (w_replica[3] ? `W_MASK : 'h0) |
                          (w_replica[4] ? `L_MASK : 'h0));
    
    if (OUTDIR == 0)
        assign dstList_out = dstList_in & (replica ? `N_MASK : mask_out_port);
    else if (OUTDIR == 1)
        assign dstList_out = dstList_in & (replica ? `E_MASK : mask_out_port);
    else if (OUTDIR == 2)
        assign dstList_out = dstList_in & (replica ? `S_MASK : mask_out_port);
    else if (OUTDIR == 3)
        assign dstList_out = dstList_in & (replica ? `W_MASK : mask_out_port);
    
endmodule
`endif // DST_MGMT