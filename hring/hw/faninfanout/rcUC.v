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
    
    reg [`NUM_PORT-1:0] preferPortVector;
    
    always @ * begin
        
        preferPortVector = `NUM_PORT'd0;
        
        if (dst_x > neigh_coord_x) 
            preferPortVector[1] = 1'b1;
        else if (dst_x < neigh_coord_x)
            preferPortVector[3] = 1'b1;
        else if (dst_y > neigh_coord_y)
            preferPortVector[0] = 1'b1;
        else if (dst_y < neigh_coord_y)
            preferPortVector[2] = 1'b1;
        else if (dst_x == neigh_coord_x && dst_y == neigh_coord_y)
            preferPortVector[4] = 1'b1;
        else
            preferPortVector = `NUM_PORT'd0;;
    
    end
    
    
    
endmodule
