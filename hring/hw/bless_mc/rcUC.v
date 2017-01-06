`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/10/2016 01:51:14 PM
// Design Name: 
// Module Name: rcUC
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
    `define DIMENTION_ORDER
`elsif BLESS
    `define DIMENTION_ORDER
`endif

`ifdef DIMENTION_ORDER
module rcUC(
    dst,
    preferPortVector
    );
    
    input [`DST_WIDTH-1:0] dst;
    output [`NUM_PORT-1:0] preferPortVector;
    
    wire [2:0] dst_x, dst_y;
       
    assign dst_x = dst[`X_COORD];
    assign dst_y = dst[`Y_COORD];

    assign preferPortVector [1] = dst_x > `CURR_X;
    assign preferPortVector [3] = dst_x < `CURR_X;
    assign preferPortVector [0] = (dst_y > `CURR_Y) && ~preferPortVector [1] && ~preferPortVector [3];
    assign preferPortVector [2] = (dst_y < `CURR_Y) && ~preferPortVector [1] && ~preferPortVector [3];
    assign preferPortVector [4] = (dst_x == `CURR_X) && (dst_y == `CURR_Y);    
    
endmodule
`endif  // DIMENTION_ORDER

`ifdef CARPOOL_LK_AHEAD_RC_PS

module rcUC(
    dst,
    outdir,
    preferPortVector
    );
    
    input [`DST_WIDTH-1:0] dst;
    input [`PC_INDEX_WIDTH-2:0] outdir;
    output [`NUM_PORT-1:0] preferPortVector;
    
    wire [2:0] dst_x, dst_y, neigh_coord_x, neigh_coord_y;
       
    assign dst_x = dst[`X_COORD];
    assign dst_y = dst[`Y_COORD];
    
    assign neigh_coord_x =  (outdir == 0) ? `COOR_X_N : 
                            (outdir == 1) ? `COOR_X_E : 
                            (outdir == 2) ? `COOR_X_S : 
                            `COOR_X_W ;
    assign neigh_coord_y =  (outdir == 0) ? `COOR_Y_N :
                            (outdir == 1) ? `COOR_Y_E : 
                            (outdir == 2) ? `COOR_Y_S : 
                            `COOR_Y_W ;

    assign preferPortVector [1] = dst_x > neigh_coord_x;
    assign preferPortVector [3] = dst_x < neigh_coord_x;
    assign preferPortVector [0] = (dst_y > neigh_coord_y) && ~preferPortVector [1] && ~preferPortVector [3];
    assign preferPortVector [2] = (dst_y < neigh_coord_y) && ~preferPortVector [1] && ~preferPortVector [3];
    assign preferPortVector [4] = (neigh_coord_x && dst_y) == neigh_coord_y;    

endmodule  
`endif // CARPOOL_LK_AHEAD_RC_PS

