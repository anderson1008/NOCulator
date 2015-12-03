//#define DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace ICSimulator
{
	

    public class CPU
    {
        Node m_n;

        public Node node { get { return m_n; } }
        public int ID { get { return m_ID; } }
        public int GID { get { return m_group; } }
        public int groupCount { get { return group_count; } }

        public int windowFree { get { return m_ins.windowFree; } }

        InstructionWindow m_ins;
        ulong m_last_retired;
		public bool stall;

        public struct MSHR
        {
            public bool valid;
            public ulong block;
            public bool write;
            public bool pending_write;
        }

        int mshrs_free;
        MSHR[] m_mshrs;

        Trace m_trace;
        bool m_trace_valid; // current record valid?

        int m_ID, m_group; //, m_thdID;

        int group_count;

        static Syncer m_sync;

        bool m_stats_active;
        ulong m_active_ret;
		public bool stats_active {get{ return m_stats_active;}}


        public ulong ICount { get { return m_active_ret; } }
        bool m_done;
        bool m_idle;

        //stats
        public ulong outstandingReqsNetwork = 0;
        public ulong outstandingReqsNetworkCycle;
        public ulong outstandingReqsMemory = 0;
        public ulong outstandingReqsMemoryCycle;

        ulong alone_t;

		// By Xiyue
		//	 for quantification of slowdown
		public ulong m_rob_last_retire = 0;
		// end Xiyue

        public CPU(Node n)
        {
            m_n = n;
            m_ID = m_n.coord.ID;
            m_ins = new InstructionWindow(this);
            if (m_sync == null)
                m_sync = new Syncer();

			m_group = Simulator.network.workload.getGroup(m_ID);

		    group_count = Simulator.network.workload.GroupCount;

            openTrace();
            m_trace_valid = false;

            m_mshrs = new MSHR[Config.mshrs];

            for (int i = 0; i < Config.mshrs; i++)
            {
                m_mshrs[i].valid = false;
                m_mshrs[i].block = 0;
                m_mshrs[i].write = false;
                m_mshrs[i].pending_write = false;
            }
            mshrs_free = Config.mshrs;
            m_stats_active = true;
            alone_t = ulong.MaxValue;
        }

        public bool Finished
        {
            get
            {
                if (m_trace == null)
                    return true;
                else if (Config.trace_wraparound)
                    return m_done;
                else if (m_trace != null)
                    return m_trace.EOF;
                else
                    return true;
            }
        }

        public bool Livelocked
        { get { return (Simulator.CurrentRound - m_last_retired) > Config.livelock_thresh; } }

		void openTrace()  // By Xiyue: called by public CPU(Node n)
        {
            if (Config.bochs_fe)
            {
                m_trace = new TraceBochs(m_ID);
                return;
            }

            string tracefile;
			tracefile = Simulator.network.workload.getFile(m_ID);
            if (tracefile == "null")
            {
                m_trace = null;
                Console.WriteLine("tracefile at {0} is null",m_ID);
                return;
            }
            
            if (tracefile == "synth")
                m_trace = new TraceSynth(m_group);
            else if (tracefile.EndsWith(".gz"))
                m_trace = new TraceFile_Old(tracefile, m_group);
            else if (tracefile.EndsWith(".bin"))
                m_trace = new TraceFile_Old_Scalable(tracefile, m_group);
            else
                m_trace = new TraceFile_New(tracefile, m_group); // by Xiyue: executing trace on core[m_group]

            if (Config.randomize_trace > 0)
            {
                ulong start = (ulong)(Simulator.rand.NextDouble() * Config.randomize_trace);

                Console.WriteLine("CPU {0} starting at insn {1}", m_ID, start);
                Simulator.stats.skipped_insns_persrc[m_ID].Add(start);
                m_trace.seek(start);
                m_ins.SeekLog(start);
            }
        }


		// by Xiyue: 
		//   Called by Node::receivePacket ()
		//   upon calling p.cb(), CmpCache::pkt_callback() will be invoked to
		//     1) wake up other depended packet
		//     2) calling reqDone
        public void receivePacket(CachePacket p) 
        {
            if (p.cb != null) 
			{
				p.txn.interferenceCycle = p.intfCycle;  // The interference cycle of a txn is determined by that of the response packet.
				p.cb();
			}
        }

        void doStats(ulong retired)
        {
			
            Simulator.stats.every_insns_persrc[m_ID].Add(retired);
            if(m_idle)
                Simulator.stats.idle_cycles[m_ID].Add();

            if (Simulator.Warming)
            {
                Simulator.stats.warming_insns_persrc[m_ID].Add(retired);
                return;
            }

            if(!m_stats_active) return;

            Simulator.stats.mshrs.Add(mshrs_free);
            Simulator.stats.mshrs_persrc[m_ID].Add(mshrs_free);

            m_active_ret += retired;

            if (alone_t == ulong.MaxValue)
                alone_t = m_ins.oldestT;
            ulong alone_cyc = m_ins.oldestT - alone_t;
            alone_t = m_ins.oldestT;

            Simulator.stats.insns_persrc[m_ID].Add(retired);
        	Simulator.stats.insns_persrc_period[m_ID].Add(retired);
			Simulator.stats.insns_persrc_ewma [m_ID].Add (retired);
            Simulator.stats.active_cycles[m_ID].Add();
            Simulator.stats.active_cycles_alone[m_ID].Add(alone_cyc);

            Simulator.network._cycle_insns += retired;

            if (Simulator.CurrentRound % (ulong)100000 == 0)// && Simulator.CurrentRound != 0)
            {
				Console.WriteLine("Processor {0}: {1} ({2} outstanding)",
                                  m_ID, m_ins.totalInstructionsRetired,
                                  m_ins.outstandingReqs);
#if DEBUG
                Console.WriteLine("-- outstanding:");
                foreach (MSHR m in m_mshrs)
                {
                    if (m.block != null) Console.Write(" {0:X}", m.block);
                }
                Console.WriteLine();
#endif
            }

            bool windowFull = m_ins.isFull();
            bool nextIsMem = (m_trace.type == Trace.Type.Rd || m_trace.type == Trace.Type.Wr);
            bool noFreeMSHRs = true;
            for (int i = 0; i < m_mshrs.Length; i++)
            {
                if (!m_mshrs[i].valid)
                    noFreeMSHRs = false;
            }
            
			// use up credit
			bool stallThrottle = false;
			if (Config.throttle_enable)
				stallThrottle=!windowFull && ((Config.mshrs - mshrs_free) > Controller_QoSThrottle.mshr_quota [m_ID]) && nextIsMem;

            // any stall: either (i) window is full, or (ii) window is not full
            // but next insn (LD / ST) can't be issued
			stall = windowFull || (nextIsMem && noFreeMSHRs) || stallThrottle;

            // MSHR stall: window not full, next insn is memory, but we have no free MSHRs
            bool stallMem = !windowFull && (nextIsMem && noFreeMSHRs);

            if (stall)
                Simulator.stats.cpu_stall[m_ID].Add();
            if (stallMem)
                Simulator.stats.cpu_stall_mem[m_ID].Add();
			if (stallThrottle)
				Simulator.stats.cpu_stall_throttle [m_ID].Add();

        }

        bool advanceTrace()
        {
            if (m_trace == null)
            {
                m_trace_valid = false;
                return false;
            }

            if (!m_trace_valid)
                m_trace_valid = m_trace.getNext();

            if (Config.trace_wraparound)
            {
                if (!m_trace_valid)
                {
                    // TODO: Check this part
                    if(Config.endOfTraceSync == true && !Simulator.network.endOfTraceAllDone())
                    {
                        Simulator.network.endOfTraceBarrier[m_ID] = true;
                        m_idle = true;
					}
                    else
                    {   
                        if(Config.endOfTraceSync == true)
                            Simulator.network.endOfTraceReset();
                        m_idle = false;
                        m_trace.rewind();
                        m_trace_valid = m_trace.getNext();
                    }
                }

                if (Simulator.network.finishMode == Network.FinishMode.app && 
                        m_active_ret >= Config.insns)
                {
                    m_stats_active = false;
                    m_done = true;
                }
            }

            return m_trace_valid;
        }

        bool canIssueMSHR(ulong addr)
        {
            ulong block = addr >> Config.cache_block;

            for (int i = 0; i < m_mshrs.Length; i++)
                if (m_mshrs[i].block == block)
                    return true;

			// Without throttling, an mshr entry can be issued as long as there is an available entry.
			// In the case of throttling, we need to check credit.
			if (!Config.throttle_enable)
				return mshrs_free > 0;
			else
				return (mshrs_free > 0)&&((Config.mshrs - mshrs_free)<= Controller_QoSThrottle.mshr_quota [m_ID]);
        }

		// called when completing a LD/ST request
		void reqDone(ulong addr, int mshr, bool write)   
		{
			m_ins.setReady(addr, write);

			#if DEBUG
			//Console.WriteLine ("DONE at node = {1}, req_addr = {2}, time = {0}", Simulator.CurrentRound, m_ID, addr);
			#endif

			if (!write && m_mshrs[mshr].pending_write)
			{
				m_mshrs[mshr].pending_write = false;
				m_mshrs[mshr].write = true;

				_issueReq(mshr, m_mshrs[mshr].block << Config.cache_block, true);
			}
			else
			{
				m_mshrs[mshr].valid = false;
				m_mshrs[mshr].block = 0;
				m_mshrs[mshr].write = false;
				m_mshrs[mshr].pending_write = false;
				mshrs_free++;
			}
		}

        void issueReq(Request req)
        {
            for (int i = 0; i < m_mshrs.Length; i++)
                if (m_mshrs[i].block == req.blockAddress)
                {
                    if (req.write && !m_mshrs[i].write)   //by Xiyue: Prevent Write After Read hazard???
                        m_mshrs[i].pending_write = true;
                    return;
                }

            int mshr = -1;
            for (int i = 0; i < m_mshrs.Length; i++)
                if (!m_mshrs[i].valid)
                {
                    mshr = i;
                    break;
                }
            Debug.Assert(mshr != -1);

            mshrs_free--;

            m_mshrs[mshr].valid = true;
            m_mshrs[mshr].block = req.blockAddress;
            m_mshrs[mshr].write = req.write;

            _issueReq(mshr, req.address, req.write);

        }

		// By Xiyue
        void _issueReq(int mshr, ulong addr, bool write)
		{
			qos_stat_delegate QOS_delegate = delegate (CmpCache_Txn txn) { computePenalty(txn, write); };

			Simulator.network.cache.access(m_ID, addr, write, m_stats_active, 
				delegate() { reqDone(addr, mshr, write); }, QOS_delegate);
        }


		public delegate void qos_stat_delegate (CmpCache_Txn txn);
		void computePenalty(CmpCache_Txn txn, bool write){
			if (m_stats_active) 
				m_ins.setIntfCycle (write, txn);
		}

		// end Xiyue

		public bool doStep()
		{
			if (m_trace == null) {return true;}

			int syncID;

			ulong retired =
				(ulong)m_ins.retire(m_ID, Config.proc.instructionsPerCycle, m_last_retired);

			if (retired > 0)
				m_last_retired = Simulator.CurrentRound;
		
			if (!m_trace_valid)
				m_trace_valid = advanceTrace(); // doStats needs to see the next record

			doStats(retired); // by Xiyue: periodical slowdown is also logged here.

			if (m_ins.isFull()) 
				return true;

			bool done = false;
			int nIns = Config.proc.instructionsPerCycle;
			int nMem = 1;

			while (!done && nIns > 0 && !m_ins.isFull())
			{
				if (!m_trace_valid) ///By Xiyue: Why advance twice?
					m_trace_valid = advanceTrace();
				if (!m_trace_valid)
					return false;

				if (m_trace.type == Trace.Type.Pause) // when execution-driven, source has nothing to give
				{
					m_trace_valid = false;
					return true;
				}

				if (m_trace.type == Trace.Type.Sync)
				{
					// `from' field: translate referrent from thd ID to physical CPU id
					syncID = Simulator.network.workload.mapThd(m_group, m_trace.from);
				}
				else
					syncID = m_trace.from;

				switch (m_trace.type)
				{
				case Trace.Type.Rd:
				case Trace.Type.Wr:
					if (nMem == 0 || !canIssueMSHR(m_trace.address))
					{
						done = true;
						break;
					}
					nMem--;
					nIns--;

					ulong addr = m_trace.address;
					bool isWrite = m_trace.type == Trace.Type.Wr;
					bool inWindow = m_ins.contains(addr, isWrite);

					Request req = inWindow ? null : new Request(m_ID, addr, isWrite);
					m_ins.fetch(req, addr, isWrite, inWindow);

					if (!inWindow)
						issueReq(req);

					m_trace_valid = false;
					break;

				case Trace.Type.NonMem:
					if (m_trace.address > 0)
					{
						m_ins.fetch(null, InstructionWindow.NULL_ADDRESS, false, true);
						m_trace.address--;
						nIns--;
					}
					else
						m_trace_valid = false;

					break;

				case Trace.Type.Label:
					if(m_sync.Label(m_ID, syncID, m_trace.address))
						m_trace_valid = false; // allowed to continue
					else
					{ // shouldn't ever block...
						if (m_stats_active)
							Simulator.stats.cpu_sync_memdep[m_ID].Add();
						done = true; // blocking: done with this cycle
					}

					break;

				case Trace.Type.Sync:
					if (m_sync.Sync(m_ID, syncID, m_trace.address))
						m_trace_valid = false;
					else
					{
						if (m_stats_active)
							Simulator.stats.cpu_sync_memdep[m_ID].Add();
						done = true;
					}

					break;

				case Trace.Type.Lock:
					//Console.WriteLine("Lock" + ' ' + m_ID.ToString() + ' ' + syncID.ToString() + ' ' + m_trace.address.ToString());
					if (m_sync.Lock(m_ID, syncID, m_trace.address))
						m_trace_valid = false;
					else
					{
						if (m_stats_active)
							Simulator.stats.cpu_sync_lock[m_ID].Add();
						done = true;
					}

					break;

				case Trace.Type.Unlock:
					//Console.WriteLine("Unlock" + ' ' + m_ID.ToString() + ' ' + syncID.ToString() + ' ' + m_trace.address.ToString());
					if (m_sync.Unlock(m_ID, syncID, m_trace.address))
						m_trace_valid = false;
					else
					{ // shouldn't ever block...
						if (m_stats_active)
							Simulator.stats.cpu_sync_lock[m_ID].Add();
						done = true;
					}

					break;

				case Trace.Type.Barrier:
					//Console.WriteLine("Barrier" + ' ' + m_ID.ToString() + ' ' + syncID.ToString() + ' ' + m_trace.address.ToString());
					if (m_sync.Barrier(m_ID, syncID, m_trace.address))
						m_trace_valid = false;
					else
					{
						if (m_stats_active)
							Simulator.stats.cpu_sync_barrier[m_ID].Add();
						done = true;
					}

					break;
				}
			}

			return true;
		}

		public ulong get_stalledSharedDelta() { throw new NotImplementedException(); }



		//   Compute the non-overlapped latency.
		//   Latency is accounted for if the associated instruction is at the head of ROB.
		//   Then, exclude the overlapped portion.
		//   Potential hardware implementation:
		//   Use a register storing the address/vitual_address of the instruction at the head of ROB.
		//   Another register storing the accumulated non-overlapped latency and the time the previous instruction is from ROB.
		/*
		ulong rob_last_retire {
			get { return m_rob_last_retire; }
			set { m_rob_last_retire = value; }
			}

		public delegate void qos_stat_delegate (CmpCache_Txn txn);
		void computePenalty(CmpCache_Txn txn, bool write){
			ulong actual_interference = 0;
			ulong t_no_interference = 0;
			ulong addr = txn.req_addr;
			bool isHead = m_ins.probe_head_rob(addr, write);
			if (m_stats_active) {
				if (txn.interferenceCycle != 0 && isHead) {
					t_no_interference = Simulator.CurrentRound - txn.interferenceCycle; 
					if (t_no_interference > m_rob_last_retire) {
						actual_interference = txn.interferenceCycle;
					} else if (t_no_interference < m_rob_last_retire && Simulator.CurrentRound > m_rob_last_retire) {
						actual_interference = Simulator.CurrentRound - m_rob_last_retire;
					} else {
						// the interference cycle is completely overlapped with the previous request
						// no action is requied.
					}
					// track the nonoverlapped penalty
					Simulator.stats.non_overlap_penalty [m_ID].Add (actual_interference);
					Simulator.stats.non_overlap_penalty_period [m_ID].Add (actual_interference);
					#if DEBUG
					//Console.WriteLine ("At time {0}: INTERFERENCE for node {1} req_addr= {2} is {3}", Simulator.CurrentRound, m_ID, addr, actual_interference);
					Console.WriteLine ("At time {0}: INTERFERENCE Cycle for node {1} is {2}; Inst_Count = {3}", 
						Simulator.CurrentRound, m_ID, Simulator.stats.non_overlap_penalty [m_ID].Count, Simulator.stats.insns_persrc[m_ID].Count);
					#endif

					//m_rob_last_retire = Simulator.CurrentRound;
				} else if (txn.interferenceCycle != 0 && !isHead) { // just curious
					#if DEBUG
					//Console.WriteLine ("At time {0}: IGNORE interference for node {1} req_addr= {2} is {3}", Simulator.CurrentRound, m_ID, addr, txn.interferenceCycle);
					#endif
				}
				if (txn.causeIntf != 0) {
					Simulator.stats.causeIntf [m_ID].Add (txn.causeIntf);
				}
			}
		}
		*/
		// end Xiyue

    }
}
