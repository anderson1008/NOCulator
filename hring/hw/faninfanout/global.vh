`timescale 1ns / 1ps

`define     NETWORK_SIZE    64
`define     NUM_PORT        5
`define     CURR_X          3
`define     CURR_Y          3
`define     COOR_X_N        `CURR_X
`define     COOR_Y_N        `CURR_Y+1
`define     COOR_X_E        `CURR_X
`define     COOR_Y_E        `CURR_Y
`define     COOR_X_S        `CURR_X
`define     COOR_Y_S        `CURR_Y-1
`define     COOR_X_W        `CURR_X-1
`define     COOR_Y_W        `CURR_Y
`define     NODE_LTB        1'b1
`define     NODE_RTB        1'b1
// VC arbiter is hard-coded   for 4VCs each port
`define     NUM_VC          6       // number of VC; affect the arb structure
`define     VC_INDEX_WIDTH  3       // VC4 is master hs flit buffer, VC5 is the incoming flit
`define     PC_INDEX_WIDTH  3
`define     NULL_PC         5

// Mask in each router (taking one for example)
// STRAIGHT_N means the mask of the STRAIGHT directed port in the next node when flit travel through port N at the current node
// LEFT_N means the node mask of the LEFT-turn port (i.e. West) in the next node when the flit travels through port N at the current node
`define     STRAIGHT_N    `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     STRAIGHT_E    `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     STRAIGHT_S    `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     STRAIGHT_W    `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     LEFT_N        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     LEFT_E        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     LEFT_S        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     LEFT_W        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     RIGHT_N       `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     RIGHT_E       `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     RIGHT_S       `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     RIGHT_W       `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     LOCAL_N        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     LOCAL_E        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     LOCAL_S        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     LOCAL_W        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF


// flit format
// Add hs Flow ID
//  |    ------------------------------------          Header         ------------------------------------  |  ------------------ Payload ---------------
//  LTB, RTB, preferredVector (bypass), time   hs, mc,sectorID, mshrID, pktSize, flitNum, src_y_coord, src_x_coord, dst_y_coord, dst_x_coord, vc, |  dst/src List or hi-64 payload, lo-64 payload
//  1     1   5                          8     1  1   4          6       3         3         3           3           3               3          2 |         64                     64
// payload field will be shared by srcList/dstList (64-bit)

`define     HEADER_WIDTH    47
`define     ADVANCE_WIDTH   0
`define     PAYLOAD_WIDTH   128
`define     SRC_LIST_WIDTH  `NETWORK_SIZE
`define     LO_PAYLOAD_POS  63:0
`define     SRC_LIST_POS    64+`NETWORK_SIZE-1:64
`define     SRC_END         64+`NETWORK_SIZE // end of source field, temporarily used in merge_buf.v
`define     DST_LIST_WIDTH  `NETWORK_SIZE 
`define     DST_LIST_POS    `SRC_LIST_POS  
`define     VC_LEFT_POS     130 //use to substitute new VC
`define     VC_RIGHT_POS    127 //use to substitute new VC
`define     VC_POS          129:128
`define     DST_WIDTH       6
`define     DST_POS         135:130
`define     DST_X_COORD     132:130
`define     DST_Y_COORD     135:133
`define     SRC_WIDTH       6
`define     SRC_X_COORD     138:136
`define     SRC_Y_COORD     141:139
`define     FLIT_NUM_POS    144:142
`define     PKT_SIZE_POS    147:145
`define     MSHR_ID_POS     153:148
`define     SECTOR_ID_POS   157:154
`define     MC_POS          158
`define     HS_POS          159
`define     TIME_WIDTH      8           // width of the time stamp
`define     TIME_POS        167:160
`define     PPV_START       168
`define     PPV_POS         172:168
`define     PPV_END         172
`define     AD_RTB          173
`define     AD_LTB          174

`define     MEM_ADDR_WIDTH  42
`define     MEM_ADDR_POS    63:64-`MEM_ADDR_WIDTH
`define     X_COORD         2:0
`define     Y_COORD         5:3
// Internally, we need to append precomputed 5bit PPV and 2-bit turn bit associated with each projected outport
// in total 7 * 4 = 28 bits are appended 
// if PPV is 0, we consider the flit as null flit
`define     IR_WIDTH         28
`define     IR_DATA_WIDTH   `DATA_WIDTH+`IR_WIDTH
`define     PRE_ROUTE_POS   `IR_DATA_WIDTH-1:`DATA_WIDTH // precomputed routing position

`define     DATA_WIDTH_XBAR `DATA_WIDTH

`define     ADDR_WIDTH      2
`define     RAM_DEPTH       (1 << `ADDR_WIDTH)
`define     DATA_WIDTH      `HEADER_WIDTH+`PAYLOAD_WIDTH