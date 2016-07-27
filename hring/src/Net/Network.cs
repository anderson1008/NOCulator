//#define TEST

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ICSimulator
{
    public class Network
    {
        // dimensions
        public int X, Y;

        // every node has a router and a processor; not every processor is an application proc
        public Router[] routers;
        public Node[] nodes;
        public List<Link> links;
        public Workload workload;
        public Golden golden;
        public NodeMapping mapping;
        public CmpCache cache;
        public Router[] nodeRouters;
        public Router[] connectRouters;
		public Router[] switchRouters;
		public Router[] bridgeRouters;
		public Router[] L1bridgeRouters;
		public Router[] L2bridgeRouters;
		public Router[] L3bridgeRouters;
		public Router[] L4bridgeRouters;
		public Router[] nics;
		public Router[] iris;

        public bool[] endOfTraceBarrier;
        public bool canRewind;

        // finish mode
        public enum FinishMode { app, insn, synth, cycle, barrier };
        FinishMode m_finish;
        ulong m_finishCount;

        public FinishMode finishMode { get { return m_finish; } }

        public double _cycle_netutil; // HACK -- controllers can use this.
        public ulong _cycle_insns;    // HACK -- controllers can use this.
		
		public int Local_CW = 0;
		public int Local_CCW = 1;
		public int GL_CW = 2;
		public int GL_CCW = 3;

        public virtual int [] GetCWnext {get {return null;}}
        public virtual int [] GetCCWnext {get {return null;}}

        public Network(int dimX, int dimY)
        {
            X = dimX;
            Y = dimY;
        }

        public bool endOfTraceAllDone()
        {
            if(canRewind == true)
                return true;
            for (int n = 0; n < Config.N; n++)
                if(endOfTraceBarrier[n]==false)
                    return false;
            canRewind = true;
            return true;
        }

        // resetting all the barrier synchronously, set canRewind to false 
        // when the reset is done
        public bool endOfTraceReset()
        {
            for (int n = 0; n < Config.N; n++)
                if(endOfTraceBarrier[n]==true)
                {
                    endOfTraceBarrier[n] = false;
                    return false;
                }
            canRewind = false;
            return true;
        }

  
        public virtual void setup()
        {
            routers = new Router[Config.N];
            nodes = new Node[Config.N];
            links = new List<Link>();
            cache = new CmpCache();

            endOfTraceBarrier = new bool[Config.N];
            canRewind = false;

            ParseFinish(Config.finish);

            workload = new Workload(Config.traceFilenames);

            mapping = new NodeMapping_AllCPU_SharedCache();

            // create routers and nodes
            for (int n = 0; n < Config.N; n++)
            {
                Coord c = new Coord(n);
                nodes[n] = new Node(mapping, c);
                routers[n] = MakeRouter(c);
                nodes[n].setRouter(routers[n]);
                routers[n].setNode(nodes[n]);
                endOfTraceBarrier[n] = false;
				#if DEBUG
				Console.WriteLine ("*******This is Node {0} **********", n);
				((Router_BLESS_MC)nodes [n].router).PrintMask ();
				#endif
            }


			//Console.WriteLine ("Exit Normally!");
			//System.Environment.Exit (1);

            // create the Golden manager
            golden = new Golden();

            if (Config.RouterEvaluation)
                return;

            // connect the network with Links
            for (int n = 0; n < Config.N; n++)
            {
                int x, y;
                Coord.getXYfromID(n, out x, out y);

                // inter-router links
                for (int dir = 0; dir < 4; dir++)
                {

					// Clockwise 0->3 map to N->E->S->W
					/* Coordinate Mapping (e.g. 16 nodes)
					 * (0,3) (1,3) .....     |||||    3  7 ...
					 * ...					 |||||	  2  6 ...
					 * ...					 |||||    1  5 ...
					 * (0,0) (1,0) ......	 |||||    0  4 ...
					 * */

                    int oppDir = (dir + 2) % 4; // direction from neighbor's perspective

                    // determine neighbor's coordinates
                    int x_, y_;
                    switch (dir)
                    {
                        case Simulator.DIR_UP: x_ = x; y_ = y + 1; break;
                        case Simulator.DIR_DOWN: x_ = x; y_ = y - 1; break;
                        case Simulator.DIR_RIGHT: x_ = x + 1; y_ = y; break;
                        case Simulator.DIR_LEFT: x_ = x - 1; y_ = y; break;
                        default: continue;
                    }

                    // If we are a torus, we manipulate x_ and y_
                    if(Config.torus)
                    {
                      if(x_ < 0)
                        x_ += X;
                      else if(x_ >= X)
                        x_ -= X;

                      if(y_ < 0)
                        y_ += Y;
                      else if(y_ >= Y)
                        y_ -= Y;
                    }
                    // mesh, not torus: detect edge
                    else if (x_ < 0 || x_ >= X || y_ < 0 || y_ >= Y)
                    {
                        if (Config.edge_loop)
                        {
                            Link edgeL = new Link(Config.router.linkLatency - 1);
                            links.Add(edgeL);

                            routers[Coord.getIDfromXY(x, y)].linkOut[dir] = edgeL;
                            routers[Coord.getIDfromXY(x, y)].linkIn[dir] = edgeL;
                            routers[Coord.getIDfromXY(x, y)].neighbors++;
                            routers[Coord.getIDfromXY(x, y)].neigh[dir] =
                                routers[Coord.getIDfromXY(x, y)];
                        }

                        continue;
                    }

                    // ensure no duplication by handling a link at the lexicographically
                    // first router
                    if (x_ < x || (x_ == x && y_ < y)) continue;

                    // Link param is *extra* latency (over the standard 1 cycle)
                    Link dirA = new Link(Config.router.linkLatency - 1);
                    Link dirB = new Link(Config.router.linkLatency - 1);
                    links.Add(dirA);
                    links.Add(dirB);

                    // link 'em up
                    routers[Coord.getIDfromXY(x, y)].linkOut[dir] = dirA;
                    routers[Coord.getIDfromXY(x_, y_)].linkIn[oppDir] = dirA;

                    routers[Coord.getIDfromXY(x, y)].linkIn[dir] = dirB;
                    routers[Coord.getIDfromXY(x_, y_)].linkOut[oppDir] = dirB;

                    routers[Coord.getIDfromXY(x, y)].neighbors++;
                    routers[Coord.getIDfromXY(x_, y_)].neighbors++;

                    routers[Coord.getIDfromXY(x, y)].neigh[dir] = routers[Coord.getIDfromXY(x_, y_)];
                    routers[Coord.getIDfromXY(x_, y_)].neigh[oppDir] = routers[Coord.getIDfromXY(x, y)];

                    if (Config.router.algorithm == RouterAlgorithm.DR_SCARAB)
                    {
                        for (int wireNr = 0; wireNr < Config.nack_nr; wireNr++)
                        {
                            Link nackA = new Link(Config.nack_linkLatency - 1);
                            Link nackB = new Link(Config.nack_linkLatency - 1);
                            links.Add(nackA);
                            links.Add(nackB);
                            ((Router_SCARAB)routers[Coord.getIDfromXY(x, y)]).nackOut[dir * Config.nack_nr + wireNr] = nackA;
                            ((Router_SCARAB)routers[Coord.getIDfromXY(x_, y_)]).nackIn[oppDir * Config.nack_nr + wireNr] = nackA;

                            ((Router_SCARAB)routers[Coord.getIDfromXY(x, y)]).nackIn[dir * Config.nack_nr + wireNr] = nackB;
                            ((Router_SCARAB)routers[Coord.getIDfromXY(x_, y_)]).nackOut[oppDir * Config.nack_nr + wireNr] = nackB;
                        }
                    }
                }
            }

			// Just to verify the formed topology
			//for ( int i=0; i<16; i++) {
			//	Console.WriteLine ("Router {0} has {1} neighbors", i, routers[i].neighbors);
			//}

            if (Config.torus)
                for (int i = 0; i < Config.N; i++)
                    if (routers[i].neighbors < 4)
                        throw new Exception("torus construction not successful!");
        }


        // Pins specification for nodeRouters
        public const int CW = 0;				//clockwise 
        public const int CCW = 1;			//counter clockwise
		        
        // Pins specification for connectRouters
        public const int L0 = 0;
        public const int L1 = 1;
        public const int L2 = 2;
        public const int L3 = 3;
        
       	public void setup_RingCluster_4()
       	{
       		nodeRouters = new Router_Node[Config.N];
       		connectRouters = new Router_Connect[6];
            nodes = new Node[Config.N];
            links = new List<Link>();
            cache = new CmpCache();

            ParseFinish(Config.finish);

            workload = new Workload(Config.traceFilenames);

            mapping = new NodeMapping_AllCPU_SharedCache();

            // create routers and nodes
            for (int n = 0; n < Config.N; n++)
            {
                Coord c = new Coord(n);
                nodes[n] = new Node(mapping, c);
                 
                nodeRouters[n] = new Router_Node(c); 
                nodes[n].setRouter(nodeRouters[n]);                                
                nodeRouters[n].setNode(nodes[n]);
            }
            for (int n = 0; n < 6; n++)
            	connectRouters[n] = new Router_Connect(n);
            
            for (int n = 0; n < Config.N / 4; n++)  // n is the cluster number
            {
            	for (int i = 0; i < 4; i++)  // for each node in the same cluster
            	{
            		if (n == 0 && i == 3 || n == 1 && i == 1 || n == 2 && i == 1 || n == 3 && i == 3)
            		{
	            		Link dirA = new Link(0);   
    	                Link dirB = new Link(0);                  	
    	                links.Add(dirA);                    	
    	                links.Add(dirB);
    	                int next = (i + 1 == 4)? n * 4 : n * 4 + i + 1;                    
    	        		nodeRouters[n * 4 + i].linkOut[CW] = dirA;
    	        		nodeRouters[next].linkIn[CW] = dirA;
    	        		nodeRouters[next].linkOut[CCW] = dirB;
    	        		nodeRouters[n * 4 + i].linkIn[CCW] = dirB;            		
            		}
            	}
            }
            for (int n = 0; n < 6; n++)
            {
            	Link dirA = new Link(Config.router.linkLatency - 1);	            	
            	Link dirB = new Link(Config.router.linkLatency - 1);	            	
            	links.Add(dirA);
            	links.Add(dirB);
            	int[] node1 = new int[6] {1, 6, 11, 12, 0, 4};
            	nodeRouters[node1[n]].linkOut[CW] = dirA;
            	nodeRouters[node1[n]].linkIn[CCW] = dirB;
            	connectRouters[n].linkIn[L0] = dirA;
            	connectRouters[n].linkOut[L0] = dirB;
            	
            	dirA = new Link(Config.router.linkLatency - 1);
            	dirB = new Link(Config.router.linkLatency - 1);	            	            	
            	links.Add(dirA);
            	links.Add(dirB);
            	int[] node2 = new int[6] {2, 7, 8, 13, 1, 5};
            	nodeRouters[node2[n]].linkOut[CCW] = dirA;
            	nodeRouters[node2[n]].linkIn[CW] = dirB;
            	connectRouters[n].linkIn[L3] = dirA;
            	connectRouters[n].linkOut[L3] = dirB;
            	
            	dirA = new Link(Config.router.linkLatency - 1);
            	dirB = new Link(Config.router.linkLatency - 1);	            	            	
            	links.Add(dirA);
            	links.Add(dirB);
            	int[] node3 = new int[6] {4, 9, 14, 3, 15, 11};
            	nodeRouters[node3[n]].linkOut[CCW] = dirA;
            	nodeRouters[node3[n]].linkIn[CW] = dirB;
            	connectRouters[n].linkIn[L1] = dirA;
            	connectRouters[n].linkOut[L1] = dirB;
            	
            	dirA = new Link(Config.router.linkLatency - 1);
            	dirB = new Link(Config.router.linkLatency - 1);	            	            	
            	links.Add(dirA);
            	links.Add(dirB);
            	int[] node4 = new int[6] {7, 8, 13, 2, 14, 10};
            	nodeRouters[node4[n]].linkOut[CW] = dirA;
            	nodeRouters[node4[n]].linkIn[CCW] = dirB;
            	connectRouters[n].linkIn[L2] = dirA;
            	connectRouters[n].linkOut[L2] = dirB;
            }  
                    
       	}
       	
       	public void setup_TorusSingleRing()
       	{
       		nodeRouters = new Router_TorusRingNode[Config.N];
       		connectRouters = new Router_TorusRingConnect[8];
            nodes = new Node[Config.N];
            links = new List<Link>();
            cache = new CmpCache();

            ParseFinish(Config.finish);
            workload = new Workload(Config.traceFilenames);
            mapping = new NodeMapping_AllCPU_SharedCache();
            
            for (int n = 0; n < Config.N; n++)
            {
                Coord c = new Coord(n);
                nodes[n] = new Node(mapping, c);                 
                nodeRouters[n] = new Router_TorusRingNode(c); 
                nodes[n].setRouter(nodeRouters[n]);                                
                nodeRouters[n].setNode(nodes[n]);
            }
            for (int n = 0; n < 8; n++)
            	connectRouters[n] = new Router_TorusRingConnect(n);

       		int[] CW_Connect = new int[16] {7,0,6,1,3,1,2,0,2,5,3,4,6,4,7,5};
       		int[] CCW_Connect = new int[16] {1,7,0,6,0,3,1,2,4,2,5,3,5,6,4,7};	
       		int[] dirCW = new int[16] {1,0,0,1,1,0,0,1,1,0,0,1,1,0,0,1};
       		int[] dirCCW = new int[16] {1,1,0,0,1,1,0,0,1,1,0,0,1,1,0,0};
       		for (int n = 0; n < Config.N; n++)
       		{
       			Link dirA = new Link(Config.router.linkLatency - 1);	            	
            	Link dirB = new Link(Config.router.linkLatency - 1);
            	links.Add(dirA);
            	links.Add(dirB);
            	nodeRouters[n].linkOut[0] = dirA;
            	nodeRouters[n].linkIn[0] = dirB;
            	connectRouters[CW_Connect[n]].linkIn[dirCW[n]] = dirA;
            	connectRouters[CCW_Connect[n]].linkOut[dirCCW[n]] = dirB;
       		}
       	}
       	
        public virtual void doStep()
        {
        
            doStats();

            // step the golden controller
           // golden.doStep();

            // step the nodes
            
            for (int n = 0; n < Config.N; n++)
                nodes[n].doStep();
            // step the network sim: first, routers
            if (Config.RingClustered == false && Config.TorusSingleRing == false)			
            	for (int n = 0; n < Config.N; n++)	
                	routers[n].doStep();
            else								// RingClustered Topology dostep() OR TorusRing Topology
            {           	
            	for (int n = 0; n < Config.N; n++)
            		nodeRouters[n].doStep();
            	for (int n = 0; n < 6; n++)
            		connectRouters[n].doStep();            	
            }
            // now, step each link
            foreach (Link l in links)
                l.doStep();
        }

        public void doStats()
        {
			/*
			if (Simulator.CurrentRound % Config.subnet_reset_period == 0 && Simulator.CurrentRound > 0)
				for (int i = 0; i < Config.sub_net; i++)
					Simulator.stats.subnet_util[i].EndPeriod();
			*/
            int used_links = 0;
            foreach (Link l in links)
                if (l.Out != null)
                    used_links++;

            this._cycle_netutil = (double)used_links / links.Count;

            Simulator.stats.netutil.Add((double)used_links / links.Count);

            this._cycle_insns = 0; // CPUs increment this each cycle -- we want a per-cycle sum
			if (this is HR_Network && Config.N == 16)
			{
				int used_local = 0;
				int used_global = 0;
				for (int i  = 0; i < Config.N; i++)
				{
					if (nodeRouters[i].linkIn[CW].Out != null) used_local ++;
					if (nodeRouters[i].linkIn[CCW].Out != null) used_local ++;
					if (switchRouters[i].linkIn[Local_CW].Out != null) used_local ++;
					if (switchRouters[i].linkIn[Local_CCW].Out != null) used_local ++;
					if (switchRouters[i].linkIn[GL_CW].Out != null) used_global ++;
					if (switchRouters[i].linkIn[GL_CCW].Out != null) used_global ++;
				}
				Simulator.stats.local_net_util.Add((double)used_local / 64); // totally 32 slots in local rings
				Simulator.stats.global_net_util.Add((double)used_global / 32); // totally 32 slots in global rings, but half are not visible
			}
			if (this is HR_4drop_Network || this is HR_8drop_Network || this is HR_16drop_Network)
			{
				int used_local = 0;
				int used_global = 0;
				for (int i  = 0; i < Config.N; i++)
				{
					if (nodeRouters[i].linkIn[CW].Out != null) used_local ++;
					if (nodeRouters[i].linkIn[CCW].Out != null) used_local ++;
				} 
				int bridgeNum = (this is HR_8drop_Network) ? 8 : ((this is HR_4drop_Network)? 4 : ((this is HR_16drop_Network)? 16 : 0));
				for (int i = 0; i < bridgeNum; i++)
				{					
					if (bridgeRouters[i].LLinkIn[CW].Out != null) used_local ++;
					if (bridgeRouters[i].LLinkIn[CCW].Out != null) used_local ++;					
					for (int j = 0; j < 2*Config.GlobalRingWidth; j++)
						if (bridgeRouters[i].GLinkIn[j].Out != null)
 						{
							if(i==0)
								Simulator.stats.bridge0Count.Add();                                                
							if(i==1)
								Simulator.stats.bridge1Count.Add();                                                
							if(i==2)
								Simulator.stats.bridge2Count.Add();                                                
							if(i==3)
								Simulator.stats.bridge3Count.Add();                                                
							if(i==4)
								Simulator.stats.bridge4Count.Add();                                                
							if(i==5)
								Simulator.stats.bridge5Count.Add();                                                
							if(i==6)
								Simulator.stats.bridge6Count.Add();                                                
							if(i==7)
								Simulator.stats.bridge7Count.Add();                                                

							used_global ++;
						}
				}

				if (this is HR_4drop_Network)
				{					
					Simulator.stats.local_net_util.Add((double)used_local / 40);
					Simulator.stats.global_net_util.Add((double)used_global / 8 / Config.GlobalRingWidth); // totally 32 slots in global rings, but half are not visible
				}
				if (this is HR_8drop_Network)
				{
					Simulator.stats.local_net_util.Add((double)used_local / 48);
					Simulator.stats.global_net_util.Add((double)used_global / 16 / Config.GlobalRingWidth);
				}
				if (this is HR_16drop_Network)
				{
					Simulator.stats.local_net_util.Add((double)used_local / 64);
					Simulator.stats.global_net_util.Add((double)used_global / 32 / Config.GlobalRingWidth);
				}
			}
			if (this is HR_8_8drop_Network)
			{
				int used_local = 0;
				int used_g1 = 0;
				int used_g2 = 0;
				for (int i  = 0; i < Config.N; i++)
				{
					if (nodeRouters[i].linkIn[CW].Out != null) used_local ++;
					if (nodeRouters[i].linkIn[CCW].Out != null) used_local ++;
				} 
				for (int i = 0; i < 32; i++)
				{					
					if (L1bridgeRouters[i].LLinkIn[CW].Out != null) used_local ++;
					if (L1bridgeRouters[i].LLinkIn[CCW].Out != null) used_local ++;
					for (int j = 0; j < 2 * 2; j++)
						if (L1bridgeRouters[i].GLinkIn[j].Out != null)
							used_g1 ++;
				}
				for (int i = 0; i < 8; i++)
				{	
					for (int j = 0; j < 2 * 2; j++)
						if (L2bridgeRouters[i].LLinkIn[j].Out != null)
							used_g1 ++;
					for (int j = 0; j < 4 * 2; j++)
						if (L2bridgeRouters[i].GLinkIn[j].Out != null)
							used_g2 ++;
				}
				Simulator.stats.local_net_util.Add((double)used_local / 192);
				Simulator.stats.g1_net_util.Add((double)used_g1 / 160);
				Simulator.stats.g2_net_util.Add((double)used_g2 / 64);
			}

        }

        public bool isFinished()
        {
            switch (m_finish)
            {
                case FinishMode.app:
                    int count = 0;
                    for (int i = 0; i < Config.N; i++)
                        if (nodes[i].Finished) count++;

                    return count == Config.N;

                case FinishMode.cycle:
                    return Simulator.CurrentRound >= m_finishCount;

                case FinishMode.barrier:
                    return Simulator.CurrentBarrier >= (ulong)Config.barrier;
            }

            throw new Exception("unknown finish mode");
        }

        public bool isLivelocked()
        {
            for (int i = 0; i < Config.N; i++)
                if (nodes[i].Livelocked) return true;
            return false;
        }

        public void ParseFinish(string finish)
        {
            // finish is "app", "insn <n>", "synth <n>", or "barrier <n>"

            string[] parts = finish.Split(' ', '=');
            if (parts[0] == "app")
                m_finish = FinishMode.app;
            else if (parts[0] == "cycle")
                m_finish = FinishMode.cycle;
            else if (parts[0] == "barrier")
                m_finish = FinishMode.barrier;
            else
                throw new Exception("unknown finish mode");

            if (m_finish == FinishMode.app || m_finish == FinishMode.barrier)
                m_finishCount = 0;
            else
                m_finishCount = UInt64.Parse(parts[1]);
        }


        public virtual void close()
        {
        	if (Config.RingClustered == false)
	            for (int n = 0; n < Config.N; n++)
    	            routers[n].close();
        }

        public void visitFlits(Flit.Visitor fv)
        {
            foreach (Link l in links)
                l.visitFlits(fv);
            foreach (Router r in routers)
                r.visitFlits(fv);
            foreach (Node n in nodes)
                n.visitFlits(fv);
        }

        public delegate Flit FlitInjector();

        public int injectFlits(int count, FlitInjector fi)
        {
            int i = 0;
            for (; i < count; i++)
            {
                bool found = false;
                foreach (Router r in routers)
                    if (r.canInjectFlit(null))
                    {
                        r.InjectFlit(fi());
                        found = true;
                        break;
                    }

                if (!found)
                    return i;
            }

            return i;
        }

        public static Router MakeRouter(Coord c)
        {
            switch (Config.router.algorithm)
            {
		case RouterAlgorithm.DR_FLIT_SW_OF_MC:
		    return new Router_BLESS_MC (c);

		case RouterAlgorithm.BLESS_BYPASS:
                    return new Router_BLESS_BYPASS(c);

                case RouterAlgorithm.DR_AFC:
                    return new Router_AFC(c);

                case RouterAlgorithm.DR_FLIT_SWITCHED_CTLR:
                    return new Router_Flit_Ctlr(c);

                case RouterAlgorithm.DR_FLIT_SWITCHED_OLDEST_FIRST:
                    return new Router_Flit_OldestFirst(c);

                case RouterAlgorithm.DR_SCARAB:
                    return new Router_SCARAB(c);

                case RouterAlgorithm.DR_FLIT_SWITCHED_GP:
                    return new Router_Flit_GP(c);

                case RouterAlgorithm.DR_FLIT_SWITCHED_CALF:
                    return new Router_SortNet_GP(c);

                case RouterAlgorithm.DR_FLIT_SWITCHED_CALF_OF:
                    return new Router_SortNet_OldestFirst(c);

                case RouterAlgorithm.DR_FLIT_SWITCHED_RANDOM:
                    return new Router_Flit_Random(c);

                case RouterAlgorithm.ROUTER_FLIT_EXHAUSTIVE:
                    return new Router_Flit_Exhaustive(c);

                case RouterAlgorithm.OLDEST_FIRST_DO_ROUTER:
                    return new OldestFirstDORouter(c);

                case RouterAlgorithm.ROUND_ROBIN_DO_ROUTER:
                    return new RoundRobinDORouter(c);

                case RouterAlgorithm.STC_DO_ROUTER:
                    return new STC_DORouter(c);

                default:
                    throw new Exception("invalid routing algorithm " + Config.router.algorithm);
            }
        }

		public string portMap (int i) {
			/// Helper function to map port index to N/W/E/S
			/// Clockwise 0->3 map to N->E->S->W
			switch (i) {
			case 0: return "N"; 
			case 1: return "E"; 
			case 2: return "S"; 
			case 3: return "W";

			default:
				return "Warning: network::portMap only map port 0-3. Need to extend it for high-radix router."; 
			}

		}


    } // end of network calss
}
