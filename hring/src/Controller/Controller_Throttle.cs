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


	public enum APP_TYPE {LATENCY_SEN, THROUGHPUT_SEN}; // latency sensitive = non-memory intensive; throughput_sensitive = memory intensive

	public class Controller_QoSThrottle : Controller
	{

		public static ulong [] app_rank = new ulong[Config.N];
		public static APP_TYPE [] app_type = new APP_TYPE[Config.N];
		public static bool [] most_mem_inten = new bool[Config.N];
		public static bool enable_qos, enable_qos_mem, enable_qos_non_mem;
		public static ulong max_rank, max_rank_mem, max_rank_non_mem;
		public static int [] mshr_quota = new int[Config.N];
		public bool [] rst_quota = new bool[Config.N];
		private int m_slowest_core;
        private int m_fastest_core;
		private int m_philanthropic_core, m_philanthropic_core_old;
		private ArrayList m_phil_core_candidate = new ArrayList ();
        private int m_consecutive_fair_counter;
		private int m_consecutive_unfair_counter;
		private string [] throttle_node;	
		private int m_least_stc;
		ulong[] app_index = new ulong[Config.N]; // Sorted. construct a one-dimensional array, indicating the application ID
		double[] app_stc = new double[Config.N]; // Sorted.	
		double[] app_sd = new double[Config.N];
		ulong[] app_index_stc = new ulong[Config.N];
		ulong[] app_index_sd = new ulong[Config.N];
		
		enum OPT_STATE {
			IDLE,
			PERFORMANCE_OPT,
			FAIR_OPT
		};
		
		OPT_STATE [] m_opt_state = new OPT_STATE[Config.N];
		double unfairness = 0;
		double unfairness_old = 0;
		private bool[] pre_throt = new bool[Config.N];

		double[] m_throttleRates = new double[Config.N];
		IPrioPktPool[] m_injPools = new IPrioPktPool[Config.N];
		double[] m_lastCheckPoint = new double[Config.N];

		private static double TH_OFF = 0.0;
		public Controller_QoSThrottle()
		{
			Console.WriteLine("init Controller_QoSThrottle");
			for (int i = 0; i < Config.N; i++)
			{
				//Throttle every single cycle,so only configure the throttle rate once.
				throttle_node=Config.throttle_node.Split(',');
				if(throttle_node.Length!=Config.N)
				{
					Console.WriteLine("Specified string {0} for throttling nodes do not match "+
					                  "with the number of nodes {1}",Config.throttle_node,Config.N);
					throw new Exception("Unmatched length");
				}	

				if(String.Compare(throttle_node[i],"1")==0)
					setThrottleRate(i, Config.sweep_th_rate);
				else
					setThrottleRate(i, TH_OFF);

				// assign initial mshr quota
				mshr_quota [i] = Config.mshrs;
				rst_quota[i] = false;

				m_lastCheckPoint[i] = 0;
				app_rank [i] = 0;
				app_type [i] = APP_TYPE.LATENCY_SEN;
				most_mem_inten [i] = false;
				pre_throt  [i] = false;
				m_opt_state [i] = OPT_STATE.PERFORMANCE_OPT;
			}
			enable_qos = false;
			max_rank = 0;
			m_slowest_core = 0;
            m_fastest_core = 0;
            m_consecutive_fair_counter = 0;
			m_consecutive_unfair_counter = 0;
		}
        
 
		
        public void throttle_up (int node)
		{
            if (mshr_quota [node] < Config.mshrs)
			{
                mshr_quota [node] = mshr_quota [node] + 1;
				Console.WriteLine ("Throttle Up Core {0} to {1}", node, mshr_quota[node]);
			}
        }
        
        public void throttle_down (int node)
        {
			if (mshr_quota [node] == Config.mshrs)
				mshr_quota [node] = (int)(Config.mshrs * 0.7);			
			else if (mshr_quota[node] > Config.throt_min*Config.mshrs && mshr_quota [node] != Config.mshrs)
				mshr_quota [node] = mshr_quota [node] - 1;
			else
				throttle_node[node] = "0";

			Console.WriteLine ("Throttle Down Core {0} to {1}", node, mshr_quota[node]);

        }

		public void throttle_up ()
		{
			for (int i = 0; i < Config.N; i++) {
				if (app_rank [i] == max_rank_mem && app_type [i] == APP_TYPE.THROUGHPUT_SEN && mshr_quota [i] < Config.mshrs) {
					mshr_quota [i] = mshr_quota [i] + 1;
					Console.WriteLine ("Throttle Up Core {0} to {1}", i, mshr_quota[i]);
				}
			}

		}
		
		int retain_phase = 0;
		public void throttle_down ()
		{
			// throttle all faster memory intensive app
			
			bool no_throttle = true;
			ulong throttle_rank = 0;
			while (no_throttle && throttle_rank < max_rank_mem) {
				Console.WriteLine ("Will throttle rank {0}", throttle_rank);
				for (int i = 0; i < Config.N; i++) {
					if (app_rank [i] == throttle_rank && app_type [i] == APP_TYPE.THROUGHPUT_SEN && mshr_quota [i] > Config.throt_min * Config.mshrs) {
						// Since this is more aggressive than just throttling a single node, we only throttle a fixed percentage each phase.
						mshr_quota [i] = mshr_quota [i] - 1;
						Console.WriteLine ("Throttle Down Core {0} to {1}", i, mshr_quota [i]);
						no_throttle = false;
					} else if (app_rank [i] == throttle_rank && app_type [i] == APP_TYPE.THROUGHPUT_SEN && mshr_quota [i] <= Config.throt_min * Config.mshrs) {
						Console.WriteLine ("Stop throttle down Core {0}", i);
						
					} 

					if (rst_quota [i] && mshr_quota [i] < Config.mshrs) { // prevent throttle an application too much
						mshr_quota [i] = Config.mshrs;
						rst_quota [i] = true;
					}		
				}			
				throttle_rank++;
			}
			if (no_throttle)
				retain_phase++;
			if (retain_phase == 5) {
				Console.WriteLine ("Reset quota!");
				for (int i = 0; i < Config.N; i++) {
					mshr_quota [i] = Config.mshrs;
				}
				retain_phase = 0;
			}			

		}


        public void throttle_all_up ()
		{
			for (int i = 0; i < Config.N; i++) {
				if (mshr_quota [i] < Config.mshrs) {
					mshr_quota [i] = mshr_quota [i] + 1;
					Console.WriteLine ("Throttle Up Core {0} to {1}", i, mshr_quota[i]);

				}
			}
			throttle_node = Config.throttle_node.Split(','); // use throttle up as triggering condition to reset the flag
		}

		public void throttle_reset ()
		{
			Console.WriteLine("MSHR is reset");
			throttle_node = Config.throttle_node.Split(',');
			for (int i = 0; i < Config.N; i++)
				mshr_quota [i] = Config.mshrs;
			m_consecutive_unfair_counter = 0;
		}
		
		
		
		public int throttle_down_possible ()
		{
			int throttled=0;
			rank_by_stc ();
			for (int i = 0; i < Config.N && throttled < Config.throt_app_count; i++) { 
				int pick = (int) app_index_stc[i];
				if (mshr_quota [pick] == Config.mshrs && String.Compare(throttle_node[pick],"1")==0)
				{
					mshr_quota [pick] = (int)(Config.mshrs * 0.7);
					throttled++;
					Console.WriteLine ("Throttle Down Core {0} to {1}", pick, mshr_quota[pick]);
				}			
				else if (mshr_quota[pick] > Config.throt_min*Config.mshrs && mshr_quota [pick] != Config.mshrs && String.Compare(throttle_node[pick],"1")==0)
				{		
					mshr_quota [pick] = mshr_quota [pick] - 1;
					throttled++;
					Console.WriteLine ("Throttle Down Core {0} to {1}", pick, mshr_quota[pick]);
				}
			}
			return throttled;

		}

		public void throttle_stc()
		{
		

			double max_sd = Simulator.stats.estimated_slowdown[m_slowest_core].LastPeriodValue;
            double min_sd = Simulator.stats.estimated_slowdown[m_fastest_core].LastPeriodValue;
            unfairness = max_sd - min_sd;
			if (unfairness < 0)
            	throw new Exception("unfairness is negative.");
			int throttled = -1;
			// throttle only the least stc critical app
			if (unfairness > Config.th_unfairness)
			{
				mark_unthrottled();
				throttled = throttle_down_possible ();
				m_consecutive_unfair_counter = m_consecutive_unfair_counter + 1;
				Console.WriteLine ("Unfair Phase: {0}", m_consecutive_unfair_counter);
				Simulator.stats.consec_fair.Add (m_consecutive_fair_counter);
				m_consecutive_fair_counter = 0;
			}
			else {
				throttle_all_up ();
				m_consecutive_fair_counter = m_consecutive_fair_counter + 1;
				Simulator.stats.consec_unfair.Add (m_consecutive_unfair_counter);
				Console.WriteLine ("Fair Phase: {0}", m_consecutive_fair_counter);
				m_consecutive_unfair_counter = 0;
			}
			if (m_consecutive_unfair_counter >= Config.consec_throt_max || (throttled != -1 && throttled < Config.throt_app_count))
				throttle_reset ();
			for (int i = 0; i < Config.N; i++)
				Simulator.stats.mshrs_credit [i].Add (Controller_QoSThrottle.mshr_quota [i]);
			unfairness_old = unfairness;
		}

    
		// sort from low stc to high stc
		public void rank_by_stc ()
		{
			for (int i = 0; i < Config.N; i++) {
				app_index_stc [i] = (ulong)i;
				app_stc [i] = Simulator.stats.noc_stc[i].LastPeriodValue;
			}
			Array.Sort (app_stc, app_index_stc);
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

		// sort from low to high slowdown
		public void rank_by_sd ()
		{
			for (int i = 0; i < Config.N; i++) {
				app_index_sd [i] = (ulong)i;
				app_sd [i] = Simulator.stats.estimated_slowdown [i].LastPeriodValue;
			}
			IComparer revSort =  new myReverseSort ();
			Array.Sort (app_sd, app_index_sd, revSort);
		}

		public void mark_unthrottled ()
		{
			int unthrottled=0;
			rank_by_sd ();
			for (int i = 0; i < Config.N && unthrottled < Config.opt_app_count; i++) { 
				throttle_node[app_index_sd[i]] = "0";
				unthrottled ++;
			}
		}
		
		public void OptimizePerformance ()
		{
			MarkNoThrottle();			
			
			int throttled=0;
			
			for (int i = 0; i < Config.N && throttled < Config.throt_app_count; i++) { 
				int pick = (int) app_index_stc[i];
				if (ThrottleDown (pick)) {
					pre_throt[pick] = true;
					throttled++;
					Console.WriteLine ("Throttle Down Core {0} to {1}", pick, mshr_quota[pick]);
				}
			}
			
			if (throttled < Config.throt_app_count)
				throttle_reset();			


		}
		
		public void MarkNoThrottle () 
		{
			if (m_opt_state == OPT_STATE.PERFORMANCE_OPT)
			{
				for (int i = 0; i < Config.N; i++){
					if (mshr_quota[i] - 1 <= Config.throt_min*Config.mshrs) {
						Console.WriteLine ("Reach minimum MSHR, mark core {0} UNTHROTTLE", i);
						throttle_node[i] = "0";
					}
					else if (unfairness - unfairness_old > 0 && pre_throt[i] == true) // previous decision is wrong
					{
						Console.WriteLine ("Previous decision is wrong, mark core {0} UNTHROTTLE", i);
						throttle_node[i] = "0"; // unthrottled core
					}
					pre_throt[i] = false;	// reset pre_throt here
				}
			}
			else if (m_opt_state == OPT_STATE.FAIR_OPT)
			{

			}

		}

		public bool ThrottleDown (int node)
		{
			bool throttled = false;
			if (mshr_quota [node] == Config.mshrs && String.Compare(throttle_node[node],"1")==0)
			{
				mshr_quota [node] = (int)(Config.mshrs * 0.7);
				throttled = true;
			}			
			else if (mshr_quota[node] > Config.throt_min*Config.mshrs && mshr_quota [node] != Config.mshrs && String.Compare(throttle_node[pick],"1")==0)
			{		
				mshr_quota [node] = mshr_quota [node] - 1;
				throttled = true;
			}
			return throttled;
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

		void setThrottleRate(int node, double rate)
		{
			m_throttleRates[node] = rate;
			#if DEBUG
			Console.WriteLine ("At time {0}, set node {1} throttling rate to be {2} ", Simulator.CurrentRound, node, rate);
			#endif
		}
			
		public override bool tryInject(int node)
		{
			if (m_throttleRates[node] > 0.0)
				return Simulator.rand.NextDouble() > m_throttleRates[node];
			else
				return true;
		}


		public override void doStep()
		{
			// track the nonoverlapped penalty
			double estimated_slowdown_period, estimated_slowdown;
			double stc;
			double mpki_max = 0;
			double mpki_sum=0;
			int high_mpki_app = 0; 
			double max_sd = double.MinValue;
			double min_sd = double.MaxValue;
			double min_stc = double.MaxValue;
			double unfairness;
			double sd_sum = 0;

			if (Simulator.CurrentRound > (ulong)Config.warmup_cyc && Simulator.CurrentRound % Config.slowdown_epoch == 0 ) {

				for (int i = 0; i < Config.N; i++) 
				{
					// compute the metrics
					ulong penalty_cycle = (ulong)Simulator.stats.non_overlap_penalty_period [i].Count;
					estimated_slowdown_period = (double)(Simulator.CurrentRound-m_lastCheckPoint [i]-Config.warmup_cyc)/((int)Simulator.CurrentRound-Config.warmup_cyc-m_lastCheckPoint [i]-penalty_cycle);
					estimated_slowdown =  (double)((int)Simulator.CurrentRound-Config.warmup_cyc)/((int)Simulator.CurrentRound-Config.warmup_cyc-Simulator.stats.non_overlap_penalty [i].Count);									
					sd_sum = sd_sum + estimated_slowdown;
					m_lastCheckPoint [i] = Simulator.CurrentRound;
					if (estimated_slowdown > max_sd) {
						m_slowest_core = i;
						max_sd = estimated_slowdown;
					}
					if (estimated_slowdown < min_sd) {
						m_fastest_core = i;
						min_sd = estimated_slowdown;
					}
					
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
					most_mem_inten[i] = false;  // TBD
					if (MPKI[i] > mpki_max) 
					{
						mpki_max = MPKI[i];
						high_mpki_app = i;
					}
					
					// Classify applications
					if (MPKI[i] >= Config.mpki_threshold) app_type[i] = APP_TYPE.THROUGHPUT_SEN;
					else app_type[i] = APP_TYPE.LATENCY_SEN;

					// compute noc stall time criticality
					// use L1miss in denominator. So L1miss will be affected by the throttling mechanism, however, MPKI won't
					if (L1misses[i]-prev_L1misses[i] > 0)
						stc = estimated_slowdown / (L1misses[i]-prev_L1misses[i]);
					else {
						stc = double.MaxValue;
					}
					if (stc < min_stc) {
						m_least_stc = i;
						min_stc = stc;
					}
				

					#if DEBUG
					Console.WriteLine ("at time {0,-10}: Core {1,-5} Slowdow {2, -5:0.00} MPKI {3, -6:0.00} STC {4, -5:0.0000}",
 						Simulator.CurrentRound, i, estimated_slowdown, MPKI[i], stc);
					#endif
					prev_L1misses[i]=L1misses[i];
					Simulator.stats.mpki_bysrc[i].Add(MPKI[i]);
					Simulator.stats.estimated_slowdown_period [i].Add (estimated_slowdown_period);
					Simulator.stats.estimated_slowdown [i].Add (estimated_slowdown);
					Simulator.stats.noc_stc[i].Add(stc);
					Simulator.stats.app_rank [i].Add(app_rank [i]);
					Simulator.stats.estimated_slowdown_period [i].EndPeriod ();
					Simulator.stats.estimated_slowdown [i].EndPeriod ();
					Simulator.stats.insns_persrc_period [i].EndPeriod ();
					Simulator.stats.non_overlap_penalty_period [i].EndPeriod ();
					Simulator.stats.causeIntf [i].EndPeriod ();
					Simulator.stats.noc_stc[i].EndPeriod();
					Simulator.stats.app_rank [i].EndPeriod ();				
				} // end for
				unfairness = Simulator.stats.estimated_slowdown[m_slowest_core].LastPeriodValue -
					 Simulator.stats.estimated_slowdown[m_fastest_core].LastPeriodValue;
				throttle_stc();
				Simulator.stats.unfairness.Add(unfairness);
				Simulator.stats.total_sum_mpki.Add(mpki_sum);
				
				//most_mem_inten[high_mpki_app] = true;  // TBD

				//ranking_app_global_1 ();
				//throttle_ctrl ();

				//adjust_app_ranking (); // skip the first epoch

			} // end if
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
