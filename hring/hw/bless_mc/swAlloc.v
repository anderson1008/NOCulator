`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 12/19/2016 12:22:57 AM
// Design Name: 
// Module Name: swAlloc
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


`ifdef BLESS
// To enable parallel port allocator, set PARALLEL_PA in global.vh
module swAlloc # (parameter PARALLEL = 1'b0) (
    numFlit_in,
    ppv_0,
    ppv_1,
    ppv_2,
    ppv_3,
    allocPV_0,
    allocPV_1,
    allocPV_2,
    allocPV_3
    );
    
    input [`PC_INDEX_WIDTH-1:0] numFlit_in;
    input [`NUM_PORT-2:0] ppv_0, ppv_1, ppv_2, ppv_3;
    output [`NUM_PORT-2:0] allocPV_0, allocPV_1, allocPV_2, allocPV_3; 

    generate
    if (PARALLEL == 1'b0) begin : seqAlloc
        wire [`NUM_PORT-2:0] w_availPort [1:3];
        wire [`PC_INDEX_WIDTH-1:0] w_numFlit [1:3];
     
        seqPortAlloc rank0PortAlloc(
        .numFlit_in             (numFlit_in),
        .availPortVector_in     (4'b1111),
        .ppv                    (ppv_0),
        .allocatedPortVector    (allocPV_0),
        .availPortVector_out    (w_availPort[1]),
        .numFlit_out            (w_numFlit[1])
        );
      
                             
        seqPortAlloc rank1PortAlloc(
        .numFlit_in             (w_numFlit[1]),
        .availPortVector_in     (w_availPort[1]),
        .ppv                    (ppv_1),
        .allocatedPortVector    (allocPV_1),
        .availPortVector_out    (w_availPort[2]),
        .numFlit_out            (w_numFlit[2])
        );    
        
    
        seqPortAlloc rank02ortAlloc(
        .numFlit_in             (w_numFlit[2]),
        .availPortVector_in     (w_availPort[2]),
        .ppv                    (ppv_2),
        .allocatedPortVector    (allocPV_2),
        .availPortVector_out    (w_availPort[3]),
        .numFlit_out            (w_numFlit[3])
        );
    
        seqPortAlloc rank3PortAlloc(
        .numFlit_in             (w_numFlit[3]),
        .availPortVector_in     (w_availPort[3]),
        .ppv                    (ppv_3),
        .allocatedPortVector    (allocPV_3),
        .availPortVector_out    (),
        .numFlit_out            ()
        );
    end
    
    else begin : paraAlloc
    
       // not implemetned;
       // refer to IPDPS'16 DeC paper  
    end
    endgenerate   
endmodule
`endif // BLESS


`ifdef CARPOOL
// To enable parallel port allocator, set PARALLEL_PA in global.vh
module swAlloc # (parameter PARALLEL = 1'b0) (
    mc_0,
    mc_1,
    mc_2,
    mc_3,
    numFlit_in,
    ppv_0,
    ppv_1,
    ppv_2,
    ppv_3,
    allocPV_0,
    allocPV_1,
    allocPV_2,
    allocPV_3
    );
    
    input mc_0, mc_1, mc_2, mc_3;
    input [`PC_INDEX_WIDTH-1:0] numFlit_in;
    input [`NUM_PORT-2:0] ppv_0, ppv_1, ppv_2, ppv_3;
    output [`NUM_PORT-2:0] allocPV_0, allocPV_1, allocPV_2, allocPV_3; 

    generate
    if (PARALLEL == 1'b0) begin : seqAlloc
        wire [`NUM_PORT-2:0] w_availPort [1:3];
        wire [`PC_INDEX_WIDTH-1:0] w_numFlit [1:3];
     
        seqPortAlloc rank0PortAlloc(
        .mc                     (mc_0),
        .numFlit_in             (numFlit_in),
        .availPortVector_in     (4'b1111),
        .ppv                    (ppv_0),
        .allocatedPortVector    (allocPV_0),
        .availPortVector_out    (w_availPort[1]),
        .numFlit_out            (w_numFlit[1])
        );
      
                             
        seqPortAlloc rank1PortAlloc(
        .mc                     (mc_1),
        .numFlit_in             (w_numFlit[1]),
        .availPortVector_in     (w_availPort[1]),
        .ppv                    (ppv_1),
        .allocatedPortVector    (allocPV_1),
        .availPortVector_out    (w_availPort[2]),
        .numFlit_out            (w_numFlit[2])
        );    
        
    
        seqPortAlloc rank02ortAlloc(
        .mc                     (mc_2),
        .numFlit_in             (w_numFlit[2]),
        .availPortVector_in     (w_availPort[2]),
        .ppv                    (ppv_2),
        .allocatedPortVector    (allocPV_2),
        .availPortVector_out    (w_availPort[3]),
        .numFlit_out            (w_numFlit[3])
        );
    
        seqPortAlloc rank3PortAlloc(
        .mc                     (mc_3),
        .numFlit_in             (w_numFlit[3]),
        .availPortVector_in     (w_availPort[3]),
        .ppv                    (ppv_3),
        .allocatedPortVector    (allocPV_3),
        .availPortVector_out    (),
        .numFlit_out            ()
        );
    end
    
    else begin : paraAlloc
    
        wire [`NUM_PORT-2:0]  apv_p_0, apv_p_1, apv_p_2, apv_p_3, ppv_p_0, ppv_p_1, ppv_p_2, ppv_p_3;

        paraPortAlloc_st1 paraPA_st1_ch0(
        .mc             (mc_0),
        .treat_as_uc    (1'b0),
        .numFlit_in     (numFlit_in),
        .ppv            (ppv_0),
        .ppv_others     ({ppv_1, ppv_2, ppv_3}),
        .apv_p          (apv_p_0),
        .ppv_p          (ppv_p_0)
        );
        
        paraPortAlloc_st1 paraPA_st1_ch1(
        .mc             (mc_1),
        .treat_as_uc    (mc_0),
        .numFlit_in     (numFlit_in),        
        .ppv            (ppv_1),
        .ppv_others     ({ppv_0, ppv_2, ppv_3}),
        .apv_p          (apv_p_1),
        .ppv_p          (ppv_p_1)
        );

        paraPortAlloc_st1 paraPA_st1_ch2(
        .mc             (mc_2),
        .treat_as_uc    (mc_0|mc_1),
        .numFlit_in     (numFlit_in),        
        .ppv            (ppv_2),
        .ppv_others     ({ppv_0, ppv_1, ppv_3}),
        .apv_p          (apv_p_2),
        .ppv_p          (ppv_p_2)
        );    

        paraPortAlloc_st1 paraPA_st1_ch3(
        .mc             (mc_3),
        .treat_as_uc    (mc_0|mc_1|mc_2),
        .numFlit_in     (numFlit_in),                
        .ppv            (ppv_3),
        .ppv_others     ({ppv_0, ppv_1, ppv_2}),
        .apv_p          (apv_p_3),
        .ppv_p          (ppv_p_3)
        ); 
        
        paraPortAlloc_st2 # (.CH_INDEX (2'd0)) paraPA_st2_ch0(
        .apv_p_0        (apv_p_0),
        .apv_p_1        (apv_p_1),
        .apv_p_2        (apv_p_2),
        .apv_p_3        (apv_p_3),
        .ppv_p_0        (ppv_p_0),
        .ppv_p_1        (ppv_p_1),
        .ppv_p_2        (ppv_p_2),
        .ppv_p_3        (ppv_p_3),
        .apv            (allocPV_0)
        );   

        paraPortAlloc_st2 # (.CH_INDEX (2'd1)) paraPA_st2_ch1(
        .apv_p_0        (apv_p_0),
        .apv_p_1        (apv_p_1),
        .apv_p_2        (apv_p_2),
        .apv_p_3        (apv_p_3),
        .ppv_p_0        (ppv_p_0),
        .ppv_p_1        (ppv_p_1),
        .ppv_p_2        (ppv_p_2),
        .ppv_p_3        (ppv_p_3),
        .apv            (allocPV_1)
        );

        paraPortAlloc_st2 # (.CH_INDEX (2'd2)) paraPA_st2_ch2(
        .apv_p_0        (apv_p_0),
        .apv_p_1        (apv_p_1),
        .apv_p_2        (apv_p_2),
        .apv_p_3        (apv_p_3),
        .ppv_p_0        (ppv_p_0),
        .ppv_p_1        (ppv_p_1),
        .ppv_p_2        (ppv_p_2),
        .ppv_p_3        (ppv_p_3),
        .apv            (allocPV_2)
        );
        
        paraPortAlloc_st2 # (.CH_INDEX (2'd3)) paraPA_st2_ch3(
        .apv_p_0        (apv_p_0),
        .apv_p_1        (apv_p_1),
        .apv_p_2        (apv_p_2),
        .apv_p_3        (apv_p_3),
        .ppv_p_0        (ppv_p_0),
        .ppv_p_1        (ppv_p_1),
        .ppv_p_2        (ppv_p_2),
        .ppv_p_3        (ppv_p_3),
        .apv            (allocPV_3)
        );  
              
    end
    endgenerate
        
endmodule
`endif // CARPOOL