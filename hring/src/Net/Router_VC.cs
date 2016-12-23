//#define DEBUG

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace ICSimulator
{

	// optimization 
	// 1. Each flit now has a destination list to support MC and pruning the multicast tree
	// 2. Each port has three lists: straight, left-turn, and right-turn
	// 3. The hotspot flits do not use "whirl routing" as merging in our case is performed for both ACK and Sync pkt
	// 4. Merging 1: Holding all master flits require additional storage. So, instead of holding master flits and wait, we perform 
	//    opportunistic merging. All incoming hs flits are compared with each other. 
	//    Merging 2: one possible way is to add a heap to each port (like an additional vc) to store master hs flits.
	//      The incoming hs flit will compare with all hs flits before buffer write. 
	//      The hs vc will perform VA and SA as normal flits.
	//      We can also control the wait time.
	//      The hs flit will skip the BW if merged. Other perform BW as usual.
	//      We will use vc = Config.vnet to hold the master hs flits. Each master hs flit vc has only 1 entry.

	// Missing function:
	// Deadlock avoidance: partitioning the VC

	// Define a flit type just for VC router
	public class vcFlit : IComparable
	{
		Flit m_f;
		bool [] m_allocPort;

		public Flit flit {get {return m_f;} set {m_f = value;}}
		public bool [] allocPort {get {return m_allocPort; } set { m_allocPort = value;}}

		public vcFlit (Flit f)
		{
			m_f = f;
			m_allocPort = new bool[5] {false, false, false, false, false};
		}

		public int CompareTo (object o)
		{
			if (o is vcFlit)
				return Router_Flit_OldestFirst._rank (m_f, (o as vcFlit).m_f);
			else
				throw new ArgumentException("bad comparison");
		}

	}


	public class Router_VC:Router
	{
		/* To check the direction the flit travel,  outdir =
		 * indir+1: turn left
		 * indir+2: go straight
		 * indir+3 turn right
		 * */


		// Buffer pool. reuse the allocated memory
		Queue<vcFlit> m_vcFlitSlots;

		// buffers, indexed by physical channel and virtual network
		protected MinHeap<vcFlit>[,] m_buf;

		// for debug
		int m_load =0;

		// optimization 2: use to determine if a flit needs be dropped 
		// if the destination list does not contain any nodes along the current direction, and
		// if the RTB and LTB of this direction is 0, implying that a flit do not destine to any node by going Right and Left,
		// it should be dropped. 
		protected int [,] nodeStraight, nodeLeft, nodeRight, mcMask;
		public bool [] LTB, RTB;

		public Router_VC (Coord myCoord) : base (myCoord)
		{
			m_vcFlitSlots = new Queue<vcFlit> ();
			m_buf = new MinHeap<vcFlit>[5, Config.vnets+1]; //m_buf [Config.vnets] can be used to hold master hs flit
			LTB = new bool[4];
			RTB = new bool[4];

			for (int pc = 0; pc < 5; pc++)
				for (int i = 0; i < Config.vnets+1; i++)
				{
					m_buf[pc, i] = new MinHeap<vcFlit>();
				}

			nodeStraight = new int[4,Config.N]; // Clockwise 0->3 map to N->E->S->W
			nodeLeft = new int[4,Config.N]; // Clockwise 0->3 map to N->E->S->W
			nodeRight = new int[4,Config.N]; // Clockwise 0->3 map to N->E->S->W
			mcMask = new int[5,Config.N]; // Clockwise 0->3 map to N->E->S->W

			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < Config.N; j++) {
					nodeStraight [i, j] = 0;
					nodeLeft [i, j] = 0;
					nodeRight [i, j] = 0;
					mcMask [i, j] = 0;
				}
				LTB [i] = false;
				RTB [i] = false;
			}

			// set up the mask for routing
			straightNodeMask ();
			leftTurnMask ();
			rightTurnMask ();
			SetupMask (); // not used
		}


		protected void SetupMask () {
			Coord dstCoord; 

			for (int j=0; j<Config.N; j++){
				if (j == coord.ID) // coord is the coordinate of the current node.
					continue;
				dstCoord = new Coord (j);
				if (coord.y < dstCoord.y)
					mcMask [(int)DIR.N, j] = 1;
				if (coord.x < dstCoord.x)
					mcMask [(int)DIR.E, j] = 1;
				if (coord.y > dstCoord.y)
					mcMask [(int)DIR.S, j] = 1;
				if (coord.x > dstCoord.x)
					mcMask [(int)DIR.W, j] = 1;
				if (coord.x == dstCoord.x && coord.y == dstCoord.y)
					mcMask [(int)DIR.L, j] = 1;
				
			}

		}

		protected void leftTurnMask () {
			Coord dstCoord; // coord is the coordinate of the current node.

			for (int j=0; j<Config.N; j++){
				if (j == coord.ID)
					continue;
				dstCoord = new Coord (j);

				if (coord.x > dstCoord.x && coord.y < dstCoord.y)
					nodeLeft [(int)DIR.N, j] = 1;
				else if (coord.x < dstCoord.x && coord.y < dstCoord.y)
					nodeLeft [(int)DIR.E, j] = 1;
				else if (coord.x < dstCoord.x && coord.y > dstCoord.y)
					nodeLeft [(int)DIR.S, j] = 1;
				else if (coord.x > dstCoord.x && coord.y > dstCoord.y)
					nodeLeft [(int)DIR.W, j] = 1;
				else
					Debug.Assert (false, "ERROR: Mask Setup\n");
			}

		}

		protected void rightTurnMask () {
			Coord dstCoord; // coord is the coordinate of the current node.

			for (int j=0; j<Config.N; j++){
				if (j == coord.ID)
					continue;
				dstCoord = new Coord (j);
				if (coord.x < dstCoord.x && coord.y < dstCoord.y)
					nodeRight [(int)DIR.N, j] = 1;
				else if (coord.x < dstCoord.x && coord.y > dstCoord.y)
					nodeRight [(int)DIR.E, j] = 1;
				else if (coord.x > dstCoord.x && coord.y > dstCoord.y)
					nodeRight [(int)DIR.S, j] = 1;
				else if (coord.x > dstCoord.x && coord.y < dstCoord.y)
					nodeRight [(int)DIR.W, j] = 1;
				else
					Debug.Assert (false, "ERROR: Mask Setup\n");
			}

		}

		protected void straightNodeMask () {
			
			Coord dstCoord; // the coord of the destination node


			for (int j=0; j<Config.N; j++){
				if (j == coord.ID) // coord is the coordinate of the current node
					continue;
				dstCoord = new Coord (j);
				if (coord.x == dstCoord.x && coord.y < dstCoord.y)
					nodeStraight [(int)DIR.N, j] = 1;
				else if (coord.x < dstCoord.x && coord.y == dstCoord.y)
					nodeStraight [(int)DIR.E, j] = 1;
				else if (coord.x == dstCoord.x && coord.y > dstCoord.y)
					nodeStraight [(int)DIR.S, j] = 1;
				else if (coord.x > dstCoord.x && coord.y == dstCoord.y)
					nodeStraight [(int)DIR.W, j] = 1;
				else
					Debug.Assert (false, "ERROR: Mask Setup\n");
			}

		}
			
		protected new void determineDirection (Flit f) {
			PreferredDirection pd;
			//bool drop = true;

			bool stop = false;
			if (Simulator.CurrentRound == 11536 && coord.ID == 8)
			{
				stop = true;
			}

			f.ClearRoutingInfo ();// must clear the route computation result from the previous iteration

			if (!f.packet.mc) {
				// this is an uni-cast flit
				pd = determineDirection (f, coord);
				// U-turn is not allowed
				if (pd.yDir == Simulator.DIR_UP && f.inDir != Simulator.DIR_UP)
					f.preferredDirVector [Simulator.DIR_UP] = true;
				else if (pd.xDir == Simulator.DIR_RIGHT && f.inDir != Simulator.DIR_RIGHT)
					f.preferredDirVector [Simulator.DIR_RIGHT] = true;
				else if (pd.yDir == Simulator.DIR_DOWN && f.inDir != Simulator.DIR_DOWN)
					f.preferredDirVector [Simulator.DIR_DOWN] = true;
				else if (pd.xDir == Simulator.DIR_LEFT && f.inDir != Simulator.DIR_LEFT)
					f.preferredDirVector [Simulator.DIR_LEFT] = true;
				else if (f.dest.ID == ID)
					f.preferredDirVector [Simulator.DIR_LOCAL] = true;

			} else {
				// Multicast flit

				/*
				 * For local mc flit,
				 * 1) clear the LTB and RTB, because LTB [i] and RTB [i] at the source change each cycle.
				 * 2) compute the productive ports based on turn bit and if there is any destination nodes resided in the region after turn
				 * 3) assign LTB and RTB
				 * 
				 * For non-local mc flit,
				 * 1) given flit goes straight, if dest is in the straight, left-turned, or right-turned domain
				 * 2) given flit turns left, if dest is in the straight domain (flit can only make turn once)
				 * 3) given flit turns right, if dest is in the straight domain (flit can only make turn once)
				 * 4) check if this is one of the destination
				 * */

				// local injected flit, need to set the LTB and RTB
				if (f.inDir == 4) {
					// recompute the LTB and RTB of the local flit to clear out the LTB and RTB from the previous iteration.
					f.LTB = false; 
					f.RTB = false;

					for (int dir = 0; dir < 4; dir++) {
						if (neigh [dir] == null)
							continue;

						// check if there is any destination can be reached through this direction.
						// avoid sending additional flit
						foreach (Coord dest in f.destList) {

							if (nodeStraight [dir, dest.ID] == 1 ) {
								f.preferredDirVector [dir] = true;
								f.LTB = LTB [dir]; // is it necessary?
								f.RTB = RTB [dir]; // is it necessary?
							}
							if (nodeLeft [dir, dest.ID] == 1 && LTB [dir]) {
								f.preferredDirVector [dir] = true;
								f.LTB = LTB [dir];
							}
							if (nodeRight [dir, dest.ID] == 1 && RTB [dir]) {
								f.preferredDirVector [dir] = true;
								f.RTB = RTB [dir];
							}
						}
					}
				} else {
					foreach (Coord dest in f.destList) {
						
						// given flit goes straight, if dest is in the straight, left-turned, or right-turned domain
						if (nodeStraight [(f.inDir + 2) % 4, dest.ID] == 1 || (nodeRight[(f.inDir + 2) % 4, dest.ID] == 1 && f.RTB) || (nodeLeft [(f.inDir + 2) % 4, dest.ID] == 1 && f.LTB)) {
							f.preferredDirVector [(f.inDir + 2) % 4] = true;
						}
						// given flit turns left, if dest is in the straight domain (flit can only make turn once)
						if (f.LTB && neigh [(f.inDir + 1) % 4] != null && (nodeStraight [(f.inDir + 1) % 4, dest.ID] == 1)) {
							f.preferredDirVector [(f.inDir + 1) % 4] = true;
						}
						// given flit turns right, if dest is in the straight domain (flit can only make turn once)
						if (f.RTB && neigh [(f.inDir + 3) % 4] != null && (nodeStraight [(f.inDir + 3) % 4, dest.ID] == 1)) {
							f.preferredDirVector [(f.inDir + 3) % 4] = true;
						}
						// check if this is one of the destination
						if (dest.ID == coord.ID) {
							f.preferredDirVector [4] = true;
						}
					}
				}
			}
		}

		protected Router_VC getNeigh(int dir)
		{
			return neigh[dir] as Router_VC;
		}

		// accept one ejected flit into rxbuf
		protected void acceptFlit(Flit f)
		{
			statsEjectFlit(f);
			if (f.packet.mc) {
				if (f.packet.nrOfArrivedFlitsMC[ID] + 1 == f.packet.nrOfFlits)
					statsEjectPacket (f.packet);
			} else {
				if (f.packet.nrOfArrivedFlits + 1 == f.packet.nrOfFlits)
					statsEjectPacket (f.packet);
			}

			m_n.receiveFlit(f);
		}

		protected bool merging (Flit f, int indir) {
			// return true if merged or stored in master hs vc; 

			vcFlit vcflit, master;

			if (f.packet.gather) {
				// compare with other master hs flits
				for (int i = 0; i < 5; i++) {
					if (m_buf [i, Config.vnets].Count < 1)
						continue;
					master = m_buf [i, Config.vnets].Peek ();

					// merge
					if (f.packet.gather && (f.flitNr == master.flit.flitNr) &&
						(f.dest.ID == master.flit.dest.ID) && (f.packet.hsFlowID == master.flit.packet.hsFlowID)) {
						master.flit.ackCount += f.ackCount;
						ScoreBoard.UnregPacket (f.packet.dest.ID, f.packet.ID);
						Simulator.stats.merge_flit.Add ();;
						return true;
					}
				}

				// If not merged, put into m_buf[indir, Config.vnets] if possible
				if (m_buf [indir, Config.vnets].Count < 1) {
					vcflit = getFreeSlot(f);
					m_buf [indir, Config.vnets].Enqueue (vcflit);
					Simulator.stats.vc_buf_wr.Add ();
					return true;
				}
			}
			return false;

		}

		protected void bufferWrite () {

			bool stop = false;
			if (Simulator.CurrentRound == 655 && ID == 7)
			{
				stop = true;
			}

			vcFlit vcflit;

			// grab inputs into buffers
			for (int dir = 0; dir < 4; dir++)
			{
				if (linkIn[dir] != null && linkIn[dir].Out != null)
				{
					Flit f = linkIn[dir].Out;
					f.inDir = dir;
					linkIn[dir].Out = null;
					f.ClearRoutingInfo ();	
					bool merged = merging (f, dir); // perform merging
					if (merged) 
						continue;
					vcflit = getFreeSlot(f);
					m_buf[dir, f.virtualChannel].Enqueue(vcflit);
					Simulator.stats.vc_buf_wr.Add ();
				}
			}
		}

		// Function to reuse the allocated memory (i.e., m_vcFlitSlots)
		// This can reduce the memory footprint.
		vcFlit getFreeSlot (Flit f)
		{
			if (m_vcFlitSlots.Count > 0) {
				vcFlit s = m_vcFlitSlots.Dequeue ();
				s.flit = f;
				for (int i = 0; i < 5; i++)
					s.allocPort [i] = false;
				return s;
			} else
				return new vcFlit (f);
		}

		// Just recycle the allocated memory.
		// The content of the old flit will be overriden upon calling getFreeSlot()
		void returnFreeSlot (vcFlit s) {
			//s.flit = null;
			m_vcFlitSlots.Enqueue (s);
		}

		// InjectFlit is called in Node.cs 
		// Do not need to call explictly in a router
		public override void InjectFlit(Flit f) {

			bool stop = false;
			if (Simulator.CurrentRound == 661 && coord.ID == 7)
			{
				stop = true;
			}

			//set LTB randomly for each direction and determine the RTB accordingly.
			for (int i = 0; i < 4; i++) {
				if (neigh [i] != null && (neigh [i].neigh[(i+2+1)%4] != null))
					LTB [i] = (Simulator.rand.Next (2) == 0) ? false : true;
				else
					LTB [i] = false;
			}
			for (int j = 0; j < 4; j++) {
				if (neigh [j] != null && (neigh [j].neigh[(j+2+3)%4] != null))
					RTB [j] = !LTB [(j + 1) % 4];
				else
					RTB [j] = false;
			}
				
			// inject into router
			f.inDir = 4;
			statsInjectFlit (f);
			bool merged = merging (f, 4);
			if (merged)
				return;
			vcFlit slot = getFreeSlot (f);
			m_buf [4, f.virtualChannel].Enqueue (slot);
			Simulator.stats.vc_buf_wr.Add ();
		}

		public override bool canInjectFlit(Flit f)
		{
			int cl = f.virtualChannel;
		
			return m_buf[4, cl].Count < capacity(cl);

		}

		int capacity(int cl)
		{
			// size vc capacity for ctrl and data flit
			return Config.vnetsDepth;
		}


		protected void routerCompute () {
			
			bool stop = false;
			if (Simulator.CurrentRound == 674 && coord.ID == 5)
			{
				stop = true;
			}
			for (int pc = 0; pc < 5; pc++)
				for (int vc = 0; vc < Config.vnets+1; vc++)
					if (m_buf [pc, vc].Count > 0) {
						determineDirection (m_buf [pc, vc].Peek().flit);
						Simulator.stats.vc_buf_rd.Add ();  // read buffer once. Do not need to read it again for switch allocation
					}
		}
			
		// find a winner for each downstream output port
		//    arbitrate among flit requesting the same output port 
		//    a flit will back off if the downstream VC is full, but it can still claim the output port if other vc has available credit.
		protected void vcArbitration () {

			vcFlit winner;
			ulong time = Simulator.CurrentRound;

			for (int outdir = 0; outdir < 5; outdir++) {
				winner = null;
				for (int outvc = 0; outvc < Config.vnets; outvc++) {
					for (int indir = 0; indir < 5; indir++) {
						for (int invc = 0; invc < Config.vnets+1; invc++) {
							if (m_buf [indir, invc].Count > 0) {
								vcFlit top = m_buf [indir, invc].Peek ();
								Simulator.stats.vc_vc_arb.Add ();

								// check the downstream vc credit, local flit skip here
								if (top.flit.preferredDirVector[outdir] == true && top.flit.virtualChannel == outvc && outdir != 4) {
									Router_VC nrouter = (Router_VC)neigh [outdir];
									int ndir = (outdir + 2) % 4;
									if (nrouter.m_buf [ndir, outvc].Count >= capacity (outvc)) 
										continue; // skip the current competitor as its requested downstream vc is unavailable
								}

								// compete for the output port
								if (top.flit.preferredDirVector [outdir] == true) {
									// CompareTo return -1 means the top flit has higher priority
									if (winner == null) {
										winner = top;
										winner.flit.virtualChannel = outvc; // may switch to VC with available credit 
										winner.allocPort [outdir] = true;
									} else if (top.CompareTo (winner) < 0) {
										winner.allocPort [outdir] = false;
										winner = top;
										winner.flit.virtualChannel = outvc; // may switch to VC with available credit
										winner.allocPort [outdir] = true;
									}
								}
							}
						}
					}
				}
			}				
		}

		// find the highest-priority flit from each physical channel
		protected void swArbTraversal () {
			bool noReq;
			vcFlit top, winner;

			for (int indir = 0; indir < 5; indir++) {
				noReq = true; // by default, this flit has no port request
				winner = null; // winner can keep the allocated port. 
				for (int vc = 0; vc < Config.vnets+1; vc++) {
					if (m_buf [indir, vc].Count > 0) {
						top = m_buf [indir, vc].Peek ();
						Simulator.stats.vc_sw_arb.Add ();

						noReq = !(top.allocPort [0] | top.allocPort [1] | top.allocPort [2] | top.allocPort [3] | top.allocPort [4]);
						if (noReq == true)
							continue;
						
						if (winner == null)
							winner = top;
						else if (top.CompareTo (winner) < 0) {
							for (int i = 0; i < 5; i++)
								winner.allocPort [i] = false; // all the allocated port request of the losing flit will be nullified
							winner = top;
						}
						
					}
				}
			}
		}
			
		// put the flit on the output ports
		protected void swTraversal ()
		{
			/*   create replica if neccessary (i.e., when allocport > 1)
			 *   assign LTB and RTB to the newly created replica
			 *   remove the destination covered by the replica from the original destination list (based on mask, turn bit)
			 *   reset the turn bit after each turn, as flit only allows to turn once
			 *   put the flit on output port or eject
			 *   nullify the input buffer based on the switch allocation result 
			 *     (The flits which do not get all the requested ports will stay in the buffer).
			 * */

			Coord dstCoord;
			int dstID;
			Flit outflit; // the flit will be put on the output port
			int replicaNeed, replicaCreate; // if replicaNeed == replicaCreate, do not create replica.

			bool stop = false;
			if (Simulator.CurrentRound == 819 && coord.ID == 7)
			{
				stop = true;
			}


			// several things are needed prior to put on output port
			// 1) modify LTB, RTB;
			// 2) fork new flit
			// 3) manage dest list; 

			for (int indir = 0; indir < 5; indir++) {
				for (int vc = 0; vc < Config.vnets+1; vc++) {
					outflit = null;
					replicaNeed = -1; // start from -1; so if a flit only travel to one direction, no replica is created
					replicaCreate = 0;
					if (m_buf [indir, vc].Count > 0) {
						vcFlit vcflit = m_buf [indir, vc].Peek ();
						for (int i = 0; i < 5; i++)
							if (vcflit.flit.preferredDirVector[i])
								replicaNeed++;
						for (int outdir = 0; outdir < 5; outdir++) {
							if (vcflit.allocPort [outdir]) {

								Simulator.stats.vc_sw_traversal.Add ();

								if (replicaNeed != replicaCreate && replicaNeed > 0) {
									// Create new replica
									outflit = new Flit (vcflit.flit.packet, vcflit.flit.flitNr);
									outflit.inDir = vcflit.flit.inDir;
									statsInjectFlit (vcflit.flit); // increase the flit count, as a flit is generated
									m_load++;
									outflit.destList = new List <Coord> ();
									outflit.injectionTime = Simulator.CurrentRound;
									replicaCreate++;


									if (outflit.inDir == 4) {
										// Must assign LTB and RTB for local flit here!
										// I forget the reason why am I doing this, but it has to be here.......
										outflit.LTB = LTB [outdir];
										outflit.RTB = RTB [outdir];

									} else {
										outflit.LTB = vcflit.flit.LTB;
										outflit.RTB = vcflit.flit.RTB;
									}		
								
									// Destination list management
									if (outdir == 4) {
										vcflit.flit.destList.Remove (coord); // remove from the master flit (i.e., the first flit got the productive port)
									} else {
										foreach (Coord c in vcflit.flit.destList.ToList()) {
											dstCoord = c;
											dstID = c.ID;
											// if a lift has turned left, only carry the ones in the straightnodes and rightturn domain.
											// Same for the other direction
											// flit go straight
											if (
												(nodeStraight [outdir, dstID] == 1) ||
												((nodeLeft [outdir, dstID] == 1) && outflit.LTB) ||
												((nodeRight [outdir, dstID] == 1) && outflit.RTB)) {
												outflit.destList.Add (dstCoord);
												outflit.packet.creationTimeMC [dstID] = Simulator.CurrentRound;
												vcflit.flit.destList.Remove (c); // remove from the master flit (i.e., the first flit got the productive port)
											} 
										}
									}
								} else {
									outflit = vcflit.flit;
								}

								// reset the turn bit after each turn, as flit only allows to turn once
								if (outflit.inDir != 4 && outflit.packet.mc) {
									if ((outflit.inDir + 1) % 4 == outdir) {
										outflit.LTB = false;
										outflit.RTB = false;
									} else if ((outflit.inDir + 3) % 4 == outdir) {
										outflit.LTB = false;
										outflit.RTB = false;
									}
										
								}
									
								// Put the flit on output port
								if (outdir == 4)
									acceptFlit (outflit);
								else {
									linkOut [outdir].In = outflit;
									Simulator.stats.vc_link_traversal.Add ();
								}

								m_load--;
							}
						}
					}
				}
			}

			nullifyBuffer ();
		}

		protected  void nullifyBuffer () {
			bool nullify;
			for (int indir = 0; indir < 5; indir++) {
				for (int vc = 0; vc < Config.vnets+1; vc++) {
					if (m_buf [indir, vc].Count > 0) {
						vcFlit flit = m_buf [indir, vc].Peek ();
						nullify = true;
						for (int outdir = 0; outdir < 5; outdir++) {
							// nullify a flit if all the requested ports are granted.
							if (flit.allocPort [outdir] == false && flit.flit.preferredDirVector [outdir] == true) {
								nullify = false;
							} else if (flit.allocPort [outdir] == true && flit.flit.preferredDirVector [outdir] == true) {
								flit.allocPort [outdir] = false;
								flit.flit.preferredDirVector [outdir] = false; // just clear the allocated port
							}
						}
						if (nullify) {
							returnFreeSlot (flit);
							m_buf [indir, vc].Dequeue ();
							flit = null;
						}
					}
				}
			}
		}

	
		protected override void _doStep()
		{

			bufferWrite ();

			getInitLoad (); // for debug

			printFlitIn (); // for debug

			routerCompute ();

			vcArbitration ();

			swArbTraversal ();

			swTraversal ();

			compareLoad (); // for debug
			
			printFlitOut (); // for debug

		}


		void printFlitIn() {

			vcFlit f;
			bool printTime = false; // ensure only print time once for those non-empty router;

			bool stop = false;
			if (Simulator.CurrentRound == 653 && coord.ID == 7)
			{
				stop = true;
			}

			// interate through PC and VC, print out every flits
			for (int pc = 0; pc < 5; pc++)
				for (int vc = 0; vc < Config.vnets+1; vc++) {
					for (int i = 1; i < m_buf [pc, vc].Count+1; i++) {
						f = m_buf [pc, vc].Peek (i);
						
						if (f != null) {
							if (printTime == false) {
								Console.WriteLine ("\nTime {0} @ Router {1}", Simulator.CurrentRound, ID);
								printTime = true;
							}

							if (f.flit.packet.mc) {
								Console.Write ("Packet {0}.{1} In [{2},{3},{4}], Turn[{5}, {6}], Dst: ", f.flit.packet.ID, f.flit.flitNr, Simulator.network.portMap (pc), vc, i, f.flit.LTB, f.flit.RTB);
								foreach (Coord c in f.flit.destList)
									Console.Write ("{0} ", c.ID);
								Console.Write ("\n");

							} else {
								Console.WriteLine ("Packet {0}.{1} In [{2},{3},{4}], Dst: {5}", f.flit.packet.ID, f.flit.flitNr, Simulator.network.portMap (pc), vc, i, f.flit.dest.ID);
							}

						}
					}
				}
		}

		void printFlitOut() {
			Flit f;
			for (int dir = 0; dir < 4; dir++)
				if (linkOut [dir] != null && linkOut [dir].In != null) {
					f = linkOut [dir].In;
					if (f.packet.mc){
						Console.Write ("Packet {0}.{1} Out {2}, Turn[{3}, {4}], Dst: ", f.packet.ID, f.flitNr, Simulator.network.portMap(dir), f.LTB, f.RTB);
						foreach (Coord c in f.destList)
							Console.Write ("{0} ", c.ID);
						Console.Write ("\n");
					}
					else
						Console.WriteLine ("Packet {0}.{1} Out {2}, Dst: {3}", f.packet.ID, f.flitNr, Simulator.network.portMap(dir), f.dest.ID);
				}
		}


		int getLoad() {
			int load = 0;
			for (int pc = 0; pc < 5; pc++)
				for (int vc = 0; vc < Config.vnets+1; vc++) {
					load = load + m_buf [pc, vc].Count;
				}
			return load;
		}

		void getInitLoad() {
			m_load = getLoad ();
		}

		void compareLoad() {
			int load_now = getLoad ();
			Debug.Assert (load_now==m_load,"Flits count did not match!");
		}
	}
}
