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

	public enum APP_TYPE {LATENCY_SEN, THROUGHPUT_SEN, STC, NSTC}; // latency sensitive = non-memory intensive; throughput_sensitive = memory intensive, Stall Time Critical, Non-Stall Time Critical

	public class Controller_QoSThrottle : Controller
	{

		private double [] throttle_rate = new double[Config.N];


		private double [] m_est_sd = new double[Config.N];
		private double [] m_est_sd_unsort = new double[Config.N];
		private double [] m_stc = new double[Config.N];
		private double [] m_stc_unsort = new double[Config.N];
		private ulong[] m_index_stc = new ulong[Config.N];
		private ulong[] m_index_sd = new ulong[Config.N];
		private double m_l1miss_avg;
		private double m_sd_avg;
		private double m_uf_target;

		public static ulong [] app_rank = new ulong[Config.N];
		public static APP_TYPE [] app_type = new APP_TYPE[Config.N];
		public static APP_TYPE [] fast_app_type = new APP_TYPE[Config.N];
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

		private int m_perf_opt_counter, m_fair_opt_counter, m_idle_counter, m_succ_meet_counter;
		private double m_currunfairness, m_oldunfairness;
		private double m_currperf, m_oldperf;
		private string [] throttle_node = new string[Config.N];	// to enable/disable the throttling mechanism 
		private double m_init_unfairness, m_init_perf;
		private bool m_throttle_enable;
		private int unthrottable_count;
		private THROTTLE_ACTION m_action;
	

		private double m_stc_max, m_stc_avg;
		private double m_fast_throttle_stc_threshold;
		private THROTTLE_ACTION [] m_actionQ = new THROTTLE_ACTION[Config.N];

		enum OPT_STATE {
			IDLE,
			PERFORMANCE_OPT,
			FAIR_OPT
		};

		enum THROTTLE_ACTION {
			NO_OP,
			UP,
			DOWN,
			RESET
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
				throttle_rate [i] = 0;

				throttle_node [i] = "1";
				m_est_sd [i] = 1;
				m_stc [i] = 0;
				m_stc_unsort[i] = 0;
				m_est_sd_unsort [i] = 0;

				// assign initial mshr quota
				mshr_quota [i] = Config.mshrs;
				mshr_best_sol [i] = Config.mshrs;
				app_rank [i] = 0;
				app_type [i] = APP_TYPE.LATENCY_SEN;
				fast_app_type [i] = APP_TYPE.STC;
				most_mem_inten [i] = false;
				pre_throt  [i] = false;
				bad_decision_counter [i] = 0;
				m_actionQ [i] = THROTTLE_ACTION.NO_OP;
				m_uf_target = 0;
			}

			m_opt_state = OPT_STATE.PERFORMANCE_OPT;

			max_rank = 0;
			m_slowest_core = 0;
            m_fastest_core = 0;
			m_perf_opt_counter = 0;
			m_fair_opt_counter = 0;
			m_succ_meet_counter = 0;
			m_init_unfairness = 0;
			m_init_perf = 0;
			m_epoch_counter = 0;
			m_idle_counter = 0;
			m_oldperf = 0;
			m_oldunfairness = 0;
			m_opt_counter = 0;
			m_throttle_enable = Config.throttle_enable;
			m_l1miss_avg = 0;
			m_sd_avg = 0;
			unthrottable_count = 0;
			m_action = THROTTLE_ACTION.NO_OP;
		}

		public override void doStep()
		{

			if (Simulator.CurrentRound > (ulong)Config.warmup_cyc && Simulator.CurrentRound % Config.slowdown_epoch == 0 ) {

				Console.WriteLine ("\n---- A new epoch starts from here. -----\n");

				Console.WriteLine ("@TIME = {0}", Simulator.CurrentRound);

				ComputeSD ();

				ComputePerformance ();

				ComputeUnfairness ();

				ComputeMPKI (); 

				ComputeSTC ();
							
				RankBySTC (); // use to mark unthrottled core

				RankBySD (); // use to select the throttled core
		
				if (Config.throttle_enable) {
					
					throttleFAST ();

					//ThrottleSTC ();
				}

				doStat();
			} // end if
		}	
		
		public void ComputeSD ()
		{	
			double max_sd = double.MinValue;
			double min_sd = double.MaxValue;
			double sum_sd = 0;

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
				sum_sd += m_est_sd [i];
				m_est_sd_unsort [i] = m_est_sd [i];
			} // end for

			m_sd_avg = sum_sd / Config.N;
		}

		public void ComputeMPKI ()
		{
			double mpki_max = 0;
			double mpki_sum=0;
			double l1miss_sum = 0;

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
				l1miss_sum += curr_L1misses [i];
			}
			m_l1miss_avg = l1miss_sum / Config.N;

			Simulator.stats.total_sum_mpki.Add(mpki_sum);
		}

		public void ComputeSTC ()
		{
			// NoC stall-time criticality = Slowdown / #_of_L1misses
			//   Approach 1: Reason to use the absolute number of L1misses:
			//      L1miss will be affected by the throttling mechanism, however, MPKI won't
			//   Approach 2: Use MPKI since it is more statble

			double stc_sum = 0;
			double count = 0;

			for (int i = 0; i < Config.N; i++) 
			{
				curr_L1misses[i] = L1misses[i]-prev_L1misses[i];
				prev_L1misses[i]=L1misses[i];
				if (curr_L1misses [i] > 0) {
					m_stc [i] = m_est_sd [i] / curr_L1misses [i];
					//m_stc [i] = m_est_sd[i] / MPKI[i];
					stc_sum += m_stc[i];
					count++;
				}
				else 
					m_stc [i] = double.MaxValue;
				m_stc_unsort [i] = m_stc [i];

			}

			m_stc_avg = stc_sum / count;
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
			for (int i = 0; i < Config.N; i++)
				app_rank [m_index_stc [i]] = (ulong) i;
			m_stc_max = m_stc [Config.N-1];
		}

		// sort from low to high slowdown
		public void RankBySD ()
		{
			for (int i = 0; i < Config.N; i++)
				m_index_sd [i] = (ulong)i;
			IComparer revSort =  new myReverseSort ();
			Array.Sort (m_est_sd, m_index_sd, revSort);
		}


		public bool badDecision ()
		{
			// return true for bad, otherwise good
			double sd_delta, uf_delta;
	
			sd_delta = m_currperf - m_oldperf;
			uf_delta = m_currunfairness - m_oldunfairness;
			Console.WriteLine ("sd_delta = {0:0.000}", sd_delta);
			Console.WriteLine ("uf_delta = {0:0.000}", uf_delta);

			// the average slowdown decrease of each node exceed the increased unfairness.
			// or
			// the unfairness reduction outweight the average increased slowdown at each node.
			if (Config.favor_performance == true) {
				if (sd_delta < 0) {
					Console.WriteLine ("Good deal!");
					return false; // the average of slowdown reduction of each core outweight the degradation of unfairness; Or simply both of them are improved.
				} else {
					Console.WriteLine ("Bad Idea! Rollback is needed.");
					return true;
				}
			} else {
				if (sd_delta / Config.N + uf_delta <= 0) {
					Console.WriteLine ("Good deal!");
					return false; // the average of slowdown reduction of each core outweight the degradation of unfairness; Or simply both of them are improved.
				} else {
					Console.WriteLine ("Bad Idea! Rollback is needed.");
					return true;
				}
			}
		}

		public void throttleFAST()
		{

			classifyApp();

			Adjust_opt_target ();

			throttleAction();



		}

		public void classifyApp()
		{
			
			// applications cannot have exact the same STC.
			// FAST will converge if the STC of each application stays within Config.fast_throttle_threshold % of avg stc.
			// Smaller (i.e. < 1) Config.fast_throttle_threshold is likely to prevent more application being throttling down and therefore improve system performance.
			// And vice versa.
			m_fast_throttle_stc_threshold = Config.fast_throttle_threshold * m_stc_avg;
			for (int i = 0; i < Config.N; i++)
			{
				// stall-time critical applications
				// a * m_fact_throttle_stc_threshold 
				if (m_stc_unsort [i] >= m_fast_throttle_stc_threshold)
					fast_app_type [i] = APP_TYPE.STC;			
				// non-stall-time critical applications
				else {
					fast_app_type [i] = APP_TYPE.NSTC;

				}
			}
		}


		public void throttleAction()
		{
			
			double unfairness_delta = m_est_sd [0] - m_est_sd [Config.N-1] ;
			m_throttle_enable = (unfairness_delta > Config.th_unfairness) ? true : false;

			switch (m_action) {
			case THROTTLE_ACTION.RESET:
				for (int i = 0; i < Config.N; i++) {
					m_actionQ [i] = THROTTLE_ACTION.RESET;
					throttle_rate [i] = 0;
				}
				break;
			case THROTTLE_ACTION.DOWN:
				for (int i = 0; i < Config.N; i++) {
					m_actionQ [i] = THROTTLE_ACTION.NO_OP;
					if (fast_app_type [i] == APP_TYPE.NSTC) {
						if (ThrottleDown (i))
							m_actionQ [i] = THROTTLE_ACTION.DOWN;
					}
				}
				break;
			case THROTTLE_ACTION.UP:
				for (int i = 0; i < Config.N; i++) {
					m_actionQ [i] = THROTTLE_ACTION.NO_OP;
					if (ThrottleUp(i))
						m_actionQ [i] = THROTTLE_ACTION.UP;
				}
				break;
	
				default: 
					for (int i = 0; i < Config.N; i++) 
						m_actionQ [i] = THROTTLE_ACTION.NO_OP;
				break;
			}

		}

		public void Adjust_opt_target ()
		{
			m_opt_counter++;

			// First epoch only set the initial UF target
			if (m_opt_counter == 1) {
				m_uf_target = m_currunfairness;
				m_opt_state = OPT_STATE.FAIR_OPT;
				return;
			}

			// if uf target meets
			if (m_currunfairness - m_uf_target < 0) {
				Console.WriteLine ("Meet unfairness target.");
				if (++m_succ_meet_counter > Config.succ_meet_fair) {
					m_uf_target = m_currunfairness - Config.uf_adjust;
					m_opt_state = OPT_STATE.PERFORMANCE_OPT;
					m_fair_opt_counter = 0;
					m_action = THROTTLE_ACTION.UP;
				} else {
					m_opt_state = OPT_STATE.FAIR_OPT;
					m_fair_opt_counter++;
					m_action = THROTTLE_ACTION.NO_OP;
				}
			} else {
			// uf target miss
				Console.WriteLine ("Miss unfairness target.");
				if (m_fair_opt_counter > Config.fair_keep_trying) { 
					m_uf_target = m_currunfairness - Config.uf_adjust;
					m_opt_state = OPT_STATE.PERFORMANCE_OPT;
					m_action = THROTTLE_ACTION.RESET;
					m_fair_opt_counter = 0;
					Console.WriteLine ("FAIR_OPT_COUNT reaches max value.");
				} else {
					m_fair_opt_counter++;
					m_action = THROTTLE_ACTION.DOWN;
					m_opt_state = OPT_STATE.FAIR_OPT;			
				}
				m_succ_meet_counter = 0;
			}

			Console.WriteLine ("Next state is {0}", m_opt_state.ToString());
			Console.WriteLine ("New UF target is {0:0.00}", m_uf_target);
				
		}
		public override bool tryInject(int node)
		{
			double prob = Simulator.rand.NextDouble ();
			if (prob >= throttle_rate [node])
				return true;
			else
				return false;
		}

	
	
		public bool CoidToss (int i)
		{
			int can_throttle = Config.N - unthrottable_count;
			int toss_val = Simulator.rand.Next (can_throttle);
			
			if (i < (Config.throt_prob_lv1 * Config.N) && toss_val < ((1-Config.throt_prob_lv1) * Config.N)) return true;
			else if (i < Config.throt_prob_lv2 * Config.N && toss_val < (1-Config.throt_prob_lv2) * Config.N) return true;
			//else if (toss_val < (1-Config.throt_prob_lv3) * Config.N) return true;
			else return false;
		}
			
		public bool ThrottleDown (int node)
		{
			if (throttle_rate [node] < 0.6)
				throttle_rate [node] = throttle_rate [node] + 0.3;
			else if (throttle_rate [node] < 0.8)
				throttle_rate [node] = throttle_rate [node] + 0.05;
			else if (throttle_rate [node] < 0.85)
				throttle_rate [node] = throttle_rate [node] + 0.01;
			else
				return false;
			return true;
		}

		public bool ThrottleUp (int node)
		{
			if (throttle_rate [node] < 0.6 && throttle_rate [node] > 0)
				throttle_rate [node] = Math.Max(throttle_rate [node] - 0.3, 0);
			else if (throttle_rate [node] < 0.8 && throttle_rate [node] > 0)
				throttle_rate [node] = Math.Max(throttle_rate [node] - 0.05, 0);
			else if (throttle_rate [node] < 0.85 && throttle_rate [node] > 0)
				throttle_rate [node] = Math.Max(throttle_rate [node] - 0.01, 0);
			else 
				return false;
			return true;
		}

		public void doStat()
		{
			unthrottable_count = 0;
			Console.WriteLine ("STC Threshold = {0,-8:0.00000}", m_fast_throttle_stc_threshold);
			Console.WriteLine ("SD AVG = {0,-8:0.00000}", m_sd_avg);
			Console.WriteLine ("ACTION    Core    TH      SD      STC       L1MPC   LI_miss");
			for (int i = 0; i < Config.N; i++) 
			{
				double miss_rate = curr_L1misses[i]/Config.slowdown_epoch;

				#if DEBUG
				Console.WriteLine ("{0,-10}{1,-8}{2, -8:0.00}{3, -8:0.00}{4, -10:0.00000}{5,-8:0.000}{6, -8}",
					m_actionQ[i].ToString(), i, throttle_rate[i], m_est_sd_unsort[i], m_stc_unsort[i], miss_rate, curr_L1misses[i]);
				#endif	
				
				Simulator.stats.app_stall_per_epoch.Add(Simulator.stats.non_overlap_penalty_period[i].Count);
				Simulator.stats.L1miss_persrc_period [i].Add(curr_L1misses[i]);
				Simulator.stats.mpki_bysrc[i].Add(MPKI[i]);
				Simulator.stats.estimated_slowdown [i].Add (m_est_sd_unsort[i]);
				Simulator.stats.noc_stc[i].Add(m_stc_unsort[i]);
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
			m_oldperf = m_currperf;
			m_oldunfairness = m_currunfairness;
			m_l1miss_avg = 0;
			m_sd_avg = 0;
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
