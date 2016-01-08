#define DEBUG

using System;
using System.Collections;
using System.Collections.Generic;

namespace ICSimulator
{
    class AveragingWindow
    {
        protected int len, ptr;
        protected double[] window;
        protected double sum;

        public AveragingWindow(int N)
        {
            len = N;
            window = new double[N];
            ptr = 0;
            sum = 0.0;
        }

        public virtual void accumulate(double t)
        {
            sum -= window[ptr];
            window[ptr] = t;
            sum += window[ptr];
            ptr = (ptr + 1) % len;
        }

        public double average()
        {
            return sum / (double)len;
        }
    }

    class AveragingWindow_Binary : AveragingWindow
    {
        public AveragingWindow_Binary(int N)
            : base(N)
        {
        }

        public void accumulate(bool t)
        {
            base.accumulate(t ? 1 : 0);
        }
    }

	public class myReverseSort : IComparer  {
		int IComparer.Compare( Object x, Object y )  {
			double v1 = (double) x;
			double v2 = (double) y;
			if (v1 < v2) return 1;
			if (v1 > v2) return -1;
			else return 0;
		}	
	}

	public enum APP_TYPE {LATENCY_SEN, THROUGHPUT_SEN}; // latency sensitive = non-memory intensive; throughput_sensitive = memory intensive

	public class Controller_QoSThrottle : Controller
	{

		private double [] m_est_sd = new double[Config.N];
		private double [] m_stc = new double[Config.N];
		private ulong[] m_index_stc = new ulong[Config.N];
		private ulong[] m_index_sd = new ulong[Config.N];


		public static ulong [] app_rank = new ulong[Config.N];
		public static APP_TYPE [] app_type = new APP_TYPE[Config.N];
		public static bool [] most_mem_inten = new bool[Config.N];
		public static bool enable_qos, enable_qos_mem, enable_qos_non_mem;
		public static ulong max_rank, max_rank_mem, max_rank_non_mem;
		public static int [] mshr_quota = new int[Config.N];

		public int [] mshr_best_sol = new int[Config.N];
		public int [] bad_decision_counter = new int[Config.N];
		private int m_slowest_core;
        private int m_fastest_core;
		private int m_epoch_counter;
		private int m_opt_counter;

		private int m_perf_opt_counter, m_fair_opt_counter, m_idle_counter;
		private double m_currunfairness, m_oldunfairness;
		private double m_currperf, m_oldperf;
		private string [] throttle_node = new string[Config.N];	// to enable/disable the throttling mechanism 
		private double m_init_unfairness, m_init_perf;
	
		
		enum OPT_STATE {
			IDLE,
			PERFORMANCE_OPT,
			FAIR_OPT
		};
		
		OPT_STATE m_opt_state;
		double unfairness_old = 0;
		private bool[] pre_throt = new bool[Config.N];

		IPrioPktPool[] m_injPools = new IPrioPktPool[Config.N];

		public Controller_QoSThrottle()
		{
			Console.WriteLine("init Controller_QoSThrottle");
			for (int i = 0; i < Config.N; i++)
			{
				throttle_node [i] = "1";
				m_est_sd [i] = 1;
				m_stc [i] = 0;


				// assign initial mshr quota
				mshr_quota [i] = Config.mshrs;
				mshr_best_sol [i] = Config.mshrs;
				app_rank [i] = 0;
				app_type [i] = APP_TYPE.LATENCY_SEN;
				most_mem_inten [i] = false;
				pre_throt  [i] = false;
				bad_decision_counter [i] = 0;
			}

			m_opt_state = OPT_STATE.PERFORMANCE_OPT;

			max_rank = 0;
			m_slowest_core = 0;
            m_fastest_core = 0;
			m_perf_opt_counter = 0;
			m_fair_opt_counter = 0;
			m_init_unfairness = 0;
			m_init_perf = 0;
			m_epoch_counter = 0;
			m_idle_counter = 0;
			m_oldperf = 0;
			m_oldunfairness = 0;
			m_opt_counter = 0;
		}

		public override void doStep()
		{

			if (Simulator.CurrentRound > (ulong)Config.warmup_cyc && Simulator.CurrentRound % Config.slowdown_epoch == 0 ) {

				ComputeSD ();

				ComputePerformance ();

				ComputeUnfairness ();

				ComputeMPKI (); 

				ComputeSTC ();

				RankBySTC (); // use to mark unthrottled core

				RankBySD (); // use to select the throttled core

				//throttle_stc();
				ThrottleSTC ();

				doStat();
				

			} // end if
		}	
		
		public void ComputeSD ()
		{	
			double max_sd = double.MinValue;
			double min_sd = double.MaxValue;

			for (int i = 0; i < Config.N; i++) 
			{
				// compute slowdown
				m_est_sd[i] =  (double)((int)Simulator.CurrentRound-Config.warmup_cyc)/((int)Simulator.CurrentRound-Config.warmup_cyc-Simulator.stats.non_overlap_penalty [i].Count);												
				// find the slowest and fastest core
				// slowest core will determine the unfairness of the system
				if (m_est_sd[i] > max_sd) {
					m_slowest_core = i;
					max_sd = m_est_sd[i];
				}
				if (m_est_sd[i] < min_sd) {
					m_fastest_core = i;
					min_sd = m_est_sd[i];
				}
			
				#if DEBUG
				Console.WriteLine ("at time {0,-10}: Core {1,-5} Slowdow {2, -5:0.00} L1misses {3, -6:0.00} STC {4, -5:0.0000}, MSHR {5, -5}",
					Simulator.CurrentRound, i, m_est_sd[i], curr_L1misses[i], m_stc[i], mshr_quota[i]);
				#endif					
			} // end for

		}

		public void ComputeMPKI ()
		{
			double mpki_max = 0;
			double mpki_sum=0;

			for (int i = 0; i < Config.N; i++) 
			{
				// compute MPKI
				prev_MPKI[i]=MPKI[i];
				if(num_ins_last_epoch[i]==0)
					MPKI[i]=((double)(L1misses[i]*1000))/(Simulator.stats.insns_persrc[i].Count);
				else
				{
					if(Simulator.stats.insns_persrc[i].Count-num_ins_last_epoch[i]>0)
						MPKI[i]=((double)(L1misses[i]*1000))/(Simulator.stats.insns_persrc[i].Count-num_ins_last_epoch[i]);
					else if(Simulator.stats.insns_persrc[i].Count-num_ins_last_epoch[i]==0)
						MPKI[i]=0;
					else
						throw new Exception("MPKI error!");
				}
				mpki_sum+=MPKI[i];
				most_mem_inten[i] = false;  
				if (MPKI[i] > mpki_max) 
					mpki_max = MPKI[i];
				
			}
			Simulator.stats.total_sum_mpki.Add(mpki_sum);
		}

		public void ComputeSTC ()
		{
			// NoC stall-time criticality = Slowdown / #_of_L1misses
			//   Approach 1: Reason to use the absolute number of L1misses:
			//      L1miss will be affected by the throttling mechanism, however, MPKI won't
			//   Approach 2: Use MPKI since it is more statble
	
			for (int i = 0; i < Config.N; i++) 
			{
				curr_L1misses[i] = L1misses[i]-prev_L1misses[i];
				prev_L1misses[i]=L1misses[i];
				if (curr_L1misses[i] > 0)
					m_stc [i] = m_est_sd[i] / curr_L1misses[i];
					//m_stc [i] = m_est_sd[i] / MPKI[i];
				else 
					m_stc [i] = double.MaxValue;
			}
		}

		public void ComputePerformance ()
		{
			m_currperf = 0;
			// performance is the sum of slowdown of each core
			for (int i = 0; i < Config.N; i++)
				m_currperf = m_currperf + m_est_sd[i];
			Console.WriteLine("Current Perf is {0:0.000}", m_currperf);
		}

		public void ComputeUnfairness ()
		{
			m_currunfairness = m_est_sd[m_slowest_core];	
			Console.WriteLine("Current Unfairness is {0:0.000}", m_currunfairness);
		}

		// sort from low stc to high stc
		public void RankBySTC ()
		{
			for (int i = 0; i < Config.N; i++)
				m_index_stc [i] = (ulong)i;		
			Array.Sort (m_stc, m_index_stc);
		}

		// sort from low to high slowdown
		public void RankBySD ()
		{
			for (int i = 0; i < Config.N; i++)
				m_index_sd [i] = (ulong)i;
			IComparer revSort =  new myReverseSort ();
			Array.Sort (m_est_sd, m_index_sd, revSort);
		}

		public void ThrottleSTC ()
		{
			double sd_delta, uf_delta;

		
			sd_delta = m_currperf - m_oldperf;
			uf_delta = m_currunfairness - m_oldunfairness;
			Console.WriteLine ("sd_delta = {0:0.000}", sd_delta);
			Console.WriteLine ("uf_delta = {0:0.000}", uf_delta);

			if (sd_delta > 0 || uf_delta > 0) {
				// previous iteration made a bad decision
				SetBestSol ();
				MarkBadDecision ();
			} 
			else 
				AddBestSol (); // previous iteration made a wise decision
			ThrottleUpBySDRank ();
			PickNewThrtDwn ();
			m_opt_counter++;			
			ThrottleFlagReset ();
		

			if (m_opt_counter % Config.th_bad_rst_counter == 0)
				ThrottleCounterReset();
			
			m_oldperf = m_currperf;
			m_oldunfairness = m_currunfairness;
			
		}
		
		public void SetBestSol ()
		{
			Console.WriteLine("Use the best solution so far.");
			for (int i = 0; i < Config.N; i++)
				mshr_quota[i] = mshr_best_sol[i];
		}

		public void MarkBadDecision ()
		{
			for (int i = 0; i < Config.N; i++) {
				if (pre_throt [i] == true) {
					bad_decision_counter [i]++;
					Console.WriteLine ("Previous decision sucks. Increase the bad_decision_counter {0} to {1}", i, bad_decision_counter[i]);
				}
			}
		}

		public void AddBestSol ()
		{
			Console.WriteLine("This is the best solution so far.");
			for (int i = 0; i < Config.N; i++)
				mshr_best_sol[i] = mshr_quota[i];
		}


		public void ThrottleUpBySDRank ()
		{
			int unthrottled = -1;
			int pick = -1;
			for (int i = 0; i < Config.N && unthrottled < Config.thrt_up_slow_app - 1; i++) { 
				pick = (int)m_index_sd [i];
				throttle_node [pick] = "0";
				Console.WriteLine ("Core {0} is SLOW and marked unthrottable.", pick);
				unthrottled++;
				ThrottleUp (pick);
			}
			for (int i = 0; i < Config.N; i++) {
				double miss_rate = curr_L1misses [i] / Config.slowdown_epoch;
				if (miss_rate < Config.curr_L1miss_threshold) {
					throttle_node [i] = "0";
					Console.WriteLine ("Core {0} miss rate is {1} which is LIGHT and marked unthrottable.", i, miss_rate);
					ThrottleUp (i);
				}
			}

		}
		
		public void PickNewThrtDwn ()
		{
			int throttled = -1;
			MarkUnthrottled_v2();
			for (int i = 0; i < Config.N; i++) 
				pre_throt [i] = false;
			for (int i = 0; i < Config.N && throttled < Config.thrt_down_stc_app-1; i++) {
				if (CoidToss (i) == false) continue; 
 				int pick = (int)m_index_stc [i]; 
				if (ThrottleDown (pick)) { 
					pre_throt[pick] = true;
					throttled ++;
					Console.WriteLine ("Throttle Down Core {0} to {1}", pick, mshr_quota[pick]);
				}
			}
		}
		
		public bool CoidToss (int i)
		{
			int toss_val = Simulator.rand.Next (Config.N);
			
			if (i < (Config.throt_prob_lv1 * Config.N) && toss_val < ((1-Config.throt_prob_lv1) * Config.N)) return true;
			else if (i < Config.throt_prob_lv2 * Config.N && toss_val < (1-Config.throt_prob_lv2) * Config.N) return true;
			else if (toss_val < (1-Config.throt_prob_lv3) * Config.N) return true;
			else return false;
		}

		public void ThrottleFlagReset ()
		{
			for (int i = 0; i < Config.N; i++)
				throttle_node [i] = "1";
		}


		public void ThrottleCounterReset ()
		{
			Console.WriteLine("Reset bad decision counter");
			for (int i = 0; i < Config.N; i++)
				bad_decision_counter[i] = 0;
		}


		public void MarkUnthrottled_v2 ()
		{
			// mark the slowest core and the all core with MPI < MPI_threshold (light apps)
			for (int i = 0; i < Config.N; i++) {
				if ((mshr_quota[i] - 1 ) < Config.throt_min*Config.mshrs || bad_decision_counter[i] >= Config.th_bad_dec_counter) {
					Console.WriteLine("Core {0} is unthrottable.", i);
					throttle_node [i] = "0";
				}
			}
		}		

		public bool ThrottleDown (int node)
		{
			bool throttled = false;
			if (mshr_quota [node] >= Config.mshrs * 0.7 && String.Compare(throttle_node[node],"1")==0)
			{
				mshr_quota [node] = (int)(Config.mshrs * 0.7);
				throttled = true;
			}			
			else if (mshr_quota[node] > Config.throt_min*Config.mshrs && mshr_quota [node] != Config.mshrs && String.Compare(throttle_node[node],"1")==0)
			{		
				mshr_quota [node] = mshr_quota [node] - 1;
				throttled = true;
			}
			return throttled;
		}

		public void ThrottleUp (int pick)
		{
			if (mshr_quota [pick] < Config.mshrs) {
				//mshr_quota [pick] = mshr_quota [pick] + 1;
				mshr_quota [pick] = Config.mshrs;
				mshr_best_sol[pick] = mshr_quota[pick];
				//Console.WriteLine("Core {0} is throttled UP to {1} because it is too slow.", pick, mshr_quota[pick]);			
			}
		}

		public void doStat()
		{
			for (int i = 0; i < Config.N; i++) 
			{
				Simulator.stats.L1miss_persrc_period [i].Add(curr_L1misses[i]);
				Simulator.stats.mpki_bysrc[i].Add(MPKI[i]);
				Simulator.stats.estimated_slowdown [i].Add (m_est_sd[i]);
				Simulator.stats.noc_stc[i].Add(m_stc[i]);
				Simulator.stats.app_rank [i].Add(app_rank [i]);
				Simulator.stats.L1miss_persrc_period [i].EndPeriod();
				Simulator.stats.estimated_slowdown [i].EndPeriod ();
				Simulator.stats.insns_persrc_period [i].EndPeriod ();
				Simulator.stats.non_overlap_penalty_period [i].EndPeriod ();
				Simulator.stats.causeIntf [i].EndPeriod ();
				Simulator.stats.noc_stc[i].EndPeriod();
				Simulator.stats.app_rank [i].EndPeriod ();	
				Simulator.stats.mshrs_credit [i].Add (Controller_QoSThrottle.mshr_quota [i]);
			}

		}

		public override void setInjPool(int node, IPrioPktPool pool)
		{
			m_injPools[node] = pool;
			pool.setNodeId(node);
		}

		public override IPrioPktPool newPrioPktPool(int node)
		{
			return MultiQThrottlePktPool.construct();
		}		




		/// <summary>
		/// 	throttle_stc() 
		/// </summary>		

		public void throttle_stc ()
		{
			m_currunfairness = m_est_sd [m_slowest_core];	
			ComputePerformance ();
			RankBySD (); // use to mark unthrottled core
			RankBySTC (); 
			if (m_epoch_counter == 0)
				m_init_perf = m_currperf;
			if (m_opt_state == OPT_STATE.PERFORMANCE_OPT) {
				//if (m_perf_opt_counter == Config.opt_perf_bound || m_currunfairness >= m_budget_unfairness) {
				if (m_perf_opt_counter == Config.opt_perf_bound) {
					//m_init_unfairness = m_currunfairness;
					//m_budget_perf = m_currperf + (m_init_perf - m_currperf) * Config.perf_budget_factor;
					//m_budget_perf = m_currperf * 1.05;
					//throttle_reset ();
					//if (m_budget_perf < m_currperf)
					//	m_opt_state = OPT_STATE.IDLE;
					//else
						m_opt_state = OPT_STATE.FAIR_OPT;
					m_perf_opt_counter = 0;
				} else {
					Optimize ();
					Simulator.stats.opt_perf.Add(1);
					Console.WriteLine ("Optimize Performance epoch: {0}", Simulator.stats.opt_perf.Count);
					m_perf_opt_counter++;
				}	
			} else if (m_opt_state == OPT_STATE.FAIR_OPT) {
				//if (m_fair_opt_counter == Config.opt_fair_bound || m_currperf >= m_budget_perf) {
				if (m_fair_opt_counter == Config.opt_fair_bound) {
					//m_init_perf = m_currperf;
					//m_budget_unfairness = m_currunfairness + (m_init_unfairness - m_currunfairness) * Config.fair_budget_factor;
					//throttle_reset ();
					//if (m_budget_unfairness < m_currunfairness)
					//	m_opt_state = OPT_STATE.IDLE;
					//else
						m_opt_state = OPT_STATE.PERFORMANCE_OPT;
					m_fair_opt_counter = 0;
				} else {
					Optimize ();
					Simulator.stats.opt_fair.Add(1);
					Console.WriteLine ("Optimize Fairness epoch: {0}", Simulator.stats.opt_fair.Count);
					m_fair_opt_counter++;
				}
				
			} else if (m_opt_state == OPT_STATE.IDLE) {
				if (m_currperf - m_init_perf > Config.th_init_perf_loss) {
					m_opt_state = OPT_STATE.PERFORMANCE_OPT;
					m_init_perf = m_currperf;
				} else if (m_currunfairness - m_init_unfairness > Config.th_init_fair_loss) {
					m_opt_state = OPT_STATE.FAIR_OPT;
					m_init_unfairness = m_currunfairness;
				}
				m_idle_counter++;
				Console.WriteLine("Stay idle epoch: {0}", m_idle_counter);
			}

			m_epoch_counter++;
			unfairness_old = m_currunfairness;
			m_oldperf = m_currperf;
			// just log the mshr quota
			for (int i = 0; i < Config.N; i++)
				Simulator.stats.mshrs_credit [i].Add (Controller_QoSThrottle.mshr_quota [i]);
			
		}

		public void throttle_reset ()
		{
			Console.WriteLine("MSHR is reset");
			for (int i = 0; i < Config.N; i++) {
				mshr_quota [i] = Config.mshrs;
				throttle_node[i] = "1";
			}
		}
    	
		
		public void ClassifyAPP ()
		{
			for (int i = 0; i < Config.N; i++) 
				// Classify applications
				if (MPKI[i] >= Config.mpki_threshold) app_type[i] = APP_TYPE.THROUGHPUT_SEN;
				else app_type[i] = APP_TYPE.LATENCY_SEN;

		}

		public void Optimize ()
		{
			MarkNoThrottle();			
			int throttled=0;
			for (int i = 0; i < Config.N && throttled < Config.throt_app_count; i++) {
				if (CoidToss (i) == false) continue; 
 				int pick = (int)m_index_stc [i]; 
				if (ThrottleDown (pick)) { 
					pre_throt[pick] = true;
					throttled++;
					Console.WriteLine ("Throttle Down Core {0} to {1}", pick, mshr_quota[pick]);
				}
			}
		}
		
		public void MarkNoThrottle ()
		{
			if (m_opt_state == OPT_STATE.PERFORMANCE_OPT) {
				for (int i = 0; i < Config.N; i++) {
					if (mshr_quota [i] - 1 <= Config.throt_min * Config.mshrs) {
						Console.WriteLine ("Reach minimum MSHR, mark core {0} UNTHROTTLE", i);
						throttle_node [i] = "0";
					} 
					else if (m_currperf - m_oldperf > 0 && pre_throt[i] == true && mshr_quota[i] < Config.mshrs)
					{
						mshr_quota [i] = mshr_quota[i] + 1; // rollback
						Console.WriteLine ("Previous decision is wrong, throttle up core {0} to {1}", i, mshr_quota[i]);
					}
					pre_throt [i] = false;	// reset pre_throt here
				}
			} else if (m_opt_state == OPT_STATE.FAIR_OPT) {
				int unthrottled=0;
				int pick = -1;
				for (int i = 0; i < Config.N && unthrottled < Config.unthrot_app_count; i++) { 
					pick = (int)m_index_sd[i];
					throttle_node[pick] = "0";
					unthrottled ++;
					if (mshr_quota[pick] < Config.mshrs)
						mshr_quota [pick] = mshr_quota[pick] + 1;
					Console.WriteLine("Core {0} is protected and throttled up to {1} because it is too slow.", pick, mshr_quota[pick]);
				}
				for (int i = 0; i < Config.N; i++) {
					if (mshr_quota [i] - 1 <= Config.throt_min * Config.mshrs) {
						Console.WriteLine ("Reach minimum MSHR, mark core {0} UNTHROTTLE", i);
						throttle_node [i] = "0";
					}
					else if (m_currunfairness - unfairness_old > 0 && pre_throt [i] == true && mshr_quota[i] < Config.mshrs) { // previous decision is wrong
						mshr_quota [i] = mshr_quota[i] + 1; // rollback
						Console.WriteLine ("Previous decision is wrong, throttle up core {0} to {1}", i, mshr_quota[i]);
					}
					pre_throt [i] = false;	// reset pre_throt here
				}				
			}
		}							
	}

    public class Controller_Throttle : Controller_ClassicBLESS
    {
        double[] m_throttleRates = new double[Config.N];
        IPrioPktPool[] m_injPools = new IPrioPktPool[Config.N];
        bool[] m_starved = new bool[Config.N];

        AveragingWindow_Binary[] avg_starve =
            new AveragingWindow_Binary[Config.N];
        AveragingWindow[] avg_qlen =
            new AveragingWindow[Config.N];

        public Controller_Throttle()
        {
            Console.WriteLine("init");
            for (int i = 0; i < Config.N; i++)
			{
				avg_starve[i] = new AveragingWindow_Binary(Config.throttle_averaging_window);
                avg_qlen[i] = new AveragingWindow(Config.throttle_averaging_window);
            }
        }

        void setThrottleRate(int node, double rate)
        {
            m_throttleRates[node] = rate;
        }

        public override IPrioPktPool newPrioPktPool(int node)
        {
            return MultiQThrottlePktPool.construct();
        }

        // true to allow injection, false to block (throttle)
        public override bool tryInject(int node)
        {
            if (m_throttleRates[node] > 0.0)
                return Simulator.rand.NextDouble() > m_throttleRates[node];
            else
                return true;
        }

        public override void setInjPool(int node, IPrioPktPool pool)
        {
            m_injPools[node] = pool;
            pool.setNodeId(node);
        }


        public override void reportStarve(int node)
        {
            m_starved[node] = true;
        }

        public override void doStep()
        {
            for (int i = 0; i < Config.N; i++)
            {
                int qlen = Simulator.network.nodes[i].RequestQueueLen;
                avg_starve[i].accumulate(m_starved[i]);
                avg_qlen[i].accumulate(qlen);
                m_starved[i] = false;
            }

            if (Simulator.CurrentRound > 0 &&
                    (Simulator.CurrentRound % (ulong)Config.throttle_epoch) == 0)
                setThrottling();
        }

        void setThrottling()
        {
            // find the average IPF
            double avg = 0.0;
            for (int i = 0; i < Config.N; i++)
                avg += avg_qlen[i].average();
            avg /= Config.N;

            // determine whether any node is congested
            bool congested = false;
            for (int i = 0; i < Config.N; i++)
                if (avg_starve[i].average() > Config.srate_thresh)
                {
                    congested = true;
                    break;
                }

#if DEBUG
            Console.WriteLine("throttle: cycle {0} congested {1}\n---",
                   Simulator.CurrentRound, congested ? 1 : 0);

            Console.WriteLine("avg qlen is {0}", avg);
#endif

            for (int i = 0; i < Config.N; i++)
                if (congested && avg_qlen[i].average() > avg)
                {
                    setThrottleRate(i, Math.Min(Config.throt_max, Config.throt_min+Config.throt_scale * avg_qlen[i].average()));
#if DEBUG
                    Console.WriteLine("node {0} qlen {1} rate {2}", i,
                           avg_qlen[i].average(),
                           Math.Min(Config.throt_max, Config.throt_min+Config.throt_scale * avg_qlen[i].average()));
#endif
                }
                else
                {
                    setThrottleRate(i, 0.0);
#if DEBUG
                    Console.WriteLine("node {0} qlen {1} rate 0.0", i, avg_qlen[i].average());
#endif
                }
#if DEBUG
            Console.WriteLine("---\n");
#endif
        }
    }
}
