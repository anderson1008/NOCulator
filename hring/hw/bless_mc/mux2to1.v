// 2 to 1 mux

module mux2to1 (aIn, bIn, sel, dataOut);

input 	aIn, bIn, sel;
output 	dataOut;

// sel =	1: dataOut = bIn; 
// sel =	0: dataOut = aIn;
assign dataOut = sel ? bIn : aIn; 

endmodule