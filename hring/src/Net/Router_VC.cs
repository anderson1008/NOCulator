//#define DEBUG

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace ICSimulator
{

	// Define a flit type just for VC router
	public class vcFlit : IComparable
	{
		Flit m_f;

		public Flit flit {get {return m_f;} set {m_f = value;}}

		public vcFlit (Flit f)
		{
			m_f = f;
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
		// Buffer pool. reuse the allocated memory
		Queue<vcFlit> m_vcFlitSlots;

		// buffers, indexed by physical channel and virtual network
		protected MinHeap<vcFlit>[,] m_buf;

		// use to select the high-priority flit in VA and SA
		vcFlit[] requesters;
		int[] requester_dir;


		public Router_VC (Coord myCoord) : base (myCoord)
		{
			m_vcFlitSlots = new Queue<vcFlit> ();
			m_buf = new MinHeap<vcFlit>[5, Config.vnets];
			requesters = new vcFlit[5];
			requester_dir = new int[5];

			for (int pc = 0; pc < 5; pc++)
				for (int i = 0; i < Config.vnets; i++)
				{
					m_buf[pc, i] = new MinHeap<vcFlit>();
				}
		}

		protected Router_VC getNeigh(int dir)
		{
			return neigh[dir] as Router_VC;
		}

		protected void acceptFlit(Flit f)
		{
			statsEjectFlit(f);
			if (f.packet.nrOfArrivedFlits + 1 == f.packet.nrOfFlits)
				statsEjectPacket (f.packet);
			m_n.receiveFlit (f);

			//Console.WriteLine ("Packet {0}.{1} Eject at {2}", f.packet.ID, f.flitNr, f.dest.ID);
		}

		protected void bufferWrite () {
			// grab inputs into buffers
			for (int dir = 0; dir < 4; dir++)
			{
				if (linkIn[dir] != null && linkIn[dir].Out != null)
				{
					Flit f = linkIn[dir].Out;
					linkIn[dir].Out = null;
					vcFlit vcFlit = getFreeSlot(f);
					m_buf[dir, f.packet.getClass()].Enqueue(vcFlit);
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
				return s;
			} else
				return new vcFlit (f);
		}

		// Just recycle the allocated memory.
		// The content of the old flit will be overriden upon calling getFreeSlot()
		void returnFreeSlot (vcFlit s) {
			m_vcFlitSlots.Enqueue (s);
		}

		void clearPrioBuffer ()
		{
			for (int i = 0; i < 5; i++)
			{
				requesters[i] = null;
				requester_dir[i] = -1;
			}
		}

		// InjectFlit is called in Node.cs 
		// Do not need to call explictly in a router
		public override void InjectFlit(Flit f) {
			vcFlit slot = getFreeSlot (f);
			m_buf [4, f.packet.getClass ()].Enqueue (slot);
		}

		public override bool canInjectFlit(Flit f)
		{
			int cl = f.packet.getClass();
		
			return m_buf[4, cl].Count < capacity(cl);

		}

		int capacity(int cl)
		{
			// size vc capacity for ctrl and data flit
			return Config.vnetsDepth;
		}

		// find the highest-priority vnet head for each input physical channel (PC)
		protected void vcArbitration () {
			
			for (int pc = 0; pc < 5; pc++) {
				for (int vnet = 0; vnet < Config.vnets; vnet++) {
					if (m_buf [pc, vnet].Count > 0) {
						vcFlit top = m_buf [pc, vnet].Peek ();
						PreferredDirection pd = determineDirection (top.flit, coord);
						int outdir = (pd.xDir != Simulator.DIR_NONE) ? pd.xDir : pd.yDir;
						if (outdir == Simulator.DIR_NONE)
							outdir = 4; // local ejection

						// check the downstream vc credit
						if (outdir != 4) {
							Router_VC nrouter = (Router_VC)neigh [outdir];
							int ndir = (outdir + 2) % 4;
							if (nrouter.m_buf [ndir, vnet].Count >= capacity (vnet))
								continue; // skip the current competitor as its requested downstream vc is unavailable
						}

						// otherwise, contend for top requester from this physical channel
						// CompareTo return -1 means the top flit has higher priority
						if (requesters [pc] == null || top.CompareTo (requesters [pc]) < 0) {
							requesters [pc] = top;
							requester_dir [pc] = outdir;
						} 
					}
				}
			}
		}

		// find the highest-priority requester for each output
		protected void swArbTraversal () {
			
			vcFlit top;
			int top_indir;

			for (int outdir = 0; outdir < 5; outdir++)
			{
				top = null;
				top_indir = -1;
				// sweep the one requester from each PC
				for (int req = 0; req < 5; req++) {
					if (requesters [req] != null && requester_dir [req] == outdir) {
						if (top == null || requesters [req].CompareTo (top) < 0) {
							top = requesters [req];
							top_indir = req;
						}
					}
				}

				// switch traversal here
				// put the flit on the output ports

				if (top_indir != -1) {
					m_buf[top_indir, top.flit.packet.getClass()].Dequeue();

					//log the injected flit here, TODO: but need to double check 
					if (top_indir == 4)
						statsInjectFlit(top.flit);

					// propagate to next router (or eject)
					if (outdir == 4)
						acceptFlit(top.flit);
					else
						linkOut[outdir].In = top.flit;

					returnFreeSlot(top);
				}
			}
		}

	

	
		protected override void _doStep()
		{
			bufferWrite ();

			//PrintFlitIn ();

			// perform arbitration: (i) collect heads of each virtual-net
			// heap (which represents many VCs) to obtain a single requester
			// per physical channel; (ii)  request outputs among these
			// requesters based on DOR; (iii) select a single winner
			// per output

			clearPrioBuffer ();

			vcArbitration ();

			swArbTraversal ();

			//PrintFlitOut ();

		}


		void PrintFlitIn() {

			vcFlit f;
			bool printTime = false; // ensure only print time once for those non-empty router;

			// interate through PC and VC, print out every flits
			for (int pc = 0; pc < 5; pc++)
				for (int vc = 0; vc < Config.vnets; vc++)
					for (int i = 0; i < Config.vnetsDepth; i++) {
						f = m_buf [pc, vc].Peek (i);
						if (f != null) {
							if (printTime == false) {
								Console.WriteLine ("\nTime {0} @ Router {1}", Simulator.CurrentRound, ID);
								printTime = true;
							}

							if (f.flit.packet.mc) {
								Console.Write ("Packet {0}.{1} In [{2},{3}], Dst: ", f.flit.packet.ID, f.flit.flitNr, Simulator.network.portMap(pc), vc);
								foreach (Coord c in f.flit.destList)
									Console.Write ("{0} ", c.ID);
								Console.Write ("\n");

							} else {
								Console.WriteLine ("Packet {0}.{1} In [{2},{3}], Dst: {4}", f.flit.packet.ID, f.flit.flitNr, Simulator.network.portMap(pc), vc, f.flit.dest.ID);
							}

						}
					}
		}

		void PrintFlitOut() {
			Flit f;
			for (int dir = 0; dir < 4; dir++)
				if (linkOut [dir] != null && linkOut [dir].In != null) {
					f = linkOut [dir].In;
					if (f.packet.mc){
						Console.Write ("Packet {0}.{1} Out {2}, Dst: ", f.packet.ID, f.flitNr, Simulator.network.portMap(dir));
						foreach (Coord c in f.destList)
							Console.Write ("{0} ", c.ID);
						Console.Write ("\n");
					}
					else
						Console.WriteLine ("Packet {0}.{1} Out {2}, Dst: {3}", f.packet.ID, f.flitNr, Simulator.network.portMap(dir), f.dest.ID);
				}
		}
	}
}

