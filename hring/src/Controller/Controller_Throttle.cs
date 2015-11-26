//#define DEBUG

using System;
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
		private int m_slowest_core;
        private int m_fastest_core;
		private int m_philanthropic_core, m_philanthropic_core_old;
        private int m_consecutive_fair_counter;
		private int m_consecutive_unfair_counter;
		private string [] throttle_node;		

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

				m_lastCheckPoint[i] = 0;
				app_rank [i] = 0;
				app_type [i] = APP_TYPE.LATENCY_SEN;
				most_mem_inten [i] = false;
				enable_qos = false;
				max_rank = 0;
				m_slowest_core = 0;
                m_fastest_core = 0;
                m_consecutive_fair_counter = 0;
				m_consecutive_unfair_counter = 0;
			}
		}
        
        public void find_phil_core ()
        {
            ulong dist_slowest_old = ulong.MaxValue;
			double slowdown_old = double.MaxValue;
            for (int i = 0; i < Config.N; i++)
            {
                ulong dist_slowest = Simulator.distance(m_slowest_core, i); // absolute distances from the slowest core
                double slowdown = Simulator.stats.estimated_slowdown[i].Count;
                bool mem_inten = app_type [i] == APP_TYPE.THROUGHPUT_SEN;
                bool closet = (dist_slowest < dist_slowest_old);
                if (slowdown < slowdown_old && mem_inten && closet && String.Compare(throttle_node[i],"1")==0)
                {
                    m_philanthropic_core = i;
                    dist_slowest_old = dist_slowest;
					slowdown_old = slowdown;
                }    
            }
			if (m_consecutive_unfair_counter == 0) m_philanthropic_core_old = m_philanthropic_core;
        }

        public void throttle_up (int node)
        {
            if (mshr_quota [node] < Config.mshrs)
                mshr_quota [node] = mshr_quota [node] + 1;
        }
        
        public void throttle_down (int node)
        {
            if (mshr_quota [node] > Config.throt_min * Config.mshrs)
                mshr_quota [node] = mshr_quota [node] - 1;
        }

        public void throttle_all_up ()
		{
            for (int i = 0; i < Config.N; i++)
			{
				if (mshr_quota [i] < Config.mshrs)
                	mshr_quota [i] = mshr_quota [i] + 1;
			}

		}

		public void throttle_reset ()
		{
			throttle_node = Config.throttle_node.Split(',');
			m_philanthropic_core_old = -1;
			m_philanthropic_core = -1;
		}
		
		double unfairness_old = 0;

        public void throttle_ctrl ()
        {
            
            double max_sd = Simulator.stats.estimated_slowdown[m_slowest_core].Count;
            double min_sd = Simulator.stats.estimated_slowdown[m_fastest_core].Count;
            double unfairness = max_sd - min_sd;
			if (unfairness < 0)
            	throw new Exception("unfairness is negative.");
            
			find_phil_core ();
			
			double unfairness_delta = unfairness - unfairness_old;
			if (unfairness > Config.th_unfairness)
			{
				if (unfairness_delta >= Config.th_unfairness_delta)
                {
					throttle_up(m_slowest_core);
                    throttle_down(m_philanthropic_core_old);
                }
                else if (unfairness_delta > 0 && unfairness_delta < Config.th_unfairness_delta)
				{
					throttle_up(m_slowest_core);
                    throttle_down(m_philanthropic_core);
				}
				else
				{
					throttle_up(m_slowest_core);
					throttle_up(m_philanthropic_core_old);
					throttle_node[m_philanthropic_core_old] = "0";
					find_phil_core ();  // call again in case m_philanthropic_core = m_philanthropic_core_old
                    throttle_down(m_philanthropic_core);	
				}
				m_consecutive_unfair_counter = m_consecutive_unfair_counter + 1;
				m_consecutive_fair_counter = 0;
			}
			else
			{
				if (m_consecutive_fair_counter > Config.th_consecutive_fair)
				{
					throttle_all_up ();
					throttle_reset ();
				}
				m_consecutive_fair_counter = m_consecutive_fair_counter + 1;
				m_consecutive_unfair_counter = 0;
			}
			unfairness_old = unfairness;
			m_philanthropic_core_old = m_philanthropic_core;
        }

		public void ranking_app_global_1 ()
		{
			ulong[] app_index = new ulong[Config.N]; // construct a one-dimensional array, indicating the application ID
			double[] app_slowdown = new double[Config.N];
			for (int i = 0; i < Config.N; i++) 
			{
				Simulator.stats.app_rank [i].EndPeriod();
				app_index [i] = (ulong)i;
				//app_slowdown [i] = Simulator.stats.etimated_slowdown [i].ExpWhtMvAvg();
				app_slowdown [i] = Simulator.stats.estimated_slowdown [i].Count;
			}

			Array.Sort (app_slowdown, app_index);
			m_fastest_core = (int) app_index[0];

			double delta_sd = Config.slowdown_delta;
			ulong app_rank_mem = 0;
			ulong app_rank_non_mem = 0;
			double base_non_mem_sd = 0;
			double base_mem_sd = 0;
			double non_mem_count = 0;
			double mem_count = 0;
			for (int i=0; i<Config.N; i++) // remember, you are dealing with sorted array.
			{
				if (app_type [app_index [i]] == APP_TYPE.LATENCY_SEN)
				{
					if (non_mem_count == 0)
						base_non_mem_sd = app_slowdown[i];

					if (app_slowdown[i] > (1+(app_rank_non_mem+1)*delta_sd)*base_non_mem_sd)
						app_rank_non_mem ++;
					app_rank[app_index [i]] = app_rank_non_mem;
					non_mem_count ++;

				}
				else if (app_type [app_index [i]] == APP_TYPE.THROUGHPUT_SEN)
				{
					if (mem_count == 0)
						base_mem_sd = app_slowdown[i];
					
					if (app_slowdown[i] > (1+(app_rank_mem+1)*delta_sd)*base_mem_sd)
						app_rank_mem ++;
					app_rank[app_index [i]] = app_rank_mem;
					m_slowest_core = (int) app_index [i]; // will only keep the core id in the last iteration (slowest core). This core will be throttled down.
					mem_count ++;
				}
			}

			max_rank_non_mem = app_rank_non_mem;
			max_rank_mem = app_rank_mem;
			enable_qos_non_mem = (max_rank_non_mem > Config.enable_qos_non_mem_threshold) ? true : false;
			enable_qos_mem = (max_rank_mem > Config.enable_qos_mem_threshold) ? true : false;
			if (enable_qos_non_mem) Console.WriteLine("Enable qos_non_mem at {0}", Simulator.CurrentRound);
			if (enable_qos_mem) Console.WriteLine("Enable qos_mem at {0}", Simulator.CurrentRound);
		}


		public void ranking_app_global ()
		{
			ulong[] app_index = new ulong[Config.N]; // construct a one-dimensional array, indicating the application ID
			double[] app_slowdown = new double[Config.N];
			for (int i = 0; i < Config.N; i++) 
			{
				Simulator.stats.app_rank [i].EndPeriod();
				app_index [i] = (ulong)i;
				app_slowdown [i] = Simulator.stats.estimated_slowdown [i].ExpWhtMvAvg();
			}

			Array.Sort (app_slowdown, app_index);

			if ((app_slowdown[Config.N-1]-app_slowdown[0]) / app_slowdown[0]  > 0.10) {
				enable_qos = true;
				//Console.WriteLine("Enable qos at {0}", Simulator.CurrentRound);
			}
			else enable_qos = false;

			for (int i = 0; i < Config.N; i++)
			{
				int temp_reverse_rank = Array.IndexOf (app_index, (ulong)i);
				//app_rank [i] = (ulong)((temp_reverse_rank+Config.N-1) % Config.N);
				app_rank [i] = (ulong)temp_reverse_rank;

				//Simulator.stats.app_rank [i].Add(app_rank [i]);

			}
		}

		public void adjust_app_ranking ()
		{
			ulong adjust_rank = 0;
			for (int i = 0; i < Config.N; i++)
			{
				if (most_mem_inten[i] == true)
				{
					adjust_rank = app_rank[i];
					break;
				}
			}

			for (int i = 0; i < Config.N; i++)
			{
				if (app_rank[i] < adjust_rank) app_rank[i] = app_rank[i] + 1;
				else if (app_rank[i] == app_rank [i]) app_rank [i] = 0;
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


		public bool globalSlowdown ()
		{
			bool need_slowdown_qos = false;
			for (int i = 0; i < Config.N; i++) {
				double estimated_slowdown;
				ulong penalty_cycle = (ulong)Simulator.stats.non_overlap_penalty_period [i].Count;
				estimated_slowdown = (double)Config.slowdown_epoch / (Config.slowdown_epoch - penalty_cycle);
				need_slowdown_qos = need_slowdown_qos | (estimated_slowdown >= Config.target_slowdown [i]);
			}
			return need_slowdown_qos;
		}



		public override void doStep()
		{
			// track the nonoverlapped penalty
			double estimated_slowdown_period, estimated_slowdown;
			double xxx;
			if (Simulator.CurrentRound > (ulong)Config.warmup_cyc && Simulator.CurrentRound % Config.slowdown_epoch == 0 ) {

				for (int i = 0; i < Config.N; i++) 
				{
					ulong penalty_cycle = (ulong)Simulator.stats.non_overlap_penalty_period [i].Count;
					estimated_slowdown_period = (double)(Simulator.CurrentRound-m_lastCheckPoint [i])/(Simulator.CurrentRound-m_lastCheckPoint [i]-penalty_cycle);
					estimated_slowdown =  (double)Simulator.CurrentRound/(Simulator.CurrentRound-Simulator.stats.non_overlap_penalty [i].Count);
					
					//#if DEBUG
					Console.WriteLine ("at time {0}: Core {1} Slowdown rate is {2} ", Simulator.CurrentRound, i, estimated_slowdown_period);
					//#endif
					Simulator.stats.estimated_slowdown_period [i].Add (estimated_slowdown_period);
					Simulator.stats.estimated_slowdown [i].Add (estimated_slowdown);

					m_lastCheckPoint [i] = Simulator.CurrentRound;
				}

				double mpki_max = 0;
				//record mpki vals every 20k cycles
				for (int i = 0; i < Config.N; i++)
				{
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
				}

				double mpki_sum=0;
				double mpki=0;
				int jj = 0;
				for(int i=0;i<Config.N;i++)
				{
					mpki=MPKI[i];
					mpki_sum+=mpki;
					most_mem_inten[i] = false;
					if (MPKI[i] > mpki_max) 
					{
						mpki_max = MPKI[i];
						jj = i;
					}
					Simulator.stats.mpki_bysrc[i].Add(mpki);
					// Classify applications
					if (MPKI[i] >= Config.mpki_threshold) app_type[i] = APP_TYPE.THROUGHPUT_SEN;
					else app_type[i] = APP_TYPE.LATENCY_SEN;
				}

				Simulator.stats.total_sum_mpki.Add(mpki_sum);
				most_mem_inten[jj] = true;

				ranking_app_global_1 ();
				throttle_ctrl ();

				//adjust_app_ranking (); // skip the first epoch
				for(int i=0;i<Config.N;i++)
				{
					Simulator.stats.app_rank [i].Add(app_rank [i]);
					//reset
					Simulator.stats.estimated_slowdown_period [i].EndPeriod ();
					Simulator.stats.estimated_slowdown [i].EndPeriod ();
					Simulator.stats.insns_persrc_period [i].EndPeriod ();
					Simulator.stats.non_overlap_penalty_period [i].EndPeriod ();
					Simulator.stats.causeIntf [i].EndPeriod ();
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
