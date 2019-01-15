//#define DEBUG
//#define PKTDUMP
//#define CLEARUP
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

            clearTempHeader();

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
        protected void Compare(Flit f1, Flit f2)
        {
            if (f1.packet.dest.ID == f2.packet.dest.ID &&
                f1.packet.src.ID == f2.packet.src.ID &&
                f1.packet.creationTime == f2.packet.creationTime &&
                f1.packet.nrOfFlits == f2.packet.nrOfFlits &&
                f1.inDir == f2.inDir &&
                f1.prefDir == f2.prefDir)
            {
                return;
            }
            else
            {
                //Console.WriteLine("Cloned flits are different");
                throw new Exception("Cloned flits are different");
            }

        }
        void clearTempHeader()
        {
            //dbg_idx = 0;
            for (int dir = 0; dir < CHNL_CNT; dir++)
            {
                tempHeader[dir] = null;
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
                    Console.WriteLine("Router {0}/{8} : Buffer Outport {1} {2} @ {3}:from INPort {4} for Destination->{5}::{6} subNetwork {7} ",
                        coord.ID, Simulator.network.portMap(dir), linkOut[dir].In.ToString(), Simulator.CurrentRound, linkOut[dir].In.inDir,
                        linkOut[dir].In.dest, linkOut[dir].In.packet.nrOfFlits, linkOut[dir].In.subNetwork, subnet);
#endif

                }
                else if (dir == (int)DIR.BYPASS)
                {
                    if (bypassLinkOut[dir - 4].In != null)
                        throw new Exception("Bypass port is not idle");
                    if (flit_buf[1][dir].subNetwork == 0)
                        flit_buf[1][dir].subNetwork = 1;
                    else
                        flit_buf[1][dir].subNetwork = 0;

                    bypassLinkOut[dir - 4].In = flit_buf[1][dir];
#if DEBUG
                    Console.WriteLine("Router {0}/{8} : Buffer Outport {1} {2} @ {3}:from INPort {4} for Destination->{5}::{6} subNetwork {7} ",
                        coord.ID, Simulator.network.portMap(dir), bypassLinkOut[dir - 4].In.ToString(), Simulator.CurrentRound, bypassLinkOut[dir - 4].In.inDir,
                        bypassLinkOut[dir - 4].In.dest, bypassLinkOut[dir - 4].In.packet.nrOfFlits, bypassLinkOut[dir - 4].In.subNetwork, subnet);
#endif

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
                    if (header_buf[0][from] != null && flit_buf[0][from] != null)
                    {
                        if (flit_buf[0][from].isHeadFlit)
                        {
                            if (dir < 4) // Only four neighbors exist, BYPASS router has the same coordinate
                                routeComputeNext(ref flit_buf[0][from], (DIR)dir);
                            else
                                routeCompute(ref flit_buf[0][from]);
                        }
                        flit_buf[1][dir] = flit_buf[0][from]; // move the flit from-port buffer to to-port buffer
                        if (flit_buf[1][dir].isTailFlit)
                        {
                            port_alloc_buf[dir] = DIR.INV; // if tail flit, reset port allocation
                            header_buf[0][from] = null;
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
                            if (dir < 4) // Only four neighbors exist, BYPASS router has the same coordinate
                                routeComputeNext(ref flit_buf[0][from], (DIR)dir);
                            else
                                routeCompute(ref flit_buf[0][from]);
                        }
                        flit_buf[1][dir] = flit_buf[0][from]; // move the flit from-port buffer to to-port buffer
                        if (flit_buf[1][dir].isTailFlit)
                        {
                            port_alloc_buf[dir] = DIR.INV; // if tail flit, reset port allocation
                            header_buf[0][from] = null;
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
                    Console.WriteLine("Router {0}/{8} : Buffer Inport {1} {2} @ {3}:from INPort {4} for Destination->{5}::{6} subNetwork {7} FlitNr = {9} isHead {10}",
                         coord.ID, Simulator.network.portMap(dir), linkIn[dir].Out.ToString(), Simulator.CurrentRound, linkIn[dir].Out.inDir,
                         linkIn[dir].Out.dest, linkIn[dir].Out.packet.nrOfFlits, linkIn[dir].Out.subNetwork, subnet, linkIn[dir].Out.flitNr, linkIn[dir].Out.isHeadFlit);
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
                    Console.WriteLine("Router {0}/{8} : Buffer Inport {1} {2} @ {3}:from INPort {4} for Destination->{5}::{6} subNetwork {7} FlitNr = {9} isHead {10}",
                        coord.ID, Simulator.network.portMap(4), bypassLinkIn[bp].Out.ToString(), Simulator.CurrentRound, bypassLinkIn[bp].Out.inDir,
                        bypassLinkIn[bp].Out.dest, bypassLinkIn[bp].Out.packet.nrOfFlits, bypassLinkIn[bp].Out.subNetwork, subnet, bypassLinkIn[bp].Out.flitNr, bypassLinkIn[bp].Out.isHeadFlit);

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
            ret = (m_injectSlot == null);
            return ret;
        }
        public bool canInjectLocal()
        {
            bool ret = true;
            //Check whether truncation happens to LOCAL injection
            if (header_buf[0][(int)DIR.LOCAL] != null &&
                isOutPortEnable((int)header_buf[0][(int)DIR.LOCAL].prefDir) &&
                port_alloc_buf[(int)header_buf[0][(int)DIR.LOCAL].prefDir] == DIR.LOCAL &&
                flit_buf[1][(int)header_buf[0][(int)DIR.LOCAL].prefDir] != null)
            {
                ret = false;
                Console.WriteLine("Local Injection throttle due to truncation");
            }
            return ret;
        }
        private void _inject(Flit f)
        {
            // Console.WriteLine(" Compare 1 " + object.ReferenceEquals(f, header_buf[0][(int)DIR.LOCAL]));
            if (port_alloc_buf[header_buf[0][(int)DIR.LOCAL].prefDir] != DIR.LOCAL) // need to double check this. Better way is first assigned port allocation and use same port 
            {
                throw new Exception("Port must be allocated here but it seems something wrong");
            }
            // what should we do for header and port allocation table??
            int dir = header_buf[0][(int)DIR.LOCAL].prefDir;
            f.inDir = (int)DIR.LOCAL;// dir;
            if (f.isHeadFlit && dir < 4)
            {
                routeComputeNext(ref f, (DIR)dir);
            }
            else
            {
                routeCompute(ref f);
            }
            if (flit_buf[1][dir] != null)
            {
                throw new Exception("flit_buf[1][dir] is not clear, please check _inject()");
            }
            flit_buf[1][dir] = f; // move to output buffer for TX
            if (f.isTailFlit)
            {
                port_alloc_buf[header_buf[0][(int)DIR.LOCAL].prefDir] = DIR.INV; // Reset Port_alloc buffer
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
                for (int i = 0; i < CHNL_CNT - 1; i++)
                {
                    if (isOutPortEnable(i) &&
                        port_alloc_buf[i] == DIR.LOCAL &&
                        flit_buf[1][i] == null)
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
            if (canInjectLocal())
            {
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
                        if (isOutPortEnable(m_injectSlot.prefDir) &&
                            port_alloc_buf[m_injectSlot.prefDir] == DIR.INV &&
                            flit_buf[1][m_injectSlot.prefDir] == null)
                        {
                            port_alloc_buf[m_injectSlot.prefDir] = DIR.LOCAL;
                            outPort = m_injectSlot.prefDir;
                        }
                        else
                        {   //check BYPASS first
                            if (isOutPortEnable((int)DIR.BYPASS) &&
                                port_alloc_buf[(int)DIR.BYPASS] == DIR.INV &&
                                flit_buf[1][(int)DIR.BYPASS] == null)
                            {
                                port_alloc_buf[(int)DIR.BYPASS] = DIR.LOCAL; // assign port in port allocator
                                outPort = (int)DIR.BYPASS;
                            }
                            else
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    if (isOutPortEnable(i) &&
                                        port_alloc_buf[i] == DIR.INV &&
                                        flit_buf[1][i] == null)
                                    {
                                        port_alloc_buf[i] = DIR.LOCAL; // assign port in port allocator
                                        outPort = i;
                                        break;
                                    }
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
                        m_injectSlot.inDir = (int)DIR.LOCAL;
                        // Console.WriteLine(" Compare 1" + object.ReferenceEquals(m_injectSlot, header_buf[0][(int)DIR.LOCAL]));
                        header_buf[0][(int)DIR.LOCAL] = m_injectSlot.CloneSource();

                        tempHeader[(int)DIR.LOCAL] = m_injectSlot.CloneSource();
                        Compare(header_buf[0][(int)DIR.LOCAL], tempHeader[(int)DIR.LOCAL]);
                        // Console.WriteLine(" Compare 2" + object.ReferenceEquals(m_injectSlot, header_buf[0][(int)DIR.LOCAL]));
                    }
                    _inject(m_injectSlot);
                    m_injectSlot = null; // clear the m_injectSlot
                }
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
            if (f.takeX)
            {
                if (pd.xDir != (int)DIR.INV)
                    f.prefDir = pd.xDir;
                else
                    f.prefDir = pd.yDir;

                f.takeX = false;
            }
            else
            {
                if (pd.yDir != (int)DIR.INV)
                    f.prefDir = pd.yDir;
                else
                    f.prefDir = pd.xDir;

                f.takeX = true;

            }

        }

        void routeComputeNext(ref Flit f, DIR dir)
        {
            PreferredDirection pd;

            pd = determineDirection(f.dest, neigh[(int)dir].coord);

            if (f.takeX)
            {
                if (pd.xDir != (int)DIR.INV)
                    f.prefDir = pd.xDir;
                else
                    f.prefDir = pd.yDir;

                f.takeX = false;
            }
            else
            {
                if (pd.yDir != (int)DIR.INV)
                    f.prefDir = pd.yDir;
                else
                    f.prefDir = pd.xDir;

                f.takeX = true;

            }
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
            // printMe(printHeader());
            for (int dir = 0; dir < 4; dir++) // first 4 ports compete for each other when sorted
            {
                if (flit_buf[0][dir] != null)
                {
                    if (flit_buf[0][dir].isHeadFlit)
                    {
                        newHeader = true;
                        header_buf[0][dir] = flit_buf[0][dir].CloneSource();
                        Compare(header_buf[0][dir], flit_buf[0][dir]);
                        if (isOlder(header_buf[0][dir], tempHeader[0])) // tempHeader is sorted                       
                            newIsOldest = true;
                    }
                    if (header_buf[0][dir] == null)// || (header_buf[0][dir].packet.ID != flit_buf[0][dir].packet.ID)) // NI does not works
                    {
                        // Console.WriteLine("Router {0}/{1} Flit-Packet no {2} in Port {3} @ {4}",
                        //     ID, subnet, flit_buf[0][dir], dir, Simulator.CurrentRound);
                        throw new Exception("Flit without its' header information arrived, check setHeaderTable()");
                    }
                }
                else
                    header_buf[0][dir] = null;
            }
            //Check BYPASS port
            if (flit_buf[0][(int)DIR.BYPASS] != null)
            {
                if (flit_buf[0][(int)DIR.BYPASS].isHeadFlit)
                {
                    newHeader = true; // BYPASS port does not compet for truncation
                    header_buf[0][(int)DIR.BYPASS] = flit_buf[0][(int)DIR.BYPASS].CloneSource();
                    Compare(header_buf[0][(int)DIR.BYPASS], flit_buf[0][(int)DIR.BYPASS]);
                }
                if (header_buf[0][(int)DIR.BYPASS] == null)// || header_buf[0][(int)DIR.BYPASS].packet.ID != flit_buf[0][(int)DIR.BYPASS].packet.ID)
                {
                    // Console.WriteLine("Router {0}/{1} Flit-Packet no {2} in Port {3} @ {4}",
                    //    ID, subnet, flit_buf[0][(int)DIR.BYPASS], (int)DIR.BYPASS, Simulator.CurrentRound);
                    throw new Exception("Flit without its' header information arrived, check setHeaderTable()");
                }
            }
            else
            {
                header_buf[0][(int)DIR.BYPASS] = null;
            }

            // printMe(printHeader());
            /*
            printPacketLocation(265979);
            printPacketLocation(278934);
            printPacketLocation(283930);
            printPacketLocation(329922);
            printPacketLocation(364585);
            // printPacketLocation(405330);
            // printPacketLocation(668550);
            // */
        }
        protected string printHeader()
        {
            string text = "Header Information ";
            int i;
            for (i = 0; i < 6; i++)
            {
                if (header_buf[0][i] != null)
                    text = text + "[" + i + "] = " + header_buf[0][i].packet.ID + " prefDir = " + header_buf[0][i].prefDir + " InfDir = " + header_buf[0][i].inDir + ", ";
            }
            //Console.WriteLine("Router {0}/{1}:{2}", ID, subnet, text);
            return text;
        }

        protected void printPacketLocation(ulong packetNo)
        {

            int i;
            for (i = 0; i < 6; i++)
            {
                if ((header_buf[0][i] != null) && (header_buf[0][i].packet.ID == packetNo) && flit_buf[0][i] != null)
                    Console.WriteLine("Router {0}/{1} arrived port {2} prefered out going port {3}: Packet-->{4} @{5} Source {6} Destination {7}",
                        ID, subnet, i, header_buf[0][i].prefDir, flit_buf[0][i], Simulator.CurrentRound, flit_buf[0][i].packet.src.ID, flit_buf[0][i].packet.dest.ID);
            }
        }

        protected void copyHeaderToTemp()

        {
            int i = 0;
            for (i = 0; i < CHNL_CNT; i++)
            {
                if (header_buf[0][i] != null)
                {
                    tempHeader[i] = header_buf[0][i].CloneSource(); // header_buf[0] holds the most recent headers
                    Compare(tempHeader[i], header_buf[0][i]);
                }
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
        public override int rank(Flit f1, Flit f2)
        {
            if (f1 == null && f2 == null) return 0;
            if (f1 == null) return 1;
            if (f2 == null) return -1;

            int c0 = 0;

            int c1 = 0, c2 = 0;
            if (f1.packet != null && f2.packet != null)
            {
                c1 = -age(f1).CompareTo(age(f2));
                c2 = f1.packet.ID.CompareTo(f2.packet.ID);
            }

            int c3 = f1.flitNr.CompareTo(f2.flitNr);

            int zerosSeen = 0;
            foreach (int i in new int[] { c0, c1, c2, c3 })
            {
                if (i == 0)
                    zerosSeen++;
                else
                    break;
            }
            Simulator.stats.net_decisionLevel.Add(zerosSeen);
            return
                (c0 != 0) ? c0 :
                (c1 != 0) ? c1 :
                 c2;
            /*
            (c0 != 0) ? c0 :
            (c1 != 0) ? c1 :
            (c2 != 0) ? c2 :
            c3;
            */
        }
        public ulong age(Flit f)
        {
            if (Config.net_age_arbitration)
                return Simulator.CurrentRound - f.packet.injectionTime;
            else
                return (Simulator.CurrentRound - f.packet.creationTime) /
                    (ulong)Config.cheap_of;
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

        protected bool isNIDownloading()
        {
            bool ret = false;
            for (int i = 0; i < 2; i++)
            {
                if (ejector[i] != DIR.INV)
                {
                    if (header_buf[0][(int)ejector[i]].prefDir == (int)DIR.NI)
                    {
                        ret = true;
                    }
                }
            }
            return ret;
        }

        protected bool canTruncate()
        {
            bool ret = false;
            if ((isNIDownloading() == false) ||
                (getFreeOutPort(0) != (int)DIR.NI))
            {
                ret = false;
            }
            return ret;
        }
        /* Look-ahead truncation
  // Override truncation detect

  Problem: for a given flit, whether or not there is any older worm which will arrive next cycle competes for the same output port. If there is, its header flit must be replicated and injected to an available port, or informs the ejector to remove its subsequent body flit next cycle.

  N, E, S, W, L might be truncated by one of N-2 incoming flit
  B will not be truncated 

  Simplified problem: find out if a flit using port i is younger than anyone of the incoming flit and the particular older flit also requests the same output port i.

*/
        //This method test whether the truncation happens or not; if happents, it also mapped the prefered port mapp for truncation
        protected void truncateWorm()
        {
            int contendingPort = (int)DIR.INV;
            int freeOutPort = (int)DIR.INV;
            newIsOldest = true;
            cleanUpMapping(); // Cleanup mapping if not cleared
            if (canTruncate() && newIsOldest)
            {
                printMe("TruncateWorm executed");
                newIsOldest = false; // reset flag for next round
                // oldest is locally destined
                if ((tempHeader[0] != null) &&
                    ((tempHeader[0].prefDir == (int)DIR.INV) || (tempHeader[0].prefDir == (int)DIR.NI)))
                {
                    if (ejector[0] == (DIR)tempHeader[0].inDir ||
                        ejector[1] == (DIR)tempHeader[0].inDir)
                    {   // already attached to ejector
                        ;
                    }
                    /*you can't map to ejector here
                    else if (ejector[0] == DIR.INV || ejector[1] == DIR.INV) // map to free ejector (NI is generated from truncation)
                    {
                        if (ejector[0] == DIR.INV)
                        {
                            ejector[0] = (DIR)tempHeader[0].inDir;
                        }
                        else
                        {
                            ejector[1] = (DIR)tempHeader[0].inDir;
                        }
                    }
                    */
                    else //deflect to BYPASS port
                    {
                        if (port_alloc_buf[(int)DIR.BYPASS] == DIR.INV) // no need to check flit_buf[1][BYPASS]==null
                        {
                            tempHeader[0].prefDir = (int)DIR.BYPASS; // divert the worm through BYPASS 
                            if (header_buf[0][tempHeader[0].inDir] == null)
                            {
                                // Console.WriteLine("1:tempHeader[0]={0} inDir = {1} PreferDir ={2}", tempHeader[0], tempHeader[0].inDir, tempHeader[0].prefDir);
                                throw new Exception("1:Uninitialized Header, Please check isTruncationHappens()");
                            }
                            header_buf[0][tempHeader[0].inDir].prefDir = (int)DIR.BYPASS;
                            port_alloc_buf[(int)DIR.BYPASS] = (DIR)tempHeader[0].inDir;
                        }
                        else
                        {
                            tempHeader[0].prefDir = (int)DIR.BYPASS;
                            if (header_buf[0][tempHeader[0].inDir] == null)
                            {
                                // Console.WriteLine("1:tempHeader[0]={0} inDir = {1} PreferDir ={2}", tempHeader[0], tempHeader[0].inDir, tempHeader[0].prefDir);
                                throw new Exception("1:Uninitialized Header, Please check isTruncationHappens()");
                            }
                            header_buf[0][tempHeader[0].inDir].prefDir = (int)DIR.BYPASS;
                            contendingPort = (int)port_alloc_buf[(int)DIR.BYPASS];
                            //map to outgoing port
                            port_alloc_buf[tempHeader[0].prefDir] = (DIR)tempHeader[0].inDir;

                            //If contending port has incomming flit, truncate;
                            if ((contendingPort != (int)DIR.INV) &&
                                (header_buf[0][contendingPort] != null) &&
                                (header_buf[0][contendingPort].prefDir == tempHeader[0].prefDir))
                            {
                                freeOutPort = getFreeOutPort(contendingPort);
                                if (freeOutPort != (int)DIR.NI) // NI is to download the truncated worm
                                {
                                    header_buf[0][contendingPort].prefDir = freeOutPort; // update header's prefere outport                    
                                    flit_buf[1][freeOutPort] = header_buf[0][contendingPort].CloneSource(); // header to output buffer of freeOutPort
                                    Compare(flit_buf[1][freeOutPort], header_buf[0][contendingPort]);
                                    flit_buf[1][freeOutPort].isTruncatedHead = true; // set truncatedHead flag
                                    if (freeOutPort < 4)
                                    {
                                        routeComputeNext(ref flit_buf[1][freeOutPort], (DIR)freeOutPort); // update next-hop's prefered direction
                                    }
                                    else
                                    {
                                        routeCompute(ref flit_buf[1][freeOutPort]);
                                    }
                                    port_alloc_buf[freeOutPort] = (DIR)contendingPort;
                                    //Console.WriteLine("1:: Router {0}/{1}:Truncation Happens for port {2} from port {3} to port {4} PacketID {5} by {6}",
                                    //       ID, subnet, contendingPort, tempHeader[0].prefDir, freeOutPort, header_buf[0][contendingPort], tempHeader[0]);
                                }
                                // free port is not found for deflection
                                else
                                {
                                    if (contendingPort == (int)DIR.LOCAL)
                                    {
                                        header_buf[0][contendingPort].isTruncatedHead = true; // set truncated flag
                                        acceptTruncatedFlit(header_buf[0][contendingPort], 0);
                                        header_buf[0][contendingPort] = null;

                                        if (flit_buf[0][contendingPort] != null) // Does Local Injection places flits in flit_buf[0][Local]?? (NO)
                                        {
                                            acceptTruncatedFlit(flit_buf[0][contendingPort], 0);
                                            //header_buf[0][contendingPort] = null; // clear header buffer
                                            flit_buf[0][contendingPort] = null; // clear flit buffer
                                        }
                                        if (m_injectSlot != null) // if injecting, Flit must be there
                                        {
                                            acceptLocalTruncatedFlit(m_injectSlot);// this method moves the remaining flit to NI                           
                                            m_injectSlot = null; // clear buffer
                                        }
                                        // Console.WriteLine("1:: Local_Download: Router {0}/{1}:Truncation Happens for port {2} from port {3} to port {4} PacketID {5} by {6}",
                                        //    ID, subnet, contendingPort, tempHeader[0].prefDir, DIR.NI, header_buf[0][contendingPort], tempHeader[0]);

                                    }
                                    else if (contendingPort != (int)DIR.LOCAL)
                                    {
                                        Flit temp = header_buf[0][contendingPort].CloneSource(); // make a copy of header and proceed
                                        Compare(temp, header_buf[0][contendingPort]);
                                        temp.isTruncatedHead = true; // set truncated flag
                                        acceptTruncatedFlit(temp, 0);
                                        header_buf[0][contendingPort].prefDir = (int)DIR.NI; // Mark the incoming body flits to follow the header
                                                                                             //port_alloc_buf[(int)DIR.NI] = (DIR)victimInPort; // point port_allocation as NI buffer
                                        if (ejector[0] != DIR.INV && ejector[1] != DIR.INV)
                                        { //should never happens
                                            throw new Exception("ERROR: both ejectors should not be busy at the same time");
                                        }
                                        //mark one of the ejector as busy
                                        if (ejector[1] == DIR.INV)
                                            ejector[1] = (DIR)contendingPort;
                                        else if (ejector[0] == DIR.INV)
                                            ejector[0] = (DIR)contendingPort;

                                        // Console.WriteLine("2:: Local_Download: Router {0}/{1}:Truncation Happens for port {2} from port {3} to port {4} PacketID {5} by {6}",
                                        //     ID, subnet, contendingPort, tempHeader[0].prefDir, DIR.NI, header_buf[0][contendingPort], tempHeader[0]);
                                    }
                                }
                            }
                        }

                    }
                }
                else if (tempHeader[0] != null) // oldest is not the locally destined
                {
                    if (tempHeader[0].prefDir < (int)DIR.LOCAL &&
                        tempHeader[0].inDir == (int)port_alloc_buf[tempHeader[0].prefDir])
                    { // already mapped, no thing to do
                        ;
                    }
                    else if ((int)tempHeader[0].prefDir < (int)DIR.LOCAL)
                    {
                        if (!isOutPortEnable((int)tempHeader[0].prefDir))
                        {
                            throw new Exception("Highest ranked port is not enable, plese check isTruncationHappens()");
                        }
                        if (port_alloc_buf[(int)tempHeader[0].prefDir] == DIR.INV)
                        {
                            port_alloc_buf[tempHeader[0].prefDir] = (DIR)tempHeader[0].inDir;
                        }
                        else
                        {
                            if (header_buf[0][tempHeader[0].inDir] == null)
                            {
                                // Console.WriteLine("2:tempHeader[0]={0} inDir = {1} PreferDir ={2}", tempHeader[0], tempHeader[0].inDir, tempHeader[0].prefDir);
                                throw new Exception("2:Uninitialized Header, Please check isTruncationHappens()");
                            }
                            if (header_buf[0][tempHeader[0].inDir].prefDir != tempHeader[0].prefDir)
                            {
                                throw new Exception("Header and TempHeader not match, Please check isTruncationHappens()");
                            }
                            contendingPort = (int)port_alloc_buf[tempHeader[0].prefDir]; //get to be truncated port

                            port_alloc_buf[tempHeader[0].prefDir] = (DIR)tempHeader[0].inDir; //mapp the oldest worm 

                            if ((contendingPort != (int)DIR.INV) &&
                                (header_buf[0][contendingPort] != null) &&
                                (header_buf[0][contendingPort].prefDir == tempHeader[0].prefDir))
                            {
                                freeOutPort = getFreeOutPort(contendingPort);
                                if (freeOutPort != (int)DIR.NI) // NI is to download the truncated worm
                                {
                                    header_buf[0][contendingPort].prefDir = freeOutPort; // update header's prefered outport                    
                                    flit_buf[1][freeOutPort] = header_buf[0][contendingPort].CloneSource(); // header to output buffer of freeOutPort
                                    Compare(flit_buf[1][freeOutPort], header_buf[0][contendingPort]);
                                    flit_buf[1][freeOutPort].isTruncatedHead = true;
                                    if (freeOutPort < 4)
                                    {
                                        routeComputeNext(ref flit_buf[1][freeOutPort], (DIR)freeOutPort); // update next-hop's prefered direction
                                    }
                                    else
                                    {
                                        routeCompute(ref flit_buf[1][freeOutPort]);
                                    }
                                    port_alloc_buf[freeOutPort] = (DIR)contendingPort;
                                    //Console.WriteLine("2:: Router {0}/{1}:Truncation Happens for port {2} from port {3} to port {4} PacketID {5} by {6}",
                                    //        ID, subnet, contendingPort, tempHeader[0].prefDir, freeOutPort, header_buf[0][contendingPort], tempHeader[0]);
                                }
                                // free port is not found for deflection
                                else
                                {
                                    if (contendingPort == (int)DIR.LOCAL) // for local port; the data is not populated yet ??
                                    {
                                        header_buf[0][contendingPort].isTruncatedHead = true;
                                        acceptTruncatedFlit(header_buf[0][contendingPort], 0);
                                        header_buf[0][contendingPort] = null;//Updated

                                        if (flit_buf[0][contendingPort] != null) // Does Local Injection places flits in flit_buf[0][Local]?? (NO)
                                        {
                                            acceptTruncatedFlit(flit_buf[0][contendingPort], 0);
                                            //header_buf[0][contendingPort] = null; // clear header buffer
                                            flit_buf[0][contendingPort] = null; // clear flit buffer
                                        }
                                        if (m_injectSlot != null)
                                        {
                                            acceptLocalTruncatedFlit(m_injectSlot);// this method moves the remaining flit to NI                           
                                            m_injectSlot = null; // clear buffer
                                        }
                                    }
                                    else if (contendingPort != (int)DIR.LOCAL)
                                    {
                                        Flit temp = header_buf[0][contendingPort].CloneSource(); // make a copy of header and proceed
                                        Compare(temp, header_buf[0][contendingPort]);
                                        temp.isTruncatedHead = true;
                                        acceptTruncatedFlit(temp, 0);
                                        header_buf[0][contendingPort].prefDir = (int)DIR.NI; // Mark the incoming body flits to follow the header
                                                                                             //port_alloc_buf[(int)DIR.NI] = (DIR)victimInPort; // point port_allocation as NI buffer
                                        if (ejector[0] != DIR.INV && ejector[1] != DIR.INV)
                                        { //should never happens
                                            throw new Exception("ERROR: both ejectors should not be busy at the same time");
                                        }
                                        //mark one of the ejector as busy
                                        if (ejector[1] == DIR.INV)
                                            ejector[1] = (DIR)contendingPort;
                                        else if (ejector[0] == DIR.INV)
                                            ejector[0] = (DIR)contendingPort;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Please verify the Prefered direction, Check truncateWorm()");
                    }
                }
            }
        }

        /* This method truncates the worm and forward to the one of the available port if any; 
        * otherwise, it directs the truncated worm to to NI buffer with updating the port allocation.
        */
                    /*
                    protected void truncateWorm()
                    {
                        int victimInPort;
                        bool mapped = false;
                        isTruncationHappens(out victimInPort);

                        if (victimInPort != (int)DIR.INV && header_buf[0][victimInPort] != null)
                        {
                            for (int i = 0; i < CHNL_CNT - 1; i++)
                            {
                                if ((int)port_alloc_buf[i] == victimInPort)
                                {
                                    if (header_buf[0][victimInPort].prefDir == i)
                                    {
                                        mapped = true;
                                        break;
                                    }
                                }
                            }
                            if (!mapped)
                            {
                                int freeOutPort = getFreeOutPort(); // get a free outPut port
                                if (freeOutPort != (int)DIR.NI) // NI is to download the truncated worm
                                {
                                    header_buf[0][victimInPort].prefDir = freeOutPort; // update header's prefere outport                    
                                    flit_buf[1][freeOutPort] = header_buf[0][victimInPort].CloneSource(); // header to output buffer of freeOutPort
                                    Compare(flit_buf[1][freeOutPort], header_buf[0][victimInPort]);
                                    if (freeOutPort < 4)
                                    {
                                        routeComputeNext(ref flit_buf[1][freeOutPort], (DIR)freeOutPort); // update next-hop's prefered direction
                                    }
                                    port_alloc_buf[freeOutPort] = (DIR)victimInPort;
                                }
                                // free port is not found for deflection
                                else
                                {
                                    if (victimInPort == (int)DIR.LOCAL) // for local port; the data is not populated yet ??
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
                                    else if (victimInPort != (int)DIR.LOCAL)
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
                                        else if (ejector[0] == DIR.INV)
                                            ejector[0] = (DIR)victimInPort;
                                    }                   
                                }
                            }
                        }
                    }
                    */

                    protected int getFreeOutPort(int escapeIfPossible)
        {
            //Try BYPASS first
            if (escapeIfPossible != (int)DIR.BYPASS)
            {
                if (bypassLinkOut[4 - (int)DIR.BYPASS] != null &&
                    port_alloc_buf[(int)DIR.BYPASS] == DIR.INV &&
                    flit_buf[1][(int)DIR.BYPASS] == null)
                {
                    return (int)DIR.BYPASS;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (i != escapeIfPossible)
                {
                    if (linkOut[i] != null &&
                        port_alloc_buf[i] == DIR.INV &&
                        flit_buf[1][i] == null)
                    {
                        return i;
                    }
                }
            }
            //bounce to same direction
            if (escapeIfPossible == (int)DIR.BYPASS)
            {
                if (bypassLinkOut[4 - (int)DIR.BYPASS] != null &&
                    port_alloc_buf[(int)DIR.BYPASS] == DIR.INV &&
                    flit_buf[1][(int)DIR.BYPASS] == null)
                {
                    return (int)DIR.BYPASS;
                }
            }
            if (escapeIfPossible < 4)
            {
                if (linkOut[escapeIfPossible] != null &&
                    port_alloc_buf[escapeIfPossible] == DIR.INV &&
                    flit_buf[1][escapeIfPossible] == null)
                {
                    return escapeIfPossible;
                }
            }
            //none are available
            return (int)DIR.NI; // if no free outPut port found, download the worm to NI buffer, which must be available.
        }

        //getFreeOutPortForNextCycle()::no need to check whetehr flit_buf[1][dir] == null or not
        protected int getFreeOutPortForNextCycle(int escapeIfPossible)
        {
            //Try BYPASS first
            if (escapeIfPossible != (int)DIR.BYPASS)
            {
                if (bypassLinkOut[4 - (int)DIR.BYPASS] != null &&
                    port_alloc_buf[(int)DIR.BYPASS] == DIR.INV)
                {
                    return (int)DIR.BYPASS;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (i != escapeIfPossible)
                {
                    if (linkOut[i] != null &&
                        port_alloc_buf[i] == DIR.INV)
                    {
                        return i;
                    }
                }
            }
            //bounce to same direction
            if (escapeIfPossible == (int)DIR.BYPASS)
            {
                if (bypassLinkOut[4 - (int)DIR.BYPASS] != null &&
                    port_alloc_buf[(int)DIR.BYPASS] == DIR.INV)
                {
                    return (int)DIR.BYPASS;
                }
            }
            if (escapeIfPossible < 4)
            {
                if (linkOut[escapeIfPossible] != null &&
                    port_alloc_buf[escapeIfPossible] == DIR.INV)
                {
                    return escapeIfPossible;
                }
            }
            //none are available
            return (int)DIR.NI; // if no free outPut port found, download the worm to NI buffer, which must be available.
        }
        protected bool isOutPortFree()
        {

            int i = getFreeOutPort((int)DIR.NI);
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
            // if (!f.isTruncatedHead) // discard the head-flit of the truncated worm
            {
                statsEjectFlit(f);
                if (f.packet.nrOfArrivedFlits + 1 == f.packet.nrOfFlits)
                    statsEjectPacket(f.packet);

                m_n.receiveFlit(f);
            }
        }

        protected void printAt(int routerID, ulong time, Flit flit)
        {

            if (ID == routerID && Simulator.CurrentRound == time)
            {
                Console.WriteLine("Router {0}/{1} Flag: printAt packetID {2} Source {3} Destination {4} ejecting @ {5} Ej[0]={6} Ej[1]={7},noOfFlits {8}",
                    ID, subnet, flit, flit.packet.src.ID, flit.packet.dest.ID, Simulator.CurrentRound, ejector[0], ejector[1], flit.packet.nrOfFlits);
            }



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
                    {
                        Console.WriteLine("PKT {0}-{1}/{2} EJECT FAIL router {3}/{4} at time {5}",
                                      ret.packet.ID, ret.flitNr + 1, ret.packet.nrOfFlits, coord, subnet, Simulator.CurrentRound);
                    }
#endif
                    ret = flit_buf[0][dir];
                    bestDir = dir;
                   /*Tracing flit
                    ejectMe(ret, 265979, 4);
                    ejectMe(ret, 278934, 4);
                    ejectMe(ret, 283930, 4);
                    ejectMe(ret, 329922, 4);
                    ejectMe(ret, 364585, 4);

                    //*/
                }

            if (bestDir != -1)
            {
                if (flit_buf[0][bestDir].isTailFlit)
                {
                    header_buf[0][bestDir] = null;
                    flit_buf[0][bestDir] = null;
                }
                else if (flit_buf[0][bestDir].isHeadFlit)
                {
                    //setup header and reserve ejector
                    // header_buf[0][bestDir] = flit_buf[0][bestDir].CloneSource(); // does it matters for previously recorded header_buf[0][dir]??
                    Compare(header_buf[0][bestDir], flit_buf[0][bestDir]);
                    flit_buf[0][bestDir] = null;
                    ejector[ejectorID] = (DIR)bestDir;
                    // Console.WriteLine("Ejector[{0}] reserved: header_buf[0][{1}]={2}::{3}:{4}", ejectorID, bestDir, header_buf[0][bestDir], ejector[0], ejector[1]);
                }
                else
                    flit_buf[0][bestDir] = null;
            }
#if PKTDUMP
            if (ret != null)
                Console.WriteLine("PKT {0}-{1}/{2} EJECT from port {3} router {4}/{5} at time {6}",
                                      ret.packet.ID, ret.flitNr + 1, ret.packet.nrOfFlits, bestDir, coord, subnet, Simulator.CurrentRound);
#endif
            return ret;
        }
        //truncating from local port does not reache here 
        protected Flit ejectFromPort(int dir, int EJ)
        {
            Flit ret = null;

            if (flit_buf[0][dir] != null)
            {
                if (flit_buf[0][dir].dest.ID != ID ||
                    header_buf[0][dir].packet.ID != flit_buf[0][dir].packet.ID)
                {
                    //Console.WriteLine("Router {0}/{1}  PacketID: Header-- {2} Flit-- {3} @ {4}",
                    //                 ID, subnet, header_buf[0][dir].packet.ID, flit_buf[0][dir], Simulator.CurrentRound);
                    throw new Exception("Something goes wrong, please check ejectFromPort()");
                }
                else
                {
                    ret = flit_buf[0][dir];
                    if (flit_buf[0][dir].isTailFlit)
                    {
                        header_buf[0][dir] = null;
                        ejector[EJ] = DIR.INV;
                    }
                    flit_buf[0][dir] = null;
                   /* Testing for ejected flit
                    // ejectMe(ret,255979);
                    ejectMe(ret, 265979, 2);
                    ejectMe(ret, 278934, 2);
                    ejectMe(ret, 283930, 2);
                    ejectMe(ret, 329922, 2);
                    ejectMe(ret, 364585, 2);
                    // */
                }
            }
            else
            {
                header_buf[0][dir] = null; // clear header buffer
            }
#if PKTDUMP
            if (ret != null)
                Console.WriteLine("PKT {0}-{1}/{2} EJECT from port {3} router {4}/{5} at time {6}",
                                      ret.packet.ID, ret.flitNr + 1, ret.packet.nrOfFlits, dir, coord, subnet, Simulator.CurrentRound);
#endif

            return ret;
        }
        protected Flit downloadFromPort(int dir, int EJ)
        {
            Flit ret = null;
            //Console.WriteLine("Enter to DownloadFromPort(): Ejector={0} dir={1}", EJ, dir);
            if (flit_buf[0][dir] != null)
            {
                //Console.WriteLine("DownloadedFromPort(): {0}", flit_buf[0][dir]);
                if ((header_buf[0][dir].prefDir != (int)DIR.NI) ||
                    (header_buf[0][dir].packet.ID != flit_buf[0][dir].packet.ID))
                {
                    throw new Exception("Something goes wrong, please check downloadFromPort()");
                }
                else
                {
                    ret = flit_buf[0][dir];
                    if (flit_buf[0][dir].isTailFlit)
                    {
                        // printMe("It's Tail, Clearing header");
                        header_buf[0][dir] = null;
                        ejector[EJ] = DIR.INV;
                    }
                    flit_buf[0][dir] = null;
                    /* Testing for ejected from
                    ejectMe(ret, 265979, 3);
                    ejectMe(ret, 278934, 3);
                    ejectMe(ret, 283930, 3);
                    ejectMe(ret, 329922, 3);
                    ejectMe(ret, 364585, 3);
                    //*/
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
            int EJ_Count = 0;
            int flitsTryToEject = 0;
            for (int dir = 0; dir < CHNL_CNT; dir++)
                if (flit_buf[0][dir] != null && flit_buf[0][dir].dest.ID == ID)
                {
                    flitsTryToEject++;
                    if (flit_buf[0][dir].ejectTrial == 0)
                        flit_buf[0][dir].firstEjectTrial = Simulator.CurrentRound;
                    flit_buf[0][dir].ejectTrial++;
                    /* Testing for ejected from
                    ejectMe(flit_buf[0][dir], 265979, 5);
                    ejectMe(flit_buf[0][dir], 278934, 5);
                    ejectMe(flit_buf[0][dir], 283930, 5);
                    ejectMe(flit_buf[0][dir], 329922, 5);
                    ejectMe(flit_buf[0][dir], 364585, 5);
                    printAt(1, 165947, flit_buf[0][dir]);
                    printAt(1, 174148, flit_buf[0][dir]);
                    //*/

#if PKTDUMP
                    Console.WriteLine("PKT {0}-{1}/{2} TRY to EJECT from port {3} router {4}/{5} at time {6}",
                                      flit_buf[0][dir].packet.ID, flit_buf[0][dir].flitNr + 1, flit_buf[0][dir].packet.nrOfFlits, dir,
                                      coord, subnet, Simulator.CurrentRound);
#endif
                }

            Simulator.stats.flitsTryToEject[flitsTryToEject].Add();
            Flit f1 = null;
            for (int i = 0; i < Config.meshEjectTrial; i++)
            {
                if (ejector[i] != DIR.INV)
                {
                    //printMe("Hi:1 " + ejector[i]);
                    if (header_buf[0][(int)ejector[i]] == null)// this might happens for receiving a truncated worm
                    {
                        ejector[i] = DIR.INV; //release ejector1
                                              // printMe("Hi:2 " + ejector[i]);
                    }
                    else if ((header_buf[0][(int)ejector[i]].dest.ID == ID) &&
                            (header_buf[0][(int)ejector[i]].prefDir == (int)DIR.INV))
                    {
                        //printMe("Hi:3 " + ejector[i]);
                        f1 = ejectFromPort((int)ejector[i], i);
                        if (f1 != null)
                        {
                            // printMe("Hi:4 " + ejector[i]);
                            EJ_Count++;
                            acceptFlit(f1);
                        }
                        else
                            ejector[i] = DIR.INV; //release ejector1
                    }
                    else if (header_buf[0][(int)ejector[i]].prefDir == (int)DIR.NI) // truncated worm 
                    {
                        // printMe("Hi:5 " + ejector[i]);
                        f1 = downloadFromPort((int)ejector[i], i);
                        if (f1 != null)
                        {
                            // printMe("Hi:6 " + ejector[i]);
                            EJ_Count++;
                            if (f1.isTailFlit)
                            {

                                acceptTruncatedFlit(f1, 1); // how can I pass the flag = 1
                                //ejector[i] = DIR.INV; //don't release ejector, it might eject twice                                                     
                            }
                            else
                            {
                                acceptTruncatedFlit(f1, 0);
                            }
                        }
                        else
                        {
                            ejector[i] = DIR.INV; //release ejector1 
                                                  //TODO:: mark injector is free ?? or left as it is, at the end of injector_buffer, it will automatically clear the flag (in Node)
                        }
                    }
                }
            }

            for (int i = 0; (i < Config.meshEjectTrial) && (EJ_Count < 2); i++)
            {
                if (ejector[i] == DIR.INV)
                {
                    // printMe("Hi:7 " + ejector[i]);
                    // Only support dual ejection (MAX.Config.meshEjectTrial = 2)
                    Flit eject = ejectLocal(i);
                    if (eject != null)
                    {
                        /* printMe("Hi:8 " + ejector[i]);
                        //ejectMe(eject,14597,1);
                        //ejectMe(eject,25597,1);
                        ejectMe(eject, 265979, 1);
                        ejectMe(eject, 278934, 1);
                        ejectMe(eject, 283930, 1);
                        ejectMe(eject, 329922, 1);
                        ejectMe(eject, 364585, 1);
                        //*/
                        acceptFlit(eject);  // Eject flit
                        EJ_Count++;
                    }
                }
            }
        }

        protected void ejectMe(Flit flit, ulong idNo, int flag)
        {
            if (flit.packet.ID == idNo)
            {
                Console.WriteLine("Router {0}/{1} Flag: {5} packetID {2} Source {3} Destination {4} ejecting @ {6} Ej[0]={7} Ej[1]={8}",
                    ID, subnet, flit, flit.packet.src.ID, flit.packet.dest.ID, flag, Simulator.CurrentRound, ejector[0], ejector[1]);
            }
        }

        protected void printMe(string text)
        {
            Console.WriteLine(text);
        }
        protected void cleanUpMapping()
        {
            int i;
#if CLEARUP
            Console.WriteLine("Before Cleaning up: Router {8}/{9} port_alloc_buf- {0}:{1}:{2}:{3}:{4}:{5}==>{6}:{7}", port_alloc_buf[0], port_alloc_buf[1], port_alloc_buf[2],
                port_alloc_buf[3], port_alloc_buf[4], port_alloc_buf[5], ejector[0], ejector[1], ID, subnet);
#endif
            for (i = 0; i < 5; i++)
            {
                if (port_alloc_buf[i] != DIR.INV)
                {
                    // Console.WriteLine("1:Router {0}/{1} port_alloc_buf[{2}] = {3}", ID, subnet, i, port_alloc_buf[i]);
                    if (header_buf[0][(int)port_alloc_buf[i]] != null)
                    {
                        if (port_alloc_buf[i] != DIR.LOCAL)//except for injecting port
                        {
                            if (flit_buf[1][i] != null &&
                                header_buf[0][(int)port_alloc_buf[i]].packet.ID == flit_buf[1][i].packet.ID &&
                                header_buf[0][(int)port_alloc_buf[i]].prefDir == i)
                            {
                                // Console.WriteLine("2:Router {0}/{1} port_alloc_buf[{2}] = {3}", ID, subnet, i, port_alloc_buf[i]);
                                continue;
                            }
                            else
                            {
                                port_alloc_buf[i] = DIR.INV;
                                // Console.WriteLine("3:Router {0}/{1} port_alloc_buf[{2}] = {3}", ID, subnet, i, port_alloc_buf[i]);
                                //Console.WriteLine("Router {0}/{1} Cleaned Packet {2}", ID, subnet,header_buf[0][(int)port_alloc_buf[i]]);
                            }
                        }
                        else
                        {
                            //Console.WriteLine("Indir: "+header_buf[0][(int)port_alloc_buf[i]].inDir +" "+ port_alloc_buf[i]);
                            if (header_buf[0][(int)port_alloc_buf[i]].inDir == (int)port_alloc_buf[i] &&
                                header_buf[0][(int)port_alloc_buf[i]].prefDir == i)
                            {
                                // Console.WriteLine("4:Router {0}/{1} port_alloc_buf[{2}] = {3}", ID, subnet, i, port_alloc_buf[i]);
                                continue;

                            }
                            else
                            {
                                port_alloc_buf[i] = DIR.INV;
                                // Console.WriteLine("5:Router {0}/{1} port_alloc_buf[{2}] = {3}", ID, subnet, i, port_alloc_buf[i]);
                            }

                        }
                        // Console.WriteLine("6:Router {0}/{1} port_alloc_buf[{2}] = {3}", ID, subnet, i, port_alloc_buf[i]);
                    }
                    else
                    {
                        port_alloc_buf[i] = DIR.INV;
                        // Console.WriteLine("6:Router {0}/{1} port_alloc_buf[{2}] = {3}", ID, subnet, i, port_alloc_buf[i]);
                    }
                }
            }
            for (i = 0; i < 2; i++)
            {
                if (ejector[i] != DIR.INV)
                {
                    //Console.WriteLine("1:Router {0}/{1} ejector[{2}] = {3}", ID, subnet, i, ejector[i]);
                    if ((header_buf[0][(int)ejector[i]] != null) &&
                       ((header_buf[0][(int)ejector[i]].prefDir == (int)DIR.NI) || (header_buf[0][(int)ejector[i]].prefDir == (int)DIR.INV)))
                    {
                        //Console.WriteLine("2:Router {0}/{1} ejector[{2}] = {3} preferDir = {4}", ID, subnet, i, ejector[i], header_buf[0][(int)ejector[i]].prefDir);

                        continue;
                    }
                    else
                    {
                        ejector[i] = DIR.INV;
                        //  Console.WriteLine("3:Router {0}/{1} ejector[{2}] = {3}", ID, subnet, i, ejector[i]);
                    }
                    // Console.WriteLine("4:Router {0}/{1} ejector[{2}] = {3}", ID, subnet, i, ejector[i]);
                }
            }
#if CLEARUP
            Console.WriteLine("After Cleaning up: Router {8}/{9} port_alloc_buf- {0}:{1}:{2}:{3}:{4}:{5}==>{6}:{7}", port_alloc_buf[0], port_alloc_buf[1], port_alloc_buf[2],
                port_alloc_buf[3], port_alloc_buf[4], port_alloc_buf[5], ejector[0], ejector[1],ID,subnet);
#endif
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
            if (dir > 4)
            {
                throw new Exception("port_alloc ID should not be greater than 4, check isOutPortEnable");
            }
            if (dir != (int)DIR.BYPASS)
            {
                ret = (linkOut[dir] != null);
            }
            else
            {
                ret = (bypassLinkOut[4 - (int)DIR.BYPASS] != null);
            }
            return ret;
        }

        //This module either download or deflect the worm
        protected bool checkLocallyDestined(int dir)
        {
            bool ret = false;
            if (header_buf[0][dir].prefDir == (int)DIR.INV) //locally destined has prefDir equal DIR.INV
            {
                // Console.WriteLine("LocallyDestined:: Router {0}/{1} Packet {2} ", ID, subnet, header_buf[0][dir].packet.ID);
                if (header_buf[0][dir].dest.ID == ID) //locally destined worm
                {
                    if (ejector[0] == (DIR)dir || ejector[1] == (DIR)dir) // already mapped
                    {
                        ;// ret = true;
                    }
                    else // deflect to another port, we can't map to ejector here no matter whether the ejector is free or not
                    {
                        /*
                        if (ejector[0] == DIR.INV)
                        {
                            ejector[0] = (DIR)dir;
                        }
                        else if (ejector[1] == DIR.INV)
                        {
                            ejector[1] = (DIR)dir;
                        }
                        else //deflect to another port
                        {
                            deflectWormToFreePort(dir);
                        }*/
                        deflectWormToFreePort(dir);
                    }
                    ret = true;
                }
                else
                {
                    // Console.WriteLine("Router {0}/{1} Packet {2} PrefDir {3} InPort {4}",
                    //     ID, subnet, header_buf[0][dir].packet.ID, header_buf[0][dir].prefDir, dir);
                    throw new Exception("Seems to be locally destined, but not, please check port_alloc()");
                }
            }
            return ret;
        }

        protected bool checkFreePort(int dir)
        {
            bool ret = false;
            //Console.WriteLine("checkFreePort:: Router {0}/{1} Packet {2} ", ID, subnet, header_buf[0][dir].packet.ID);
            int availablePort = getFreeOutPortForNextCycle(dir);
            if (availablePort != (int)DIR.NI)
            {
                port_alloc_buf[availablePort] = (DIR)dir;
                header_buf[0][dir].prefDir = availablePort;
                //flit_buf[0][dir].prefDir = availablePort;

                // Console.WriteLine("Free Port found and mapped. Router {0}/{1} Packet {2}  assigned port {3}",
                //     ID, subnet, header_buf[0][dir].packet.ID, availablePort);
                ret = true;
            }
            return ret;
        }

        protected bool downloadNonLocalWorm(int dir)
        {
            bool ret = false;
            if (dir != (int)DIR.LOCAL)
            {
                // Console.WriteLine("Downloading Non-Local Packet-{0} preferDir--{1}",
                //    header_buf[0][dir].packet.ID, header_buf[0][dir].prefDir);
                Flit temp = header_buf[0][dir].CloneSource(); // make a copy of header and proceed
                Compare(temp, header_buf[0][dir]);
                acceptTruncatedFlit(temp, 0);
                header_buf[0][dir].prefDir = (int)DIR.NI; // Mark the incoming body flits to follow the header
                                                          //port_alloc_buf[(int)DIR.NI] = (DIR)victimInPort; // point port_allocation as NI buffer
                if (ejector[0] != DIR.INV && ejector[1] != DIR.INV)
                { //should never happens
                    throw new Exception("ERROR: both ejectors should not be busy at the same time");
                }
                //mark one of the ejector as busy
                if (ejector[1] == DIR.INV)
                    ejector[1] = (DIR)dir;
                else if (ejector[0] == DIR.INV)
                    ejector[0] = (DIR)dir;
                ret = true;
                // Console.WriteLine("1:Worm mapped to NI for downloading");
            }
            return ret;
        }

        protected bool downloadLocalWorm(int dir)
        {
            bool ret = false;

            if (dir == (int)DIR.LOCAL) // for local port; the data is not populated yet ??
            {
                Flit temp = header_buf[0][dir].CloneSource(); // make a copy of header and proceed
                Compare(temp, header_buf[0][dir]);
                acceptTruncatedFlit(temp, 0);
                acceptTruncatedFlit(header_buf[0][dir], 0);
                header_buf[0][dir] = null;//Updated

                if (flit_buf[0][dir] != null) // Does Local Injection places flits in flit_buf[0][Local]?? (NO)
                {
                    acceptTruncatedFlit(flit_buf[0][dir], 0);
                    //header_buf[0][contendingPort] = null; // clear header buffer
                    flit_buf[0][dir] = null; // clear flit buffer
                }
                if (m_injectSlot != null)
                {
                    acceptLocalTruncatedFlit(m_injectSlot);// this method moves the remaining flit to NI                           
                    m_injectSlot = null; // clear buffer
                }
                //Console.WriteLine("2:Local Worm mapped to NI for downloading");
                ret = true;
            }
            return ret;
        }

        protected bool checkDownloadToNI(int dir)
        {
            bool ret = false;
            // Console.WriteLine("checkDownladToNI:: Router {0}/{1} Packet {2} ", ID, subnet, header_buf[0][dir].packet.ID);
            int availablePort = getFreeOutPortForNextCycle(dir);
            if (availablePort == (int)DIR.NI)// Download the flit to NI
            {
                if (downloadNonLocalWorm(dir))
                {
                    //Non-Local packet downloaded
                    ret = true;
                }

                // It should never happens
                else //if (downloadLocalWorm(dir)) // for local port; the data is not populated yet ??
                {
                    throw new Exception("ERROR: It should never be Locally injected worm ejectors should not be busy at the same time");
                    //ret = true; 
                }
            }
            return ret;
        }


        protected bool checkPreferedPort(int dir)
        {
            bool ret = false;
            if (isOutPortEnable(header_buf[0][dir].prefDir))
            {
                // Console.WriteLine("PreferedPortEnable:: Router {0}/{1} Packet {2} Prefer Dir {3}",
                //    ID, subnet, header_buf[0][dir].packet.ID, header_buf[0][dir].prefDir);
                if (port_alloc_buf[header_buf[0][dir].prefDir] == (DIR)dir) // already mapped
                {
                    //Console.WriteLine("1. PreferedPort is Enabled and Mapped correctly");
                    ret = true;
                }
                else if (port_alloc_buf[header_buf[0][dir].prefDir] == DIR.INV) // prefered direction available
                {
                    //  Console.WriteLine("2. PreferedPort is Enabled, free, and Mapped into it");
                    port_alloc_buf[header_buf[0][dir].prefDir] = (DIR)dir;  // no need to update prefered direction in header                         
                    ret = true;
                }
            }
            return ret;
        }

        protected bool deflectWormToFreePort(int dir)
        {
            bool ret = false;
            //defelct the worm to available port
            int freeOutPort = getFreeOutPortForNextCycle(dir); // get a free outPut port :
            if (freeOutPort != (int)DIR.NI) // NI is to download the truncated worm
            {
                header_buf[0][dir].prefDir = freeOutPort; // update header's prefere outport                    
                //flit_buf[0][dir].prefDir = freeOutPort; // header to output buffer of freeOutPort                                                   
                port_alloc_buf[freeOutPort] = (DIR)dir;
                if (!flit_buf[0][dir].isHeadFlit) // is head Flit
                {
                    throw new Exception("Flit must be a Header flit, but it's not, , Please check deflectWormToFreePort()");
                }
                ret = true;
            }
            else
            {
                throw new Exception("Ports not available, Please check deflectWormToFreePort()");
            }
            return ret;
        }

        /*
         * To be noted here, all header[0][dir] and flit[0][dir] are same under the port_alloc(); expected changes must be already adjusted.
         * If any changes required, it must be dine through truncatWorm
        */
        protected void port_alloc()
        {
            int NICount = 0;
            newHeader = true;
            // Console.WriteLine("Port_alloc_1:: Router{7}/{8} @{9}:Port allocation port_alloc [0]-[4] and Ejector[0]-[1]:: {0}:{1}:{2}:{3}:{4}:{5}:{6}\n {10}",
            //       port_alloc_buf[0], port_alloc_buf[1], port_alloc_buf[2], port_alloc_buf[3], port_alloc_buf[4], ejector[0], ejector[1],
            //      ID, subnet, Simulator.CurrentRound, printHeader());
            if (newHeader) // a new header is arrived, so port allocation must be executed
            {
                newHeader = false; // clear flag  

                // map all worms comming from input ports to respective output  ports
                for (int dir = 0; dir < CHNL_CNT; dir++)
                {
                    if (header_buf[0][dir] == null)
                    {
                        continue; // no need to assign outPort
                    }
                    //check if mapped to NI
                    else if (header_buf[0][dir].prefDir == (int)DIR.NI)
                    {
                        // Console.WriteLine("PrefDir:: Router {0}/{1} Packet {2} ", ID, subnet, header_buf[0][dir].packet.ID);
                        NICount = NICount + 1;
                        continue; // this represent NI buffer and already adjusted
                    }
                    //is mapped to LOCAL outPort?
                    else if (header_buf[0][dir].prefDir == (int)DIR.LOCAL) //this should not happen
                    {
                        throw new Exception("Prefered port is Local port, Please verify");
                    }

                    else if (checkLocallyDestined(dir)) //locally destined ?
                    {
                        //does for locally destined
                        ;
                    }
                    //The worm intended to pass through this Router are processed after here  
                    else if (checkPreferedPort(dir)) //Check for prefered port
                    {
                        ;
                    }
                    //prefered port is already taken, check for a free port only
                    else if (checkFreePort(dir))
                    {
                        //check FreePort
                        ;
                    }
                    else if (NICount < 1) //download to NI
                    {
                        if (checkDownloadToNI(dir))// Download the flit to NI
                        {
                            NICount = NICount + 1;
                        }
                        else
                        {
                            throw new Exception("Port is not available, please check at the end of the port_alloc()");
                        }
                    }
                    else
                    {
                        throw new Exception("More than one NI-buffer is used in a single Router, Please check port_alloc()");
                    }
                }

            }
            // Console.WriteLine("Port_alloc_2:: Router{7}/{8} @{9}:Port allocation port_alloc [0]-[4] and Ejector[0]-[1]:: {0}:{1}:{2}:{3}:{4}:{5}:{6}\n{10}",
            //        port_alloc_buf[0], port_alloc_buf[1], port_alloc_buf[2], port_alloc_buf[3], port_alloc_buf[4], ejector[0], ejector[1],
            //        ID, subnet, Simulator.CurrentRound, printHeader());
        }


        /*
        protected void port_alloc()
        {
            int NICount = 0;
            newHeader = true;
            Console.WriteLine("Port_alloc_1:: Router{7}/{8} @{9}:Port allocation port_alloc [0]-[4] and Ejector[0]-[1]:: {0}:{1}:{2}:{3}:{4}:{5}:{6}",
                   port_alloc_buf[0], port_alloc_buf[1], port_alloc_buf[2], port_alloc_buf[3], port_alloc_buf[4], ejector[0], ejector[1], ID, subnet, Simulator.CurrentRound);
            if (newHeader) // a new header is arrived, so port allocation must be executed
            {
                newHeader = false; // clear flag  

                // Assign the remaining ports
                for (int dir = 0; dir < CHNL_CNT; dir++)
                {
                    if (header_buf[0][dir] == null) continue; // no need to assign outPort
                    //check if the port is enabled 
                    else if (header_buf[0][dir].prefDir == (int)DIR.NI)
                    {
                        Console.WriteLine("Hi:1:: Router {0}/{1} Packet {2} ", ID, subnet, header_buf[0][dir].packet.ID);
                        NICount = NICount + 1;
                        continue; // this represent NI buffer and already adjusted
                    }
                    else if (header_buf[0][dir].prefDir == (int)DIR.LOCAL)
                    {
                        throw new Exception("Prefered port is Local port, Please verify");
                    }

                    else if (header_buf[0][dir].prefDir == (int)DIR.INV) //locally destined
                    {
                        Console.WriteLine("Hi:2:: Router {0}/{1} Packet {2} ", ID, subnet, header_buf[0][dir].packet.ID);
                        if (header_buf[0][dir].dest.ID == ID) //locally destined worm
                        {
                            if (ejector[0] == (DIR)dir || ejector[1] == (DIR)dir)
                            {
                                continue;
                            }
                            else
                            {
                                if (ejector[0] == DIR.INV)
                                {
                                    ejector[0] = (DIR)dir;
                                }
                                else if (ejector[1] == DIR.INV)
                                {
                                    ejector[1] = (DIR)dir;
                                }
                                else
                                {
                                    //defelct the worm to available port
                                    int freeOutPort = getFreeOutPortForNextCycle(dir); // get a free outPut port :
                                    if (freeOutPort != (int)DIR.NI) // NI is to download the truncated worm
                                    {
                                        header_buf[0][dir].prefDir = freeOutPort; // update header's prefere outport                    
                                        flit_buf[0][dir].prefDir = freeOutPort; // header to output buffer of freeOutPort                                                   
                                        port_alloc_buf[freeOutPort] = (DIR)dir;
                                    }
                                    else
                                        throw new Exception("Ports not available, Please check port_alloc()");
                                }
                            }
                            continue; // locally destined flit:TODO (investigate more on it)
                        }
                        else
                        {
                            Console.WriteLine("Router {0}/{1} Packet {2} PrefDir {3} InPort {4}",
                                ID, subnet, header_buf[0][dir].packet.ID, header_buf[0][dir].prefDir, dir);
                            throw new Exception("Seems to be locally destined, but not, please check port_alloc()");
                        }
                    }
                    else if (isOutPortEnable(header_buf[0][dir].prefDir))
                    {
                        Console.WriteLine("Hi:3:: Router {0}/{1} Packet {2} Prefer Dir {3}", ID, subnet, header_buf[0][dir].packet.ID, header_buf[0][dir].prefDir);
                        if (port_alloc_buf[header_buf[0][dir].prefDir] == (DIR)dir) // already mapped
                        {
                            Console.WriteLine("Hi:3.1");
                            continue;
                        }
                        else if (port_alloc_buf[header_buf[0][dir].prefDir] == DIR.INV) // prefered direction available
                        {
                            Console.WriteLine("Hi:3.2");
                            port_alloc_buf[header_buf[0][dir].prefDir] = (DIR)dir;  // no need to update prefered direction in header                         
                            continue;
                        }
                        //prefered port is already taken, check for a free port only
                        else
                        {
                            Console.WriteLine("Hi:4:: Router {0}/{1} Packet {2} ", ID, subnet, header_buf[0][dir].packet.ID);
                            //bool found = false;
                            int availablePort = getFreeOutPortForNextCycle(dir);
                            if (availablePort != (int)DIR.NI)
                            {
                                port_alloc_buf[availablePort] = (DIR)dir;
                                header_buf[0][dir].prefDir = availablePort;
                                flit_buf[0][dir].prefDir = availablePort;
                                //found = true;
                                Console.WriteLine("Router {0}/{1} Packet {2}  assigned port {3}", ID, subnet, header_buf[0][dir].packet.ID, availablePort);
                                continue;
                            }
                            else if (availablePort == (int)DIR.NI)// Download the flit to NI
                            {
                                NICount++;
                                if (dir != (int)DIR.LOCAL)
                                {
                                    Console.WriteLine("Downloading Packet-{0} preferDir--{1}", header_buf[0][dir].packet.ID, header_buf[0][dir].prefDir);
                                    acceptTruncatedFlit(header_buf[0][dir], 0);
                                    header_buf[0][dir].prefDir = (int)DIR.NI; // Mark the incoming body flits to follow the header
                                                                              //port_alloc_buf[(int)DIR.NI] = (DIR)victimInPort; // point port_allocation as NI buffer
                                    if (ejector[0] != DIR.INV && ejector[1] != DIR.INV)
                                    { //should never happens
                                        throw new Exception("ERROR: both ejectors should not be busy at the same time");
                                    }
                                    //mark one of the ejector as busy
                                    if (ejector[1] == DIR.INV)
                                        ejector[1] = (DIR)dir;
                                    else if (ejector[0] == DIR.INV)
                                        ejector[0] = (DIR)dir;

                                    Console.WriteLine("1:Worm mapped to NI for downloading");
                                }
                                // It should never happens
                                else if (dir == (int)DIR.LOCAL) // for local port; the data is not populated yet ??
                                {
                                    acceptTruncatedFlit(header_buf[0][dir], 0);
                                    header_buf[0][dir] = null;//Updated

                                    if (flit_buf[0][dir] != null) // Does Local Injection places flits in flit_buf[0][Local]?? (NO)
                                    {
                                        acceptTruncatedFlit(flit_buf[0][dir], 0);
                                        //header_buf[0][contendingPort] = null; // clear header buffer
                                        flit_buf[0][dir] = null; // clear flit buffer
                                    }
                                    if (m_injectSlot != null)
                                    {
                                        acceptLocalTruncatedFlit(m_injectSlot);// this method moves the remaining flit to NI                           
                                        m_injectSlot = null; // clear buffer
                                    }
                                    Console.WriteLine("2:Worm mapped to NI for downloading");
                                }
                                continue;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Prefered port is not enable, please check at the end of the port_alloc()");
                    }

                }
                if (NICount > 1)
                    throw new Exception("More than one NI-buffer is used in a single Router, Please check port_alloc()");
            }
            Console.WriteLine("Port_alloc_2:: Router{7}/{8} @{9}:Port allocation port_alloc [0]-[4] and Ejector[0]-[1]:: {0}:{1}:{2}:{3}:{4}:{5}:{6}",
                   port_alloc_buf[0], port_alloc_buf[1], port_alloc_buf[2], port_alloc_buf[3], port_alloc_buf[4], ejector[0], ejector[1], ID, subnet, Simulator.CurrentRound);
        }
        */

    }

}
