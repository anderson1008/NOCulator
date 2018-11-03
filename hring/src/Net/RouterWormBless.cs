//#define DEBUG

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;


/*

  if a flits is a header flit, perform RC, permutation sort, port allocation, and port switching.
    - data path must maintain the arbitration and allocation result until tail flit is detected.

  if a flit is a body flit, skip RC and permutation sort, port allocation.
  
  if a flit is both a tail and header flit, make arbitration, but release the channel

  if a flit is a tail flit, release the channel

  any head flit will enable arbitration

*/

namespace ICSimulator
{
  public class RouterWormBufferless : Router {
	}	
	}
/////////////////////////////////////////////////////////////////
// Define class member variable 
/////////////////////////////////////////////////////////////////
  	Flit[, ] flit_buf, header_buf;  // First dimention is pipeline stage; Second dimension is channel index
    int CHNL_CNT = 4 + Config.num_bypass;
    int STAGE_CNT = 2;
    int [] port_alloc_buf; // record the port allocation result. Each output has an entry indicating the channel idx of the flit owning it. 
  	enum DIR {NORTH, EAST, SOUTH, WEST, LOCAL, BYPASS_0, BYPASS_1, BYPASS_2, BYPASS_3, INV};


/////////////////////////////////////////////////////////////////
// Define constructor 
/////////////////////////////////////////////////////////////////
    public RouterWormBufferless (Coord myCoord) : base (myCoord) {
  		m_injectSlot = null;
  		flit_buf = new Flit[STAGE_CNT, CHNL_CNT];
  		header_buf = new Flit[STAGE_CNT, CHNL_CNT];
      port_alloc_buf = new int [CHNL_CNT]; 
  	  for (int dir = 0; dir < CHNL_CNT; dir++)
  		{

        port_alloc_buf[dir] = DIR.INV;
  	    for (int stg = 0; stg < STAGE_CNT; stg++)
  		  {
  		    flit_buf [stg][dir] = null;
  		    header_buf [stg][dir] = null;
        }
  		}
  	}

/////////////////////////////////////////////////////////////////
// Define class method 
/////////////////////////////////////////////////////////////////



/*
  doStep(): wrapper function glues all operations in a router together

    pipeline implemetation:

    1) Pipeline stage 0 operation using flit_buf[0][*]
    2) Pipeline stage 1 operation using flit_buf[1][*]
    3) linkOut[*] = flit_buf[1][*];  
    4) flit_buf[1][*] = flit_buf[0][*];
    5) flit_buf[0][*] = linkIn[*];

*/
  protected override void _doStep(){

    injectToRouter();



    buffer_out ();
  }

/*
  Design option 1: 
  	We can sort the flit. So the allocation can be done in order of sorted flit. But mapping the sorted flit to the header table entry is needed.

  Design option 2:
    We can sort {chnl_idx, desired_port_vector}. So no mapping between header table and flit is needed. However, we need to select the correponding flit during allocation. This option seems to match the hw design.

  Select option 1 as it is easier.

*/



/*
  Buffer write (Order must be enforced)
    linkOut[*].In = flit_buf[1][*];  
    flit_buf[1][*] = flit_buf[0][*];
    flit_buf[0][*] = linkIn[*].Out;
*/

    // Put the flit in the pipeline buffer onto the link
  void buffer_out () { 
    for (int dir = 0; dir < CHNL_CNT; dir) {
      if (filt_buf[1][dir] == null) continue;
      int prefDir = flit_buf[1][dir].prefDir;   
      // Check output port to prevent sending flit to a busy port
      if (linkOut[prefDir].In == null) throw new Exception("Output port is not idle");

      linkOut[prefDir].In = flit_buf[1][dir];
      flit_buf[1][dir] = null;
    }
  }

  void pipeline_1to2 () { 
    for (int st = 0; st < STAGE_CNT-1; st++) { 
      for (int dir = 0; dir < CHNL_CNT; dir++) { 
  		  flit_buf[st+1][dir] = flit_buf[st][dir];
  		  flit_buf[st+1][dir] = null;
        header_buf[st+1][dir] = header_buf[st][dir];
  	  }
    }
  }

  void buffer_in () {
    // record the input port of each flit
    for (int dir = 0; dir < CHNL_CNT; dir++) 
  	  if (linkIn[dir] != null && linkIn[dir].Out != null)
  		{
  			#if DEBUG
  			Console.WriteLine ("#3 BufferWrite: Time {0}: @ node {1} Inport {2} {3} ", Simulator.CurrentRound,coord.ID, Simulator.network.portMap(dir),linkIn[dir].Out.ToString() );
  			#endif
  			flit_buf[0][dir] = linkIn[dir].Out; 
  			flit_buf[0][dir].inDir = dir;   
  			linkIn[dir].Out = null;
  		}


  	}
  }




  // Compute the desired port vector of the next hop
  //   Do it after PA


  // TODO: Packet injection should not be interrupted, unless local packet is truncated and all other ports are busy.

  // Inject 
  //   measure buffer occupancy
  // grant : determined by port allocator
  protected void injectToRouter(bool grant, int inj_channel) {
 
    bool want_to_inject = m_injectSlot != null;

    if ((want_to_inject && grant) != true) return;

    if (flit_buf[1][inj_channel] == null) {
  		flit_buf[1] [inj_channel] = m_injectSlot;
  		#if DEBUG
  		Console.WriteLine ("#1 InjectToRouter: Time {0}: Inject @ Router {1} {2}", Simulator.CurrentRound, ID, m_injectSlot.ToString());
  		#endif
  		statsInjectFlit (m_injectSlot);
  		m_injectSlot = null;
  	}
  	else
  		throw new Exception("Channel is not idle");
  }

// Ejection

// Port allocator
//  determine the channel which can inject the flit, grant injection, and allocate port for injected flit

// Truncation detect

// Switch traversal
  


/*
 WormBypass has 6 ports. It use lookahead truncation.
*/
  public class RouterWormBypass : RouterWormBufferless{

/////////////////////////////////////////////////////////////////
// Define class member variable 
/////////////////////////////////////////////////////////////////
  
/////////////////////////////////////////////////////////////////
// Define constructor 
/////////////////////////////////////////////////////////////////
    
    public RouterWormBypass (Coord myCoord) : base (myCoord)
    {
      age_mask = new bool [CHNL_CNT][CHNL_CNT];
      contention_mask = new bool [CHNL_CNT][CHNL_CNT];
      truncation_detected = new bool [CHNL_CNT][CHNL_CNT];
      for (int i = 0; i < CHNL_CNT; i++) {
        for (int j = 0; j < CHNL_CNT; j++) {
          age_mask[i][j] = false;
          contention_mask[i][j] = false;
          truncation_detected[i][j] = false;
  			}
  		}
  	}

/////////////////////////////////////////////////////////////////
// Define class member function 
/////////////////////////////////////////////////////////////////

  protected override void _doStep(){




  }



  }
}

