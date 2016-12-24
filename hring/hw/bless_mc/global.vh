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
`define     PC_INDEX_WIDTH  3
`define     NULL_PC         5

`define     MEM_ADDR_WIDTH  42
`define     MEM_ADDR_POS    63:64-`MEM_ADDR_WIDTH
`define     X_COORD         2:0
`define     Y_COORD         5:3

// i_MASK_j
// j MASK of intermediate node connected to port i 
`define     N_MASK_N        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     N_MASK_E        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     N_MASK_W        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     N_MASK_L        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     E_MASK_N        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     E_MASK_E        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     E_MASK_S        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     E_MASK_L        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     S_MASK_E        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     S_MASK_S        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     S_MASK_W        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     S_MASK_L        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     W_MASK_N        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     W_MASK_S        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     W_MASK_W        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF
`define     W_MASK_L        `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF

`define     N_MASK          `N_MASK_N | `N_MASK_E | `N_MASK_W | `N_MASK_L
`define     E_MASK          `E_MASK_N | `E_MASK_E | `E_MASK_S | `E_MASK_L
`define     S_MASK          `S_MASK_E | `S_MASK_S | `S_MASK_W | `S_MASK_L
`define     W_MASK          `W_MASK_N | `W_MASK_S | `W_MASK_W | `W_MASK_L
`define     L_MASK          `NETWORK_SIZE'hFFFF_FFFF_FFFF_FFFF


// packet format
// Header 
// timeStamp(advanced), ppv, hs, mc, requesterID, mshrID, pktSize, flitSeqNum, dst/(sectorID, reserved), dstList/srcList, payload 
// 8                    5    1   1   6            6       3/4      3/4         6/4                       64               64
// we conservatively assume pktSize and flitSeqNum are 4-bit and sectorID is also 4-bit
// the last bit of payload is used as valid bit (which is usually contained in the cache line)
// time is advanced signals
`define     DATA_WIDTH      `HEADER_WIDTH+`PAYLOAD_WIDTH
`define     HEADER_WIDTH    43 // 4-bit are conservatively provisioned. Also for ease of programming
`define     PAYLOAD_WIDTH   128

// Internally, we need to append precomputed 5bit PPV associated with each projected outport
// in total 5 * 4 = 20 bits are appended 
`define     IR_WIDTH         20
`define     IR_DATA_WIDTH   `DATA_WIDTH+`IR_WIDTH
`define     PRE_ROUTE_POS   `IR_DATA_WIDTH-1:`DATA_WIDTH // precomputed routing position


`define     SRC_LIST_WIDTH  `NETWORK_SIZE
`define     DST_LIST_WIDTH  `NETWORK_SIZE
`define     SRC_LIST_POS    64+`NETWORK_SIZE-1:64 
`define     SRC_END         64+`NETWORK_SIZE 
`define     DST_LIST_POS    `SRC_LIST_POS  
`define     LO_PAYLOAD_POS  63:0
`define     VALID_POS       127

`define     DST_POS         133:128
`define     DST_X_COORD     130:128
`define     DST_Y_COORD     133:131
`define     DST_WIDTH       6
`define     FLIT_NUM_POS    136:133
`define     PKT_SIZE_POS    140:137
`define     NUM_FLIT_WDITH  4
`define     MC_POS          152
`define     HS_POS          153
`define     PPV_START       154
`define     PPV_POS         158:154     // internally, we use this field to carry allocatedPPV
`define     NL_PPV_POS      157:154     // non-local ppv
`define     LOCAL_PV_POS    158
`define     PPV_END         158
`define     TIME_WIDTH      8           // width of the time stamp
`define     TIME_POS        166:159
`define     MAX_TIME        255

// Component Config
// Permutation Network
`define     PERM_WIDTH      2 + `TIME_WIDTH // 2-bit indir + time width
//Xbar
`define     DATA_WIDTH_XBAR `IR_DATA_WIDTH

