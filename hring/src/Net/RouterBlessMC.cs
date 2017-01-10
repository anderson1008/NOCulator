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
	// This mode is currently disabled as it cannot provide delivery guarantee for mc flits.

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

			// If the hs flit can be merged with flit in the channel, skip the injection.
			// Check merge

			for (int j = 0; j < 4; j++) {
				if (Config.mergeEnable == false)
					break;
				if (inputBuffer [j] == null || m_injectSlot == null)
					continue;
				if (inputBuffer [j].packet.gather && m_injectSlot.packet.gather && 
					inputBuffer[j].packet.dest.ID == m_injectSlot.packet.dest.ID &&
					inputBuffer[j].flitNr == m_injectSlot.flitNr &&
					inputBuffer[j].packet.hsFlowID == m_injectSlot.packet.hsFlowID
				) {
					inputBuffer [j].ackCount=inputBuffer[j].ackCount + 1;
					ScoreBoard.UnregPacket (m_injectSlot.packet.dest.ID, m_injectSlot.packet.ID); // merged flit is removed from the score board right away
					Simulator.stats.merge_flit.Add ();
					#if DEBUG
					Console.WriteLine ("#4 InjectToRouter: Time {0}: Inject @ Router {1} {2}", Simulator.CurrentRound, ID, m_injectSlot.ToString());
					#endif
					statsInjectFlit (m_injectSlot);
					m_injectSlot = null;
					return;
				}
			}
				
			bool canInject = (numFlitIn + outCount) < neighbors;
			bool starved = wantToInject && !canInject;

			if (starved)
			{
				Flit starvedFlit = null;
				if (starvedFlit == null) starvedFlit = m_injectSlot;

				Simulator.controller.reportStarve(coord.ID);
				statsStarve(starvedFlit);
				starveCount++;
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
					if (Config.adaptiveMC) {
						if (starveCount < Config.starveThreshold && numMC > 1)
							m_injectSlot.replicateNeed = true;
					} else {
						if (numMC > 1)
							m_injectSlot.replicateNeed = true;
					}

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

		// TODO: Change hsFlowID to address (42-bit)			
		protected void Merge () {
			int priority_inv = 0;
			// merge as much as possible. May compare the flit only on certain channels to reduce the number of comparator.
			for (int i = 0; i < 4; i++) {
				if (inputBuffer [i] == null)
					continue;
			
				// Only pooling from the higher channels
				// This is enough to cover all possible merging combination in the router.
				for (int j = i+1; j < 4; j++) {
					if (inputBuffer [j] == null)
						continue;
					if (inputBuffer [i].packet.gather && inputBuffer [j].packet.gather &&
						(inputBuffer [i].flitNr == inputBuffer[j].flitNr) &&
						(inputBuffer [i].dest.ID == inputBuffer[j].dest.ID) &&
						(inputBuffer [i].packet.hsFlowID == inputBuffer[j].packet.hsFlowID) //hsFlowID is used for synthetic traffic
					) {

						if (priority_inv == 0) {
							inputBuffer [i].ackCount=inputBuffer[i].ackCount + inputBuffer[j].ackCount;
							ScoreBoard.UnregPacket (inputBuffer[j].packet.dest.ID, inputBuffer[j].packet.ID);
							inputBuffer [j] = null;
						} else if (priority_inv == 1) { // reverse the priority for selecting the flit being dropped.
							inputBuffer [j].ackCount=inputBuffer[i].ackCount + inputBuffer[j].ackCount;
							ScoreBoard.UnregPacket (inputBuffer[i].packet.dest.ID, inputBuffer[i].packet.ID); 
							inputBuffer [i] = null;
						}
						numFlitIn--; // This will increase the probability of flit replication, as it is used to prevent deadlock during replication
						Simulator.stats.merge_flit.Add ();;
					}
				}
			}

		}

		protected void Clear () {

			for (int i = 0; i < 4; i++) 
				inputBuffer [i] = null;
			
			numFlitIn = 0;

			resetStarveCounter ();
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
					inputBuffer[dir].inDir = dir;  // May use for MCmask table look up 
					inputBuffer[dir].ClearRoutingInfo();
					linkIn[dir].Out = null;
				}
		}
			
		protected void RouteCompute(Flit f) {
			
			PreferredDirection pd;
			if (f == null)
				return;
			bool stop;

			if (Simulator.CurrentRound == 8 && ID == 9)
				stop = true;	

			if (!f.packet.mc) {
				// this is an uni-cast flit

				pd = determineDirection (f, coord);

				// U-turn is not allowed
				// U-turn MUST be disabled
				// Because it may cause livelock
				// e.g. All flits router to neighbors and routed back, then repeat.
				//      As a result, no one moves forward
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

				if (Config.adaptiveMC && starveCount >= Config.starveThreshold && !inputBuffer [dir].preferredDirVector[4])
					continue;
				
				if (numMC > 1 | (inputBuffer [dir].preferredDirVector[4] && inputBuffer[dir].packet.mc && inputBuffer[dir].destList.Count > 1)) {
						inputBuffer [dir].replicateNeed = true;
				}


			}
		}

		// TODO: can reduce the complexity by using CHIPPER 
		public static ulong age(Flit f)
		{
			return Simulator.CurrentRound - f.packet.injectionTime;
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
			// However, doing this will cause livelock for those MC flits.
			//if (f1.replicateNeed && !f2.replicateNeed)
			//	c0 = 1;
			//else if (!f1.replicateNeed && f2.replicateNeed)
			//	c0 = -1;
			
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
				if (!inputBuffer [bestDir].packet.mc || 
					inputBuffer [bestDir].packet.mc && !inputBuffer [bestDir].replicateNeed) {
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
			bool stop = false;
			if (f.packet.ID == 861888 && m_n.coord.ID == 0)
				stop = true;
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


		protected void parallelST () {
	
			bool stop;

			if (Simulator.CurrentRound == 198 && ID == 10)
				stop = true;	

			// ST1:
			int [,] apv;
 			bool [] availPV;
			int i,j,k;
			bool isFirst;
			int maxReplica;
			bool lowerChannelHasMC = false; // once lower channel has MC, higher MC flit is treated as UC flit.
			apv = new int[4,4];
			availPV = new bool[4];
			for (i=0; i<4; i++) {
				availPV[i] = false;
				for (j=0; j<4; j++)
					apv[i,j]=0;
			}
			int[] routeOrder = new int[4] {1,3,0,2};
			int dir;

			// allocate the non-contented port

			for (k=0; k<4; k++) { // output direction
				//dir = routeOrder [k];
				for (i=0; i<4; i++) // targeted channel 
				{
					if (inputBuffer [i] == null)
						continue;
					if (inputBuffer [i].preferredDirVector [k] == false || linkOut [k] == null)
						continue;
					//assume no competion
					apv[i,k] = 1;
					//Check other channels to see if there is contention
					for (j=0; j<4; j++) {
						if (inputBuffer [j] == null || i==j)
							continue;
						apv[i,k] = ((apv[i,k]==1) && !inputBuffer[j].preferredDirVector[k]) ? 1 : 0;
					}
				}
			}

			maxReplica = neighbors - numFlitIn;

			for (i=0; i<4; i++) {
				if (inputBuffer [i] == null)
					continue;
				isFirst = true;
				if (inputBuffer [i].packet.mc == false || lowerChannelHasMC) {
					// uc only keep one ppv
					for (j=0; j<4; j++) {
						dir = routeOrder [j];
						if (apv[i,dir]==1 && isFirst && linkOut [dir] != null) // output link must exists
							isFirst = false;
						else // not first allocated port or the port is not requested
							apv[i,dir] = 0; 
					}
				} //uc
				else if (inputBuffer[i].packet.mc == true) {
				//check deadlock for mc flit
					lowerChannelHasMC = true;
					for (j=0; j<4; j++) {
						dir = routeOrder [j];
						if (apv [i, dir] == 1 && isFirst)
							isFirst = false;
						else if (apv[i,dir]==1 && isFirst == false && maxReplica > 0 && linkOut [dir] != null)
							maxReplica--;
						else
							apv[i,dir] = 0;
					}	
				}
			}		

			// ST2: Clear ppv of mc flit if it has claimed at least one port in ST1

			for (i=0; i<4; i++) {
				if (inputBuffer [i] == null)
					continue;
				isFirst = true;
				for (j=0; j<4; j++) {
					dir = routeOrder [j];

					if (apv[i,dir]==1) {
						for (k=0; k<4; k++)
							inputBuffer[i].preferredDirVector[k]=false;
					}

					if (inputBuffer [i].preferredDirVector [dir] && isFirst)
						isFirst = false;
					else
						inputBuffer [i].preferredDirVector [dir] = false;
				}
			}				

			// ST3: Parallel LUT
			//int numDefBeforeCh2, numDefBeforeCh3; // number of deflection in lower channel, compute before allocation in this stage
			//numDefBeforeCh2 = inputBuffer[1]==null ? 0 : inputBuffer[1].preferredDirVector & 
			int numDef = 0;
			int posOne = 0;

			if (inputBuffer [0] != null) {
				if (apv [0, 0] + apv [0, 1] + apv [0, 2] + apv [0, 3] == 0)
					for (i = 0; i < 4; i++) {
						dir = routeOrder [i];
						apv [0, dir] = (inputBuffer [0].preferredDirVector [dir] & linkOut [dir] != null) ? 1 : 0;
						if (apv [0, dir] == 1)
							break;
					}
				// it is possible that flit 0 does not have any productive port.
				// e.g., its productive port is U-turn, however, U-turn is not allowed. 
				// So simply pass through the current node.
				// deflect flit 0
				if (apv [0, 0] + apv [0, 1] + apv [0, 2] + apv [0, 3] == 0) {
					for (j = 0; j < 4; j++) {
						availPV [j] = !(apv [0, j] == 1 | apv [1, j] == 1 | apv [2, j] == 1 | apv [3, j] == 1);
						if (availPV [j] && linkOut[j] != null) {
							apv [0, j] = 1;
							break;
						}
					}
				}
			}
			
			for (i=1; i<4; i++) { // channel
				for (j=0; j<4; j++)
					// in hardware, it is computed in each allcation unit
					availPV[j] = !(apv[0,j]==1 | apv[1,j]==1 | apv[2,j]==1 | apv[3,j]==1);
							
				if (inputBuffer [i] == null)
					continue;
				
				if (numDef == 0)
					for (j = 0; j < 4; j++) {
						dir = routeOrder [j];
						apv [i, dir] = (availPV [dir] & inputBuffer [i].preferredDirVector [dir] | apv [i, dir] == 1 & linkOut [dir] != null) ? 1 : 0;
						if (apv [i, dir] == 1)
							break;
					}

				// In hardware, we will take the numDef-th available port.
				// This implicitly reserve the first (numDef-1)-th ports to lower channels.
				if (apv[i,0]+apv[i,1]+apv[i,2]+apv[i,3] == 0)
					for (j=0; j<4; j++) {
						if (availPV[j] && linkOut [j] != null) {
							apv[i,j] = 1;
							numDef++; // this can be computed at each stage, however, tracked here directly to avoid redudant coding.
							break;
						}
					}
			}
				
			// Populate on output
			int outDir;
			int numOutFlit = 0;
			int dstID = -1;
			int numReplica = 0;
			Coord dstCoord;

			for (i = 0; i < 4; i++) {
				if (inputBuffer [i] == null)
					continue;
				outDir = -1;

				for (j = 0; j < 4; j++) {
					if (apv [i, j] != 1)
						continue;
					if (linkOut [j] == null)
						throw new Exception ("Allocated direction does not have a neighbor");
					if (linkOut [j].In != null)
						throw new Exception (String.Format("Port {0} is assigned to multiple flits", j));
					// Check if need to update the destination list
					if (outDir == -1)
						linkOut [j].In = inputBuffer [i];
					else {
						if (inputBuffer [i].packet.mc!=true)
							throw new Exception ("CANNOT be a uc flit");
						// new flit needs to be generated when replication happens
						Flit f = new Flit(inputBuffer[i].packet,inputBuffer[i].flitNr);

						Simulator.stats.inject_flit.Add();  // increase the flit count, as a flit is generated
						f.destList = new List <Coord> ();
						f.injectionTime = Simulator.CurrentRound;

						// update the destination list of the replicated flit
						foreach (Coord c in inputBuffer[i].destList.ToList()) {
							dstCoord = c;
							dstID = c.ID;
							if (mcMask [j, dstID] == 1) {
								f.destList.Add (dstCoord);
								f.packet.creationTimeMC [dstID] = Simulator.CurrentRound;
								inputBuffer [i].destList.Remove (c);

							}
						}
						numReplica++;
						linkOut [j].In = f;
					}
						
					outDir = j;
					numOutFlit++;

					// check if every flit has taken some ports
					if (outDir == -1)
						throw new Exception (
							String.Format ("Flit fails to find a outPort at node {0} ", coord.ID));
				}
			}
			// check if the number of output flits is more than the number of ports
			if (numOutFlit > 4)
				throw new Exception (
					String.Format ("Ran out of outlinks in arbitration at node {0} ", coord.ID));

			// check if all the incoming flits and their replica have been routed
			if (numReplica + numFlitIn != numOutFlit)
				throw new Exception ("numReplica + numFlitIn != numOutFlit");
		}

		protected void SAPlusST () {

			int outDir; // use to make sure all flits find at least an outport
			int defDir; // deflected direction 
			int maxReplica = neighbors - numFlitIn; // TODO: check neighbors == 5?
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
							break; 
						} else
							continue; // Next port
					}

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
	
								// update the destination list of the replicated flit
								foreach (Coord c in inputBuffer[i].destList.ToList()) {
									dstCoord = c;
									dstID = c.ID;
									if (mcMask [dir, dstID] == 1) {
										f.destList.Add (dstCoord);
										f.packet.creationTimeMC [dstID] = Simulator.CurrentRound;
										inputBuffer [i].destList.Remove (c);

									}
								}
									
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




		protected void resetStarveCounter () {
			if (Simulator.CurrentRound % Config.starveResetEpoch == 0) 
				starveCount = 0;
		}

		protected override void _doStep(){

			bool stop;

			if (Simulator.CurrentRound == 6 && ID == 3)
				stop = true;		

			Clear ();

			//PrintFlitIn ();

			BufferWrite ();

			// Merge 
			if (Config.mergeEnable)
				Merge ();
			

			for (int i = 0; i < 4; i++) 
				RouteCompute (inputBuffer [i]); // each flit contains a preferred 5 bits diection vector.

			ReplicaFlitTagging (); // MUST BE HERE!

			if (Config.sortMode == 0)
	    		_fullSort(ref inputBuffer); // defined in RouterFlit.cs
			else if (Config.sortMode == 1)
	    		_partialSort(ref inputBuffer); // defined in RouterFlit.cs

			// eject
			ejection ();

			// inject
			InjectToRouter (); // RC of local injected flit is computed inside 

			// arbitration and switch traversal
			// Flit replication and merging take place here
			if (Config.swAllocMode == 0)
				SAPlusST ();
			else if (Config.swAllocMode == 1)
				parallelST ();
			
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

