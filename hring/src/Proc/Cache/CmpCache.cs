/*
 * CmpCache
 *
 * Chris Fallin <cfallin@ece.cmu.edu>, 2010-09-12
 */

//#define DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;

/*
   The coherence timing model is a compromise between 100% fidelity (full
   protocol FSMs) and speed/simplicity. The model has a functional/timing
   split: state is updated immediately in the functional model (cache contents
   and block states), and a chain (actually, DAG) of packets with dependencies
   is generated for each transaction. This chain of packets is then sent
   through the interconnect timing model, respecting dependencies, and the
   request completes in the timing model when all packets are delivered.

   Note that when there is no write-contention, and there are no
   memory-ordering races, this model should give 100% fidelity. It gives up
   accuracy in the case where write contention and/or interconnect reordering
   hit corner cases of the protocol; however, the hope is that these cases will
   be rare (if the protocol is well-behaved). Functional-level behavior such as
   ping-ponging is still captured; only behavior such as protocol-level retries
   (in a NACK-based recovery scheme) or unfairness due to interconnect delay in
   write contention are not captured. The great strength of this approach is
   that we *need not work out* the corner cases -- this gives robustness and
   speed, and allows implementations to be very simple, by considering
   transactions as atomic DAGs of packets that never execute simultaneously
   with any other transaction on the same block.

   The sequence for a request is:

   - A CmpCache_Txn (transaction) is generated for a read-miss, write-miss, or
     write-upgrade. The functional state is updated here, and the deltas (nodes
     that were invalidated, downgraded; type of transaction; whether it missed
     in shared cache) are recorded in the txn for timing to use. The functional
     update *must* happen immediately: if it waits until the packets are
     actually delivered (i.e., the timing model says that things are actually
     done), then multiple transactions might start with the same (out-of-date)
     state without awareness of each other.

   - The subclass of CmpCache that implements the protocol implements
     gen_proto(). This routine takes the transaction and generates a DAG of
     packets.

   - Packets flow through the interconnect. When each is delivered, it iterates
     over its wakeup list and decrements the remaining-deps counts on each
     dependent packet. If this reaches zero on a packet, that packet is sent.
     Also, the remaining-packets count on the transaction is decremented at
     each delivery.

   - When all of a transaction's packets are delivered, it is complete.
 */



namespace ICSimulator
{
    struct CmpCache_Owners
    {
        ulong[] bitmask;
        const int ULONG_BITS = 64;   // maximum of 4096 caches

        public CmpCache_Owners(int junk)
        {
          this.bitmask = new ulong[ULONG_BITS];
          for(int i=0; i < ULONG_BITS; i++)
            bitmask[i] = 0;
        }

        public void set(int i) { int r = i/ULONG_BITS; bitmask[r] |= ((ulong)1) << (i % ULONG_BITS); }
        public void unset(int i) { int r = i/ULONG_BITS; bitmask[r] &= ~( ((ulong)1) << (i % ULONG_BITS) ); }

        public void reset() { for(int i=0; i<ULONG_BITS; i++) bitmask[i] =0; }

        public bool is_set(int i) { int r = i/ULONG_BITS; return (bitmask[r] & ( ((ulong)1) << (i % ULONG_BITS) )) != 0; }
        public bool others_set(int i) { 
          int r = i/ULONG_BITS; 

          // Special check for the bits in the same ulong
          for(int j=0; j< ULONG_BITS; j++) {
            if(r==j) {
              if( (bitmask[j] & ~( ((ulong)1) << (i % ULONG_BITS) )) != 0)
                return true;
            }
            else if(bitmask[j] != 0)
              return true;
          }
          return false;
        }
        public bool any_set() { 
          for(int i=0; i<ULONG_BITS; i++) 
            if(bitmask[i] != 0)
              return true;
          return false;
        }
    }

    class CmpCache_State 
    {
        public CmpCache_Owners owners = new CmpCache_Owners(0); // bitmask of owners
        public int excl; // single node that has exclusive grant, -1 otherwise
        public bool modified; // exclusive copy is modified?
        public bool sh_dirty; // copy in shared cache is dirty?

        public CmpCache_State()
        {
            excl = -1;
            modified = false;
            sh_dirty = false;
        }
    }

    // a DAG of CmpCache_Pkt instances is created for each CmpCache_Txn. The set
    // of packets, with dependencies, is the work required by the cache coherence
    // protocol to complete the transaction.
    public class CmpCache_Pkt 
    {
        public bool send; // send a packet at this node (nodes can also be join-points w/o send)
        public int from, to;
        public ulong id;
        public int flits;
        public bool off_crit;

        public int vc_class;

		public bool done; // critical-path coaddrmpletion after this packet?

        public bool mem; // virtual node for going to memory
        public ulong mem_addr;
        public bool mem_write;
        public int mem_requestor;

        public ulong delay; // delay before sending (e.g., to model cache response latency)

        // out-edges
        public List<CmpCache_Pkt> wakeup; // packets that depend on this one
        // in-edges
        public int deps; // number of packets for which we are still waiting

        // associated txn
        public CmpCache_Txn txn;

		// by Xiyue:
		public int tier;
		public bool critical;
		//public ulong minimalCycle;
		// end Xiyue

        public CmpCache_Pkt ()
        {
            send = false;
            from = to = 0;
            id = 0;
            done = false;
            off_crit = false;

            mem = false;
            mem_addr = 0;

            delay = 0;
            deps = 0;

            txn = null;

			tier = 0; // by Xiyue: for categorize and prioritize network packet
        }
    }

    // a transaction is one client-initiated operation that requires a protocol
    // interaction (read or write miss, or upgrade).
    //
    // A generic cache-coherence transaction is classified along these axes:
    //
    //    Control ops:
    //  - data grant: we grant a lease (either shared or excl) to zero or one
    //                nodes per transaction. Could be due to miss or upgrade
    //                (i.e., may or may not transfer data).
    //  - data rescind: we rescind a lease (downgrade or invalidate) from these
    //                  nodes. Could be due to another node's upgrade or due to
    //                  a shared-cache replacement.
    //        - additionally, we distinguish data rescinds due to private-cache invalidates
    //          from those due to shared-cache evictions that require private cache invals.
    //
    //    Data ops:
    //  - data sh->prv: transfer data from shared to private (normal data grant)
    //  - data prv->prv: transfer data from private to private (cache-to-cache xfer)
    //  - data prv->sh: transfer data from private to shared (writeback)
    //  - data mem->sh: transfer data from memory to shared (sh cache miss)
    //  - data sh->mem: transfer data from shared to memory (sh cache writeback)
    //
    // The dependency ordering of the above is:
    //
    //    (CASE 1) not in requestor's cache, present in other private caches:
    //
    //    control request (client -> shared)
    //    inval data rescinds (others -> shared or client directly) if exclusive req
    //    inval prv->sh / prv->prv (data writeback if others invalidated)
    //    -OR-
    //    transfer prv->prv
    //    WB of evicted block if dirty
    //
    //    (CASE 2) not present in other private caches, but present in sh cache: 
    //
    //    control request (client->shared)
    //    data grant (sh->prv), transfer (sh->prv)
    //    data transfer (writeback) prv->sh upon replacement (must hit, due to inclusive cache)
    //    (CASE 3) not present in other private caches, not present in sh cache:
    //
    //    control request (client->mem)
    //    mem request (shared->mem)
    //    mem response
    //    data grant/transfer to prv, AND inval:
    //            - inval requests to owners of evicted sh-cache block, and WB to mem if clean, or WB prv->mem if dirty
    //    
    public class CmpCache_Txn
    {
        /* initiating node */
        public int node;

		/* protocol packet DAG */
        public CmpCache_Pkt pkts;
        public int n_pkts;

        /* updated at each packet arrival */
		public int n_pkts_remaining;

        /* timing completion callback */
        public Simulator.Ready cb;
		public CPU.qos_stat_delegate qos_cb;

		// by Xiyue
		public int mshr;
		public ulong minimalCycle;
		public int interferenceCycle;
		public ulong throttleCycle;
		public ulong causeIntf;
		public ulong req_addr;
		public ulong queue_latency;
		public ulong serialization_latency;
    };

    public class CmpCache
    {
        ulong m_prvdelay; // private-cache probe delay (both local accesses and invalidates)
        ulong m_shdelay; // probe delay at shared cache (for first access)
        ulong m_opdelay; // pass-through delay once an operation is in progress
        int m_datapkt_size;
		int m_ctrlpkt_size;
        bool m_sh_perfect; // shared cache perfect?
        Dictionary<ulong, CmpCache_State> m_perf_sh;

        int m_blkshift;
        int m_N;

        Sets<CmpCache_State> m_sh;
        Sets<bool>[] m_prv;

        // address mapping for shared cache slices
        //int map_addr(ulong addr) { return Simulator.network.mapping.homeNode(addr >> Config.cache_block).ID; }
        
        int map_addr(int node, ulong addr) { return Simulator.controller.mapCache(node, addr >> Config.cache_block); }

        // address mapping for memory controllers
        //int map_addr_mem(ulong addr) { return Simulator.network.mapping.memNode(addr >> Config.cache_block).ID; }

        int map_addr_mem(int node, ulong addr) { return Simulator.controller.mapMC(node, addr >> Config.cache_block); }

        // closest out of 'nodes' set
        int closest(int node, CmpCache_Owners nodes)
        {
            int best = -1;
            int best_dist = m_N;
            Coord here = new Coord(node);

            for (int i = 0; i < m_N; i++)
                if (nodes.is_set(i))
                {
                    int dist = (int)Simulator.distance(new Coord(i), here);
                    if (dist < best_dist)
                    {
                        best = i;
                        best_dist = dist;
                    }
                }

            return best;
        }

		// construct the cache
        public CmpCache()
        {
            m_N = Config.N;
            m_blkshift = Config.cache_block;
            m_prv = new Sets<bool>[m_N];
            for (int i = 0; i < m_N; i++)
                m_prv[i] = new Sets<bool>(m_blkshift, 1 << Config.coherent_cache_assoc, 1 << (Config.coherent_cache_size - Config.cache_block - Config.coherent_cache_assoc));

            if (!Config.simple_nocoher)
            {
                if (Config.sh_cache_perfect)
                    m_perf_sh = new Dictionary<ulong, CmpCache_State>();
                else
                    m_sh = new Sets<CmpCache_State>(m_blkshift, 1 << Config.sh_cache_assoc, (1 << (Config.sh_cache_size - Config.cache_block - Config.sh_cache_assoc)) * m_N);
            }

            m_prvdelay = (ulong)Config.cohcache_lat;
            m_shdelay = (ulong)Config.shcache_lat;
            m_opdelay = (ulong)Config.cacheop_lat;
            m_datapkt_size = Config.router.dataPacketSize;
			m_ctrlpkt_size = Config.router.addrPacketSize;
            m_sh_perfect = Config.sh_cache_perfect;
        }

		// By Xiyue: yanked from CPU.cs
		void do_stats(bool stats_active, int node, bool L1hit, bool L1upgr, bool L1ev, bool L1wb,
			bool L2access, bool L2hit, bool L2ev, bool L2wb, bool c2c)
		{
			if(!L1hit)
				Simulator.controller.L1misses[node]++;

			if (stats_active)
			{
				Simulator.stats.L1_accesses_persrc[node].Add();

				if (L1hit)
					Simulator.stats.L1_hits_persrc[node].Add();
				else
				{
					Simulator.stats.L1_misses_persrc[node].Add();
					Simulator.stats.L1_misses_persrc_period[node].Add();
				}

				if (L1upgr)
					Simulator.stats.L1_upgr_persrc[node].Add();
				if (L1ev)
					Simulator.stats.L1_evicts_persrc[node].Add();
				if (L1wb)
					Simulator.stats.L1_writebacks_persrc[node].Add();
				if (c2c)
					Simulator.stats.L1_c2c_persrc[node].Add();

				if (L2access)
				{
					Simulator.stats.L2_accesses_persrc[node].Add();

					if (L2hit)
						Simulator.stats.L2_hits_persrc[node].Add();
					else
						Simulator.stats.L2_misses_persrc[node].Add();

					if (L2ev)
						Simulator.stats.L2_evicts_persrc[node].Add();
					if (L2wb)
						Simulator.stats.L2_writebacks_persrc[node].Add();
				}
			}
		}


		public void access(int node, ulong addr, int mshr, bool write, bool stats_active, 
			Simulator.Ready cb, CPU.qos_stat_delegate qos_cb)
        {
	
            int sh_slice = map_addr(node, addr);

            // ------------- first, we probe the cache (private, and shared if necessary) to
            //               determine current state.

            // probe private cache
            CmpCache_State state;
            bool prv_state;
            bool prv_hit = m_prv[node].probe(addr, out prv_state);

			bool sh_hit = false;
            
            if (m_sh_perfect)
            {
                ulong blk = addr >> m_blkshift;
                sh_hit = true;
                if (m_perf_sh.ContainsKey(blk))
                    state = m_perf_sh[blk];
                else
                {
                    state = new CmpCache_State();
                    m_perf_sh[blk] = state;
                }
            }
            else
                sh_hit = m_sh.probe(addr, out state);

            bool prv_excl = sh_hit ? (state.excl == node) : false;

            if (prv_hit)
                // we always update the timestamp on the private cache
                m_prv[node].update(addr, Simulator.CurrentRound);

         
			bool L1hit = false, L1upgr = false, L1ev = false, L1wb = false;
			bool L2access = false, L2hit = false, L2ev = false, L2wb = false, c2c = false;

            L1hit = prv_hit;
            L1upgr = L1hit && !prv_excl;
            L2hit = sh_hit;

            // ----------------- now, we execute one of four cases:
            //                   1a. present in private cache, with appropriate ownership.
            //                   1b. present in private cache, but not excl (for a write)
            //                   2. not present in private cache, but in shared cache.
            //                   3. not present in private or shared cache.
            //
            // in each case, we update functional state and generate the packet DAG as we go.

            if (prv_hit && (!write || prv_excl)) // CASE 1a: present in prv cache, have excl if write
            {
                // just set modified-bit in state, then we're done (no protocol interaction)
                if (write) state.modified = true;

				CmpCache_Txn txn = null; 
				txn_schedule (txn, cb); 
				do_stats(stats_active, node, L1hit, L1upgr, L1ev, L1wb,
					L2access, L2hit, L2ev, L2wb, c2c);
            }
            else if (prv_hit && write && !prv_excl) // CASE 1b: present in prv cache, need upgr
            {
				
				CmpCache_Txn txn = null;
                txn = new CmpCache_Txn();
                txn.node = node;
				txn.cb = cb;
				txn.qos_cb = qos_cb;

                // request packet
                CmpCache_Pkt req_pkt = add_ctl_pkt(txn, node, sh_slice, false, 1, false);
                CmpCache_Pkt done_pkt = null;

                // present in others?
                if (state.owners.others_set(node))
                {
                    done_pkt = do_inval(txn, state, req_pkt, node, addr);
                }
                else
                {
                    // not present in others, but we didn't have excl -- send empty grant
                    // (could happen if others have evicted and we are the only one left)
					done_pkt = add_ctl_pkt(txn, sh_slice, node, true, 2, false);
                    done_pkt.delay = m_shdelay;
                    add_dep(req_pkt, done_pkt);
                }

                state.owners.reset();
                state.owners.set(node);
                state.excl = node;
                state.modified = true;

				txn_schedule (txn, cb);
				do_stats(stats_active, node, L1hit, L1upgr, L1ev, L1wb,
					L2access, L2hit, L2ev, L2wb, c2c);
            }
            else if (!prv_hit && sh_hit) // CASE 2: not in prv cache, but in sh cache
            {	
				// by Xiyue: share cache access.
				L2_access (node, mshr, addr, sh_slice, write,  state, cb, qos_cb,
					stats_active, L1hit, L1upgr, L1ev, L1wb,
					L2access, L2hit, L2ev, L2wb, c2c, 0);
            }
            else if (!prv_hit && !sh_hit) // CASE 3: not in prv or shared cache
            {
				Mem_access (node, mshr, addr,  sh_slice, write, state, cb, qos_cb,
					stats_active, L1hit, L1upgr, L1ev, L1wb,
					L2access, L2hit, L2ev, L2wb, c2c, 0);
            }
			else // shouldn't happen.addr
                Debug.Assert(false);      
        }

		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *                                
		 *                               Shared Cache Access                                     *
		 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * 
		Packet in Step 1 & 2 are on the critical path. When invalidating shared copies, only the cloest node access is on the critical path. 

		Step 1: Send Reqeust
			PKT1: req_pkt (node -> sh_slice)

		Step 2: L2 coherence operation
			PKT6: done_pkt (virtual packet indicating L2 coherent operation completes)
			
			Case 2.1: request is write, has only an exclusive node (no shared copy)
				need to invalidate the exclusive node and forward the up-to-date copy to requester
				Invoke sequence: PKT1 -> PKT2 -> PKT3 -> PKT6
				PKT2: xfer_req (sh_slice -> exclusive node)
				PKT3: xfer_dat (exclusive -> node)


			Case 2.2: request is write, has sharers.
				need to invalidate all shared copies. The closest sharer need to forward the data to requester to make sure it is the latest copy.
				Invoke sequence: PKT1 -> PKT4 -> PKT5 -> PKT 6
				PKT4: invl_pkt (sh_slice -> sharer/owner)
				PKT5: invl_resp (sharer/owner -> node) this is data if sharer is the closest node to the requester, otherwise it is a control packet.

			Case 2.3: request is read, has only an exclusive node (no shared copy)
				need to downgrade the exclusive node to share/own. Also, writeback data to L2 and forward data to requester.
				Invoke sequence: PKT1 -> PKT2 -> PKT3 & PKT7 -> PKT6
				PKT7: wb_dat (exclusive node -> sh_slice)

			Case 2.4: request is read, has sharers.
				just get the copy and become a sharer.
				Invoke sequence: PKT1 -> PKT8 -> PKT9 -> PKT6
				PKT8: xfer_req (sh_slice -> closest sharer)
				PKT9: xfer_dat (closest sharer -> node)

			Case 2.5: Not in L1 (= no sharer or exclusive node)
				just get it from L2
				Invoke sequence: PKT1 -> PKT10 -> PKT6
				PKT10: dat_resp (sh_slice -> node)
				
		Step 3: insert data into L1, may trigger replacement
			Invoke Sequence: L6 -> PKT11 / PKT12
			PKT11: wb_pkt (node -> sh_slice) if state is Modified.
			PKT12: releast_pkt (node -> sh_slice) if state is not Modified.
		*/

		void L2_access (int node, int mshr, ulong addr, int sh_slice, bool write,  CmpCache_State state, Simulator.Ready cb, CPU.qos_stat_delegate qos_cb,
			bool stats_active, bool L1hit, bool L1upgr, bool L1ev, bool L1wb,
			bool L2access, bool L2hit, bool L2ev, bool L2wb, bool c2c, ulong throttleCycle)
		{
			/*
			// do not throttle L2 local access
			// TODO: add this when throttle at the l2 access/NI injection  
			if (Simulator.controller.tryInject (node) == false && Config.throttle_enable == true && (node != sh_slice) && Config.controller == ControllerType.THROTTLE_QOS) {
				#if DEBUG
				Console.WriteLine ("THROTTLED Req_addr = {1}, Node = {2} thrtCyc = {3} time = {0}", Simulator.CurrentRound, addr, node, throttleCycle + 1);
				#endif
				Simulator.Defer (delegate() {
					#if DEBUG
					Console.WriteLine ("RETRY to issue Req_addr = {1}, Node = {2} time = {0}", Simulator.CurrentRound, addr, node);
					#endif
					L2_access (node, mshr, addr, sh_slice, write,  state, cb, qos_cb,
						stats_active, L1hit, L1upgr, L1ev, L1wb,
						L2access, L2hit, L2ev, L2wb, c2c, throttleCycle + 1);
				}, Simulator.CurrentRound + 1);	
			} 
			else 
			{
				*/
				#if DEBUG
				//if (throttleCycle != 0)
				//Console.WriteLine ("ISSUE L2 Req_addr = {1}, Node = {2} time = {0}", Simulator.CurrentRound, addr, node);
				#endif
	
				CmpCache_Txn txn = null;
				txn = new CmpCache_Txn ();
				txn.node = node;
				txn.throttleCycle = txn.throttleCycle + throttleCycle;
				txn.qos_cb = qos_cb;
				txn.req_addr = addr;
				txn.cb = cb;
				txn.mshr = mshr;

				// update functional shared state
				if (!m_sh_perfect)
					m_sh.update (addr, Simulator.CurrentRound);

				// request packet
				CmpCache_Pkt req_pkt = add_ctl_pkt (txn, node, sh_slice, false, 3, true);
				CmpCache_Pkt done_pkt = null;

				if (state.owners.any_set ()) { // in other caches? - invoke 3 objects operations
					if (write) { // need to invalidate?
						if (state.excl != -1) { // someone else has exclusive -- c-to-c xfer
							c2c = true; // out-param

							CmpCache_Pkt xfer_req = add_ctl_pkt (txn, sh_slice, state.excl, false, 4, true); // by Xiyue: emulate directory control packet (from requester -> directory)
							CmpCache_Pkt xfer_dat = add_data_pkt (txn, state.excl, node, true, 5, true); // by Xiyue: the owner node forwards the up-to-date cache block to the requester
							done_pkt = xfer_dat;

							xfer_req.delay = m_shdelay;
							xfer_dat.delay = m_prvdelay;

							min_cycle (txn, m_shdelay);
							min_cycle (txn, m_prvdelay);

							add_dep (req_pkt, xfer_req);
							add_dep (xfer_req, xfer_dat);

							bool evicted_state;
							m_prv [state.excl].inval (addr, out evicted_state);
						} else { // others have it -- inval to all, c-to-c from closest
							int close = closest (node, state.owners);
							if (close != -1)
								c2c = true; // out-param

							done_pkt = do_inval (txn, state, req_pkt, node, addr, close);
						}

						// for a write, we need exclusive -- update state
						state.owners.reset ();
						state.owners.set (node);
						state.excl = node;
						state.modified = true;
					} else { // just a read -- joining sharer set, c-to-c from closest

						if (state.excl != -1) {
							CmpCache_Pkt xfer_req = add_ctl_pkt (txn, sh_slice, state.excl, false, 6, true);
							CmpCache_Pkt xfer_dat = add_data_pkt (txn, state.excl, node, true, 7, true);
							done_pkt = xfer_dat;

							c2c = true; // out-param

							xfer_req.delay = m_shdelay;
							xfer_dat.delay = m_prvdelay;

							min_cycle (txn, m_shdelay);
							min_cycle (txn, m_prvdelay);

							add_dep (req_pkt, xfer_req);
							add_dep (xfer_req, xfer_dat);

							// downgrade must also trigger writeback
							if (state.modified) {
								CmpCache_Pkt wb_dat = add_data_pkt (txn, state.excl, sh_slice, false, 8, true);
								add_dep (xfer_req, wb_dat);
								state.modified = false;
								state.sh_dirty = true;
							}
						} else {
							int close = closest (node, state.owners);
							if (close != -1)
								c2c = true; // out-param

							CmpCache_Pkt xfer_req = add_ctl_pkt (txn, sh_slice, close, false, 9, true);
							CmpCache_Pkt xfer_dat = add_data_pkt (txn, close, node, true, 10, true);
							done_pkt = xfer_dat;

							xfer_req.delay = m_shdelay;
							xfer_dat.delay = m_prvdelay;

							min_cycle (txn, m_shdelay);
							min_cycle (txn, m_prvdelay);

							add_dep (req_pkt, xfer_req);
							add_dep (xfer_req, xfer_dat);
						}

						state.owners.set (node);
						state.excl = -1;
					}
				} else {
					// not in other prv caches, need to get from shared slice
					L2access = true;

					CmpCache_Pkt dat_resp = add_data_pkt (txn, sh_slice, node, true, 11, true);
					done_pkt = dat_resp;

					add_dep (req_pkt, done_pkt);

					dat_resp.delay = m_shdelay;

					min_cycle (txn, m_shdelay);

					state.owners.reset ();
					state.owners.set (node);
					state.excl = node;
					state.modified = write;
				}

				// insert into private cache, get evicted block (if any)
				ulong evict_addr;
				bool evict_data;
				bool evicted = m_prv [node].insert (addr, true, out evict_addr, out evict_data, Simulator.CurrentRound);

				// add either a writeback or a release packet
				if (evicted) {
					L1ev = true;
					do_evict (txn, done_pkt, node, evict_addr, out L1wb);
				}

				do_stats(stats_active, node, L1hit, L1upgr, L1ev, L1wb,
					L2access, L2hit, L2ev, L2wb, c2c);
				txn_schedule (txn, cb);
			//}
		}

		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *                                
		 *                               Memory Access                                           *
		 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * 
		 Here, node is the requester. PKT1-5 are on the critical path. 
		  
		 Step 1: regular memory access
			Invoke sequence: PKT1 -> PKT2 -> PKT3 -> PKT4 -> PKT5
			PKT1: req_pkt (node -> sh_slice)
		 	PKT2: memreq_pkt (sh_slice -> mem_slice)
		 	PKT3: mem_access (mem_slice (node 0) -> virtual_node (node 0))
		 	PKT4: memresp_pkt (mem_slice (node 0) -> sh_slice) [Step 2]
		 	PKT5: resp_pkt (sh_slice -> node) [ Step 5 ]
			
		 Step 2: Insert fetched data into L2 (may trigger step 3, step 4, step 5)

		 Step 3: L2-eviction caused L1 eviction 
			PKT8: prv_evict_join (0 -> 0) a virtual pkt indicates private L1 eviction completes.
			
			case 2.1: L1_state = E or M
			has only one copy in exclusive state
			Invoke sequence: PKT4 -> PKT6 -> PKT7 -> PKT8
			PKT6: prv_invl (sh_slice -> exclusive_node)
			PKT7: prv_wb (exclusive_node -> sh_slice) can be data or ack depending on if state is Modified (data) or E (ack).


			case 2.2: L1_state = O (has owners in L1)
			has one or more sharers. multiple round-trip pkts are involved.
			Invoke sequece: PKT4 -> PKT6 -> PKT8
			PKT6: prv_invl (sh_slice -> owner)
			PKT7: prv_ack (owner -> sh_slice)

			case 2.3: L1_state = 0 (but has no owner. it is only in L2)
			Invoke sequece: PKT4 -> PKT8

		  Step 4: L2 eviction
		    Invoke sequence: PKT8 -> PKT9 -> PKT10
		    PKT9: mem_wb (sh_slice -> mem_slice (node 0))
		    PKT10: mem_wb_op (mem_slice (node 0) -> sh_slice)

		  Step 5: insert fetched data into L1 (may trigger step 6)

		  Step 6: L1 eviction caused by replacement
			Invoke sequence:  PKT5 -> PKT11; PKT5 -> PKT12
			PKT11: wb_pkt (node -> sh_slice) if L1_state = Modified
			PKT12: release_pkt (node -> sh_slice) if L1_state = other

		   ----------------------------------------------------------------
		   --------------       Possible Operations        ----------------
		   ----------------------------------------------------------------
		   PKT1 -> PKT2 -> PKT3 -> PKT4 -> PKT5  (regular memory access without any repercussion)
		   PKT1 -> PKT2 -> PKT3 -> PKT4 -> PKT5 -> PKT11 (memory access triggers L1 replacement and writeback)
		   PKT1 -> PKT2 -> PKT3 -> PKT4 -> PKT5 -> PKT12 (memory access triggers L1 replacement)
		   PKT1 -> PKT2 -> PKT3 -> PKT4 -> PKT6 -> PKT7 -> PKT8 -> PKT9 -> PKT10 (memory access triggers evctions in both L2 and L1, and L1 state is E or M) 
		   PKT1 -> PKT2 -> PKT3 -> PKT4 -> PKT6 -> PKT8 -> PKT9 -> PKT10 (memory access triggers evctions in both L2 and L1, and L1 state is O. Multiple round-trip packets may be involved.)
		   PKT1 -> PKT2 -> PKT3 -> PKT4 -> PKT8 -> PKT9 -> PKT10 (memory access triggers evctions in only L2, because data exists only in L2)
		   
		*/



		void Mem_access (int node, int mshr, ulong addr, int sh_slice, bool write, CmpCache_State state, Simulator.Ready cb,  CPU.qos_stat_delegate qos_cb,
			bool stats_active, bool L1hit, bool L1upgr, bool L1ev, bool L1wb, bool L2access, bool L2hit, bool L2ev, bool L2wb, bool c2c, ulong throttleCycle)
		{
			
			// here, we need to go to memory

			Debug.Assert(!m_sh_perfect);

			if (Simulator.controller.tryInject (node) == false && Config.throttle_enable == true && (node != sh_slice) && Config.controller == ControllerType.THROTTLE_QOS) {
				Simulator.Defer (delegate() {
					Mem_access (node, mshr, addr, sh_slice, write, state, cb, qos_cb,
						stats_active, L1hit, L1upgr, L1ev, L1wb,
						L2access, L2hit, L2ev, L2wb, c2c, throttleCycle + 1);
				}, Simulator.CurrentRound + 1);

			} else {

				#if DEBUG
				//if (throttleCycle != 0)
				Console.WriteLine ("ISSUE MEM Req_addr = {1}, Node = {2} time = {0}", Simulator.CurrentRound, addr, node);
				#endif

				CmpCache_Txn txn = null;
				txn = new CmpCache_Txn();
				txn.node = node;
				txn.throttleCycle = throttleCycle;
				txn.cb = cb;
				txn.qos_cb = qos_cb;
				txn.req_addr = addr;
				txn.mshr = mshr;

				/*
				L2access = true;
				L2ev = false;
				L2wb = false;
				L1ev = false;
				L1wb = false;
				*/

				// request packet
				CmpCache_Pkt req_pkt = add_ctl_pkt(txn, node, sh_slice, false, 17, true);

				// cache response packet
				CmpCache_Pkt resp_pkt = add_data_pkt(txn, sh_slice, node, true, 18, true);
				resp_pkt.delay = m_opdelay; // req already active -- just a pass-through op delay here

				// memory request packet
				int mem_slice = map_addr_mem(node, addr);
				CmpCache_Pkt memreq_pkt = add_ctl_pkt(txn, sh_slice, mem_slice, false, 19, true);
				memreq_pkt.delay = m_shdelay;

				// memory-access virtual node
				CmpCache_Pkt mem_access = add_ctl_pkt(txn, 0, 0, false, 20, false);
				mem_access.send = false;
				mem_access.mem = true;
				mem_access.mem_addr = addr;
				mem_access.mem_write = false; // cache-line fill
				mem_access.mem_requestor = node;

				// memory response packet
				CmpCache_Pkt memresp_pkt = add_data_pkt(txn, mem_slice, sh_slice, false, 21, true);

				// connect up the critical path first
				add_dep(req_pkt, memreq_pkt);
				add_dep(memreq_pkt, mem_access);
				add_dep(mem_access, memresp_pkt);
				add_dep(memresp_pkt, resp_pkt);

				// now, handle replacement in the shared cache...
				CmpCache_State new_state = new CmpCache_State();

				new_state.owners.reset();
				new_state.owners.set(node);
				new_state.excl = node;
				new_state.modified = write;
				new_state.sh_dirty = false;

				ulong sh_evicted_addr;
				CmpCache_State sh_evicted_state;
				bool evicted = m_sh.insert(addr, new_state, out sh_evicted_addr, out sh_evicted_state, Simulator.CurrentRound);

				if (evicted)
				{
					// shared-cache eviction (different from the private-cache evictions elsewhere):
					// we must evict any private-cache copies, because we model an inclusive hierarchy.

					L2ev = true;

					CmpCache_Pkt prv_evict_join = add_joinpt(txn, false, 22);

					if (sh_evicted_state.excl != -1) // evicted block lives only in one prv cache
					{
						// invalidate request to prv cache before sh cache does eviction
						CmpCache_Pkt prv_invl = add_ctl_pkt(txn, sh_slice, sh_evicted_state.excl, false, 23, false);
						add_dep(memresp_pkt, prv_invl);
						CmpCache_Pkt prv_wb;

						prv_invl.delay = m_opdelay;

						if (sh_evicted_state.modified)
						{
							// writeback
							prv_wb = add_data_pkt(txn, sh_evicted_state.excl, sh_slice, false, 24, false);
							prv_wb.delay = m_prvdelay;
							sh_evicted_state.sh_dirty = true;
						}
						else
						{
							// simple ACK
							prv_wb = add_ctl_pkt(txn, sh_evicted_state.excl, sh_slice, false, 25, false);
							prv_wb.delay = m_prvdelay;
						}

						add_dep(prv_invl, prv_wb);
						add_dep(prv_wb, prv_evict_join); // By Xiyue: how to wake up a remote packet in node 0?

						bool prv_evicted_dat;
						m_prv[sh_evicted_state.excl].inval(sh_evicted_addr, out prv_evicted_dat);
					}
					else if (sh_evicted_state.owners.any_set()) // evicted block has greater-than-one sharer set
					{
						for (int i = 0; i < m_N; i++)
							if (sh_evicted_state.owners.is_set(i))
							{
								CmpCache_Pkt prv_invl = add_ctl_pkt(txn, sh_slice, i, false, 26, false);
								CmpCache_Pkt prv_ack = add_ctl_pkt(txn, i, sh_slice, false, 27, false);

								prv_invl.delay = m_opdelay;
								prv_ack.delay = m_prvdelay;

								add_dep(memresp_pkt, prv_invl);
								add_dep(prv_invl, prv_ack);
								add_dep(prv_ack, prv_evict_join);

								bool prv_evicted_dat;
								m_prv[i].inval(sh_evicted_addr, out prv_evicted_dat);
							}
					}
					else // evicted block has no owners (was only in shared cache)
					{
						add_dep(memresp_pkt, prv_evict_join);
					}

					// now writeback to memory, if we were dirty
					if (sh_evicted_state.sh_dirty)
					{
						CmpCache_Pkt mem_wb = add_data_pkt(txn, sh_slice, mem_slice, false, 28, false);
						mem_wb.delay = m_opdelay;
						add_dep(prv_evict_join, mem_wb);
						CmpCache_Pkt mem_wb_op = add_ctl_pkt(txn, 0, 0, false, 29, false);
						mem_wb_op.send = false;
						mem_wb_op.mem = true;
						mem_wb_op.mem_addr = sh_evicted_addr;
						mem_wb_op.mem_write = true;
						mem_wb_op.mem_requestor = node;
						add_dep(mem_wb, mem_wb_op);
						L2wb = true;
					}
				}

				// ...and insert and handle replacement in the private cache
				ulong evict_addr;
				bool evict_data;
				bool prv_evicted = m_prv[node].insert(addr, true, out evict_addr, out evict_data, Simulator.CurrentRound);

				// add either a writeback or a release packet
				if (prv_evicted)
				{
					L1ev = true;
					do_evict(txn, resp_pkt, node, evict_addr, out L1wb);
				}

				do_stats(stats_active, node, L1hit, L1upgr, L1ev, L1wb,
					L2access, L2hit, L2ev, L2wb, c2c);
				txn_schedule (txn, cb);
			}
		}


		// End Xiyue

		// evict a block from given node, and construct either writeback or release packet.
		// updates functional state accordingly.
		void do_evict(CmpCache_Txn txn, CmpCache_Pkt init_dep, int node, ulong evict_addr, out bool wb)
		{
			ulong blk = evict_addr >> m_blkshift;
			int sh_slice = map_addr(node, evict_addr);

			CmpCache_State evicted_st;
			if (m_sh_perfect)
			{
				Debug.Assert(m_perf_sh.ContainsKey(blk));
				evicted_st = m_perf_sh[blk];
			}
			else
			{
				bool hit = m_sh.probe(evict_addr, out evicted_st);
				Debug.Assert(hit); // inclusive sh cache -- MUST be present in sh cache
			}

			if(evicted_st.excl == node && evicted_st.modified)
			{
				CmpCache_Pkt wb_pkt = add_data_pkt(txn, node, sh_slice, false, 12, false); // by Xiyue: eviction triggers a writeback
				wb_pkt.delay = m_opdelay; // pass-through delay: operation already in progress
				add_dep(init_dep, wb_pkt);
				min_cycle (txn, m_opdelay);

				evicted_st.owners.reset();
				evicted_st.excl = -1;
				evicted_st.sh_dirty = true;
				wb = true;
			}
			else
			{
				CmpCache_Pkt release_pkt = add_ctl_pkt(txn, node, sh_slice, false, 13, false);
				release_pkt.delay = m_opdelay;
				add_dep(init_dep, release_pkt);
				min_cycle (txn, m_opdelay);

				evicted_st.owners.unset(node);
				if (evicted_st.excl == node) evicted_st.excl = -1;
				wb = false;
			}

			if (m_sh_perfect && !evicted_st.owners.any_set())
				m_perf_sh.Remove(blk);
		}



        // construct a set of invalidation packets, all depending on init_dep, and
        // joining at a join-point that we return. Also invalidate the given addr
        // in the other prv caches.
        CmpCache_Pkt do_inval(CmpCache_Txn txn, CmpCache_State state, CmpCache_Pkt init_dep, int node, ulong addr)
        {
            return do_inval(txn, state, init_dep, node, addr, -1);
        }
        CmpCache_Pkt do_inval(CmpCache_Txn txn, CmpCache_State state, CmpCache_Pkt init_dep, int node, ulong addr, int c2c)
        {
            int sh_slice = map_addr(node, addr);

            // join-point (virtual packet). this is the completion point (DONE flag)
            CmpCache_Pkt invl_join = add_joinpt(txn, true);

            // invalidate from shared slice to each other owner
            for (int i = 0; i < m_N; i++)
                if (state.owners.is_set(i) && i != node)
                {
					bool critical = (c2c == i) ? true : false; // send invalidation to all other sharers
					CmpCache_Pkt invl_pkt = add_ctl_pkt(txn, sh_slice, i, false, 14, critical);
                    invl_pkt.delay = m_shdelay;
					
					// By Xiyue: every invalidation requires response?
					// Answer: No, only the node owning the copy should response.
                    CmpCache_Pkt invl_resp =
                        (c2c == i) ?
						add_data_pkt(txn, i, node, false, 15, true) :
                        add_ctl_pkt(txn, i, node, false, 16, false);
                    invl_resp.delay = m_prvdelay;

					min_cycle (txn, m_shdelay);
					min_cycle (txn, m_prvdelay);

                    add_dep(init_dep, invl_pkt);
                    add_dep(invl_pkt, invl_resp);
                    add_dep(invl_resp, invl_join);

                    // invalidate in this prv cache.
                    bool evicted_data;
                    m_prv[i].inval(addr, out evicted_data);
                }

            return invl_join;
        }

        ulong pkt_id = 0;

        CmpCache_Pkt _add_pkt(CmpCache_Txn txn, int from, int to, bool data, bool send, bool done)
        {
            Debug.Assert(to >= 0 && to < m_N);

            CmpCache_Pkt pkt = new CmpCache_Pkt();
            pkt.wakeup = new List<CmpCache_Pkt>();
            pkt.id = pkt_id++; // assign an unique id for each node
            pkt.from = from;
            pkt.to = to;
            pkt.txn = txn;

			pkt.flits = data ? m_datapkt_size : m_ctrlpkt_size; // get the packet size here.
            pkt.vc_class = 0; // gets filled in once DAG is complete

            pkt.done = done;	// by Xiyue: indicate this is the last packet associate with a txn
            pkt.send = send;

            pkt.deps = 0;
            pkt.delay = 0;
            pkt.mem_addr = 0;


            txn.n_pkts++;
            txn.n_pkts_remaining++;

            if (txn.pkts == null)
				txn.pkts = pkt;

            return pkt;
        }

		CmpCache_Pkt add_ctl_pkt(CmpCache_Txn txn, int from, int to, bool done)
		{
			return _add_pkt(txn, from, to, false, true, done);
		}

		CmpCache_Pkt add_data_pkt(CmpCache_Txn txn, int from, int to, bool done)
		{
			return _add_pkt(txn, from, to, true, true, done);
		}

		CmpCache_Pkt add_joinpt(CmpCache_Txn txn, bool done)
		{
			return _add_pkt(txn, 0, 0, false, false, done);
		}

		// by Xiyue:
		CmpCache_Pkt _add_pkt(CmpCache_Txn txn, int from, int to, bool data, bool send, bool done, int tier, bool critical)
		{
			Debug.Assert(to >= 0 && to < m_N);

			CmpCache_Pkt pkt = new CmpCache_Pkt();
			pkt.wakeup = new List<CmpCache_Pkt>();
			pkt.id = pkt_id++; // assign an unique id for each node
			pkt.from = from;
			pkt.to = to;
			pkt.txn = txn; 
			pkt.critical = critical;

			pkt.flits = data ? m_datapkt_size : m_ctrlpkt_size;
			pkt.vc_class = 0; // gets filled in once DAG is complete

			pkt.done = done; // indicate this is the last packet associate with a txn
			pkt.send = send; 
			pkt.tier = tier; 
			Simulator.stats.pkt_tier_count[tier].Add();

			pkt.deps = 0;
			pkt.delay = 0;
			pkt.mem_addr = 0;

			txn.n_pkts++;
			txn.n_pkts_remaining++;

			if (txn.pkts == null)
				txn.pkts = pkt;

			// by Xiyue
			/* Compute the minimal cycle needed to service this request */
			min_cycle (txn, from, to); 

			return pkt;
		}

		CmpCache_Pkt add_ctl_pkt(CmpCache_Txn txn, int from, int to, bool done, int tier, bool critical)
		{
			return _add_pkt(txn, from, to, false, true, done, tier, critical);
		}

		CmpCache_Pkt add_data_pkt(CmpCache_Txn txn, int from, int to, bool done, int tier, bool critical)
		{
			return _add_pkt(txn, from, to, true, true, done, tier, critical);
		}

		CmpCache_Pkt add_joinpt(CmpCache_Txn txn, bool done, int tier)
		{
			return _add_pkt(txn, 0, 0, false, false, done, tier, false);
		}

		void min_cycle(CmpCache_Txn txn, int from, int to)
		{
			txn.minimalCycle = txn.minimalCycle + min_cycle (from, to);
		}

		void min_cycle(CmpCache_Txn txn, ulong access_latency)
		{
			txn.minimalCycle = txn.minimalCycle + access_latency;
		}

		ulong min_cycle(int from, int to)
		{

			Debug.Assert (!Config.torus);
			 
			// only work for mesh for now

			int from_x, from_y, to_x, to_y;
			Coord.getXYfromID(from, out from_x, out from_y);
			Coord.getXYfromID (to, out to_x, out to_y);
			int distance_x, distance_y;
			distance_x = Math.Abs ((from_x-to_x));
			distance_y = Math.Abs ((from_y-to_y));
			int pkt_latency;
			pkt_latency = (distance_x + distance_y) * (Config.router.linkLatency+2); // TODO: +1 is the router latency. Must by parameterized later.
			return (ulong)pkt_latency;
		
		}
		// end Xiyue

        

        void add_dep(CmpCache_Pkt from, CmpCache_Pkt to)
        {
            from.wakeup.Add(to);
            to.deps++;
        }



		void txn_schedule (CmpCache_Txn txn, Simulator.Ready cb)
		{
			// now start the transaction, if one was needed
			if (txn != null)
			{

				assignVCclasses(txn.pkts);

				min_cycle (txn, m_prvdelay); // By Xiyue: not used currently. m_prvdelay takes L1 access into account

				// start running the protocol DAG. It may be an empty graph (for a silent upgr), in
				// which case the deferred start (after cache delay)
				// By Xiyue: schedule a network packet here.
				Simulator.Defer(delegate()
					{
						start_pkts(txn);
					}, Simulator.CurrentRound + m_prvdelay);
			}
			// no transaction -- just the cache access delay. schedule deferred callback.
			else
			{
				Simulator.Defer(cb, Simulator.CurrentRound + m_prvdelay); //by Xiyue: Defer() can be considered as a scheduler. cb is the cache access action.
			}
		}

        void start_pkts(CmpCache_Txn txn)
        {
            if (txn.n_pkts_remaining > 0)
                send_pkt(txn, txn.pkts);
            else
                txn.cb();  // TODO: by Xiyue: not known what's for.
        }

        void send_pkt(CmpCache_Txn txn, CmpCache_Pkt pkt)
        {
            if (pkt.delay > 0)
            {
                ulong due = Simulator.CurrentRound + pkt.delay;
                pkt.delay = 0;
                Simulator.Defer(delegate()
                        {
                        send_pkt(txn, pkt);
                        }, due);
            }
            else if (pkt.send)
            {
                send_noc(txn.node, pkt.from, pkt.to, pkt.flits,
                        delegate()
                        {
                        pkt_callback(txn, pkt);
					}, pkt.off_crit, pkt.vc_class, txn, pkt.critical);
            }
            else if (pkt.mem)
            {
                access_mem(pkt.mem_requestor, pkt.mem_addr, pkt.mem_write,
                        delegate()
                        {
                        pkt_callback(txn, pkt);
                        });
            }
            else
                pkt_callback(txn, pkt);
        }

		// By Xiyue:
		//   { Node::doStep()->Node::receivePacket()->CPU::receivePacket()->CmpCache::pkt_callback() }
		//   Upon receiving a network packet, a node will determine
		//   1) wake up a dependent packet and schedule a transaction
		//   2) call reqDone() if no dependent packet is found, indicating a completion of LD/ST request.
        void pkt_callback(CmpCache_Txn txn, CmpCache_Pkt pkt)
        {
            txn.n_pkts_remaining--;

			if (pkt.done) {
				txn.qos_cb (txn); // Must be called before txn.cb(); Otherwise, instr entry will be reset.
				txn.cb ();
			}

            foreach (CmpCache_Pkt dep in pkt.wakeup)
            {
                if (pkt.done || pkt.off_crit) dep.off_crit = true;
                dep.deps--;
                if (dep.deps == 0)
                    send_pkt(txn, dep);
            }
        }

		void send_noc(int reqNode, int from, int to, int flits, Simulator.Ready cb, bool off_crit, int vc, CmpCache_Txn txn, bool critical)  //by Xiyue: The actual injection entry point into NoC
        {
            int cl = off_crit ? 2 : // packet class (used for split queues): 0 = ctl, 1 = data, 2 = off-crit (writebacks)
                (flits > 1 ? 1 : 0);

			CachePacket p = new CachePacket(reqNode, from, to, flits, cl, vc, cb, txn, critical);
            Simulator.network.nodes[from].queuePacket(p);
        }

        void access_mem(int requestor, ulong addr, bool write, Simulator.Ready cb)
        {
            Request req = new Request(requestor, addr, write);

            int node = map_addr_mem(requestor, addr);
            Simulator.network.nodes[node].mem.access(req, cb);
        }


		void assignVCclasses_qos (CmpCache_Pkt root)
		{	/*
			workQ.Enqueue(root);
			while (workQ.Count > 0)
			{
				CmpCache_Pkt pkt = workQ.Dequeue();
				if (pkt.flits > 1) pkt.vc_class = Math.Max(4, pkt.vc_class); 

				int succ;
				if (pkt.send)
				{

				}
				else
				{
					succ = pkt.vc_class;
				}

				int succ = pkt.send ? pkt.vc_class + 1 : pkt.vc_class;
				foreach (CmpCache_Pkt s in pkt.wakeup)
				{
					int old = s.vc_class;
					s.vc_class = Math.Max(succ, s.vc_class);
					if (s.vc_class > old) workQ.Enqueue(s); // By Xiyue ???: Why????
				}
			}
			*/
		}


        private Queue<CmpCache_Pkt> workQ = new Queue<CmpCache_Pkt>(); // avoid alloc'ing this for each call
        void assignVCclasses(CmpCache_Pkt root)
        {
            // basic idea: we traverse the DAG using a work-list algorithm, assigning VC classes as follows:
            //  - any network packet node sets the VC of its successors to at least its own VC plus 1.
            //  - any data packet gets VC at least 4.
            //  - non-network-packet nodes carry VC numbers anyway to propagate dependence information.
            //  - VC classes start at 0 and increment as this algo runs.
            workQ.Enqueue(root);
            while (workQ.Count > 0)
            {
                CmpCache_Pkt pkt = workQ.Dequeue();
                if (pkt.flits > 1) pkt.vc_class = Math.Max(4, pkt.vc_class);

                int succ = pkt.send ? pkt.vc_class + 1 : pkt.vc_class;
                foreach (CmpCache_Pkt s in pkt.wakeup)
                {
                    int old = s.vc_class;
                    s.vc_class = Math.Max(succ, s.vc_class);
                    if (s.vc_class > old) workQ.Enqueue(s); // By Xiyue ???: Why????
                }
            }
        }
    }

}

