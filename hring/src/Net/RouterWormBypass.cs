#define DEBUG
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

/*
  Behavior of new worm
    1) Make for progress
    2) Bypass: route to bypass channel
    3) Deflect: route to unproductive port

  Behavior of old worm
    1) Take the allocated port
    2) Truncate: the header will be replicated to another port, and the subsequent flits will take the newly allocated port
    3) Stall: the header injection fails. For the worm in the network, The subseqent flits will be stored in the local buffers. For local port, the injection will be cut off. 

*/

namespace ICSimulator
{
    public class RouterWormBypass : Router
    {
        Flit[][] flit_buf, header_buf; // first dimension is channel index; second dimension is the pipeline
        bool[][] age_mask, contention_mask, truncation_detected;  // first dimension is channel index; second dimension is the mask
        protected Flit m_injectSlot;
        int CHNL_CNT = 5 + Config.num_bypass;
        int STAGE_CNT = 2;
        DIR[] port_alloc_buf; // record the port allocation result. Each output has an entry indicating the channel idx of the flit owning it. 
        WORM_ST[] worm_st;
        enum DIR { NORTH, EAST, SOUTH, WEST, BYPASS, LOCAL, INV };
        enum WORM_ST { READY, BUSY }; // worm state
        int num_incoming, num_eject, num_head_reinj;
        int dbg_idx = 0;

        /////////////////////////////////////////////////////////////////
        // Define constructor 
        /////////////////////////////////////////////////////////////////
        public RouterWormBypass(Coord myCoord) : base(myCoord)
        {
            m_injectSlot = null;
            port_alloc_buf = new DIR[CHNL_CNT];
            worm_st = new WORM_ST[CHNL_CNT];
            flit_buf = new Flit[STAGE_CNT][];
            header_buf = new Flit[STAGE_CNT][]; //TODO: do I need one header table per pipeline stage?
            age_mask = new bool[CHNL_CNT][];
            contention_mask = new bool[CHNL_CNT][];
            truncation_detected = new bool[CHNL_CNT][];

            for (int stg = 0; stg < STAGE_CNT; stg++)
            {
                flit_buf[stg] = new Flit[CHNL_CNT];
                header_buf[stg] = new Flit[CHNL_CNT];

                for (int ch = 0; ch < CHNL_CNT; ch++)
                {
                    flit_buf[stg][ch] = null;
                    header_buf[stg][ch] = null;
                }
            }

            for (int ch = 0; ch < CHNL_CNT; ch++)
            {
                age_mask[ch] = new bool[CHNL_CNT];
                contention_mask[ch] = new bool[CHNL_CNT];
                truncation_detected[ch] = new bool[CHNL_CNT];
                port_alloc_buf[ch] = DIR.INV;
                worm_st[ch] = WORM_ST.READY;

                for (int dir = 0; dir < CHNL_CNT; dir++)
                {
                    age_mask[ch][dir] = false;
                    contention_mask[ch][dir] = false;
                    truncation_detected[ch][dir] = false;
                }
            }
        }

        /////////////////////////////////////////////////////////////////
        // Define class method 
        /////////////////////////////////////////////////////////////////

        /*
		doStep(): wrapper function glues all operations in a router together

		pipeline implemetation:

		1) Pipeline stage 0 operation using flit_buf[0,*]
		2) Pipeline stage 1 operation using flit_buf[1,*]
		3) linkOut[*] = flit_buf[1,*];  
		4) flit_buf[1,*] = flit_buf[0,*];
		5) flit_buf[0,*] = linkIn[*];

		*/

        protected override void _doStep()
        {

            clear();

            buffer_in();
            //////// Pipeline 1 start /////////// 

            /*
			Store header in the following case:
			1) head flit arrives
			2) trucation 
			3) injection
			*/
            setHeaderTable();

            sort();

            truncationDetect();

            ///////// Pipeline 1 end//////////////
            pipeline_1to2();
            ///////// Pipeline 2 start////////////

            injectLocal();



            buffer_out();

        }

        /*
  Design option 1: 
  	We can sort the flit. So the allocation can be done in order of sorted flit. But mapping the sorted flit to the header table entry is needed.

  Design option 2:
    We can sort {chnl_idx, desired_port_vector}. So no mapping between header table and flit is needed. However, we need to select the correponding flit during allocation. This option seems to match the hw design.

  		Select option 1 as it is easier.

		*/

        void clear()
        {
            dbg_idx = 0;
            for (int dir = 0; dir < CHNL_CNT; dir++)
            {
                flit_buf[0][dir] = null;
                flit_buf[1][dir] = null;
            }

        }


        /*
		Buffer write (Order must be enforced)
		linkOut[*].In = flit_buf[1,*];  
		flit_buf[1,*] = flit_buf[0,*];
		flit_buf[0,*] = linkIn[*].Out;
		*/

        // Put the flit in the pipeline buffer onto the link
        void buffer_out()
        {
            for (int dir = 0; dir < CHNL_CNT; dir++)
            {

                if (flit_buf[1][dir] == null)
                    continue;

                int prefDir = flit_buf[1][dir].prefDir;

                if (prefDir < 4)
                {
                    // Check output port to prevent sending flit to a busy port
                    if (linkOut[prefDir] == null)
                        throw new Exception("Output link does NOT exist!");
                    else if (linkOut[prefDir].In != null)
                        throw new Exception("Output port is not idle");

                    linkOut[prefDir].In = flit_buf[1][dir];

                }
                else
                {
                    if (bypassLinkOut[prefDir - 4].In != null)
                        throw new Exception("Bypass port is not idle");
                    bypassLinkOut[prefDir - 4].In = flit_buf[1][dir];
                }

                flit_buf[1][dir] = null;
            }

        }

        void pipeline_1to2()
        {
            for (int dir = 0; dir < CHNL_CNT; dir++)
            {
                for (int st = 0; st < STAGE_CNT - 1; st++)
                {
                    flit_buf[st + 1][dir] = flit_buf[st][dir];
                    flit_buf[st + 1][dir] = null;
                    header_buf[st + 1][dir] = header_buf[st][dir];
                }
            }
        }

        void buffer_in()
        {
            // record the input port of each flit
            for (int dir = 0; dir < 4; dir++)
            {
                if (linkIn[dir] == null)
                    continue;
                if (linkIn[dir].Out != null)
                {
#if DEBUG
                    Console.WriteLine("Router {0} : Buffer Inport {1} {2} @ {3}", coord.ID, Simulator.network.portMap(dir), linkIn[dir].Out.ToString(), Simulator.CurrentRound);
#endif
                    flit_buf[0][dir] = linkIn[dir].Out;
                    flit_buf[0][dir].inDir = dir;
                    linkIn[dir].Out = null;
                    num_incoming++;
                }
            }

            for (int bp = 0; bp < Config.num_bypass; bp++)
            {
                if (bypassLinkIn[bp].Out != null)
                {
#if DEBUG
                    Console.WriteLine("Router {0} : Bypass Inport {1} {2} @ {3}", coord.ID, bp, bypassLinkIn[bp].Out.ToString(), Simulator.CurrentRound);
#endif
                    flit_buf[0][4 + bp] = bypassLinkIn[bp].Out;
                    flit_buf[0][4 + bp].inDir = (int)DIR.BYPASS;
                    bypassLinkIn[bp].Out = null;
                    num_incoming++;
                }
            }

        }

        // Compute the desired port vector of the next hop
        //   Do it after PA



        /*
            Packet injection

            The head flit injection of a  worm is dictated by the availability of the port. 
            However, once the worm injection starts, the burst of injection should not be interrupted..
            The injected worm can subject to truncation and stall. The same rule applying to other worms also applies to the local injected worm. 
            In case the local injected is stalled, the injected flit will be looped back and stored in the stalled worm queue alone with its head flit.i

            Injection priority
            1) Inject stalled worm queue
            2) Inject new worm queue
            Within each queue, the injection is FIFO.
        */


        // This is the function called by NI
        public override void InjectFlit(Flit f)
        {
            if (!canInjectFlit(f))
                throw new Exception("ERROR: local injection type should not be interrupted");

            m_injectSlot = f;

            switch (worm_st[(int)DIR.LOCAL])
            {

                case (WORM_ST.READY):

                    // This state is only for inject head flit
                    if (m_injectSlot.isHeadFlit)
                        worm_st[(int)DIR.LOCAL] = WORM_ST.BUSY;
                    else
                        throw new Exception("ERROR: local injection type should be head flit in READY state");

                    break;

                case (WORM_ST.BUSY):

                    if (!m_injectSlot.isHeadFlit)
                        throw new Exception("ERROR: local injection type should not be head flit in BUSY state");

                    if (m_injectSlot.isTailFlit) worm_st[(int)DIR.LOCAL] = WORM_ST.READY;

                    break;

                default:
                    throw new NotImplementedException();
            }

        }

        // #_incoming - #_eject - #_header_inj < #_outport
        public override bool canInjectFlit(Flit f)
        {
            bool ret = true;
            ret = ((num_incoming - num_eject + num_head_reinj) < CHNL_CNT - 1) ? true : false;
            return ret;
        }

        private void _inject(Flit f)
        {
            for (int dir = 0; dir < CHNL_CNT - 1; dir++)
            {
                if (flit_buf[1][dir] == null)
                {
                    flit_buf[1][dir] = f;
                    flit_buf[1][dir].inDir = dir;
#if DEBUG
                    Console.WriteLine("Router {0}: Inject  {1} @ {2}", ID, f.ToString(), Simulator.CurrentRound);
#endif
                    statsInjectFlit(f);
                    return;
                }
            }
        }

        protected void injectLocal()
        {
            if (m_injectSlot == null)
                return;

            // Compute the PPV here before putting on link for local flit
            if (m_injectSlot.isHeadFlit)
            {
                routeCompute(ref m_injectSlot);
            }
            _inject(m_injectSlot);
        }


        protected PreferredDirection determineDirection(Coord dst_coord, Coord cur_coord)
        {
            PreferredDirection pd;
            if (dst_coord.x > cur_coord.x)
                pd.xDir = (int)DIR.EAST;
            else if (dst_coord.x < cur_coord.x)
                pd.xDir = (int)DIR.WEST;
            else
                pd.xDir = (int)DIR.INV;

            if (dst_coord.y > cur_coord.y)
                pd.yDir = (int)DIR.NORTH;
            else if (dst_coord.y < cur_coord.y)
                pd.yDir = (int)DIR.SOUTH;
            else
                pd.yDir = (int)DIR.INV;

            return pd;
        }

        // TODO: calculate the PPV for the local injected worm
        void routeCompute(ref Flit f)
        {

            PreferredDirection pd;

            pd = determineDirection(f.dest, coord);

            if (pd.xDir != (int)DIR.INV)
                f.prefDir = pd.xDir;
            else
                f.prefDir = pd.yDir;

        }

        void routeComputeNext(ref Flit f, DIR dir)
        {
            PreferredDirection pd;

            pd = determineDirection(f.dest, neigh[(int)dir].coord);

            if (pd.xDir != (int)DIR.INV)
                f.prefDir = pd.xDir;
            else
                f.prefDir = pd.yDir;
        }

        // Ejection

        // Port allocator
        //  determine the channel which can inject the flit, grant injection, and allocate port for injected flit

        // Truncation detect

        // Switch traversal




        /*
            Store header in the header table
        */
        protected void setHeaderTable()
        {
            for (int dir = 0; dir < CHNL_CNT; dir++)
            {
                if (flit_buf[0][dir] == null) continue;
                if (flit_buf[0][dir].isHeadFlit)
                {
                    header_buf[0][dir] = flit_buf[0][dir];
                }
            }
        }

        /*
          Clear header table entry 
        */
        protected void clearHeaderTable(int dir)
        {
            if (flit_buf[0][dir] == null) return;
            if (flit_buf[0][dir].isTailFlit)
            {
                header_buf[0][dir] = null;
            }
        }



        /*
  Sort
    The stored headers are sorted
*/
        protected void sort()
        {

            if (!newWorm(ref flit_buf[0])) return;

            // Permutation Sort
            //   Two modes: 2-stage; 3-stage
            if (Config.sortMode == 0)
                _fullSort(ref header_buf[0]); // defined in RouterFlit.cs
            else if (Config.sortMode == 1)
                _partialSort(ref header_buf[0]); // defined in RouterFlit.cs
        }

        protected void _swap(ref Flit t0, ref Flit t1)
        {
            if (t0 != null || t1 != null)
                Simulator.stats.permute.Add();
            if (rank(t1, t0) < 0)
            {
                Flit t = t0;
                t0 = t1;
                t1 = t;
            }
        }

        virtual protected void _partialSort(ref Flit[] input)
        {
            _swap(ref input[0], ref input[1]);
            _swap(ref input[2], ref input[3]);
            _swap(ref input[0], ref input[2]);
            _swap(ref input[1], ref input[3]);
        }

        protected void _fullSort(ref Flit[] input)
        {
            _swap(ref input[0], ref input[1]);
            _swap(ref input[2], ref input[3]);
            _swap(ref input[0], ref input[2]);
            _swap(ref input[1], ref input[3]);
            _swap(ref input[0], ref input[2]);
            _swap(ref input[1], ref input[3]);
        }

        /* Look-ahead truncation
  // Override truncation detect

  Problem: for a given flit, whether or not there is any older worm which will arrive next cycle competes for the same output port. If there is, its header flit must be replicated and injected to an available port, or informs the ejector to remove its subsequent body flit next cycle.

  N, E, S, W, L might be truncated by one of N-2 incoming flit
  B will not be truncated 

  Simplified problem: find out if a flit using port i is younger than anyone of the incoming flit and the particular older flit also requests the same output port i.

*/
        protected void truncationDetect()
        {

            if (!newWorm(ref flit_buf[0])) return;

            if (nxtIsOlder() && nxtHasContention())
            {

                // Find out all worms which will be truncated
                // TODO: determine the number of truncation allowed
                for (int outDir = 0; outDir < CHNL_CNT; outDir++)
                    for (int inDir = 0; inDir < CHNL_CNT; inDir++)
                        truncation_detected[outDir][inDir] = age_mask[outDir][inDir] && contention_mask[outDir][inDir];
            }
        }


        /*
  If there is any head flit arrives next cycle.
*/
        protected bool newWorm(ref Flit[] _flit_buf)
        {

            bool _newWorm = false;

            for (int i = 0; i < CHNL_CNT; i++)
                _newWorm = (_flit_buf[i] == null) ? _newWorm : _newWorm | _flit_buf[i].isHeadFlit;

            return _newWorm;
        }


        /*
  For a given flit @ outDir, check if which incoming worm is older.
*/
        protected bool nxtIsOlder()
        {
            bool ret = false;
            for (int outDir = 0; outDir < CHNL_CNT; outDir++)
            {
                // Bypass ports do not need to detect truncation
                if (outDir >= (int)DIR.BYPASS) continue;

                // Compare with everyone except itself
                for (int inDir = 0; inDir < CHNL_CNT; inDir++)
                {
                    if (inDir == (int)port_alloc_buf[outDir]) continue;

                    if (rank(header_buf[0][inDir], header_buf[1][(int)port_alloc_buf[outDir]]) > 0)
                    {
                        age_mask[outDir][inDir] = true;
                        ret = true;
                        continue;
                    }

                }
            }
            return ret;
        }


        /*
  For a given flit @ outDir, check if there is any worm which request the same output port.
*/
        protected bool nxtHasContention()
        {

            bool ret = false;
            for (int outDir = 0; outDir < CHNL_CNT; outDir++)
            {
                // Bypass ports do not need to detect truncation
                if (outDir >= (int)DIR.BYPASS) continue;

                // The port is available
                if (port_alloc_buf[outDir] == DIR.INV) continue;

                // Compare with everyone except itself
                for (int inDir = 0; inDir < CHNL_CNT; inDir++)
                {
                    if (inDir == (int)port_alloc_buf[outDir]) continue;

                    if (header_buf[0][inDir].prefDir == outDir)
                    {
                        contention_mask[outDir][inDir] = true;
                        ret = true;
                        continue;
                    }

                }

            }
            return ret;
        }



        // Override Ejection and Injection
        protected void ejection()
        {


        }

        // Header inject


        /* Port allocation

    if no truncation detected && no new worm 
      all worms take the allocated port

    else if no truncation detected && and has new worm
      Enable port allocation based on OF rule for all worms

    else if truncation detected && no new worm
      // invalid   

    else if truncation detected && has new worm

      Enable port allocation based on OF rule for the rest of worms 
      The truncated worm must takes the pre-allocated port  
      Each worm can only be bypassed X times. Once it reaches the limit, the worm is deflected. TODO: x >= #_subnet will cause deadlock such that the replicated header will compete for the bypass port with its subsequent body/tail flits 

    if truncation is detected  
      replicate the header of truncated worm on unwanted and available port {N, E, S, W, B}.
      If no such port exist, we will not perform header replication. In this case, no port will be allocated for the subsequent flits.  The subsequent flits of the truncated worm can be ejected temporarily from the network. It will be stored in the NI injection buffer and reinjected as a new worm based on the injection rule. This creates two issues: 
      1) the truncated worm competets the bandwidth with local-destined worm
      2) it may break the OF rule, causing livelock. 

    To resolve this, we can perform dual-ejection. The first eject stage is dedicated to removing local destined 
    It can
      1) provide delivery guarantee at the presence of header replication
      2) resolve network contention 
 
  */
        protected void port_alloc()
        {


        }

    }

}
