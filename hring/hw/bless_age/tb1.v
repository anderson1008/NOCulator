`include "defines.v"
`timescale 1ns/1ps

module tb(
        );

  wire injrd, injack;

  reg clk, rst;

  reg `control_w flit0c;
  reg `data_w flit0d;

  wire `control_w port0_co, port1_co, port2_co, port3_co, port4_co;
  wire `data_w port0_do, port1_do, port2_do, port3_do, port4_do;

  brouter r(
            .clk(clk),
            .rst(rst),
            
            .port0_ci(flit0c), .port0_co(port0_co),
            .port1_ci(28'h0), .port1_co(port1_co),
            .port2_ci(28'h0), .port2_co(port2_co),
            .port3_ci(28'h0), .port3_co(port3_co),
            .port4_ci(28'h0), .port4_co(port4_co),

            .port0_di(flit0d), .port0_do(port0_do),
            .port1_di(128'h0), .port1_do(port1_do),
            .port2_di(128'h0), .port2_do(port2_do),
            .port3_di(128'h0), .port3_do(port3_do),
            .port4_di(128'h0), .port4_do(port4_do),

            .port4_ready(injrd)
            );

  initial begin
$set_toggle_region(tb.r);
$toggle_start();

    clk = 0;
    rst = 0;

    flit0c = 28'h8000001;
    flit0d = 128'h0;

#1;
clk = 1;
#1;
clk = 0;

    flit0c = 28'h0;
    flit0d = 128'h0123456789abcdef0123456789abcdef;

    $display("port0 %04x, port1 %04x, port2 %04x, port3 %04x, port4 %04x\n",
        port0_co, port1_co, port2_co, port3_co, port4_co);
    $display("port0 %16x, port1 %16x, port2 %16x, port3 %16x, port4 %16x\n",
        port0_do, port1_do, port2_do, port3_do, port4_do);

#1;
clk = 1;
#1;
clk = 0;

    flit0d = 128'h0;

    $display("port0 %04x, port1 %04x, port2 %04x, port3 %04x, port4 %04x\n",
        port0_co, port1_co, port2_co, port3_co, port4_co);
    $display("port0 %16x, port1 %16x, port2 %16x, port3 %16x, port4 %16x\n",
        port0_do, port1_do, port2_do, port3_do, port4_do);

#1;
clk = 1;
#1;
clk = 0;

    $display("port0 %04x, port1 %04x, port2 %04x, port3 %04x, port4 %04x\n",
        port0_co, port1_co, port2_co, port3_co, port4_co);
    $display("port0 %16x, port1 %16x, port2 %16x, port3 %16x, port4 %16x\n",
        port0_do, port1_do, port2_do, port3_do, port4_do);

#1;
clk = 1;
#1;
clk = 0;

    $display("port0 %04x, port1 %04x, port2 %04x, port3 %04x, port4 %04x\n",
        port0_co, port1_co, port2_co, port3_co, port4_co);
    $display("port0 %16x, port1 %16x, port2 %16x, port3 %16x, port4 %16x\n",
        port0_do, port1_do, port2_do, port3_do, port4_do);

$toggle_stop();
$toggle_report("./bless_backward_1.saif", 1.0e-9, "tb.r");
$finish;

  end

endmodule
