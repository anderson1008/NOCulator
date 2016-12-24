`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/18/2016 10:56:00 PM
// Design Name: 
// Module Name: merge
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


module merge(
    hs_0,
    srcList_0,
    addr_0,   //could be flowID
    dst_0,
    flitID_0,
    hs_1,
    srcList_1,
    addr_1,   //could be flowID
    dst_1,
    flitID_1,
    hs_2,
    srcList_2,
    addr_2,   //could be flowID
    dst_2,
    flitID_2,
    hs_3,
    srcList_3,
    addr_3,   //could be flowID
    dst_3,
    flitID_3,
    hs_4,
    srcList_4,
    addr_4,   //could be flowID
    dst_4,
    flitID_4,
    kill,
    srcList_new_0,      
    srcList_new_1,      
    srcList_new_2,      
    srcList_new_3,      
    srcList_new_4      
    );
    
    input hs_0, hs_1, hs_2, hs_3, hs_4;
    input [`SRC_LIST_WIDTH-1:0] srcList_0, srcList_1, srcList_2, srcList_3, srcList_4;
    input [`MEM_ADDR_WIDTH-1:0] addr_0, addr_1, addr_2, addr_3, addr_4;
    input [`DST_WIDTH-1:0] dst_0, dst_1, dst_2, dst_3, dst_4;
    input [`NUM_FLIT_WDITH-1:0] flitID_0, flitID_1, flitID_2, flitID_3, flitID_4;
    output [`NUM_PORT-1:0] kill;
    output [`SRC_LIST_WIDTH-1:0] srcList_new_0, srcList_new_1, srcList_new_2, srcList_new_3, srcList_new_4;
    
    // merge[i][j] = 1 indicate flit at port j merge with port i
    wire [`NUM_PORT-1:0] merge [0:`NUM_PORT-2];
    
    assign merge[0][0] = 1'b0;
    assign merge[0][1] = hs_0 && hs_1 && (addr_0 == addr_1) && (dst_0 == dst_1) && (flitID_0 == flitID_1);
    assign merge[0][2] = hs_0 && hs_2 && (addr_0 == addr_2) && (dst_0 == dst_2) && (flitID_0 == flitID_2); 
    assign merge[0][3] = hs_0 && hs_3 && (addr_0 == addr_3) && (dst_0 == dst_3) && (flitID_0 == flitID_3); 
    assign merge[0][4] = hs_0 && hs_4 && (addr_0 == addr_4) && (dst_0 == dst_4) && (flitID_0 == flitID_4); 

    assign merge[1][0] = 1'b0;
    assign merge[1][1] = 1'b0;
    assign merge[1][2] = hs_1 && hs_2 && (addr_1 == addr_2) && (dst_1 == dst_2) && (flitID_1 == flitID_2); 
    assign merge[1][3] = hs_1 && hs_3 && (addr_1 == addr_3) && (dst_1 == dst_3) && (flitID_1 == flitID_3); 
    assign merge[1][4] = hs_1 && hs_4 && (addr_1 == addr_4) && (dst_1 == dst_4) && (flitID_1 == flitID_4);     
    
    assign merge[2][0] = 1'b0;
    assign merge[2][1] = 1'b0;
    assign merge[2][2] = 1'b0; 
    assign merge[2][3] = hs_2 && hs_3 && (addr_2 == addr_3) && (dst_2 == dst_3) && (flitID_2 == flitID_3); 
    assign merge[2][4] = hs_2 && hs_4 && (addr_2 == addr_4) && (dst_2 == dst_4) && (flitID_2 == flitID_4);
    
    assign merge[3][0] = 1'b0;
    assign merge[3][1] = 1'b0;
    assign merge[3][2] = 1'b0; 
    assign merge[3][3] = 1'b0; 
    assign merge[3][4] = hs_3 && hs_4 && (addr_3 == addr_4) && (dst_3 == dst_4) && (flitID_3 == flitID_4);
            
    assign kill [0] = 1'b0;
    assign kill [1] = merge[0][1];
    assign kill [2] = merge[0][2] | merge[1][2];
    assign kill [3] = merge[0][3] | merge[1][3] | merge[2][3];
    assign kill [4] = merge[0][4] | merge[1][4] | merge[2][4] | merge[3][4];
    
    // if a flit is merged with any other flit, use the updated srcList.
    assign srcList_new_0 = merge[0][1] ? (srcList_0 | srcList_1) :
                           merge[0][2] ? (srcList_0 | srcList_2) :
                           merge[0][3] ? (srcList_0 | srcList_3) :
                           merge[0][4] ? (srcList_0 | srcList_4) : srcList_0;
    assign srcList_new_1 = merge[1][2] ? (srcList_1 | srcList_2) :
                           merge[1][3] ? (srcList_1 | srcList_3) :
                           merge[1][4] ? (srcList_1 | srcList_4) : srcList_1;
    assign srcList_new_2 = merge[2][3] ? (srcList_2 | srcList_3) :
                           merge[2][4] ? (srcList_2 | srcList_4) : srcList_2;
    assign srcList_new_3 = merge[3][4] ? (srcList_3 | srcList_4) : srcList_3;
    assign srcList_new_4 = srcList_4;

endmodule
