`timescale 1ns / 1ps
//////////////////////////////////////////////////////////////////////////////////
// Company: 
// Engineer: 
// 
// Create Date: 01/01/2017 10:56:26 AM
// Design Name: 
// Module Name: paraPortAlloc_st2
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

module paraPortAlloc_st2 # (parameter CH_INDEX = 2'd3) (
    apv_p_0,
    apv_p_1,
    apv_p_2,
    apv_p_3,
    ppv_p_0,
    ppv_p_1,
    ppv_p_2,
    ppv_p_3,
    apv
    );
    
    
    input [`NUM_PORT-2:0]  apv_p_0, apv_p_1, apv_p_2, apv_p_3, ppv_p_0, ppv_p_1, ppv_p_2, ppv_p_3;
    output [`NUM_PORT-2:0] apv;
    
    wire [`NUM_PORT-2:0] availPV;
    wire [`NUM_PORT-2:0] prodAPV, uprodAPV;
 
    generate
    if (CH_INDEX == 2'd3) begin : CH3
        assign availPV = ~(apv_p_0 | apv_p_1 | apv_p_2 | apv_p_3 | ppv_p_0 | ppv_p_1 | ppv_p_2);
         
        // when more than one flits in the lower channels request a given port, deflection will happen in the lower channel.
        // Check the deflection in lower channel here.
        // ch0 never deflect
        wire d1, d2;
        assign d1 = ~| (ppv_p_1 & ~apv_p_0 & ~apv_p_2 & ~apv_p_3 & ~ppv_p_0 | apv_p_1);
        assign d2 = ~| (ppv_p_2 & ~apv_p_0 & ~apv_p_1 & ~apv_p_3 & ~ppv_p_0 & ~ppv_p_1 | apv_p_2);
        wire [1:0] numDeflect; // check numDeflect in lower channel is critical, as the port is available doesn't mean it is not taken by deflected flit.j
        assign numDeflect = d1 + d2;
        
        wire [3:0] deflectIs0, deflectIs1, deflectIs2;
        assign deflectIs0 = availPV[0] ? 4'b0001 : 
                            availPV[1] ? 4'b0010 : 
                            availPV[2] ? 4'b0100 : 
                            availPV[3] ? 4'b1000 : 4'b0000;
        assign deflectIs1 = (availPV == 4'b0011 || availPV == 4'b0111 || availPV == 4'b1011) ? 4'b0010 : 
                            (availPV == 4'b0101 || availPV == 4'b0110 || availPV == 4'b1101 || availPV == 4'b1110) ? 4'b0100 :
                            (availPV == 4'b1001 || availPV == 4'b1010 || availPV == 4'b1100) ? 4'b1000 : 4'b0000;
        assign deflectIs2 = (availPV == 4'b0111) ? 4'b0100 : (availPV == 4'b1011 || availPV == 4'b1101 || availPV == 4'b1110) ? 4'b1000 : 4'b0000;
        
        assign prodAPV = apv_p_3 | (numDeflect ==0 ? (ppv_p_3 & availPV) : 4'b0000);
        assign uprodAPV = numDeflect == 0 ? deflectIs0 : 
                          numDeflect == 1 ? deflectIs1 :
                          numDeflect == 2 ? deflectIs2 : 4'b0000;
     end
     else if (CH_INDEX == 2'd2) begin : CH2
         assign availPV = ~(apv_p_0 | apv_p_1 | apv_p_2 | apv_p_3 | ppv_p_0 | ppv_p_1); // all the allocated port in ST1 and allocated productive port at lower channels in ST2.
          
         // when more than one flits in the lower channels request a given port, deflection will happen in the lower channel.
         // Check the deflection in lower channel here.
         // ch0 never deflect
         wire d1;
         assign d1 = ~| (ppv_p_1 & ~apv_p_0 & ~apv_p_2 & ~apv_p_3 & ~ppv_p_0 | apv_p_1);
         wire numDeflect;
         assign numDeflect = d1;
         
         wire [3:0] deflectIs0, deflectIs1;
         assign deflectIs0 = availPV[0] ? 4'b0001 : 
                             availPV[1] ? 4'b0010 : 
                             availPV[2] ? 4'b0100 : 
                             availPV[3] ? 4'b1000 : 4'b0000;
         assign deflectIs1 = (availPV == 4'b0011 || availPV == 4'b0111 || availPV == 4'b1011) ? 4'b0010 : 
                             (availPV == 4'b0101 || availPV == 4'b0110 || availPV == 4'b1101 || availPV == 4'b1110) ? 4'b0100 :
                             (availPV == 4'b1001 || availPV == 4'b1010 || availPV == 4'b1100) ? 4'b1000 : 4'b0000;
         
         assign prodAPV = apv_p_2 | (numDeflect == 0 ? (ppv_p_2 & availPV) : 4'b0000); // attemps to allocate a productive port for ch2

         assign uprodAPV = numDeflect == 0 ? deflectIs0 : 
                           numDeflect == 1 ? deflectIs1 : 4'b0000;
                           
         wire [`NUM_PORT-2:0] dummy_w = ppv_p_3;

     end
     else if (CH_INDEX == 2'd1) begin : CH1
        assign availPV = ~(apv_p_0 | apv_p_1 | apv_p_2 | apv_p_3 | ppv_p_0); // all the allocated port in ST1 and allocated productive port at lower channels in ST2.
        assign prodAPV = apv_p_1 | (ppv_p_1 & availPV); // attemps to allocate a productive port for ch1

        // ch0 never deflect   
        assign uprodAPV = availPV[0] ? 4'b0001 : 
                          availPV[1] ? 4'b0010 : 
                          availPV[2] ? 4'b0100 : 
                          availPV[3] ? 4'b1000 : 4'b0000;
     end
     else if (CH_INDEX == 2'd0) begin : CH0
        // ch0 always gets at least one productive port
        assign prodAPV = apv_p_0 | ppv_p_0;
        assign uprodAPV = prodAPV;
     end
     
    endgenerate
    
    assign apv = |prodAPV ? prodAPV : uprodAPV;
    
endmodule
