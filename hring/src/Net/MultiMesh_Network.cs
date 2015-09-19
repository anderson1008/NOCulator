using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ICSimulator
{

	public class MultiMesh : Network
	{
		private MultiMeshRouter[] _routers;

		public MultiMesh (int dimX, int dimY) : base(dimX, dimY) {	}

		public override void setup()
		{
			if (Config.sub_net <= 0) 
				throw new Exception ("No subnetwork is configured (sub_net <= 0)!");

			_routers = new MultiMeshRouter [Config.N];
			nodes = new Node[Config.N];
			links = new List<Link>();
			cache = new CmpCache();

			endOfTraceBarrier = new bool[Config.N];
			canRewind = false;

			ParseFinish(Config.finish);

			workload = new Workload(Config.traceFilenames);

			mapping = new NodeMapping_AllCPU_SharedCache();

			// create routers and nodes
			// each node contains router [sub_net * node, ... ,  sub_net*(node+1)-1]
			for (int n = 0; n < Config.N; n++)
			{
				Coord c = new Coord(n);
				nodes[n] = new Node(mapping, c);
				_routers [n] = new MultiMeshRouter (c);
				nodes [n].setRouter (_routers [n]);
				_routers [n].setNode (nodes [n]);
				endOfTraceBarrier[n] = false;
			}


			// create the Golden manager
			// NOT sure if needed
			golden = new Golden();

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
						continue;

					// ensure no duplication by handling a link at the lexicographically
					// first router
					if (x_ < x || (x_ == x && y_ < y)) continue;

					int ID, ID_neighbor; 
					ID = Coord.getIDfromXY(x, y);
					ID_neighbor = Coord.getIDfromXY(x_, y_);

					for (int m = 0; m < Config.sub_net; m++)
					{
						// Link param is *extra* latency (over the standard 1 cycle)
						Link dirA = new Link(Config.router.linkLatency - 1);
						Link dirB = new Link(Config.router.linkLatency - 1);
						links.Add(dirA);
						links.Add(dirB);

						_routers [ID].subrouter [m].linkOut [dir] = dirA;
						_routers [ID_neighbor].subrouter [m].linkIn [oppDir] = dirA;
						_routers [ID].subrouter [m].linkIn [dir] = dirB;
						_routers [ID_neighbor].subrouter [m].linkOut [oppDir] = dirB;
						_routers [ID].subrouter [m].neighbors++;
						_routers [ID_neighbor].subrouter [m].neighbors++;
						_routers [ID].subrouter [m].neigh [dir] = _routers [ID_neighbor].subrouter [m];
						_routers [ID_neighbor].subrouter [m].neigh [oppDir] = _routers [ID].subrouter [m];

					}
				}

				// connect the subrouters
				if (Config.bypass_enable)
					for (int pi = 0; pi < Config.num_bypass; pi++)
						for (int m = 0; m < Config.sub_net; m++)
						{
							Link bypass = new Link(0);
							links.Add(bypass);
							_routers[n].subrouter[m].bypassLinkOut[pi] = bypass;
							_routers[n].subrouter[(m+1)%Config.sub_net].bypassLinkIn[pi] = bypass;
							_routers [n].subrouter [m].neighbors++;
							_routers [n].subrouter [m].neigh [pi] = _routers [n].subrouter [(m+1)%Config.sub_net];
						}
					
				
			}
			// TORUS: be careful about the number of neighbors, which is based on if bypass is enabled.
			if (Config.torus)
				for (int i = 0; i < Config.N; i++)
					for (int j = 0; j < Config.sub_net; j++)
						if (_routers[i].subrouter [j].neighbors < 4)
							throw new Exception("torus construction not successful!");
		}

		public override void doStep()
		{

			doStats(); // only record the link utilization. Do not need to override.

			// step the golden controller
			// golden.doStep();

			// step the nodes

			for (int n = 0; n < Config.N; n++)
				nodes[n].doStep();
			// step the network sim: first, routers
				
			for (int n = 0; n < Config.N; n++)	
				_routers[n].doStep();
	
			// now, step each link
			foreach (Link l in links)
				l.doStep();
		}

		public override void close()
		{
			if (Config.RingClustered == false)
				for (int n = 0; n < Config.N; n++)
					_routers[n].close();
		}

	}



}