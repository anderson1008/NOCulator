//#define DEBUG

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace ICSimulator
{
    public struct Coord
    {
        public int x;
        public int y;
        public int ID;    // identifier of the node. 

        public Coord(int x, int y)
        {
            this.x = x;
            this.y = y;
            ID = getIDfromXY(x, y);
        }

        public Coord(int ID)
        {
            this.ID = ID;
            getXYfromID(ID, out x, out y);
        }

        public override bool Equals(object obj)
        {
            return (obj is Coord) && ((Coord)obj).x == x && ((Coord)obj).y == y;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode(); // x.GetHashCode() ^ y.GetHashCode();
        }

        public override string ToString()
        {
            return "(" + x + "," + y + ")";
        }

        public static int getIDfromXY(int x, int y)
        {
            return x * Config.network_nrY + y;
        }

        public static void getXYfromID(int id, out int x, out int y)
        {
            x = id / Config.network_nrY;
            y = id % Config.network_nrY;
        }
    }
    
    public struct RC_Coord
    {
    	public int x;
        public int y;      // x, y: coord of the ring
        public int z;      // z : coord within a ring
        public int ID;     // identifier of the node. 

        public RC_Coord(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            ID = getIDfromXYZ(x, y, z);
        }

        public RC_Coord(int ID)
        {
            this.ID = ID;
            getXYZfromID(ID, out x, out y, out z);
        }

        public override bool Equals(object obj)
        {
            return (obj is RC_Coord) && ((RC_Coord)obj).x == x && ((RC_Coord)obj).y == y && ((RC_Coord)obj).z == z;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode(); // x.GetHashCode() ^ y.GetHashCode();
        }

        public override string ToString()
        {
            return "(" + x + "," + y + "," + z + ")";
        }

        public static int getIDfromXYZ(int x, int y, int z)
        {
            return z + x * Config.network_nrY * 2 + 4 * y;
        }

        public static void getXYZfromID(int id, out int x, out int y, out int z)
        {
        	z = id % 4;
            x = id / (Config.network_nrY * 2);
            y = id % (Config.network_nrY * 2) / 4;
        }
    }

    public class Packet
    {
        public delegate void Sender(Packet p);

        private static ulong nrPackets = 0;

        public Coord src { get { return _src; } }
        private Coord _src;
        public Coord dest { get { return _dest; } }
        private Coord _dest;
		public List <Coord> destList;
		public bool mc, gather; // multicast, gather-able packet
		public int hsFlowID;
		public int [] nrOfArrivedFlitsMC;
		public ulong [] creationTimeMC;
		public int nrMCPacket;
		public int nrArrivedMCPacket;

        public ulong ID { get { return _ID; } }
        private ulong _ID;

        public Request request { get { return _request; } }
        private Request _request;

        public int requesterID;



        public ulong seq;
		public int pktParity;   // 0:clockwise  1:counter clockwise  -1:dont care

        public bool flow_open; // grab slot; queue, bounce for retx if none avail
        public bool flow_close; // release slot
        public int retx_count;

        // N.B.:
        // block number may be different than that in the request: as with
        // CachePacket above, a request is associated with a packet because
        // that packet is due to the request, but the packet may not be[4]
        // delivering data for that request (e.g., it may be a writeback)
        public ulong block { get { return _block; } }
        private ulong _block;

        public ulong creationTime;
        public ulong injectionTime;
		//public ulong reqCreationTime;

        public Flit[] flits;
        public int nrOfFlits; // { get { return flits.Length; } }

        public int nrOfArrivedFlits = 0;

        //TODO: move these into router-policy aware structures
        #region TO_BE_ISOLATED
        public int MIN_AD_dir;

        public ulong staticPriority; //summary> Static portion of prioritization. </summary>
        public ulong batchID;
        #endregion

        // SCARAB
        public int scarab_retransmit_count;
        public Packet scarab_retransmit;
        public bool scarab_is_nack, scarab_is_teardown;

		// construct a synthetic network packet
		// hotspot packet
		public Packet(Request request, ulong block, int nrOfFlits, Coord source, Coord dest, bool _gather)
		{
			_request = request;
			if (_request != null)
				_request.beenToNetwork = true;
			_block = block; // may not come from request (see above)
			_src = source;
			_dest = dest;
			nrArrivedMCPacket = 0;
			nrOfArrivedFlitsMC = new int [Config.N]; // for safety, although mc_degree may not reach N
			creationTimeMC = new ulong[Config.N]; // index the destination node ID
			if (request != null)
				request.setCarrier (this);
			requesterID = -1;
			mc = false;
			gather = _gather;
			hsFlowID = Simulator.network.hsFlowID;
			initialize (Simulator.CurrentRound, nrOfFlits);// Flitization of each packet;
		}

		// construct a multicast packet
		// TODO: it may be merged in network. 
		public Packet(Request request, ulong block, int nrOfFlits, Coord source, List <Coord> _destList)
		{
			_request = request;
			if (_request != null)
				_request.beenToNetwork = true;
			_block = block; // may not come from request (see above)
			_src = source;
			destList = new List <Coord>(_destList);
			nrMCPacket = _destList.Count;
			nrArrivedMCPacket = 0;
			nrOfArrivedFlitsMC = new int [Config.N]; // for safety, although mc_degree may not reach N
			creationTimeMC = new ulong[Config.N]; // index the destination node ID
			if (request != null)
				request.setCarrier (this);
			requesterID = -1;
			hsFlowID = -1;
			mc = true;
			gather = false;
			initialize (Simulator.CurrentRound, nrOfFlits);// Flitization of each packet;
		}

		// regular packet
		public Packet(Request request, ulong block, int nrOfFlits, Coord source, Coord dest)
     		   {
          		  _request = request;
            if (_request != null)
                _request.beenToNetwork = true;
            _block = block; // may not come from request (see above)
            _src = source;
            _dest = dest;
            if (request != null)
                request.setCarrier(this);
            requesterID = -1;
			mc = false;
			gather = false;
            initialize(Simulator.CurrentRound, nrOfFlits);
        }

		//by Xiyue:

		// TODO: this is the packet generated by the processor
		public Packet(Request request, ulong block, int nrOfFlits, Coord source, Coord dest, CmpCache_Txn txn, bool critical)
		{
			_request = request;
			if (_request != null)
				_request.beenToNetwork = true;
			_block = block; // may not come from request (see above)
			_src = source;
			_dest = dest;
			_intfCycle = 0;
			if (request != null)
				request.setCarrier(this);
			requesterID = -1;
			initialize(Simulator.CurrentRound, nrOfFlits);
			this.txn = txn;
			this.critical = critical;
			this.rank = Controller_QoSThrottle.app_rank[txn.node];
			this.app_type = Controller_QoSThrottle.app_type[txn.node];
			this.most_mem_inten = Controller_QoSThrottle.most_mem_inten[txn.node];
			this.slowdown = Simulator.stats.estimated_slowdown [txn.node].LastPeriodValue; // TODO: by Xiyue: this is equivalent to have N ranking levels.
		}

		public bool critical;
		public double slowdown; // slowdown of the associated application
		private int _intfCycle;
		public ulong first_flit_arrival;
		public ulong rank;
		public APP_TYPE app_type;
		public bool most_mem_inten;
		public CmpCache_Txn txn;
		public void add_intf () { _intfCycle++; }
		public int intfCycle
		{
			get { return _intfCycle; }
			set {_intfCycle = value; }
		}

		public virtual string ToString()
		{
			return String.Format("PktGen: pktID {0}, src {1} dest {2} of size {3}", _ID, src.ID, dest.ID, nrOfFlits);
		}
		//end Xiyue






        /**
         * Always call this initialization method before using a packet. All flits are also appropriately initialized
         */
        public void initialize(ulong creationTime, int nrOfFlits)
        {

            _ID = Packet.nrPackets;
            Packet.nrPackets++;

            batchID = (Simulator.CurrentRound / Config.STC_batchPeriod) % Config.STC_batchCount;

            this.nrOfFlits = nrOfFlits;

            flits = new Flit[nrOfFlits];
            for (int i = 0; i < nrOfFlits; i++)
                flits[i] = new Flit(this, i);

            flits[0].isHeadFlit = true;
            for (int i = 1; i < nrOfFlits; i++)
                flits[i].isHeadFlit = false;

            this.creationTime = creationTime;
			//reqCreationTime = creationTime;
            injectionTime = ulong.MaxValue;
            nrOfArrivedFlits = 0;

            for (int i = 0; i < nrOfFlits; i++)
            {
				if (flits[i].packet.mc)
					flits[i].destList = new List <Coord> (destList);
                flits[i].hasFlitArrived = false;
                flits[i].nrOfDeflections = 0;
            }

            /* This is needed for wormhole routing in bidirectional ring.
             * Body flits have to take the same ring as the head.
             */
            pktParity = -1;

            for (int i = 0; i < nrOfFlits; i++)
            {
                flits[i].isTailFlit = false;
                flits[i].isHeadFlit = false;
            }

            flits[0].isHeadFlit = true;
            flits[nrOfFlits - 1].isTailFlit = true;

            flow_open = false;
            flow_close = false;
            retx_count = 0;

            scarab_retransmit_count = 0;
            scarab_retransmit = null;
            scarab_is_nack = false;
            scarab_is_teardown = false;
        }

        public void setRequest(Request req)
        {
            _request = req;
            req.setCarrier(this);
        }

        public static int numQueues
        {
            get
            {
                if (Config.split_queues)
                    return 3; // control, data response, WB (off critical path)
                else
                    return 1;
            }
        }

        public virtual int getQueue()
        {
            return 0; // should be overridden
        }

        public virtual int getClass()
        {
            return 0; // should be overridden
        }
    }

    public class Flit
    {
        public Packet packet;

		public List <Coord> destList;

        public int flitNr;
        public bool hasFlitArrived;
        public bool isHeadFlit;
        public bool isTailFlit;
        public ulong nrOfDeflections;
        public int virtualChannel; // to which virtual channel the packet should go in the next router. 
        public bool sortnet_winner;
		public int ackCount;

        public int currentX;
        public int currentY;

        public bool Deflected;
		public bool Bypassed;
        public bool routingOrder;  //if (false): x direction prioritized over y

        public ulong injectionTime; // absolute injection timestamp
		public ulong creationTime;
        public ulong headT; // reaches-head-of-queue timestamp

        public int nackWire; // nack wire nr. for last hop
        public int inDir;
        public int prefDir;
		public int parity;   // 0:clockwise  1:counter clockwise  -1:dont care

        public enum State { Normal, Placeholder, Rescuer, Carrier }
        public State state;
        public Coord rescuerCoord;

		// MinBD: For resubmisstion buffer
		// Indicating that it had come out of the rebuf 
		public bool  wasInRebuf;
		public ulong nrInRebuf;
		public int rebufInTime;
		public int rebufOutTime;
		public bool  isSilver;
		public bool  wasSilver;
		public int   nrWasSilver;
		public int   priority;

		// For counting the interference cycle of each flit
		public int intfCycle = 0;

		//for stats: how many useless cycles the flit is comsuming
		public ulong timeIntoTheBuffer;
		public ulong timeSpentInBuffer = 0;
		public ulong timeWaitToInject = 0;
		public ulong timeInTheSourceRing = 0;
		public ulong timeInTheTransitionRing = 0;
		public ulong timeInTheDestRing = 0;
		public ulong timeInGR = 0;
		public ulong enterBuffer = 0;

		public ulong ejectTrial = 0;
		public ulong firstEjectTrial = 0;
		public bool [] preferredDirVector;
		public bool replicateNeed = false;
		public bool replicateEnable = false;

        public BufRingMultiNetwork_Coord bufrings_coord;
		
        public Coord dest
        {
            get
            {
                switch (state)
                {
                    case State.Normal: return packet.dest;
                    case State.Carrier: return packet.dest;

                    case State.Rescuer: return rescuerCoord;

                    case State.Placeholder: return new Coord(0);
                }
                throw new Exception("Unknown flit state");
            }
        }

        public ulong distance;
        //private bool[] deflections;
        //private int deflectionsIndex;
        public Flit(Packet packet, int flitNr)
        {
            this.packet = packet;
            this.flitNr = flitNr;

            hasFlitArrived = false;
            this.Deflected = false;
			this.Bypassed = false;
			this.ackCount = 1;
			this.creationTime = Simulator.CurrentRound;
            //deflections = new bool[100];
            //deflectionsIndex = 0;
            if (packet != null)
                distance = Simulator.distance(packet.src, packet.dest);
            this.routingOrder = false;
			if (packet == null) return;
			int srcX = packet.src.ID / Config.network_nrY / 2;
			int destX = packet.dest.ID / Config.network_nrY / 2;

            if (Config.AllBiDirLink || Config.HR_NoBias || Config.topology != Topology.Mesh)
				parity = -1;
			if (Config.topology == Topology.MeshOfRings && Config.RC_mesh == true)
			{
				if (srcX == destX) 
					parity = -1;
				else if (destX > srcX)
					parity = 0;
				else  // destX < srcX
					parity = 1;
			}

			if (Config.SingleDirRing)
				parity = 0;

			preferredDirVector = new bool[5] {false, false, false, false, false};

			
            /*if ((srcCluster == 0 || srcCluster == 3) && 
            	(destCluster == 1 || destCluster == 2))
            	Simulator.stats.flitToUpper.Add();
            else if ((srcCluster == 1 || srcCluster == 2) &&
            	(destCluster == 0 || destCluster == 3))
            	Simulator.stats.flitToLower.Add();*/
        }
        /*
        public void deflectTest()
        {
            if (deflectionsIndex == 100)
                return;
            //Console.WriteLine("{0} {1}", Deflected, deflectionsIndex);
            deflections[deflectionsIndex] = this.Deflected;
            //Console.WriteLine("{0} {1}", deflections[deflectionsIndex], deflectionsIndex);

            deflectionsIndex++;
        }
        public void dumpDeflections()
        {
            for (int i = 0; i < deflectionsIndex; i++)
                Console.Write(deflections[i] ? "D" : "-");

            Console.WriteLine();
        }*/


		public void ClearRoutingInfo ()
		{
			Array.Clear (preferredDirVector, 0, 5);
			replicateNeed = false;
			replicateEnable = false;
		}
        public delegate void Visitor(Flit f);

        public override string ToString()
        {
			if (packet != null) {
				if (packet.mc)
					return String.Format ("MC Packet {0}.{1}", packet.ID, flitNr,  state);
				else
					return String.Format ("UC Packet {0}.{1}", packet.ID, flitNr,  state);

			}
            else
				return String.Format("Flit {0} of pktID <NONE> (state {1})", flitNr, state);
        }
    }
}
