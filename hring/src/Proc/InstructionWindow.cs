//#define LOG
//#define DEBUG

using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;

namespace ICSimulator {

    public class InstructionWindow {

#if LOG
        StreamWriter sw;
#endif

        public static readonly ulong NULL_ADDRESS = ulong.MaxValue;

        public int windowFree { get { return addresses.Length - load; } }

        private int load;
        private ulong[] addresses;
        private bool[] writes;
        private bool[] ready;
        private Request[] requests;
        private int next, oldest;

        private ulong[] issueT, headT;

        public ulong instrNr;
        public ulong totalInstructionsRetired;

        public ulong stallStart;

        public int outstandingReqs;

        public BinaryReader m_readlog;
        public BinaryWriter m_writelog;
        public string m_readfile, m_writefile;
        public bool readLog, writeLog;
        public int readlog_delta;

        public ulong oldestT; // kept in sync with retire point in corresponding annotate-log
        public ulong oldestT_base; // when wrapping to beginning, keep
                                   // the virtual retire time stream going

        ulong m_match_mask;
		private CPU m_cpu;
        public InstructionWindow(CPU cpu) {
            m_cpu = cpu;
            next = oldest = 0;
            addresses = new ulong[Config.proc.instWindowSize];
            writes = new bool[Config.proc.instWindowSize];
            ready = new bool[Config.proc.instWindowSize];
            requests = new Request[Config.proc.instWindowSize];
            issueT = new ulong[Config.proc.instWindowSize];
            headT = new ulong[Config.proc.instWindowSize];


            m_match_mask = ~ ( ((ulong)1 << Config.cache_block) - 1 );

            for (int i = 0; i < Config.proc.instWindowSize; i++) {
                addresses[i] = NULL_ADDRESS;
                writes[i] = false;
                ready[i] = false;
                requests[i] = null;
            }

            outstandingReqs = 0;

            readLog = writeLog = false;

            if (Config.writelog != "" && Config.writelog_node == cpu.node.coord.ID)
                SetWrite(Config.writelog);
            if (Config.readlog != "")
            {
                if (Config.readlog.StartsWith("auto"))
                {
                    string prefix = Config.readlog.StartsWith("auto_") ?
                        Config.readlog.Substring(5) :
                        Config.router.algorithm.ToString();
                    SetRead(Simulator.network.workload.getLogFile(cpu.node.coord.ID, prefix));
                }
                else
                    SetRead(Config.readlog.Split(' ')[cpu.node.coord.ID]);
            }

#if LOG
            if (Simulator.sources.soloMode && Simulator.sources.solo.ID == procID)
            {
                sw = new StreamWriter ("insn.log");
                sw.WriteLine("# cycle instrNr req_seq bank netlat injlat");
            }
            else
                sw = null;
#endif

        }

        public void SetRead(string filename)
        {
            if (!File.Exists(filename))
                filename = Config.logpath + "/" + filename;
            if (!File.Exists(filename))
            {
                Console.Error.WriteLine("WARNING: could not find annotation log {0}", filename);
                return;
            }

            m_readfile = filename;
            readLog = true;
            openReadStream();
        }

        public void openReadStream()
        {
            if (m_readlog != null) m_readlog.Close();
            m_readlog = new BinaryReader(new GZipInputStream(File.OpenRead(m_readfile)));

            readlog_delta = (int) m_readlog.ReadUInt64();
        }

        public void SetWrite(string filename)
        {
            m_writefile = filename;
            writeLog = true;
            //m_writelog = new BinaryWriter(new GZipOutputStream(File.Open(filename, FileMode.Create)));
            m_writelog = new BinaryWriter(File.Open(filename, FileMode.Create));
            m_writelog.Write((ulong)Config.writelog_delta);
        }

        public void SeekLog(ulong insns)
        {
            if (!readLog) return;

            long count = (long)insns;
            while (count > 0)
            {
                advanceReadLog();
                count -= readlog_delta;
            }
        }

        public bool isFull() {
            return (load == Config.proc.instWindowSize);
        }

        public bool isEmpty() {
            return (load == 0);
        }

        public void fetch(Request r, ulong address, bool isWrite, bool isReady) {


            if (load < Config.proc.instWindowSize) {
                load++;
                addresses[next] = address;
                writes[next] = isWrite;
                ready[next] = isReady;
                requests[next] = r;
                issueT[next] = Simulator.CurrentRound;
                headT[next] = Simulator.CurrentRound;
                if (isReady && r != null) r.service();  // service non-memory instruction directly here.
                //Console.WriteLine("pr{0}: new req in slot {1}: addr {2}, write {3} ready {4}", m_cpu.ID, next, address, isWrite, isReady);
                next++;
                if (!isReady) outstandingReqs++;
                //Console.WriteLine("pr{0}: outstanding reqs now at {1}", m_cpu.ID, outstandingReqs);
                if (next == Config.proc.instWindowSize) next = 0;
            }
            else throw new Exception("Instruction Window is full!");

        }


		public int retire(int node, int n, ulong last_retired) {
            int i = 0;
			ulong max_intf = ulong.MaxValue;
			ulong max_intf_temp;

            while (i < n && load > 0 && ready[oldest])
            {
				if (requests [oldest] != null ) {
					//Console.WriteLine ("COMMIT node = {0}, addr = {1}, intf = {3}, time = {2}", node, requests [oldest].address, Simulator.CurrentRound, requests [oldest].interferenceCycle);

					// using >0 to exclude the local packets, which tend to be accessed at the same cycle as the request is generated.
					//if (requests [oldest].interferenceCycle > 0 && load == Config.proc.instWindowSize) {
					if (requests [oldest].interferenceCycle > 0 && m_cpu.stall) {

						#if DEBUG
						//Console.WriteLine ("COMMIT node = {0}, addr = {1}, intf = {3}, time = {2}", node, requests [oldest].address, Simulator.CurrentRound, requests [oldest].interferenceCycle);
						#endif
						max_intf_temp = requests [oldest].computePenalty (last_retired, max_intf);
						//max_intf = max_intf + max_intf_temp;
						max_intf = max_intf_temp;
					
					} 				
					requests [oldest].retire ();
				} 

	

                ulong deadline = headT[oldest] - issueT[oldest];
                Simulator.stats.deadline.Add(deadline);

                oldest++;
                if (oldest == Config.proc.instWindowSize) oldest = 0;
                headT[oldest] = Simulator.CurrentRound;
                i++;
                load--;
                instrNr++;
                totalInstructionsRetired++;

                if (writeLog)
                {
                    if (instrNr % (ulong)Config.writelog_delta == 0)
                    {
                        m_writelog.Write((ulong)Simulator.CurrentRound);
                        m_writelog.Flush();
                    }
                }
                if (readLog)
                {
                    if (instrNr % (ulong)readlog_delta == 0)
                        advanceReadLog();
                }
            }
			if (load > 0 && i < n) {
				requests [oldest].backStallsCaused += (1.0 * n - i) / n; // by Xiyue:  calculate the wasted/ideal proc cycle in percentage.
				/*
				#if DEBUG
				Console.WriteLine ("WASTE {0} instructions cycle at node = {1}, time = {2}", n - i, node, Simulator.CurrentRound);
				#endif
				*/
			}
			

			if (max_intf < ulong.MaxValue && m_cpu.stats_active) {
				// Trial 1: average over i instructions
				//Simulator.stats.non_overlap_penalty [node].Add ((float)max_intf/i);
				//Simulator.stats.non_overlap_penalty_period [node].Add ((float)max_intf/i);

				Simulator.stats.non_overlap_penalty [node].Add (max_intf);
				Simulator.stats.non_overlap_penalty_period [node].Add (max_intf);

				#if DEBUG
					//Console.WriteLine ("ADD intfcyle = {0} to node = {1}, time = {2}", max_intf, node, Simulator.CurrentRound);
					//Console.WriteLine ("REPORT intfcyle = {0} to node = {1}, time = {2}", Simulator.stats.non_overlap_penalty [node].Count, node, Simulator.CurrentRound);
				#endif
			}

            return i;
        }

        void advanceReadLog()
        {
            ulong old_oldestT = oldestT;
            try
            {
                oldestT = oldestT_base + m_readlog.ReadUInt64();
            }
            catch (EndOfStreamException)
            {
                openReadStream();
                oldestT_base = old_oldestT;
                oldestT = oldestT_base + m_readlog.ReadUInt64();
            }
            catch (SharpZipBaseException)
            {
                openReadStream();
                oldestT_base = old_oldestT;
                oldestT = oldestT_base + m_readlog.ReadUInt64();
            }
        }

        public bool contains(ulong address, bool write) {
            int i = oldest;
            while (i != next) {
                if ((addresses[i] >> Config.cache_block) == (address >> Config.cache_block)) {
                    // this new request will be satisfied by outstanding request i if:
                    // 1. this new request is not a write (i.e., will be satisfied by R or W completion), OR
                    // 2. there is an outstanding write (i.e., a write completion will satisfy any pending req)
                    if (!write || writes[i])
                        return true;
                }
                i++;
                if (i == Config.proc.instWindowSize) i = 0;
            }
            return false;
        }




		public void setReady(ulong address, bool write) {
   
            if (isEmpty()) throw new Exception("Instruction Window is empty!");

            for (int i = 0; i < Config.proc.instWindowSize; i++) {
                if ((addresses[i] & m_match_mask) == (address & m_match_mask) && !ready[i]) {

					// this completion does not satisfy outstanding req i if and only if
                    // 1. the outstanding req is a write, AND
                    // 2. the completion is a read completion.
                    if (writes[i] && !write) continue;

					//Console.WriteLine ("SetReady node = {0}, addr = {1}, time = {2}", m_cpu.ID, addresses [i], Simulator.CurrentRound);

					// Need to remove interference entry

                    requests[i].service();

                    ready[i] = true;
                    addresses[i] = NULL_ADDRESS;
                    outstandingReqs--;
                }
            }
        }
		// by Xiyue
		public bool probe_head_rob (ulong address, bool write){
			if (isEmpty()) throw new Exception("Instruction Window is empty!");
			bool isHead = false;
			for (int i = 0; i < Config.proc.instWindowSize; i++) {
				if ((addresses [i] & m_match_mask) == (address & m_match_mask) && !ready [i]) {

					if (writes[i] && !write) continue;

					// probe if the serviced request is at the head of ROB.
					if (address == addresses [oldest])
						isHead = true;
				}
			}
			return isHead;
		}

		public void setIntfCycle (bool write, CmpCache_Txn txn){
			if (isEmpty()) throw new Exception("Instruction Window is empty!");
			ulong address = txn.req_addr;
			ulong interferenceCycle = (ulong) txn.interferenceCycle;
			ulong throttleCycle = txn.throttleCycle;
			ulong queueCycle = txn.queue_latency;
			ulong serializationLatency = txn.serialization_latency;
			int queueIntfCycle = (int)(queueCycle - serializationLatency);
			if (queueIntfCycle < 0)
				throw new Exception("queue cycle less than serialization latency!");

			for (int i = 0; i < Config.proc.instWindowSize; i++) 
				if ((addresses [i] & m_match_mask) == (address & m_match_mask) && !ready [i]) 
				{
					if (writes [i] && !write) 
						continue;
					// Do NOT have to consider throttleCycle, since queueIntfCycle already includes the amount of throttled cycles
					ulong interference_cycle = interferenceCycle + (ulong)queueIntfCycle;
					if (interference_cycle != 0 ) {
						requests [i].interferenceCycle = requests [i].interferenceCycle + interference_cycle;
						Simulator.stats.serialization_latency [m_cpu.ID].Add (serializationLatency);
						Simulator.stats.queue_delay [m_cpu.ID].Add (queueCycle);
						#if DEBUG
						//Console.WriteLine ("RECEIVE node = {0}, addr = {1}, intf = {2}, time = {3}", m_cpu.ID, addresses [i], requests [i].interferenceCycle, Simulator.CurrentRound);
						#endif
					}

					Simulator.stats.causeIntf [m_cpu.ID].Add (txn.causeIntf);
				}
		}

		// end Xiyue

        public bool isOldestAddrAndStalled(ulong address, out ulong stallCount)
        {
            if (!ready[oldest] && addresses[oldest] == address)
            {
                stallCount = Simulator.CurrentRound - stallStart;
                return true;
            }
            else
            {
                stallCount = 0;
                return false;
            }
        }

        /**
         * Returns true if the oldest instruction in the window is a non-ready memory request
         */
        public bool isOldestReady() {
            return ready[oldest];
        }

        public ulong stallingAddr()
        {
            return addresses[oldest];
        }

        public void close()
        {
            if (m_writelog != null)
            {
                m_writelog.Flush();
                m_writelog.Close();
            }
        }

        public void dumpOutstanding()
        {
            Console.Write("Pending blocks: ");
            for (int i = 0; i < Config.proc.instWindowSize; i++)
            {
                if (addresses[i] != NULL_ADDRESS && !ready[i])
                    Console.Write("{0:X} ", addresses[i] >> Config.cache_block);
            }
            Console.WriteLine();
        }
    }
}
