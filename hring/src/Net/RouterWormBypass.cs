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
        Flit[] tempHeader;
        // protected Flit[] input; // keep this as a member var so we don't have to allocate on every step
        //bool[][] age_mask, contention_mask, truncation_detected;  // first dimension is channel index; second dimension is the mask
        bool newIsOldest;
        bool newHeader;
        DIR[] ejector;
        protected Flit m_injectSlot;
        int CHNL_CNT = 5 + Config.num_bypass;
        int STAGE_CNT = 2;
        DIR[] port_alloc_buf; // record the port allocation result. Each output has an entry indicating the channel idx of the flit owning it. 
        WORM_ST[] worm_st;
        // WORM_ST localWorm;
        // WORM_ST truncatedWorm;       
        enum DIR { NORTH, EAST, SOUTH, WEST, BYPASS, LOCAL, NI, INV }; //Truncated worm will be downloaded to the NI
        enum WORM_ST { READY, BUSY }; // worm state
        enum STATUS { READY, BUSY };
        int num_incoming, num_eject, num_head_reinj;
        int dbg_idx = 0;
        Queue<Flit> NI_buffer;// NI_buffer2;

        /////////////////////////////////////////////////////////////////
        // Define constructor 
        /////////////////////////////////////////////////////////////////
        public RouterWormBypass(Coord myCoord) : base(myCoord)
        {
            m_injectSlot = null;
            newIsOldest = false;
            newHeader = false;
            ejector = new DIR[Config.meshEjectTrial];
            // truncatedWorm = WORM_ST.READY;
            //localWorm = WORM_ST.READY;
            //injector = STATUS.READY;
            port_alloc_buf = new DIR[CHNL_CNT];
            worm_st = new WORM_ST[CHNL_CNT];
            flit_buf = new Flit[STAGE_CNT][];
            header_buf = new Flit[STAGE_CNT][]; //TODO: do I need one header table per pipeline stage?
            tempHeader = new Flit[CHNL_CNT]; // used to hold header temporarely for sorting
                                             //input = new Flit[CHNL_CNT + 1]; // +1 for NI buffer
                                             //age_mask = new bool[CHNL_CNT][];
                                             // contention_mask = new bool[CHNL_CNT][];
                                             // truncation_detected = new bool[CHNL_CNT][];

            for (int ej = 0; ej < Config.meshEjectTrial; ej++)
            {
                ejector[ej] = DIR.INV;
                            }
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
                // input[ch] = null;
                tempHeader[ch] = null;
            }
            for (int ch = 0; ch < CHNL_CNT; ch++)
            {
                // age_mask[ch] = new bool[CHNL_CNT];
                // contention_mask[ch] = new bool[CHNL_CNT];
                // truncation_detected[ch] = new bool[CHNL_CNT];
                port_alloc_buf[ch] = DIR.INV;
                worm_st[ch] = WORM_ST.READY;

                for (int dir = 0; dir < CHNL_CNT; dir++)
                {
                    // age_mask[ch][dir] = false;
                    // contention_mask[ch][dir] = false;
                    // truncation_detected[ch][dir] = false;
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

            //clear();

            buffer_in();
            //////// Pipeline 1 start /////////// 

            /*
			Store header in the following case:
			1) head flit arrives
			2) trucation 
			3) injection
			*/
            setHeaderTable();
            ejection();
            copyHeaderToTemp();
            sort();
            /*
             * Truncate a worm if it needed
             * 1. Check the if truncated, and if it is, it moves the header of the worm that will be truncated and assign the Port
             * 2. For truncating a local injection, it moves all flits to NI and header and flit_buf[0][LOCAL] are set to null
             * 3. For truncating other, it downloads the header to NI and marks one of the ejector as busy
             */
            
            truncateWorm();            
            port_alloc();
            injectLocal();
            buffer_out();
            ///////// Pipeline 1 end//////////////
            pipeline_1to2();
            ///////// Pipeline 2 start////////////
            ///
            //injectLocal();

            //buffer_out();
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
            for (int dir = 0; dir < CHNL_CNT - 1; dir++)
            {

                if (flit_buf[1][dir] == null)
                    continue;

                //int prefDir = flit_buf[1][dir].prefDir;

                if (dir < 4)
                {
                    // Check output port to prevent sending flit to a busy port
                    if (linkOut[dir] == null)
                        throw new Exception("Output link does NOT exist!");
                    else if (linkOut[dir].In != null)
                        throw new Exception("Output port is not idle");

                    linkOut[dir].In = flit_buf[1][dir];
#if DEBUG
                    Console.WriteLine("Router {0} : Buffer Outport {1} {2} @ {3}:from INPort {4} for Destination->{5}::{6} subNetwork {7} ",
                        coord.ID, Simulator.network.portMap(dir), linkOut[dir].In.ToString(), Simulator.CurrentRound, linkOut[dir].In.inDir,
                        linkOut[dir].In.dest, linkOut[dir].In.packet.nrOfFlits, linkOut[dir].In.subNetwork);
#endif

                }
                else if (dir == (int)DIR.BYPASS)
                {
                    if (bypassLinkOut[dir - 4].In != null)
                        throw new Exception("Bypass port is not idle");
                    flit_buf[1][dir].subNetwork = ~flit_buf[1][dir].subNetwork; // invert the subNetwork info
                    bypassLinkOut[dir - 4].In = flit_buf[1][dir];
                }

                flit_buf[1][dir] = null;
            }

        }

        void pipeline_1to2()
        {
            for (int dir = 0; dir < CHNL_CNT - 1; dir++)
            {
                //flit_buf[st + 1][dir] = flit_buf[st][dir];  
                int from = (int)port_alloc_buf[dir]; // get the port mapping 

                if (from < 4) // if valid
                {
                    if (header_buf[0][from] != null && flit_buf[0][from]!=null)
                    {
                        if (flit_buf[0][from].isHeadFlit)
                        {
                            if(dir <4) // Only four neighbors exist, BYPASS router has the same coordinate
                                routeComputeNext(ref flit_buf[0][from], (DIR)dir);
                        }
                        flit_buf[1][dir] = flit_buf[0][from]; // move the flit from-port buffer to to-port buffer
                        if (flit_buf[1][dir].isTailFlit)
                        {
                            port_alloc_buf[dir] = DIR.INV; // if tail flit, reset port allocation 
                        }
                        flit_buf[0][from] = null;
                    }
                }
                else if (from == (int)DIR.BYPASS)
                {
                    if (header_buf[0][from] != null && flit_buf[0][from] != null)
                    {
                        if (flit_buf[0][from].isHeadFlit)
                        {
                            if(dir <4) // Only four neighbors exist, BYPASS router has the same coordinate
                                routeComputeNext(ref flit_buf[0][from], (DIR)dir);
                        }
                        flit_buf[1][dir] = flit_buf[0][from]; // move the flit from-port buffer to to-port buffer
                        if (flit_buf[1][dir].isTailFlit)
                        {
                            port_alloc_buf[dir] = DIR.INV; // if tail flit, reset port allocation 
                        }
                        flit_buf[0][from] = null;
                    }
                }
                // we are not worried about the header of second stage yet
                //header_buf[st + 1][dir] = header_buf[st][dir];

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
                    Console.WriteLine("Router {0} : Buffer Inport {1} {2} @ {3} : Subnet {4} prefDir {5}",
                        coord.ID, Simulator.network.portMap(dir), linkIn[dir].Out.ToString(), Simulator.CurrentRound,
                        linkIn[dir].Out.subNetwork, linkIn[dir].Out.prefDir);
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
                    // Sender must invert the subnetwork information, so no need to invert it again
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

        /*
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
        */

        public override void InjectFlit(Flit f)
        {
            if (m_injectSlot != null)
                throw new Exception("Trying to inject twice in one cycle");

            m_injectSlot = f;
        }
        // #_incoming - #_eject - #_header_inj < #_outport
        public override bool canInjectFlit(Flit f)
        {
            bool ret = false;
            // ret = ((num_incoming - num_eject + num_head_reinj) < CHNL_CNT - 1) ? true : false;
            // for current ongoing worm
            ret = m_injectSlot == null;
            return ret;
        }
        public bool canInjectLocal()
        {
            bool ret = false;

            if (header_buf[0][(int)DIR.LOCAL] != null)
            {
                if (header_buf[0][(int)DIR.LOCAL].prefDir != (int)DIR.BYPASS)
                    ret = (linkOut[header_buf[0][(int)DIR.LOCAL].prefDir] != null) && (port_alloc_buf[header_buf[0][(int)DIR.LOCAL].prefDir] == DIR.LOCAL);
                else
                    ret = (bypassLinkOut[4 - (int)DIR.BYPASS] != null) && (port_alloc_buf[header_buf[0][(int)DIR.LOCAL].prefDir] == DIR.LOCAL);
                if (!ret)
                    throw new Exception("Something goes wrong, Please check the canInjectLocal()");
            }

            if (!ret)
                ret = isOutPortFree();
            return ret;
        }
        private void _inject(Flit f)
        {
            if (port_alloc_buf[header_buf[0][(int)DIR.LOCAL].prefDir] != DIR.LOCAL) // need to double check this. Better way is first assigned port allocation and use same port 
            {
                throw new Exception("Port must be allocated here but it seems something wrong");
            }
            // what should we do for header and port allocation table??
            int dir = header_buf[0][(int)DIR.LOCAL].prefDir;
            f.inDir = (int)DIR.LOCAL;// dir;
            if (f.isHeadFlit)
                routeComputeNext(ref f, (DIR)dir);
            flit_buf[1][dir] = f; // move to output buffer for TX
            if (f.isTailFlit)
            { 
                port_alloc_buf[dir] = DIR.INV; // Reset Port_alloc buffer
                header_buf[0][(int)DIR.LOCAL] = null;
            }
#if DEBUG
            Console.WriteLine("Router {0}: Inject  {1} @ {2}", ID, f.ToString(), Simulator.CurrentRound);
#endif
            statsInjectFlit(f);
            return;

        }
        protected bool isPortAllocatedForLoaclInjection(out int p)
        {
            p = (int)DIR.INV; // invalid port
            if (header_buf[0][(int)DIR.LOCAL] != null)
            {
                for (int i = 0; i < CHNL_CNT-1; i++)
                {
                    if (isOutPortEnable(i) && port_alloc_buf[i] == DIR.LOCAL)
                    {
                        p = i;
                        return true;
                    }
                }
            }
            return false;
        }
        protected void injectLocal()
        {
            int outPort = (int)DIR.INV;
            if (m_injectSlot == null)
            {
                //release allocated port and clear header buffer
                if (header_buf[0][(int)DIR.LOCAL] != null) // only top 4-entry of temp header are affected by sorting
                {
                    if (port_alloc_buf[header_buf[0][(int)DIR.LOCAL].prefDir] == DIR.LOCAL)
                    {
                        port_alloc_buf[header_buf[0][(int)DIR.LOCAL].prefDir] = DIR.INV; // release port
                        tempHeader[(int)DIR.LOCAL] = null;
                        header_buf[0][(int)DIR.LOCAL] = null;
                    }
                    else
                        throw new Exception("Port maping not matching please check injectLocal()");
                }

                return;
            }
            if (!isPortAllocatedForLoaclInjection(out outPort))
            {
                if (m_injectSlot.isHeadFlit)
                {
                    routeCompute(ref m_injectSlot);
                    if (isOutPortEnable(m_injectSlot.prefDir) && port_alloc_buf[m_injectSlot.prefDir] == DIR.INV)
                    {
                        port_alloc_buf[m_injectSlot.prefDir] = DIR.LOCAL;
                        outPort = m_injectSlot.prefDir;
                    }
                    else
                    {
                        for (int i = 0; i < CHNL_CNT-1; i++)
                        {
                            if (isOutPortEnable(i) && port_alloc_buf[i] == DIR.INV)
                            {
                                port_alloc_buf[i] = DIR.LOCAL; // assign port in port allocator
                                outPort = i;
                                break;
                            }
                        }
                    }
                }
            }
            if (outPort != (int)DIR.INV) // output port is available
            {
                // Compute the PPV here before putting on link for local flit
                if (m_injectSlot.isHeadFlit)
                {
                    m_injectSlot.prefDir = outPort;
                    header_buf[0][(int)DIR.LOCAL] = m_injectSlot;
                    tempHeader[(int)DIR.LOCAL] = m_injectSlot;
                }
                _inject(m_injectSlot);
                m_injectSlot = null; // clear the m_injectSlot
            }
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
            for (int dir = 0; dir < 4; dir++) // first 4 ports compete for each other when sorted
            {
                if (flit_buf[0][dir] != null)
                {
                    if (flit_buf[0][dir].isHeadFlit)
                    {
                        newHeader = true;
                        header_buf[0][dir] = flit_buf[0][dir];
                        if (isOlder(header_buf[0][dir], tempHeader[0])) // tempHeader is sorted                       
                            newIsOldest = true;                      
                    }
                }else
                    header_buf[0][dir] = null;             
            }
            //Check BYPASS port
            if (flit_buf[0][(int)DIR.BYPASS] != null)
            {
                if (flit_buf[0][(int)DIR.BYPASS].isHeadFlit)
                {
                    newHeader = true; // BYPASS port does not compet for truncation
                    header_buf[0][(int)DIR.BYPASS] = flit_buf[0][(int)DIR.BYPASS];
                }
            }
            else
            {
                header_buf[0][(int)DIR.BYPASS] = null;
            }
        }
   
        protected void copyHeaderToTemp()
        {
            int i = 0;
            for (i = 0; i < CHNL_CNT; i++)
            {
                tempHeader[i] = header_buf[0][i]; // header_buf[0] holds the most recent headers
            }
        }
        protected bool isOlder(Flit f1, Flit f2)
        {
            bool ret = false;
            if (rank(f1, f2) < 0)
            {
                ret = true;
            }
            return ret;
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

            if (!newHeader) return;
            // Permutation Sort
            //   Two modes: 2-stage; 3-stage
            if (Config.sortMode == 0)
                // _fullSort(ref header_buf[0]);
                _fullSort(ref tempHeader);
            else if (Config.sortMode == 1)
                //_partialSort(ref header_buf[0]);
                _partialSort(ref tempHeader);
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
        /*
        protected void _fullSort(ref Flit[] input)
        {
            _swap(ref input[0], ref input[1]);
            _swap(ref input[2], ref input[3]);
            _swap(ref input[0], ref input[2]);
            _swap(ref input[1], ref input[3]);
            _swap(ref input[0], ref input[2]); //repeated operation
            _swap(ref input[1], ref input[3]); //repeated operation
        }
        */
        protected void _fullSort(ref Flit[] input)
        {
            _swap(ref input[0], ref input[1]);
            _swap(ref input[2], ref input[3]);
            _swap(ref input[0], ref input[2]); // oldest in input[0]
            _swap(ref input[1], ref input[3]); // youngest in input[3]
            _swap(ref input[1], ref input[2]);  // older in input[1] and younger in input[2]        
        }
        /* Look-ahead truncation
  // Override truncation detect

  Problem: for a given flit, whether or not there is any older worm which will arrive next cycle competes for the same output port. If there is, its header flit must be replicated and injected to an available port, or informs the ejector to remove its subsequent body flit next cycle.

  N, E, S, W, L might be truncated by one of N-2 incoming flit
  B will not be truncated 

  Simplified problem: find out if a flit using port i is younger than anyone of the incoming flit and the particular older flit also requests the same output port i.

*/
//This method test whether the truncation happens or not; if happents, it also elcear the prefered port maping
        protected bool isTruncationHappens(out int expectedPort)
        {
            bool ret = false;
            expectedPort = (int)DIR.INV;
            if (newIsOldest)
            {
                // newIsOldest = false;
                if (tempHeader[0] != null && tempHeader[0].dest.ID != ID) //locally destined worm, tempHeader[0] must not be null
                {
                    //dir = (int)tempHeader[0].prefDir;
                    if ((int)tempHeader[0].prefDir < (int)DIR.LOCAL)
                    {
                        if (!isOutPortEnable((int)tempHeader[0].prefDir))
                            throw new Exception("Highest ranked port is not enable, plese check isTruncationHappens()");
                        expectedPort = (int)port_alloc_buf[tempHeader[0].prefDir]; //get truncated port
                                                                                   // port_alloc_buf[tempHeader[0].prefDir] = DIR.INV;
                        ret = (expectedPort != (int)DIR.INV) && (header_buf[0][expectedPort] != null);
                    }
                    else
                        throw new Exception("Prefered Port looks not matching, Please check");
                }
                else if(tempHeader[0] != null && tempHeader[0].dest.ID == ID)
                {
                    if (ejector[0] != DIR.INV && ejector[1] != DIR.INV) // check if both ejector are busy
                    {
                        tempHeader[0].prefDir = (int)DIR.BYPASS; // divert the worm through BYPASS 
                        header_buf[0][tempHeader[0].inDir].prefDir = (int)DIR.BYPASS;
                        expectedPort = (int)port_alloc_buf[(int)DIR.BYPASS];
                        //port_alloc_buf[(int)DIR.BYPASS] = DIR.INV;
                        ret = (expectedPort != (int)DIR.INV) && (header_buf[0][expectedPort] != null);
                    }
                }
            }
            //newIsOldest = ret; 
            return ret;
        }

        /* This method truncates the worm and forward to the one of the available port if any; 
        * otherwise, it directs the truncated worm to to NI buffer with updating the port allocation.
        */
        protected void truncateWorm()
        {
            int victimInPort;
            if (isTruncationHappens(out victimInPort))
            {
                //int victimInPort = (int)port_alloc_buf[(int)tempHeader[0].prefDir];
               // port_alloc_buf[(int)tempHeader[0].prefDir] = DIR.INV; // clear the port allocation buffer for high priority worm
                int freeOutPort = getFreeOutPort(); // get a free outPut port
                if (freeOutPort != (int)DIR.NI) // NI is to download the truncated worm
                {
                    //Update prefered direction 
                    if (header_buf[0][victimInPort] != null)
                    {
                        header_buf[0][victimInPort].prefDir = freeOutPort; // update header's prefere outport                    
                        flit_buf[1][freeOutPort] = header_buf[0][victimInPort]; // header to output buffer of freeOutPort
                        routeComputeNext(ref flit_buf[1][freeOutPort], (DIR)freeOutPort); // update next-hop's prefered direction                   
                        port_alloc_buf[freeOutPort] = (DIR)victimInPort;
                    }
                    else
                        throw new Exception("Header is null, Please check");

                }
                // free port is not found for deflection
                else
                {
                    if (victimInPort == (int)DIR.LOCAL && header_buf[0][victimInPort] != null) // for local port; the data is not populated yet ??
                    {
                        acceptTruncatedFlit(header_buf[0][victimInPort], 0);

                        if (flit_buf[0][victimInPort] != null) // Does Local Injection places flits in flit_buf[0][Local]??
                        {
                            acceptTruncatedFlit(flit_buf[0][victimInPort], 0);
                            header_buf[0][victimInPort] = null; // clear header buffer
                            flit_buf[0][victimInPort] = null; // clear flit buffer
                        }
                        if (m_injectSlot != null)
                        {
                            acceptLocalTruncatedFlit(m_injectSlot);// this method moves the remaining flit to NI                           
                            m_injectSlot = null; // clear buffer
                        }
                    }
                    else if (victimInPort != (int)DIR.LOCAL && header_buf[0][victimInPort] != null)
                    {
                        acceptTruncatedFlit(header_buf[0][victimInPort], 0);
                        header_buf[0][victimInPort].prefDir = (int)DIR.NI; // Mark the incoming body flits to follow the header
                        //port_alloc_buf[(int)DIR.NI] = (DIR)victimInPort; // point port_allocation as NI buffer
                        if (ejector[0] != DIR.INV && ejector[1] != DIR.INV)
                        { //should never happens
                            throw new Exception("ERROR: both ejectors should not be busy at the same time");
                        }
                        //mark one of the ejector as busy
                        if (ejector[1] == DIR.INV)
                            ejector[1] = (DIR)victimInPort;
                        else
                            ejector[0] = (DIR)victimInPort;
                    }
                    else if (header_buf[0][victimInPort] == null)
                        throw new Exception("Header Not initialized, please check");
                }
            }
        }

        protected int getFreeOutPort()
        {
            //Try BYPASS first
            if (bypassLinkOut[4 - (int)DIR.BYPASS] != null && port_alloc_buf[(int)DIR.BYPASS] == DIR.INV)
            {
                return (int)DIR.BYPASS;
            }
            for (int i = 0; i < 4; i++)
            {
                if (linkOut[i] != null && port_alloc_buf[i] == DIR.INV)
                    return i;
            }

            return (int)DIR.NI; // if no free outPut port found, download the worm to NI buffer, which must be available.
        }
        protected bool isOutPortFree()
        {

            int i = getFreeOutPort();
            if (i != (int)DIR.NI)
            {
                return true;
            }
            return false;
        }
        protected void acceptTruncatedFlit(Flit f, int flag)
        {
            int sub_network = f.subNetwork;
            m_n.injectTruncatedFlits(f, sub_network, flag);
        }
        protected void acceptLocalTruncatedFlit(Flit f)
        {
            int sub_network = f.subNetwork;
            m_n.injectLocallyTruncatedFlits(f, sub_network);
        }
        //copied from RouterBypass
        protected void acceptFlit(Flit f)
        {
            statsEjectFlit(f);
            if (f.packet.nrOfArrivedFlits + 1 == f.packet.nrOfFlits)
                statsEjectPacket(f.packet);

            m_n.receiveFlit(f);
        }
        //Initiate the worm to be ejected
        protected virtual Flit ejectLocal(int ejectorID)
        {
            // eject locally-destined flit (highest-ranked, if multiple)
            // bypass channel can also eject to local port
            Flit ret = null;

            int bestDir = -1;
            for (int dir = 0; dir < CHNL_CNT; dir++)
                if (flit_buf[0][dir] != null &&
                    flit_buf[0][dir].state != Flit.State.Placeholder &&
                    flit_buf[0][dir].dest.ID == ID &&
                    flit_buf[0][dir].isHeadFlit == true &&
                    (ret == null || rank(flit_buf[0][dir], ret) < 0))
                {
#if PKTDUMP
				if (ret != null)
					Console.WriteLine("PKT {0}-{1}/{2} EJECT FAIL router {3}/{4} at time {5}", 
				                  ret.packet.ID, ret.flitNr+1, ret.packet.nrOfFlits, coord, subnet, Simulator.CurrentRound);
#endif
                    ret = flit_buf[0][dir];
                    bestDir = dir;
                }

            if (bestDir != -1)
            {
                if (flit_buf[0][bestDir].isTailFlit)
                {
                    header_buf[0][bestDir] = null;
                    flit_buf[0][bestDir] = null;
                }
                else if(flit_buf[0][bestDir].isHeadFlit)
                {
                    //setup header and reserve ejector
                    header_buf[0][bestDir] = flit_buf[0][bestDir];
                    flit_buf[0][bestDir] = null;
                    ejector[ejectorID] = (DIR)bestDir;
                }else
                    flit_buf[0][bestDir] = null;
            }
#if PKTDUMP
			if (ret != null)
				Console.WriteLine("PKT {0}-{1}/{2} EJECT from port {3} router {4}/{5} at time {6}", 
					                  ret.packet.ID, ret.flitNr+1, ret.packet.nrOfFlits, bestDir, coord, subnet, Simulator.CurrentRound);
#endif
            return ret;
        }
        //truncating from local port does not reache here 
        protected Flit ejectFromPort(int dir)
        {
            Flit ret = null;

            if (flit_buf[0][dir] != null)
            {
                if (flit_buf[0][dir].dest.ID != ID)
                    throw new Exception("Something goes wrong, please check ejectFromPort()");
                else
                {
                    ret = flit_buf[0][dir];
                    flit_buf[0][dir] = null;
                }
            }
            else
            {
                header_buf[0][dir] = null; // clear header buffer
            }

            return ret;
        }
        protected Flit downloadFromPort(int dir)
        {
            Flit ret = null;

            if (flit_buf[0][dir] != null)
            {
                if (header_buf[0][dir].prefDir != (int)DIR.NI)
                    throw new Exception("Something goes wrong, please check downloadFromPort()");
                else
                {
                    ret = flit_buf[0][dir];
                    if (flit_buf[0][dir].isTailFlit)
                        header_buf[0][dir] = null;
                    flit_buf[0][dir] = null;
                }
            }
            else
            {
                header_buf[0][dir] = null; // clear header buffer
            }

            return ret;
        }
        //copied from RouterBypass
        // Override Ejection and Injection
        protected void ejection()
        {
            // STEP 1: Ejection
            int flitsTryToEject = 0;
            for (int dir = 0; dir < CHNL_CNT; dir++)
                if (flit_buf[0][dir] != null && flit_buf[0][dir].dest.ID == ID)
                {
                    flitsTryToEject++;
                    if (flit_buf[0][dir].ejectTrial == 0)
                        flit_buf[0][dir].firstEjectTrial = Simulator.CurrentRound;
                    flit_buf[0][dir].ejectTrial++;
#if PKTDUMP
					Console.WriteLine("PKT {0}-{1}/{2} TRY to EJECT from port {3} router {4}/{5} at time {6}", 
					                  flit_buf[0][dir].packet.ID, flit_buf[0][dir].flitNr+1, flit_buf[0][dir].packet.nrOfFlits, dir,
					                  coord, subnet, Simulator.CurrentRound);
#endif
                }

            Simulator.stats.flitsTryToEject[flitsTryToEject].Add();

            Flit f1 = null, f2 = null;
            for (int i = 0; i < Config.meshEjectTrial; i++)
            {
                if (ejector[i] != DIR.INV)
                {
                    if(header_buf[0][(int)ejector[i]]==null) // this might happens for receiving a truncated worm
                        ejector[i] = DIR.INV; //release ejector1
                    else if (header_buf[0][(int)ejector[i]].dest.ID == ID)
                    {
                        f1 = ejectFromPort((int)ejector[i]);
                        if (f1 != null)
                            acceptFlit(f1);
                        else
                            ejector[i] = DIR.INV; //release ejector1
                    }
                    else if (header_buf[0][(int)ejector[i]].prefDir == (int)DIR.NI) // truncated worm 
                    {
                        f1 = downloadFromPort((int)ejector[i]);
                        if (f1 != null)
                            if (f1.isTailFlit)
                            {
                                acceptTruncatedFlit(f1, 1); // how can I pass the flag = 1
                                //ejector[i] = DIR.INV; //don't release ejector, it might eject twice                                                     
                            }
                            else
                                acceptTruncatedFlit(f1, 0);
                        else
                        {
                            ejector[i] = DIR.INV; //release ejector1 
                           //TODO:: mark injector is free ?? or left as it is, at the end of injector_buffer, it will automatically clear the flag (in Node)
                        }
                    }
                }
            }

            for (int i = 0; i < Config.meshEjectTrial; i++)
            {
                if (ejector[i] == DIR.INV)
                {
                    // Only support dual ejection (MAX.Config.meshEjectTrial = 2)
                    Flit eject = ejectLocal(i);
                    if (eject != null) 
                        acceptFlit(eject);  // Eject flit	
                }
            }
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
      Each worm can only be bypassed X times. Once it reaches the limit, the worm is deflected. 
      TODO: x >= #_subnet will cause deadlock such that the replicated header will compete for the bypass port with its subsequent body/tail flits 

    if truncation is detected  
      replicate the header of truncated worm on unwanted and available port {N, E, S, W, B}.
      If no such port exist, we will not perform header replication. In this case, no port will 
      be allocated for the subsequent flits.  The subsequent flits of the truncated worm can be 
      ejected temporarily from the network. It will be stored in the NI injection buffer and 
      reinjected as a new worm based on the injection rule. This creates two issues: 
      1) the truncated worm competets the bandwidth with local-destined worm
      2) it may break the OF rule, causing livelock. 

    To resolve this, we can perform dual-ejection. The first eject stage is dedicated to removing local destined 
    It can
      1) provide delivery guarantee at the presence of header replication
      2) resolve network contention 
 
  */
        protected bool isOutPortEnable(int dir)
        {
            bool ret = false;
            
            if (dir != (int)DIR.BYPASS)
            {
                ret = linkOut[dir] != null;
            }
            else
            {
                ret = bypassLinkOut[4 - (int)DIR.BYPASS] != null;
            }
            return ret;
        }
        protected void port_alloc()
        {
            if (newHeader) // a new header is arrived, so port allocation must be executed
            {
                newHeader = false; // clear flag
                if (newIsOldest) // Maps highest tenpHeader[0] first
                {
                    //check for highestPriority worm
                    if (tempHeader[0] != null)
                    {
                        if (!isOutPortEnable(tempHeader[0].prefDir))
                            throw new Exception("Prefered port not enabled");
                        if (port_alloc_buf[tempHeader[0].prefDir] != (DIR)tempHeader[0].inDir)
                        {
                            /*
                    if (port_alloc_buf[tempHeader[0].prefDir] != DIR.INV)
                    {
                        throw new Exception("Port must be empty here but it seems something wrong");
                    }
                    */
                            port_alloc_buf[tempHeader[0].prefDir] = (DIR)tempHeader[0].inDir;
                        }
                    }
                    newIsOldest = false;
                }
                // Assign the remaining ports
                for (int dir = 0; dir < CHNL_CNT; dir++)
                {
                    if (header_buf[0][dir] == null) continue; // no need to assign outPort
                    if (header_buf[0][dir].prefDir == (int)DIR.NI) continue; // this represent NI buffer and already adjusted
                    if (header_buf[0][dir].prefDir == (int)DIR.INV) //locally destined
                    {
                        if (header_buf[0][dir].dest.ID == ID) //locally destined worm
                        {
                            if (ejector[0] != (DIR)dir && ejector[1] != (DIR)dir) // both ejectors are busy
                            {
                                //defelct the worm to available port
                                int freeOutPort = getFreeOutPort(); // get a free outPut port
                                if (freeOutPort != (int)DIR.NI) // NI is to download the truncated worm
                                {
                                    header_buf[0][dir].prefDir = freeOutPort; // update header's prefere outport                    
                                    flit_buf[0][dir].prefDir = freeOutPort; // header to output buffer of freeOutPort                                                   
                                    port_alloc_buf[freeOutPort] = (DIR)dir;
                                }
                                else
                                    throw new Exception("Ports not available, Please check port_alloc()");
                            }
                            else
                            {
                                if (ejector[0] == DIR.INV)
                                    ejector[0] = (DIR)dir;
                                else if (ejector[1] == DIR.INV)
                                    ejector[1] = (DIR)dir;
                                else
                                    throw new Exception("Both ejectors are busy, please check port_alloc()");
                            }
                            continue; // locally destined flit:TODO (investigate more on it)
                        }
                        else
                            throw new Exception("Seems to be locally destined, but not, please check port_alloc()");
                    }

                    if (header_buf[0][dir].prefDir == (int)DIR.LOCAL)
                        throw new Exception("Prefered port is Local port, Please verify");

                    if (isOutPortEnable(header_buf[0][dir].prefDir))
                    {
                        if (port_alloc_buf[header_buf[0][dir].prefDir] == (DIR)dir) // already mapped 
                            continue;
                        else if (port_alloc_buf[header_buf[0][dir].prefDir] == DIR.INV) // prefered direction available
                        {
                            port_alloc_buf[header_buf[0][dir].prefDir] = (DIR)dir;  // no need to update prefered direction in header                         
                            continue;
                        }
                    }
                    //prefered port is already taken, check for a free port only
                    if (isOutPortEnable((int)DIR.BYPASS) && port_alloc_buf[(int)DIR.BYPASS] == DIR.INV)  // BYPASS is available
                    {
                        port_alloc_buf[(int)DIR.BYPASS] = (DIR)dir;
                        header_buf[0][dir].prefDir = (int)DIR.BYPASS;
                        continue;
                    }
                    else //find an availabale port
                    {
                        bool found = false;
                        for (int outdir = 0; outdir < 4; outdir++) // BYPASS is already checked
                        {
                            if (isOutPortEnable(outdir))
                                if (port_alloc_buf[outdir] == DIR.INV) // free port ?
                                {
                                    port_alloc_buf[outdir] = (DIR)dir;
                                    header_buf[0][dir].prefDir = outdir; // update header                                   
                                    found = true;
                                    break;
                                }
                        }
                        if (!found)
                        {
                            throw new Exception("Port allocation fail, please check port_alloc()");
                        }
                    }
                }
            }
        }
    }
}
