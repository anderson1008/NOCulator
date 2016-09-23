//#define DEBUG

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Linq;


namespace ICSimulator
{
	enum DIR {N, E, S, W, L};

	// MC mode 1:
	// If honor the priority of the MC flit at each router, flit replication is pure 
	// opportunistic. Replication is granted only if the productive direction is available.
	// It biases toward MC traffic. However, we need to track when MC direction is granted, 
	// and how many of replica can be made.
	// MC mode 2:
	// If MC flit has the lowest priority, besides local injected flit, replica is made only if
	// the produtive direction is available.

	public class Router_BLESS_MC: Router_Flit
	{

		Queue<Flit>	[] ejectBuffer;
		protected int [,] mcMask;
		Flit[] inputBuffer;  // keep this as a member var so we don't
		int numFlitIn = 0;   // use to prevent swamp the router (i.e., deadlock in router)

		public Router_BLESS_MC(Coord myCoord)
			: base(myCoord)
		{
			m_injectSlot = null;
			ejectBuffer = new Queue<Flit>[4];
			inputBuffer = new Flit[5];

			for (int n = 0; n < 4; n++)
				ejectBuffer[n] = new Queue<Flit>();
			mcMask = new int[4,Config.N]; // Clockwise 0->3 map to N->E->S->W
			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < Config.N; j++)
					mcMask [i, j] = 0;
			}
			SetupMask ();
		}
        

		protected void SetupMask () {
			Coord dstCoord; // coord is the coordinate of the current node.
			//DIR dir;

			for (int j=0; j<Config.N; j++){
				if (j == coord.ID)
					continue;
				dstCoord = new Coord (j);
				if (coord.x <= dstCoord.x && coord.y < dstCoord.y)
					mcMask [(int)DIR.N, j] = 1;
				else if (coord.x < dstCoord.x && coord.y >= dstCoord.y)
					mcMask [(int)DIR.E, j] = 1;
				else if (coord.x >= dstCoord.x && coord.y > dstCoord.y)
					mcMask [(int)DIR.S, j] = 1;
				else if (coord.x > dstCoord.x && coord.y <= dstCoord.y)
					mcMask [(int)DIR.W, j] = 1;
				else
					Debug.Assert (false, "ERROR: Mask Setup\n");
			}
		    
		}

		public void PrintMask () {
			Console.WriteLine ("0 1 2 3 4 5 6 7 8 9 A B C D E F");
			
			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < Config.N; j++) 		
					Console.Write ("{0} ", mcMask [i, j]);	
				Console.WriteLine ();
			}
		}

		public override bool canInjectFlit(Flit f)
		{
			// Check if injection buffer is available in NI
			return m_injectSlot == null;
		}

		public override void InjectFlit(Flit f)
		{
			// Put flit in the injection buffer in NI

			if (m_injectSlot != null)
				throw new Exception("Trying to inject twice in one cycle");

			m_injectSlot = f;
		}

		protected void InjectToRouter () {
			// outCount: # of the outstanding flits at the inport of output link
			int outCount = 0;
			for (int dir = 0; dir < 4; dir++)
				if (linkOut[dir] != null && linkOut[dir].In != null)
					outCount++;
			if (outCount != 0)
				Debug.Assert (false, "Something wrong!");
			
			bool wantToInject = m_injectSlot != null;
			bool canInject = (numFlitIn + outCount) < neighbors;
			bool starved = wantToInject && !canInject;

			if (starved)
			{
				Flit starvedFlit = null;
				if (starvedFlit == null) starvedFlit = m_injectSlot;

				Simulator.controller.reportStarve(coord.ID);
				statsStarve(starvedFlit);
			}
			if (canInject && wantToInject)
			{				
				if (m_injectSlot != null)
				{
					RouteCompute (m_injectSlot);

					int numMC = 0;
					for (int j = 0; j < 4; j++)  // DO NOT check local bit
						if (m_injectSlot.preferredDirVector [j] && m_injectSlot.packet.mc)
							numMC++;

					if (numMC > 1) 
						m_injectSlot.replicateNeed = true;

					 // This injection require flits to be fully sorted.
				
					for (int i = 0; i < 4; i++)
						if (inputBuffer [i] == null) {
							inputBuffer [i] = m_injectSlot;
							break;
						}
					numFlitIn++;	
					#if DEBUG
					Console.WriteLine ("#1 InjectToRouter: Time {0}: Inject @ Router {1} {2}", Simulator.CurrentRound, ID, m_injectSlot.ToString());
					#endif
					statsInjectFlit (m_injectSlot);
					m_injectSlot = null;
				}
				else
					throw new Exception("what???inject null flits??");
			}
		}


		protected void Merge () {
			int priority_inv = 0;

			for (int i = 0; i < 4; i++) {
				if (inputBuffer [i] == null)
					continue;
				for (int j = i+1; j < 4; j++) {
					if (inputBuffer [j] == null)
						continue;
					if (inputBuffer [i].packet.gather && inputBuffer [j].packet.gather &&
						(inputBuffer [i].flitNr == inputBuffer[j].flitNr) &&
					    (inputBuffer [i].packet.requesterID == inputBuffer [j].packet.requesterID) 
						// This way also allow MC traffic being merged. But need to set dst
					) {
						if (inputBuffer[i].packet.request != null && inputBuffer [j].packet.request != null) 
							if (inputBuffer [i].packet.request.mshr != inputBuffer [j].packet.request.mshr)
								continue;

						if (priority_inv == 0) {
							inputBuffer [i].ackCount++;
							inputBuffer [j] = null;
						} else if (priority_inv == 1) { // reverse the priority for selecting the flit being dropped.
							inputBuffer [j].ackCount++;
							inputBuffer [i] = null;
						}
					}
				}
			}

		}

		protected void Clear () {

			for (int i = 0; i < 4; i++) 
				inputBuffer [i] = null;
			
			numFlitIn = 0;
		}

		protected void BufferWrite (){

			// grab inputs into a local array so we can sort
			for (int dir = 0; dir < 4; dir++)
				if (linkIn[dir] != null && linkIn[dir].Out != null)
				{
					#if DEBUG
					Console.WriteLine ("#3 BufferWrite: Time {0}: @ node {1} Inport {2} {3} ", Simulator.CurrentRound,coord.ID, Simulator.network.portMap(dir),linkIn[dir].Out.ToString() );
					#endif
					numFlitIn++;
					inputBuffer[dir] = linkIn[dir].Out;  // c: # of incoming flits
					inputBuffer[dir].roadMap.Add(ID);
					inputBuffer[dir].inDir = dir;  // May use for MCmask table look up 
					inputBuffer[dir].ClearRoutingInfo();
					linkIn[dir].Out = null;
				}
		}

	

		protected void RouteCompute(Flit f) {
			
			PreferredDirection pd;
			if (f == null)
				return;

			if (!f.packet.mc) {
				// this is an uni-cast flit
				pd = determineDirection (f, coord);
				// U-turn is not allowed
				if (pd.yDir == Simulator.DIR_UP && f.inDir != Simulator.DIR_UP)
					f.preferredDirVector [Simulator.DIR_UP] = true;
				if (pd.xDir == Simulator.DIR_RIGHT && f.inDir != Simulator.DIR_RIGHT)
					f.preferredDirVector [Simulator.DIR_RIGHT] = true;
				if (pd.yDir == Simulator.DIR_DOWN && f.inDir != Simulator.DIR_DOWN)
					f.preferredDirVector [Simulator.DIR_DOWN] = true;
				if (pd.xDir == Simulator.DIR_LEFT && f.inDir != Simulator.DIR_LEFT)
					f.preferredDirVector [Simulator.DIR_LEFT] = true;
				if (f.dest.ID == ID)
					f.preferredDirVector [Simulator.DIR_LOCAL] = true;
				
			} else {
				f.preferredDirVector = determineDirection (f, coord, mcMask);
			}
		}

		// TODO: TBD; Do not perform full-fledged merging to reduce complexity.
		protected void MergeFlitTagging () {
			// benefit of merging: reduce the flits in the network, increase throughput, increase the chance of replication

			for (int dir = 0; dir < 4; dir++) {
				if (inputBuffer [dir] == null)
					continue;




			}
		}

		protected void ReplicaFlitTagging () {
			
			int numMC;

			for (int dir = 0; dir < 4; dir++) {
				if (inputBuffer [dir] == null)
					continue;

				// sweep through prefferred direction vector to check if it need replication at this node
				numMC = 0;
				for (int j = 0; j < 4; j++)  // DO check local bit
					if (inputBuffer [dir].preferredDirVector [j] && inputBuffer[dir].packet.mc)
						numMC++;

				if (numMC > 1 | (inputBuffer [dir].preferredDirVector[4] && inputBuffer[dir].packet.mc && inputBuffer[dir].destList.Count > 1)) {
					inputBuffer [dir].replicateNeed = true;
				}


			}
		}

		public static ulong age(Flit f)
		{

			if (Config.net_age_arbitration)
				return Simulator.CurrentRound - f.packet.injectionTime;
			else
				return (Simulator.CurrentRound - f.packet.creationTime) /
					(ulong)Config.cheap_of;
		}

		public override int rank(Flit f1, Flit f2)
		{
			if (f1 == null && f2 == null)
				return 0;

			if (f1 == null) return 1;
			if (f2 == null) return -1;

			int c0 = 0;

			//promoting MC packet is not a good idea!
			//Reason to demote MC packet:
			// 1) MC packet is likely to have multiple productive directions.
			if (f1.replicateNeed && !f2.replicateNeed)
				c0 = 1;
			else if (!f1.replicateNeed && f2.replicateNeed)
				c0 = -1;
			
			//if (f1.packet.mc && !f2.packet.mc)
			//	c0 = 1;
			//else if (!f1.packet.mc && f2.packet.mc)
			//	c0 = -1;
			
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
				(c2 != 0) ? c2 :
				c3;
		}

		override protected Flit ejectLocal()
		{
			// eject locally-destined flit (highest-ranked, if multiple)
			Flit ret = null;

			int bestDir = -1;
			for (int dir = 0; dir < 4; dir++)
				if (inputBuffer[dir] != null && inputBuffer[dir].preferredDirVector[4] == true
					&&	(ret == null || rank(inputBuffer[dir], ret) < 0))
				{
					ret = inputBuffer[dir];
					bestDir = dir;
				}
					
			if (bestDir != -1) {
				if (!inputBuffer [bestDir].replicateNeed) {
					inputBuffer [bestDir] = null;
					numFlitIn--;
				} else {
					inputBuffer [bestDir].destList.Remove (coord);
					Simulator.stats.inject_flit.Add();  // increase the flit count, as a flit is replicated
					//if (inputBuffer [bestDir].packet.creationTime == inputBuffer [bestDir].packet.reqCreationTime) {
					//	inputBuffer [bestDir].packet.creationTime = Simulator.CurrentRound;
					//}

				}
			}
					

			return ret;
		}

		protected void ejection () {
			
			// Just to profile ejection trials
			int flitsTryToEject = 0;
			for (int dir = 0; dir < 4; dir ++)
				if (inputBuffer[dir] != null && inputBuffer[dir].preferredDirVector[4] == true)
				{
					flitsTryToEject ++;
					if (inputBuffer[dir].ejectTrial == 0)
						inputBuffer[dir].firstEjectTrial = Simulator.CurrentRound;
					inputBuffer[dir].ejectTrial ++;
				}
			Simulator.stats.flitsTryToEject[flitsTryToEject].Add();            

			// Actual ejection tree
			Flit f1 = null,f2 = null; // f2 is enabled for dual ejection
			for (int i = 0; i < Config.meshEjectTrial; i++)
			{
				// Only support dual ejection (MAX.Config.meshEjectTrial = 2)
				Flit eject = ejectLocal();
				if (i == 0) f1 = eject; 
				else if (i == 1) f2 = eject;
				if (eject != null) {
					acceptFlit (eject); 	// Eject flit	
				#if DEBUG
				Console.WriteLine ("#6 Time {0}: Eject @ node {1} {2}", Simulator.CurrentRound,coord.ID, eject.ToString());
				#endif
				}
			}
			// Profile dual ejection effect
			if (f1 != null && f2 != null && f1.packet == f2.packet)
				Simulator.stats.ejectsFromSamePacket.Add(1);
			else if (f1 != null && f2 != null)
				Simulator.stats.ejectsFromSamePacket.Add(0);
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

		protected void SAPlusST () {

			int outDir; // use to make sure all flits find at least an outport
			int defDir; // deflected direction 
			int maxReplica = neighbors - numFlitIn;
			int dstID = -1;
			Coord dstCoord;
			int[] routeOrder = new int[4] {1,3,0,2};
			int dir;


			for (int i = 0; i < 4; i++) {

				if (inputBuffer[i] == null)
					continue;

				outDir = -1;

				if (!inputBuffer [i].replicateNeed | maxReplica == 0 | inputBuffer[i].packet.mc == false) {

					// UC: productive port allocation

					for (int j = 0; j < 4; j++) {
						dir = routeOrder [j];
						if (inputBuffer [i].preferredDirVector [dir] == true && linkOut [dir] != null && linkOut [dir].In == null) {
							#if DEBUG
							Console.WriteLine("#2 SAPlusST: Time {0}: Output to ProdP {1} @ node {2} {3}", Simulator.CurrentRound, Simulator.network.portMap(dir), ID, inputBuffer [i].ToString());
							#endif
							linkOut [dir].In = inputBuffer [i];
							outDir = dir;
							break; // TODO: break here may cause bias toward N, E, S, W in decending order
						} else
							continue; // Next port
					}


					/*
					for (int dir = 0; dir < 4; dir++) {

						if (inputBuffer [i].preferredDirVector [dir] == true && linkOut [dir] != null && linkOut [dir].In == null) {
							#if DEBUG
							Console.WriteLine("#2 SAPlusST: Time {0}: Output to ProdP {1} @ node {2} {3}", Simulator.CurrentRound, Simulator.network.portMap(dir), ID, inputBuffer [i].ToString());
							#endif
							linkOut [dir].In = inputBuffer [i];
							//linkOut [dir].In.ClearRoutingInfo ();
							outDir = dir;
							break; // TODO: break here may cause bias toward N, E, S, W in decending order
						} else
							continue; // Next port
					}
					*/
				} 

				else { // flits need replication
					// A flit need replication has two possibilities:
					// 1) it is routed and replicated to productive directions, possibly with no replica. 
					//    Note: replication is granted only if productive port is available
					// 2) The master copy is deflected, which means no productive port is available. 
					//    Thus no replica is generated

					// MC: productive port allocation
					//     stop replication if maxReplica == 0 to prevent deadlock
					for (dir = 0; dir < 4; dir++) {
						
						if (inputBuffer [i].preferredDirVector [dir] == true && linkOut [dir] != null && linkOut [dir].In == null && maxReplica > 0) {
							#if DEBUG
							Console.WriteLine("#2 SAPlusST: Time {0}: Output to ProdP {1} @ node {2} {3}", Simulator.CurrentRound, Simulator.network.portMap(dir), ID, inputBuffer [i].ToString());
							#endif

							if (outDir == -1) { // this is considerred to be the master copy
								linkOut [dir].In = inputBuffer [i];
							} else {
								// new flit needs to be generated when replication happens
								Flit f = new Flit(inputBuffer[i].packet,inputBuffer[i].flitNr);
								//if (f.packet.creationTime == inputBuffer [i].packet.reqCreationTime) {
								//	f.packet.creationTime = Simulator.CurrentRound;
								//}

								Simulator.stats.inject_flit.Add();  // increase the flit count, as a flit is generated
								f.destList = new List <Coord> ();
								f.injectionTime = Simulator.CurrentRound;
	
								// update the destination list of the master and replicated flit
								// TODO: think hard on how to implement in hardware

								foreach (Coord c in inputBuffer[i].destList.ToList()) {
									dstCoord = c;
									dstID = c.ID;
									if (mcMask [dir, dstID] == 1) {
										f.destList.Add (dstCoord);
										f.packet.creationTimeMC [dstID] = Simulator.CurrentRound;
										inputBuffer [i].destList.Remove (c);

									}
								}

								
								/*
								int destCount = inputBuffer [i].destList.Count;
								for (int j = 0; j < destCount; j++) {
									dstCoord = inputBuffer [i].destList;
									dstID = inputBuffer [i].destList [j].ID;
									if (mcMask [dir, dstID] == 1) {
										f.destList.Add (dstCoord);
										inputBuffer [i].destList.Remove (dstCoord);
									}
								}
								*/ 
								linkOut [dir].In = f;
								maxReplica--;
							}
							outDir = dir;
						}
					}

				}

				if (outDir != -1)
					continue;

				// unprodutive port allocation
				inputBuffer [i].Deflected = true;
				defDir = 0;
				if (Config.randomize_defl)
					defDir = Simulator.rand.Next (4); // randomize deflection dir (so no bias)
				for (int count = 0; count < 4; count++, defDir = (defDir + 1) % 4) {
					if (linkOut [defDir] != null && linkOut [defDir].In == null) {
						#if DEBUG
						Console.WriteLine("#2 SAPlusST: Time {0}: Output to UnpdP {1} @ node {2} {3}", Simulator.CurrentRound, Simulator.network.portMap(defDir), ID, inputBuffer [i].ToString());
						#endif
						linkOut [defDir].In = inputBuffer [i];
						//linkOut [defDir].In.ClearRoutingInfo ();
						outDir = defDir;
						break;
					}
				}

				if (outDir == -1)
					throw new Exception (
						String.Format ("Ran out of outlinks in arbitration at node {0} ", coord.ID));

			}


		}

		protected override void _doStep(){

			//bool stop;

			//if (Simulator.CurrentRound == 258 && ID == 7)
			//	stop = true;		

			Clear ();

			//PrintFlitIn ();

			BufferWrite ();

			// Merge 
			Merge ();

			for (int i = 0; i < 4; i++) 
				RouteCompute (inputBuffer [i]); // each flit contains a preferred 5 bits diection vector.

			ReplicaFlitTagging (); // MUST BE HERE!

		    _fullSort(ref inputBuffer); // defined in RouterFlit.cs

			// eject
			ejection ();

			// inject
			// TODO: can add merge support
			InjectToRouter (); // RC of local injected flit is computed inside 

			// arbitration and switch traversal
			// Flit replication and merging take place here
			SAPlusST ();

			//PrintFlitOut ();
		} // called from Network


		void PrintFlitIn() {
			
			Flit f;

			bool printTime = false; // do not print if no flit in router

			for (int dir = 0; dir < 4; dir++)
				if (linkIn [dir] != null && linkIn [dir].Out != null) {
					if (printTime == false) {
						Console.WriteLine ("\nTime {0} @ Router {1}", Simulator.CurrentRound, ID);
						printTime = true;
					}

					f = linkIn [dir].Out;
					if (f.packet.mc){
						Console.Write ("Packet {0}.{1} In {2}, Dst: ", f.packet.ID, f.flitNr, Simulator.network.portMap(dir));
						foreach (Coord c in f.destList)
							Console.Write ("{0} ", c.ID);
						Console.Write ("\n");
					}
					else
						Console.WriteLine ("Packet {0}.{1} In {2}, Dst: {3}", f.packet.ID, f.flitNr, Simulator.network.portMap(dir), f.dest.ID);
					
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

