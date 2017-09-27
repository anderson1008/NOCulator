#define DEBUG
#define PKTDUMP 

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace ICSimulator
{

	/* Parameters
	 * meshEjectTrial: number of ejection 
	 * 
	 * 
	 */
	public class MultiMeshRouter : Router
	{
		public Router[] subrouter = new Router[Config.sub_net];

		protected override void _doStep ()
		{
			for (int i = 0; i < Config.sub_net; i++) 
				subrouter [i].doStep ();

		}

		// only determine if can or cannot inject
		public override bool canInjectFlit (Flit f)
		{
			bool can = false;
			for (int i = 0; i < Config.sub_net; i++) 
			{
				can = can | subrouter [i].canInjectFlit (f);
			}
			return can;
		}


		public override bool canInjectFlitMultNet (int subnet, Flit f)
		{
			return subrouter [subnet].canInjectFlit (f);
		}

		public override void InjectFlitMultNet (int subnet, Flit f)
		{	
			subrouter [subnet].InjectFlit (f);
			Simulator.stats.subnet_util[subnet].Add();
			
		}

		// if more than one subrouter can inject, select one randomly
		// or inject to the available one.
		// Otherwise throttle the injection.
		public override void InjectFlit (Flit f)
		{

			Router [] availSubrouter = new Router [Config.sub_net];
			int count = 0;

			for (int i = 0; i < Config.sub_net; i++) 
			{
				if (subrouter [i].canInjectFlit (f)) 
				{
					availSubrouter [count] = subrouter [i];
					count++;
				}
			}

			if (count <= 0)
				throw new Exception ("no subrouter is available for injection.");
			else 
			{
				int selected;
				select_subnet (count, out selected);

				Simulator.stats.subnet_util[selected].Add();
				availSubrouter [selected].InjectFlit (f);

				#if PKTDUMP
				//	Console.WriteLine("PKT {0}-{1}/{2} aclaim the injection slot at subnet {3} router {4} at time {5}", 
				//                  f.packet.ID, f.flitNr+1, f.packet.nrOfFlits, selected, coord, Simulator.CurrentRound);
				#endif
			}

		}

		protected void select_subnet (int count, out int selected)
		{
			selected = -1;
			double util = double.MaxValue;
			if (Config.subnet_sel_rand)
				selected = Simulator.rand.Next(count);
			else
				for (int i = 0; i < count; i++)
			{
				if (Simulator.stats.subnet_util[i].Count < util)
				{
					selected = i;
					util = Simulator.stats.subnet_util[i].Count;
				}
			}

			if (selected == -1 || selected >= count) throw new Exception("no subnet is selected");

		}

		public MultiMeshRouter (Coord c):base(c)
		{
			for (int i=0; i<Config.sub_net; i++) 
			{
				subrouter [i] = makeSubRouters (c);
				subrouter [i].subnet = i;
			}
		}

		Router makeSubRouters (Coord c)
		{
			switch (Config.router.algorithm)
			{
				case RouterAlgorithm.BLESS_BYPASS:
				return new Router_BLESS_BYPASS (c);

				case RouterAlgorithm.DR_AFC:
				return new Router_AFC(c);

				case RouterAlgorithm.DR_FLIT_SWITCHED_CTLR:
				return new Router_Flit_Ctlr(c); // BLESS random proritization

				case RouterAlgorithm.DR_FLIT_SWITCHED_OLDEST_FIRST:
				return new Router_Flit_OldestFirst(c); // BLESS OF proritization

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
	}

	public class Router_BLESS_BYPASS : Router
	{

		protected Flit m_injectSlot, m_injectSlot2;
		protected Queue<Flit>	[] ejectBuffer;
		protected Flit[] input; // keep this as a member var so we don't
		// have to allocate on every step

		public Router_BLESS_BYPASS(Coord myCoord)
			: base(myCoord)
		{
			m_injectSlot = null;
			m_injectSlot2 = null;
			ejectBuffer = new Queue<Flit>[4+Config.num_bypass];
			input = new Flit[4+Config.num_bypass]; 
			// grab inputs into a local array so we can sort
			for (int n = 0; n < 4 + Config.num_bypass; n++) {
				ejectBuffer [n] = new Queue<Flit> ();
				input[n] = null;
			}
		}

		Flit handleGolden(Flit f)
		{
			if (f == null)
				return f;

			if (f.state == Flit.State.Normal)
				return f;

			if (f.state == Flit.State.Rescuer)
			{
				if (m_injectSlot == null)
				{
					m_injectSlot = f;
					f.state = Flit.State.Placeholder;
				}
				else
					m_injectSlot.state = Flit.State.Carrier;

				return null;
			}

			if (f.state == Flit.State.Carrier)
			{
				f.state = Flit.State.Normal;
				Flit newPlaceholder = new Flit(null, 0);
				newPlaceholder.state = Flit.State.Placeholder;

				if (m_injectSlot != null)
					m_injectSlot2 = newPlaceholder;
				else
					m_injectSlot = newPlaceholder;

				return f;
			}

			if (f.state == Flit.State.Placeholder)
				throw new Exception("Placeholder should never be ejected!");

			return null;
		}

		// accept one ejected flit into rxbuf
		protected void acceptFlit(Flit f)
		{
			statsEjectFlit(f);
			if (f.packet.nrOfArrivedFlits + 1 == f.packet.nrOfFlits)
				statsEjectPacket(f.packet);

			m_n.receiveFlit(f);
		}

		protected virtual Flit ejectLocal()
		{
			// eject locally-destined flit (highest-ranked, if multiple)
			// bypass channel can also eject to local port
			Flit ret = null;			

			int bestDir = -1;
			for (int dir = 0; dir < 4; dir++)
				if (linkIn[dir] != null && linkIn[dir].Out != null &&
				    linkIn[dir].Out.state != Flit.State.Placeholder &&
				    linkIn[dir].Out.dest.ID == ID &&
				    (ret == null || rank(linkIn[dir].Out, ret) < 0))
			{
#if PKTDUMP
				if (ret != null)
					Console.WriteLine("PKT {0}-{1}/{2} EJECT FAIL router {3}/{4} at time {5}", 
				                  ret.packet.ID, ret.flitNr+1, ret.packet.nrOfFlits, coord, subnet, Simulator.CurrentRound);
#endif
				ret = linkIn[dir].Out;
				bestDir = dir;
			}

			for (int bypass = 0; bypass < Config.num_bypass; bypass++)
				if (bypassLinkIn[bypass] != null && bypassLinkIn[bypass].Out != null &&
				    bypassLinkIn[bypass].Out.state != Flit.State.Placeholder &&
				    bypassLinkIn[bypass].Out.packet.dest.ID == ID && 
				    (ret == null || rank(bypassLinkIn[bypass].Out, ret) < 0))
			{
#if PKTDUMP
				if (ret != null)
					Console.WriteLine("PKT {0}-{1}/{2} EJECT FAIL router {3}/{4} at time {5}",
					                  ret.packet.ID, ret.flitNr+1, ret.packet.nrOfFlits, coord, subnet, Simulator.CurrentRound);
#endif
				ret = bypassLinkIn[bypass].Out;
				bestDir = 4+bypass;
			}

			if (bestDir >= 4) bypassLinkIn[bestDir - 4].Out = null;
			else if (bestDir != -1) linkIn[bestDir].Out = null;


#if PKTDUMP
			if (ret != null)
				Console.WriteLine("PKT {0}-{1}/{2} EJECT from port {3} router {4}/{5} at time {6}", 
					                  ret.packet.ID, ret.flitNr+1, ret.packet.nrOfFlits, bestDir, coord, subnet, Simulator.CurrentRound);
#endif

			//ret = handleGolden(ret);

			return ret;
		}

		protected void _sort ()
		{
			if (Config.partial_sort)
				_partialSort(ref input);
			else
				_fullSort(ref input);
		}

		protected void _bubbleSort(ref Flit[] input)
		{
			// inline bubble sort is faster for this size than Array.Sort()
			// sort input[] by descending priority. rank(a,b) < 0 if f0 has higher priority.
			for (int i = 0; i < 4+Config.num_bypass; i++)
				for (int j = i + 1; j < 4+Config.num_bypass; j++)
					if (input[j] != null &&
						(input[i] == null ||
							rank(input[j], input[i]) < 0))
					{
						Flit t = input[i];
						input[i] = input[j];
						input[j] = t;
					}
		}

		protected void _swap (ref Flit t0, ref Flit t1)
		{
			if (t0 != null || t1 != null)
				Simulator.stats.permute.Add();
			if (rank(t1, t0)<0)
			{
				Flit t = t0;
				t0 = t1;
				t1 = t;
			}
		}

		virtual protected void _partialSort(ref Flit[] input)
		{
			// only sort the first 4 flits; 
			_swap (ref input[0],ref input[1]);
			_swap (ref input[2],ref input[3]);
			_swap (ref input[0],ref input[2]);
			_swap (ref input[1],ref input[3]);
		}

		protected void _fullSort(ref Flit[] input)
		{
			// only sort the first 4 flits; 
			_swap (ref input[0],ref input[1]);
			_swap (ref input[2],ref input[3]);
			_swap (ref input[0],ref input[2]);
			_swap (ref input[1],ref input[3]);
			_swap (ref input[0],ref input[2]);
			_swap (ref input[1],ref input[3]);
		}
			
		protected virtual void _installFlit(ref Flit[] input, out int count)
		{
			bool stop = false;
			for (int i = 0; i < 4+Config.num_bypass; i++) input[i] = null;
			// grab inputs into a local array so we can sort
			count = 0;

			if (Config.partial_sort)
			{
				for (int dir = 0; dir < 4; dir++)
					if (linkIn[dir] != null && linkIn[dir].Out != null)
				{
					Console.WriteLine("PKT {0}-{1}/{2} ARRIVED at router {3} at time {4}",
							linkIn[dir].Out.packet.ID, linkIn[dir].Out.flitNr+1, linkIn[dir].Out.packet.nrOfFlits, coord, Simulator.CurrentRound);
					if (linkIn [dir].Out.packet.ID == 20394 && coord.x == 1 && coord.y == 0 && Simulator.CurrentRound == 22789)
						stop = true;
					linkIn[dir].Out.inDir = dir;
					input[dir] = linkIn[dir].Out;  // c: # of incoming flits
					count++;
					linkIn[dir].Out = null;
				}
			}
			else{
				for (int dir = 0; dir < 4; dir++)
					if (linkIn[dir] != null && linkIn[dir].Out != null)
				{
					Console.WriteLine("PKT {0}-{1}/{2} ARRIVED at router {3} at time {4}",
						linkIn[dir].Out.packet.ID, linkIn[dir].Out.flitNr+1, linkIn[dir].Out.packet.nrOfFlits, coord, Simulator.CurrentRound);
					linkIn[dir].Out.inDir = dir;
					input[count++] = linkIn[dir].Out;  // c: # of incoming flits
					//linkIn[dir].Out.inDir = dir;  // By Xiyue: what's the point? Seems redundant
					linkIn[dir].Out = null;
				}
			}

			// regardless how we sort flits, bypass flit is always at the tail of input array
			// this may affect how we will inject flit.
			for (int bypass = 0; bypass < Config.num_bypass; bypass++)
				if (bypassLinkIn[bypass] != null && bypassLinkIn[bypass].Out != null)
			{
				bypassLinkIn[bypass].Out.inDir = 4+bypass;
				input[4+bypass] = bypassLinkIn[bypass].Out;
				count++;
				bypassLinkIn[bypass].Out = null;
			}
		}


		protected void _inject (ref int c)
		{
			// sometimes network-meddling such as flit-injection can put unexpected
			// things in outlinks...
			// outCount: # of the unexpected outstanding flits at the inport of output link, usually, it is 0
			int outCount = 0;
			for (int dir = 0; dir < 4; dir++)
				if (linkOut[dir] != null && linkOut[dir].In != null)
					outCount++;
			for (int j = 0; j < Config.num_bypass; j++)
				if (bypassLinkOut[j] != null && bypassLinkOut[j].In != null)
					outCount++;
			if (outCount != 0) throw new Exception("Unexpected flit on output!");

			bool wantToInject = m_injectSlot2 != null || m_injectSlot != null;
			//bool canInject = (c + outCount) < (neighbors - 1); // conservative inject: # of input < # of port - 1 -> prevent making network more congested.
			bool canInject = (c + outCount) < neighbors;  // aggressive inject: as long as # of input < # of port
			bool starved = wantToInject && !canInject;
			bool stop;

			if (starved)
			{
				Flit starvedFlit = null;
				if (starvedFlit == null) starvedFlit = m_injectSlot2;
				if (starvedFlit == null) starvedFlit = m_injectSlot;

				#if PKTDUMP
				Console.WriteLine("PKT {0}-{1}/{2} is STARVED at router {3}/{4} at time {5}",
				                  starvedFlit.packet.ID, starvedFlit.flitNr+1, starvedFlit.packet.nrOfFlits, coord, subnet, Simulator.CurrentRound);
				#endif
				Simulator.controller.reportStarve(coord.ID);
				statsStarve(starvedFlit);
			}
			if (canInject && wantToInject)
			{				
				Flit inj_peek=null; 
				if(m_injectSlot2!=null)
					inj_peek=m_injectSlot2;
				else if (m_injectSlot!=null)
					inj_peek=m_injectSlot;
				if(inj_peek==null)
					throw new Exception("Inj flit peek is null!!");

				if(!Simulator.controller.ThrottleAtRouter || Simulator.controller.tryInject(coord.ID))
				{
					Flit inj = null;
					if (m_injectSlot2 != null)
					{
						inj = m_injectSlot2;
						m_injectSlot2 = null;
					}
					else if (m_injectSlot != null)
					{
						inj = m_injectSlot;
						m_injectSlot = null;
					}
					else
						throw new Exception("what???inject null flits??");

					inj.inDir = 4+Config.num_bypass;
					if (Config.partial_sort)
					{
						for (int i = 0; i < 4+Config.num_bypass; i++) 
							if (input[i] == null && linkIn[i] != null)
						{
							if (inj.packet.ID == 927 && coord.x == 0 && coord.y == 1 && Simulator.CurrentRound == 1032)
								stop = true;
							input[i] = inj;
							c++;
							break;
						}
					}
					else
						input[c++] = inj;
					
					#if PKTDUMP
					Console.WriteLine("PKT {0}-{1}/{2} is INJECTED at router {3}/{4} at time {5}", 
					                  inj.packet.ID, inj.flitNr+1, inj.packet.nrOfFlits, coord, subnet, Simulator.CurrentRound);
					#endif
					statsInjectFlit(inj);
				}
			}
		}

	

		protected override void _doStep()
		{

			// STEP 1: Ejection
			if (Config.EjectBufferSize != -1)
			{								
				for (int dir =0; dir < 4; dir ++)
					if (linkIn[dir] != null && linkIn[dir].Out != null && linkIn[dir].Out.packet.dest.ID == ID && ejectBuffer[dir].Count < Config.EjectBufferSize)
					{
						ejectBuffer[dir].Enqueue(linkIn[dir].Out);
						linkIn[dir].Out = null;
					}
				for (int bypass = 0; bypass < Config.num_bypass; bypass++)
					if (bypassLinkIn[bypass] != null && bypassLinkIn[bypass].Out != null && bypassLinkIn[bypass].Out.packet.dest.ID == ID && ejectBuffer[4+bypass].Count < Config.EjectBufferSize)
					{
						ejectBuffer[4+bypass].Enqueue(bypassLinkIn[bypass].Out);
						bypassLinkIn[bypass].Out = null;
					}

				int bestdir = -1;			
				for (int dir = 0; dir < 4+Config.num_bypass; dir ++)
					if (ejectBuffer[dir].Count > 0 && (bestdir == -1 || ejectBuffer[dir].Peek().injectionTime < ejectBuffer[bestdir].Peek().injectionTime))
						bestdir = dir;				
				if (bestdir != -1)
					acceptFlit(ejectBuffer[bestdir].Dequeue());
			}
			else 
			{
				int flitsTryToEject = 0;
				for (int dir = 0; dir < 4; dir ++)
					if (linkIn[dir] != null && linkIn[dir].Out != null && linkIn[dir].Out.dest.ID == ID)
				{
					flitsTryToEject ++;
					if (linkIn[dir].Out.ejectTrial == 0)
						linkIn[dir].Out.firstEjectTrial = Simulator.CurrentRound;
					linkIn[dir].Out.ejectTrial ++;
#if PKTDUMP
					Console.WriteLine("PKT {0}-{1}/{2} TRY to EJECT from port {3} router {4}/{5} at time {6}", 
					                  linkIn[dir].Out.packet.ID, linkIn[dir].Out.flitNr+1, linkIn[dir].Out.packet.nrOfFlits, dir,
					                  coord, subnet, Simulator.CurrentRound);
#endif
				}
				for (int bypass = 0; bypass < Config.num_bypass; bypass++)
					if (bypassLinkIn[bypass] != null && bypassLinkIn[bypass].Out != null && bypassLinkIn[bypass].Out.packet.dest.ID == ID)
				{
					flitsTryToEject ++;
					if (bypassLinkIn[bypass].Out.ejectTrial == 0)
						bypassLinkIn[bypass].Out.firstEjectTrial = Simulator.CurrentRound;
					bypassLinkIn[bypass].Out.ejectTrial++;
#if PKTDUMP
					Console.WriteLine("PKT {0}-{1}/{2} TRY to EJECT from Bypass port {3} router {4}/{5} at time {6}", 
					                  bypassLinkIn[bypass].Out.packet.ID, bypassLinkIn[bypass].Out.flitNr+1, bypassLinkIn[bypass].Out.packet.nrOfFlits, bypass,
					                  coord, subnet, Simulator.CurrentRound);
#endif
				}

				Simulator.stats.flitsTryToEject[flitsTryToEject].Add();            

				Flit f1 = null,f2 = null;
				for (int i = 0; i < Config.meshEjectTrial; i++)
				{
					// Only support dual ejection (MAX.Config.meshEjectTrial = 2)
					Flit eject = ejectLocal();
					if (i == 0) f1 = eject; 
					else if (i == 1) f2 = eject;
					if (eject != null)             
						acceptFlit(eject); 	// Eject flit			
				}
				if (f1 != null && f2 != null && f1.packet == f2.packet)
					Simulator.stats.ejectsFromSamePacket.Add(1);
				else if (f1 != null && f2 != null)
					Simulator.stats.ejectsFromSamePacket.Add(0);
			}


			//STEP 2 : Prioritize and Injection
			// grab inputs into a local array so we can sort
			//for (int i = 0; i < 4+Config.num_bypass; i++) input[i] = null;
			int c;
			_installFlit(ref input, out c);
			_sort();
			_inject (ref c);

			// assign outputs
			for (int i = 0; i < 4+Config.num_bypass; i++)
			{
				if (input[i]==null)
					continue;

				PreferredDirection pd = determineDirection(input[i], coord);

				#if PKTDUMP
				if (pd.xDir != Simulator.DIR_NONE)
					Console.WriteLine("PKT {0}-{1}/{2} PDir={7} ARRIVES at port {3} router {4}/{5} at time {6}",
				                  input[i].packet.ID, input[i].flitNr+1, input[i].packet.nrOfFlits, input[i].inDir, 
				                  coord, subnet, Simulator.CurrentRound, pd.xDir);
				if (pd.yDir != Simulator.DIR_NONE)
					Console.WriteLine("PKT {0}-{1}/{2} PDir={7} ARRIVES at port {3} router {4}/{5} at time {6}",
					                  input[i].packet.ID, input[i].flitNr+1, input[i].packet.nrOfFlits, input[i].inDir, 
					                  coord, subnet, Simulator.CurrentRound, pd.yDir);
				#endif

				int outDir = -1;
				bool deflect = false;
				bool bypass = false;

				if (input[i].routingOrder == false)
				{
					if (pd.xDir != Simulator.DIR_NONE && linkOut[pd.xDir].In == null)
					{
						linkOut[pd.xDir].In = input[i];
						linkOut[pd.xDir].In.routingOrder = false;
						outDir = pd.xDir;
					}
					else if (pd.yDir != Simulator.DIR_NONE && linkOut[pd.yDir].In == null)
					{
						linkOut[pd.yDir].In = input[i];
						linkOut[pd.yDir].In.routingOrder = true;
						outDir = pd.yDir;
					}
					else 
						bypass = true;
				}
				else       //y over x
				{
					if (pd.yDir != Simulator.DIR_NONE && linkOut[pd.yDir].In == null)
					{
						linkOut[pd.yDir].In = input[i];
						linkOut[pd.yDir].In.routingOrder = true;
						outDir = pd.yDir;
					}
					else if (pd.xDir != Simulator.DIR_NONE && linkOut[pd.xDir].In == null)
					{
						linkOut[pd.xDir].In = input[i];
						linkOut[pd.xDir].In.routingOrder = false;
						outDir = pd.xDir;
					}
					else 
						bypass = true;
				}

				if (bypass)
				{
					for (int j = 0; j < Config.num_bypass; j ++)
					{
						if (bypassLinkOut[j] != null && bypassLinkOut[j].In == null && Config.bypass_enable)
						{
							input[i].Bypassed = true;
							bypassLinkOut[j].In = input[i];
							outDir = 4+j;
							break;
						}
						else
							deflect = true;
					}

				}

				// deflect!
				if (deflect)
				{
					input[i].Deflected = true;
					int dir = 0;
					if (Config.randomize_defl) dir = Simulator.rand.Next(4); // randomize deflection dir (so no bias)
					for (int count = 0; count < 4; count++, dir = (dir + 1) % 4)
						if (linkOut[dir] != null && linkOut[dir].In == null  && outDir == -1) // use outDir to determine if a flit takes bypass port
					{
						linkOut[dir].In = input[i];
						outDir = dir;
						// once a flit is deflected, it must try to route along the same dimension.
						if (dir == 0 || dir ==2)
							linkOut[dir].In.routingOrder = false;
						else
							linkOut[dir].In.routingOrder = true;
						break;
					}

				}
				
				#if PKTDUMP
				if (bypass != true && deflect != true)
					Console.WriteLine("PKT {0}-{1}/{2} TAKE PROD Port {3} router {4}/{5} at time {6}",
					                  input[i].packet.ID, input[i].flitNr+1, input[i].packet.nrOfFlits, outDir, coord, subnet, Simulator.CurrentRound);
				else if (bypass == true && deflect != true)
					Console.WriteLine("PKT {0}-{1}/{2} TAKE BYPASS Port {3} router {4}/{5} at time {6}",
					                  input[i].packet.ID, input[i].flitNr+1, input[i].packet.nrOfFlits, outDir-4, coord, subnet, Simulator.CurrentRound);
				else if (deflect == true)
					Console.WriteLine("PKT {0}-{1}/{2} DEFLECTED to Port {3} router {4}/{5} at time {6}",
					                  input[i].packet.ID, input[i].flitNr+1, input[i].packet.nrOfFlits, outDir, coord, subnet, Simulator.CurrentRound);
				#endif
				if (outDir == -1) throw new Exception(
					String.Format("Ran out of outlinks in arbitration at node {0} on input {1} cycle {2} flit {3} c {4} neighbors {5}", coord, i, Simulator.CurrentRound, input[i], c, neighbors));
			} // end assign output
		}

		public override bool canInjectFlit(Flit f)
		{
			return m_injectSlot == null;
		}

		public override void InjectFlit(Flit f)
		{
			if (m_injectSlot != null)
				throw new Exception("Trying to inject twice in one cycle");

			m_injectSlot = f;
		}

		public override void flush()
		{
			m_injectSlot = null;
		}

		protected virtual bool needFlush(Flit f) { return false; }



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
				(c2 != 0) ? c2 :
				c3;
		}

		public ulong age(Flit f)
		{
			if (Config.net_age_arbitration)
				return Simulator.CurrentRound - f.packet.injectionTime;
			else
				return (Simulator.CurrentRound - f.packet.creationTime) /
					(ulong)Config.cheap_of;
		}



		public override void statsEjectPacket(Packet p) {
			ScoreBoard.UnregPacket (ID, p.ID); 
			if (Config.uniform_size_enable == false) {
				if (p.nrOfFlits == (Config.sub_net * Config.router.addrPacketSize))
					Simulator.stats.ctrl_pkt.Add ();
				else if (p.nrOfFlits == (Config.sub_net * Config.router.dataPacketSize))
					Simulator.stats.data_pkt.Add ();	
				else
					throw new Exception ("packet size is undefined, yet received!");
			}

			ulong net_latency;
			ulong total_latency;
			net_latency = Simulator.CurrentRound - p.injectionTime;
			total_latency = Simulator.CurrentRound - p.creationTime;

			Simulator.stats.net_latency.Add(net_latency);
			Simulator.stats.total_latency.Add(total_latency);
			Simulator.stats.net_latency_bysrc[p.src.ID].Add(net_latency);
			Simulator.stats.net_latency_bydest[p.dest.ID].Add(net_latency);
			Simulator.stats.total_latency_bysrc[p.src.ID].Add(total_latency);
			Simulator.stats.total_latency_bydest[p.dest.ID].Add(total_latency);

		}

	}


	public class Router_MinBD: Router_BLESS_BYPASS
	{
		
		// have to allocate on every step
		ResubBuffer rBuf;

		public Router_MinBD(Coord myCoord)
			: base(myCoord)
		{
			m_injectSlot = null;
			m_injectSlot2 = null;
			ejectBuffer = new Queue<Flit>[4+Config.num_bypass];
			for (int n = 0; n < 4+Config.num_bypass; n++)
				ejectBuffer[n] = new Queue<Flit>();
			rBuf = new ResubBuffer();
		}

		protected override void _doStep()
		{


// ----- Put flits into an array/buffers (Buffer write) ----- //
			int count_bw;
			bool redirected = false; // either redirect or eject to side buffer before populating on output port

			_installFlit(ref input, out count_bw);
			if (count_bw < 4)
				goto Reinject;

// ----- Redirection to side buufer ----- //
			// if there are 4 incoming flits, and the flit at head of side buffer stays more than threshold duration,
			// randomly pick one incoming flit and redirect to side buffer
			// This will make the channel available for reinjecting the flit from the side buffer
			if (rBuf.isEmpty())
				goto Reinject;
			Flit headOfRebuf;
			headOfRebuf = rBuf.getNextFlit();
			int timeInRebuf = (int)Simulator.CurrentRound - headOfRebuf.rebufInTime;
			if (timeInRebuf - Config.timeInRebufThreshold >= 0) {
				redirected = inputResubmitEjection( ref input );
				if (redirected)
					count_bw--;
			}


			// -----  Eject -------//

/*          if (Config.EjectBufferSize != -1)
			{								
				for (int dir =0; dir < 4; dir ++)
					if (linkIn[dir] != null && linkIn[dir].Out != null && linkIn[dir].Out.packet.dest.ID == ID && ejectBuffer[dir].Count < Config.EjectBufferSize)
					{
						ejectBuffer[dir].Enqueue(linkIn[dir].Out);
						linkIn[dir].Out = null;
					}
				int bestdir = -1;			
				for (int dir = 0; dir < 4; dir ++)
					if (ejectBuffer[dir].Count > 0 && (bestdir == -1 || ejectBuffer[dir].Peek().injectionTime < ejectBuffer[bestdir].Peek().injectionTime))
						bestdir = dir;				
				if (bestdir != -1)
					acceptFlit(ejectBuffer[bestdir].Dequeue());
			}
			else 
			{
				int flitsTryToEject = 0;
				for (int dir = 0; dir < 4; dir ++)
					if (linkIn[dir] != null && linkIn[dir].Out != null && linkIn[dir].Out.dest.ID == ID)
					{
						flitsTryToEject ++;
						if (linkIn[dir].Out.ejectTrial == 0)
							linkIn[dir].Out.firstEjectTrial = Simulator.CurrentRound;
						linkIn[dir].Out.ejectTrial ++;
					}
				Simulator.stats.flitsTryToEject[flitsTryToEject].Add();            

				Flit f1 = null,f2 = null;
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
				if (f1 != null && f2 != null && f1.packet == f2.packet)
					Simulator.stats.ejectsFromSamePacket.Add(1);
				else if (f1 != null && f2 != null)
					Simulator.stats.ejectsFromSamePacket.Add(0);
			}
*/


// ----- Reinject flit from side buffer ----  //
			Reinject:
			int count_reinjected = 0;
			if (Config.resubmitBuffer)
			{
				if (!rBuf.isEmpty())
					for (int i = 0; i < 4; i++)
						if (count_reinjected < Config.rebufInjectCount)
						{
							if (input[i] == null && !rBuf.isEmpty() && linkIn[i] != null)
							{
								input[i] = rBuf.removeFlit();
								input[i].nrInRebuf++;
								count_reinjected++;
								count_bw++;
							}
						}
			}
						
			if (Config.EjectBufferSize != -1)
			{								
				for (int dir =0; dir < 4; dir ++)
					if (linkIn[dir] != null && linkIn[dir].Out != null && linkIn[dir].Out.packet.dest.ID == ID && ejectBuffer[dir].Count < Config.EjectBufferSize)
					{
						ejectBuffer[dir].Enqueue(linkIn[dir].Out);
						linkIn[dir].Out = null;
					}
				int bestdir = -1;			
				for (int dir = 0; dir < 4; dir ++)
					if (ejectBuffer[dir].Count > 0 && (bestdir == -1 || ejectBuffer[dir].Peek().injectionTime < ejectBuffer[bestdir].Peek().injectionTime))
						bestdir = dir;				
				if (bestdir != -1)
					acceptFlit(ejectBuffer[bestdir].Dequeue());
			}
			else 
			{
				int flitsTryToEject = 0;
				for (int dir = 0; dir < 4; dir ++)
					if (input[dir] != null && input[dir].dest.ID == ID)
					{
						flitsTryToEject ++;
						if (input[dir].ejectTrial == 0)
							input[dir].firstEjectTrial = Simulator.CurrentRound;
						input[dir].ejectTrial ++;
					}
				Simulator.stats.flitsTryToEject[flitsTryToEject].Add();            

				Flit f1 = null,f2 = null;
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
				if (f1 != null && f2 != null && f1.packet == f2.packet)
					Simulator.stats.ejectsFromSamePacket.Add(1);
				else if (f1 != null && f2 != null)
					Simulator.stats.ejectsFromSamePacket.Add(0);
			}

// ----- Inject new local flit ---- //
			_inject (ref count_bw);

			for (int i = 0; i < 4; i++)
				if (input[i] != null)
				{
					PreferredDirection pd = determineDirection(input[i]);
					if (pd.xDir != Simulator.DIR_NONE)
						input[i].prefDir = pd.xDir;
					else
						input[i].prefDir = pd.yDir;
				}

// ----- Determine a silver flit --- //
			assignSilverFlit ( ref input );

// ----- Permutation ---- //
			partialPermuattion ( ref input );

// ----- Bypass deflected flits to side buffers or put on output link //
			int startDir = Simulator.rand.Next(4); 
			int outDir;
			bool rebuffered = false || redirected;
			for (int dir = 0; dir < 4; dir++) {
				outDir = (dir + startDir) % 4;
				if (input [outDir] != null && linkOut [outDir] == null)
					throw new Exception ("Attempting to put flit on a non-existed link");
				if (input [outDir] == null || linkOut [outDir] == null)
					continue;
				if (linkOut [outDir].In != null)
					throw new Exception ("Output link has an unresolved flit");
				if (input [outDir].prefDir != outDir && rebuffered == false && !rBuf.isFull()) {
					input [outDir].nrInRebuf++;
					input [outDir].wasInRebuf = true;
					rebuffered = true;
					rBuf.addFlit (input [outDir]);
					Console.WriteLine("PKT {0}-{1}/{2} Resub Buf of router {3} at time {4}",
						input[outDir].packet.ID, input[outDir].flitNr+1, input[outDir].packet.nrOfFlits, coord, Simulator.CurrentRound);
				} else {
					linkOut [outDir].In = input [outDir];
					Console.WriteLine("PKT {0}-{1}/{2} TAKE Port {3} router {4} at time {5}",
						input[outDir].packet.ID, input[outDir].flitNr+1, input[outDir].packet.nrOfFlits, outDir, coord, Simulator.CurrentRound);
				}

			}
		}


		void partialPermuattion ( ref Flit [] input ) {

			bool swap_0, swap_1;

			// Stage 1 permutation network
			arbiter (input, 1, out swap_0, out swap_1);
			swap (swap_0, ref input [0], ref input [1]);
			swap (swap_1, ref input [2], ref input [3]);

			// Stage 2 permutation network
			arbiter (input, 2, out swap_0, out swap_1);
			swap (swap_0, ref input [0], ref input [2]);
			swap (swap_1, ref input [1], ref input [3]);

		}

		void swap(bool swap_en, ref Flit t0, ref Flit t1) {
			if (t0 != null || t1 != null)
				Simulator.stats.permute.Add ();
			else
				return;
			if (swap_en) {
				Flit t = t0;
				t0 = t1;
				t1 = t;
			}
		}

		void arbiter (Flit [] channel, int index, out bool swap_0, out bool swap_1)
		{
			// Structure of permutation blocks
			// blk: 1,1 ---- 2,1
			//          \  / 
			//           \/ 
			//           /\
			//          /  \
			//         /    \
			//      1,2 ---- 2,2

			swap_0 = false;
			swap_1 = false;
		
			switch (index)
			{
			case 1:
				// Stage 1: permuter block connected to input[0] and input [1]
				if (channel [0] == null && channel [1] == null)
					swap_0 = false;
				else if (channel [0] != null && channel [1] != null) {
					if ((rank (channel [0], channel [1]) < 0 && (channel [0].prefDir == Simulator.DIR_RIGHT || channel [0].prefDir == Simulator.DIR_LEFT)) ||
					    (rank (channel [0], channel [1]) > 0 && ((channel [1].prefDir == Simulator.DIR_UP || channel [1].prefDir == Simulator.DIR_DOWN)))) {
						swap_0 = true;
					}
				} else { // the block only has one input flits 

					// The flit in blk_1_1 must be forwarded to the blk_2_2, if
					//    1) one of the output link does not exist in blk_2_1, for example, the nodes in the corner and edge have some of links missing for mesh network 
					//    2) there are two incoming flits in blk_1_2, which implies that one of flit will take the only link available connect to blk_2_1
					// Implication: oblivious of port preference
					if (channel [2] != null && channel [3] != null && (linkOut [Simulator.DIR_UP] == null || linkOut [Simulator.DIR_DOWN] == null)) {
						if (channel [0] != null)
							swap_0 = true;
						else if (channel [1] != null)
							swap_0 = false;
					}
					// The flit in blk_1_1 must be forwarded to the blk_2_1, if
					//    1) one of the output link does not exist in blk_2_2, for example, the nodes in the corner and edge have some of links missing for mesh network 
					//    2) there are two incoming flits in blk_1_2, which implies that one of flit will take the only link available connect to blk_2_2
					// Implication: oblivious of port preference
					else if (channel [2] != null && channel [3] != null && (linkOut [Simulator.DIR_LEFT] == null || linkOut [Simulator.DIR_RIGHT] == null)) {
						if (channel [0] != null)
							swap_0 = false;
						else if (channel [1] != null)
							swap_0 = true;
					}
					// The flit in blk_1_1 must be forwarded to blk_2_1, if 
					//    1) one of output link does not exist in blk_2_2
					//    2) knowing the flit in blk_1_2 will take the only link available in blk_2_2
					// Implication: oblivious of port preference
					else if (channel [2] != null && (linkOut [Simulator.DIR_LEFT] == null || linkOut [Simulator.DIR_RIGHT] == null)) {
						if (channel [2].prefDir == Simulator.DIR_LEFT || channel [2].prefDir == Simulator.DIR_RIGHT) {
							if (channel [0] != null)
								swap_0 = false;
							else if (channel [1] != null)
								swap_0 = true;
						} else {
							if ((rank (channel [0], channel [1]) < 0 && (channel [0].prefDir == Simulator.DIR_RIGHT || channel [0].prefDir == Simulator.DIR_LEFT)) ||
							    (rank (channel [0], channel [1]) > 0 && ((channel [1].prefDir == Simulator.DIR_UP || channel [1].prefDir == Simulator.DIR_DOWN)))) {
								swap_0 = true;
							}
						}
					} 
					// The flit in blk_1_1 must be forwarded to blk_2_1, if 
					//    1) one of output link does not exist in blk_2_2
					//    2) knowing the flit in blk_1_2 will take the only link available in blk_2_2
					// Implication: oblivious of port preference
					else if (channel [3] != null && (linkOut [Simulator.DIR_LEFT] == null || linkOut [Simulator.DIR_RIGHT] == null)) {
						if (channel [3].prefDir == Simulator.DIR_LEFT || channel [3].prefDir == Simulator.DIR_RIGHT) {
							if (channel [0] != null)
								swap_0 = false;
							else if (channel [1] != null)
								swap_0 = true;
						} else {
							if ((rank (channel [0], channel [1]) < 0 && (channel [0].prefDir == Simulator.DIR_RIGHT || channel [0].prefDir == Simulator.DIR_LEFT)) ||
							    (rank (channel [0], channel [1]) > 0 && ((channel [1].prefDir == Simulator.DIR_UP || channel [1].prefDir == Simulator.DIR_DOWN)))) {
								swap_0 = true;
							}
						}
					} else {
						if ((rank (channel [0], channel [1]) < 0 && (channel [0].prefDir == Simulator.DIR_RIGHT || channel [0].prefDir == Simulator.DIR_LEFT)) ||
							(rank (channel [0], channel [1]) > 0 && ((channel [1].prefDir == Simulator.DIR_UP || channel [1].prefDir == Simulator.DIR_DOWN)))) {
							swap_0 = true;
						}
					}
				}

				// Stage 1: permuter block connected to input[2] and input [3]
				if (channel [2] == null && channel [3] == null)
					swap_1 = false;
				else if (channel [2] != null && channel [3] != null) {
					if ((rank (channel[2], channel[3]) < 0 && (input[2].prefDir == Simulator.DIR_RIGHT || channel[2].prefDir == Simulator.DIR_LEFT)) ||
						(rank (channel[2], channel[3]) > 0 && ((input[3].prefDir == Simulator.DIR_UP || channel[3].prefDir == Simulator.DIR_DOWN)))) {
						swap_1 = true;
					}
				}
				else {
					// The flit in blk_1_2 must be forwarded to the blk_2_1, if
					//    1) one of the output link does not exist in blk_2_2, for example, the nodes in the corner and edge have some of links missing for mesh network 
					//    2) there are two incoming flits in blk_1_1, which implies that one of flit will take the only link available connect to blk_2_2
					// Implication: oblivious of port preference
					if (channel [0] != null && channel [1] != null && (linkOut [Simulator.DIR_LEFT] == null || linkOut [Simulator.DIR_RIGHT] == null)) {
						if (channel [2] != null)
							swap_1 = false;
						else if (channel [3] != null)
							swap_1 = true;
					} 
					// The flit in blk_1_2 must be forwarded to the blk_2_2, if
					//    1) one of the output link does not exist in blk_2_1, for example, the nodes in the corner and edge have some of links missing for mesh network 
					//    2) there are two incoming flits in blk_1_1, which implies that one of flit will take the only link available connect to blk_2_1
					// Implication: oblivious of port preference
					else if (channel [0] != null && channel [1] != null && (linkOut [Simulator.DIR_UP] == null || linkOut [Simulator.DIR_DOWN] == null)) {
						if (channel [2] != null)
							swap_1 = true;
						else if (channel [3] != null)
							swap_1 = false;
					}
					// The flit in blk_1_2 must be forwarded to blk_2_2, if 
					//    1) one of output link does not exist in blk_2_2
					//    2) knowing the flit in blk_1_1 will take the only link available in blk_2_1
					// Implication: oblivious of port preference
					else if (channel [0] != null && (linkOut [Simulator.DIR_UP] == null || linkOut [Simulator.DIR_DOWN] == null)) {
						if (channel [0].prefDir == Simulator.DIR_UP || channel [0].prefDir == Simulator.DIR_DOWN) {
							if (channel [2] != null)
								swap_1 = true;
							else if (channel [3] != null)
								swap_1 = false;
						} else {
							if ((rank (channel [2], channel [3]) < 0 && (input [2].prefDir == Simulator.DIR_RIGHT || channel [2].prefDir == Simulator.DIR_LEFT)) ||
							    (rank (channel [2], channel [3]) > 0 && ((input [3].prefDir == Simulator.DIR_UP || channel [3].prefDir == Simulator.DIR_DOWN)))) {
								swap_1 = true;
							}
						}
					} 
					// The flit in blk_1_2 must be forwarded to blk_2_2, if 
					//    1) one of output link does not exist in blk_2_2
					//    2) knowing the flit in blk_1_1 will take the only link available in blk_2_1
					// Implication: oblivious of port preference
					else if (channel [1] != null && (linkOut [Simulator.DIR_UP] == null || linkOut [Simulator.DIR_DOWN] == null)) {
						if (channel [1].prefDir == Simulator.DIR_UP || channel [1].prefDir == Simulator.DIR_DOWN) {
							if (channel [2] != null)
								swap_1 = true;
							else if (channel [3] != null)
								swap_1 = false;
						} else {
							if ((rank (channel [2], channel [3]) < 0 && (input [2].prefDir == Simulator.DIR_RIGHT || channel [2].prefDir == Simulator.DIR_LEFT)) ||
							    (rank (channel [2], channel [3]) > 0 && ((input [3].prefDir == Simulator.DIR_UP || channel [3].prefDir == Simulator.DIR_DOWN)))) {
								swap_1 = true;
							}
						}
					} else {
						if ((rank (channel [2], channel [3]) < 0 && (input [2].prefDir == Simulator.DIR_RIGHT || channel [2].prefDir == Simulator.DIR_LEFT)) ||
							(rank (channel [2], channel [3]) > 0 && ((input [3].prefDir == Simulator.DIR_UP || channel [3].prefDir == Simulator.DIR_DOWN)))) {
							swap_1 = true;
						}
					}
					
				}
				break;

			case 2:
				// Stage 2: permuter block connected to input[0] and input [1]
				// Note: connection between two stage is done here. 
				if (linkOut [Simulator.DIR_DOWN] == null) {
					if (channel [0] == null && channel [2] == null)
						swap_0 = false;
					else if (channel[0] != null)
						swap_0 = false;
					else if (channel[2] != null)
						swap_0 = true;
					else // (t0 != null && t1 != null)
						throw new Exception ("there are two flits, sth is wrong");

				} else if (linkOut [Simulator.DIR_UP] == null) {
					if (channel [0] == null && channel [2] == null)
						swap_0 = false;
					else if (channel[0] != null)
						swap_0 = true;
					else if (channel[2] != null)
						swap_0 = false;
					else // (t0 != null && t1 != null)
						throw new Exception ("there are two flits, sth is wrong");
				} else { // both output channels exist
					if ((rank (channel[0], channel[2]) < 0 && (channel[0].prefDir == Simulator.DIR_DOWN || channel[0].prefDir == Simulator.DIR_LEFT)) ||
						(rank (channel[0], channel[2]) > 0 && ((channel[2].prefDir == Simulator.DIR_UP || channel[2].prefDir == Simulator.DIR_RIGHT)))) {
						swap_0 = true;
					}
				}

				// Stage 2: permuter block connected to input[2] and input [3]
				if (linkOut [Simulator.DIR_RIGHT] == null) {
					if (channel [1] == null && channel [3] == null)
						swap_1 = false;
					else if (channel[1] != null)
						swap_1 = true;
					else if (channel[3] != null)
						swap_1 = false;
					else // (t0 != null && t1 != null)
						throw new Exception ("there are two flits, sth is wrong");

				} else if (linkOut [Simulator.DIR_LEFT] == null) {
					if (channel [1] == null && channel [3] == null)
						swap_1 = false;
					else if (channel[1] != null)
						swap_1 = false;
					else if (channel[3] != null)
						swap_1 = true;
					else // (t0 != null && t1 != null)
						throw new Exception ("there are two flits, sth is wrong");
				} else { // both output channels exist
					if ((rank (channel[1], channel[3]) < 0 && (channel[1].prefDir == Simulator.DIR_DOWN || channel[1].prefDir == Simulator.DIR_LEFT)) ||
						(rank (channel[1], channel[3]) > 0 && ((channel[3].prefDir == Simulator.DIR_UP || channel[3].prefDir == Simulator.DIR_RIGHT)))) {
						swap_1 = true;
					}
				}
				break;

			default:
				throw new Exception ("Arbiter is not implemented!");
			}

		}



		public override int rank(Flit f1, Flit f2)
		{
			return Router_Flit_GP._rank(f1, f2);
		}

		protected override Flit ejectLocal()
		{
			// eject locally-destined flit (highest-ranked, if multiple)
			// bypass channel can also eject to local port
			Flit ret = null;			

			int bestDir = -1;
			for (int dir = 0; dir < 4; dir++)
				if (input[dir] != null && 
					input[dir].state != Flit.State.Placeholder &&
					input[dir].dest.ID == ID &&
					(ret == null || rank(input[dir], ret) < 0))
				{
					#if PKTDUMP
					if (ret != null)
						Console.WriteLine("PKT {0}-{1}/{2} EJECT FAIL router {3}/{4} at time {5}", 
							ret.packet.ID, ret.flitNr+1, ret.packet.nrOfFlits, coord, subnet, Simulator.CurrentRound);
					#endif
					ret = input[dir];
					bestDir = dir;
				}


			if (bestDir != -1) input[bestDir] = null;


			#if PKTDUMP
			if (ret != null)
				Console.WriteLine("PKT {0}-{1}/{2} EJECT from port {3} router {4}/{5} at time {6}", 
					ret.packet.ID, ret.flitNr+1, ret.packet.nrOfFlits, bestDir, coord, subnet, Simulator.CurrentRound);
			#endif

			//ret = handleGolden(ret);

			return ret;
		}

		protected void assignSilverFlit (ref Flit[] newInput) {
			// If there is no golden flit, randomly pick one flit as sliver flit

			bool hasGolden = false; 
			int[] flitPositions = new int[4];
			int  flitCount = 0;
			for (int i = 0; i < 4; i++)
			{
				if (newInput[i] != null)
				{
					flitPositions[flitCount] = i;
					flitCount++;
					newInput[i].isSilver = false;
					if (Simulator.network.golden.isGolden(newInput[i]))
						hasGolden = true;
				}
			}
			if (flitCount != 0)
			{
				if (!hasGolden)
				{
					switch(Config.silverMode)
					{
					case "random": int randNum = flitPositions[Simulator.rand.Next(flitCount)];
						newInput[randNum].isSilver  = true;
						newInput[randNum].wasSilver = true;
						newInput[randNum].nrWasSilver++;
						break;
					}
				}
			}
		}

		protected bool inputResubmitEjection(ref Flit[] f)
		{
			int dir = Simulator.rand.Next(4);//startInput; 

			for (int i = 0; i < 4; i++)
			{
				int curdir = (dir + i) % 4;

				if (f[curdir] != null && i != f[curdir].prefDir && !rBuf.isFull())
				{
					bool storeInResubmitBuffer = true;
					/* If the flit is golden, don't allow it into the buffer */
					if (Simulator.network.golden.isGolden(f[i])) {
						storeInResubmitBuffer = false;
					}

					/* If this flit just came out of the rebuf, should it go back in? */
					if (f[i].wasInRebuf) {
						storeInResubmitBuffer = false; 
					}


					if(storeInResubmitBuffer)
					{
						rBuf.addFlit(f[curdir]);
						f[curdir].nrInRebuf++;
						f[curdir] = null;
						return true;
					}
				}
			}
			return false;
		} // inputResubmitEjection

	}

}