`include "defines.v"

/**
 * crossbar.v
 *
 * @author Kevin Woo <kwoo@cmu.edu>
 *
 * @brief Creates a 5x5 crossbar. Takes in two parts of data, a control line
 *        and a data line for each input/output. Connects each one based on
 *        the route_config input, a bus of 5 x 3bit control signals to select
 *        which input is connected to which output. Inputs are switched and 
 *        latched on the positive edge of the clock cycle.
 *
 *        route_config layout
 *        [Input 4], [Input 3], [Input 2], [Input 1], [Input 0]
 **/
module crossbar (   
    input       `control_w      control0_in,
    input       `control_w      control1_in,
    input       `control_w      control2_in,
    input       `control_w      control3_in,
    input       `control_w      control4_in,
    input       `data_w         data0_in,
    input       `data_w         data1_in,   
    input       `data_w         data2_in,   
    input       `data_w         data3_in,   
    input       `data_w         data4_in,
    input       `routecfg_w     route_config,
    input                       clk,
    input                       rst,
    output  reg `control_w      control0_out,
    output  reg `control_w      control1_out,
    output  reg `control_w      control2_out,
    output  reg `control_w      control3_out,
    output  reg `control_w      control4_out,
    output  reg `data_w         data0_out,
    output  reg `data_w         data1_out,
    output  reg `data_w         data2_out,
    output  reg `data_w         data3_out,
    output  reg `data_w         data4_out);

    // Switches
    wire    [`routecfg_bn-1:0]   switch0;
    wire    [`routecfg_bn-1:0]   switch1;
    wire    [`routecfg_bn-1:0]   switch2;
    wire    [`routecfg_bn-1:0]   switch3;
    wire    [`routecfg_bn-1:0]   switch4;

    // Parse route_config into the switch logic
    assign switch0 = route_config[`routecfg_0];
    assign switch1 = route_config[`routecfg_1];
    assign switch2 = route_config[`routecfg_2];
    assign switch3 = route_config[`routecfg_3];
    assign switch4 = route_config[`routecfg_4];

    always @(posedge clk) begin
        // North = Port 0
        // South = Port 1
        // East = Port 2
        // West = Port 3
        if (rst) begin
            control0_out <= 0;
            data0_out <= 0;
            control1_out <= 0;
            data1_out <= 0;
            control2_out <= 0;
            data2_out <= 0;
            control3_out <= 0;
            data3_out <= 0;
            control4_out <= 0;
            data4_out <= 0;
        end else begin

            $display("xbar: route_config = %x, c0 = %x, c1 = %x, c2 = %x, c3 = %x, d0 = %032x, d1 = %032x, d2 = %032x, d3 = %032x",
                route_config, control0_in, control1_in, control2_in, control3_in, data0_in, data1_in, data2_in, data3_in);

            // Output 0
            case (switch0)
                3'b000: 
                    begin
                        control0_out <= control0_in;
                        data0_out <= data0_in;
                    end
                3'b001:
                    begin
                        control0_out <= control1_in;
                        data0_out <= data1_in;
                    end
                3'b010:
                    begin
                        control0_out <= control2_in;
                        data0_out <= data2_in;
                    end
                3'b011:
                    begin
                        control0_out <= control3_in;
                        data0_out <= data3_in;
                    end
                3'b100:
                    begin
                        control0_out <= control4_in;
                        data0_out <= data4_in;
                    end
                3'b111:
                    begin
                        control0_out <= `control_n'd0;
                        data0_out <= `data_n'd0;
                    end
            endcase
            
            // Output 1
            case (switch1)
                3'b000: 
                    begin
                        control1_out <= control0_in;
                        data1_out <= data0_in;
                    end
                3'b001:
                    begin
                        control1_out <= control1_in;
                        data1_out <= data1_in;
                    end
                3'b010:
                    begin
                        control1_out <= control2_in;
                        data1_out <= data2_in;
                    end
                3'b011:
                    begin
                        control1_out <= control3_in;
                        data1_out <= data3_in;
                    end
                3'b100:
                    begin
                        control1_out <= control4_in;
                        data1_out <= data4_in;
                    end
                3'b111:
                    begin
                        control1_out <= `control_n'd0;
                        data1_out <= `data_n'd0;
                    end
            endcase
        
            // Output 2
            case (switch2)
                3'b000: 
                    begin
                        control2_out <= control0_in;
                        data2_out <= data0_in;
                    end
                3'b001:
                    begin
                        control2_out <= control1_in;
                        data2_out <= data1_in;
                    end
                3'b010:
                    begin
                        control2_out <= control2_in;
                        data2_out <= data2_in;
                    end
                3'b011:
                    begin
                        control2_out <= control3_in;
                        data2_out <= data3_in;
                    end
                3'b100:
                    begin
                        control2_out <= control4_in;
                        data2_out <= data4_in;
                    end
                3'b111:
                    begin
                        control2_out <= `control_n'd0;
                        data2_out <= `data_n'd0;
                    end
            endcase   
            
             // Output 3
            case (switch3)
                3'b000: 
                    begin
                        control3_out <= control0_in;
                        data3_out <= data0_in;
                    end
                3'b001:
                    begin
                        control3_out <= control1_in;
                        data3_out <= data1_in;
                    end
                3'b010:
                    begin
                        control3_out <= control2_in;
                        data3_out <= data2_in;
                    end
                3'b011:
                    begin
                        control3_out <= control3_in;
                        data3_out <= data3_in;
                    end
                3'b100:
                    begin
                        control3_out <= control4_in;
                        data3_out <= data4_in;
                    end
                3'b111:
                    begin
                        control3_out <= `control_n'd0;
                        data3_out <= `data_n'd0;
                    end
            endcase   
            
            // Output4 
            case (switch4)
                3'b000: 
                    begin
                        control4_out <= control0_in;
                        data4_out <= data0_in;
                    end
                3'b001:
                    begin
                        control4_out <= control1_in;
                        data4_out <= data1_in;
                    end
                3'b010:
                    begin
                        control4_out <= control2_in;
                        data4_out <= data2_in;
                    end
                3'b011:
                    begin
                        control4_out <= control3_in;
                        data4_out <= data3_in;
                    end
                3'b100: 
                    begin
                        control4_out <= control4_in;
                        data4_out <= data4_in;
                    end
                3'b111:
                    begin
                        control4_out <= `control_n'd0;
                        data4_out <= `data_n'd0;
                    end
            endcase
        end
    end
endmodule
