using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ICSimulator
{

	public class MultiMesh : Network
	{
		private MultiMeshRouter[] _routers ;      
		private int ID, ID_neighbor; 

		public MultiMesh (int dimX, int dimY) : base(dimX, dimY) {	}

		public enum topology {SINGLE_LOCAL_BYPASS, SINGLE_EXPRESS, X_BYPASS_Y_EXPRESS};

		public override void setup()
		{
			if (Config.sub_net <= 0) 
				throw new Exception ("No subnetwork is configured (sub_net <= 0)!");
			_routers = new MultiMeshRouter [Config.N];
           
            nodes = new Node[Config.N];
			links = new List<Link>();
			cache = new CmpCache();

			endOfTraceBarrier = new bool[Config.N];

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

					ID = Coord.getIDfromXY(x, y);
					ID_neighbor = Coord.getIDfromXY(x_, y_);

					buildMeshNetwork (dir);
				}

				// Add additional links
				addMoreLinks (n);
				
			}

			// Verify the number of links
			int num_link = (Config.network_nrX * Config.network_nrY * (4 + Config.num_bypass)  - 2 * Config.network_nrX - 2 * Config.network_nrY ) * Config.sub_net;
			if (links.Count != num_link)
				throw new Exception ("Error: Network configuration fails!");
		}

		public void buildMeshNetwork (int _dir) {

			int _oppDir = (_dir + 2) % 4; // direction from neighbor's perspective

			for (int m = 0; m < Config.sub_net; m++)
			{
				// Link param is *extra* latency (over the standard 1 cycle)
				Link dirA = new Link(Config.router.linkLatency - 1);
				Link dirB = new Link(Config.router.linkLatency - 1);
				links.Add(dirA);
				links.Add(dirB);

				_routers [ID].subrouter [m].linkOut [_dir] = dirA;
				_routers [ID_neighbor].subrouter [m].linkIn [_oppDir] = dirA;
				_routers [ID].subrouter [m].linkIn [_dir] = dirB;
				_routers [ID_neighbor].subrouter [m].linkOut [_oppDir] = dirB;
				_routers [ID].subrouter [m].neighbors++;
				_routers [ID_neighbor].subrouter [m].neighbors++;
				_routers [ID].subrouter [m].neigh [_dir] = _routers [ID_neighbor].subrouter [m];
				_routers [ID_neighbor].subrouter [m].neigh [_oppDir] = _routers [ID].subrouter [m];

			}
		}

		public void addMoreLinks (int _node) {
	
				if (Config.bypass_enable && Config.bridge_subnet)
					for (int pi = 0; pi < Config.num_bypass; pi++)
						for (int m = 0; m < Config.sub_net; m++)
						{
							Link bypass = new Link(0); // 1 cycle latency
							links.Add(bypass);
							_routers[_node].subrouter[m].bypassLinkOut[pi] = bypass;
							_routers[_node].subrouter[(m+1)%Config.sub_net].bypassLinkIn[pi] = bypass;  // forming a local ring using bypass link
							_routers [_node].subrouter[m].neighbors++;
						}

			// The bypass link connect to the intermediate neighboring router in the X-dimension
			else if (Config.bypass_enable == true && Config.bridge_subnet == false)
				for (int i = 0; i < Config.sub_net; i++)
					for (int k = 0; k < Config.network_nrY; k++)
						for (int j = 0; j < Config.network_nrX; j++)
					{
						Link bypass = new Link(0);
						links.Add(bypass);
						_routers[k * Config.network_nrX+j%Config.network_nrX].subrouter[i].bypassLinkOut[0] = bypass;
						_routers[k * Config.network_nrX+(j+1)%Config.network_nrX].subrouter[i].bypassLinkIn[0] = bypass;
						_routers[k * Config.network_nrX+j%Config.network_nrX].subrouter [i].neighbors++;
					}

		}

		public override void doStep()
		{

			doStats(); // only record the link utilization. Do not need to override.

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
