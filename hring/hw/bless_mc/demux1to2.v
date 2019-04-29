// demux 1 to 2

module demux1to2 (dataIn, sel, aOut, bOut);

input 	dataIn, sel;
output	aOut, bOut;

assign aOut = sel ? 0 : dataIn;
assign bOut = sel ? dataIn : 0;

endmodule