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



	public class Controller_QoSThrottle : Controller
	{

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
				string [] throttle_node=Config.throttle_node.Split(',');
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

				m_lastCheckPoint[i] = 0;

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
				#if DEBUG
				Console.WriteLine ("At time {0}: Core {1} Slowdown rate is {2} ", Simulator.CurrentRound, i, estimated_slowdown);
				#endif
				//Simulator.stats.etimated_slowdown [i].Add (estimated_slowdown);
				need_slowdown_qos = need_slowdown_qos | (estimated_slowdown >= Config.target_slowdown [i]);
			}
			return need_slowdown_qos;
		}

		public override void doStep()
		{
			
			double ipc_share;
			// track the nonoverlapped penalty
			double estimated_slowdown, actual_slowdown;
			double insns_ewma, insns_current;
			bool need_slowdown_qos = true;
			// update slowdown info periodically here.
			//if (Simulator.CurrentRound > 0 && Simulator.CurrentRound % Config.slowdown_epoch == 0)

			//need_slowdown_qos = globalSlowdown ();
			for (int i = 0; i < Config.N; i++) 
			{

				insns_current = Simulator.stats.insns_persrc_period[i].Count;
				if (insns_current > 0 && insns_current >= Config.slowdown_epoch )
				{
					
					ulong penalty_cycle = (ulong)Simulator.stats.non_overlap_penalty_period [i].Count;
					//estimated_slowdown = (double)Config.slowdown_epoch / (Config.slowdown_epoch - penalty_cycle);
					estimated_slowdown = (double)(Simulator.CurrentRound-m_lastCheckPoint [i])/(Simulator.CurrentRound-m_lastCheckPoint [i]-penalty_cycle);
					#if DEBUG
					Console.WriteLine ("at time {0}: Core {1} Slowdown rate is {2} ", Simulator.CurrentRound, i, estimated_slowdown);
					#endif
					//Simulator.stats.etimated_slowdown [i].Add (estimated_slowdown);
					m_lastCheckPoint [i] = Simulator.CurrentRound;

					//estimated_slowdown = Simulator.stats.etimated_slowdown [i].Count;
					insns_ewma = Simulator.stats.insns_persrc_ewma [i].ExpWhtMvAvg (); // predict performance in next epoch


					double throttle_rate = 0;
					// TODO: Sth is missing here: sometime the node does not have to be throttled.
					if (Config.throttle_enable == true) 
					{
						/*
						if (need_slowdown_qos == false) {
							throttle_rate = 0.0;
						}
						else if (estimated_slowdown < Config.target_slowdown [i]) {
							if (insns_ewma < insns_current)
								throttle_rate = Math.Min (Config.throt_max, Math.Max (Config.throt_min, m_throttleRates [i] + Config.thrt_sweep));
								//throttle_rate = m_throttleRates [i] + Config.thrt_sweep;
							else
								throttle_rate = Math.Min (Config.throt_max, Math.Max (Config.throt_min, m_throttleRates [i] - Config.thrt_sweep));
								//throttle_rate = m_throttleRates [i] - Config.thrt_sweep;
							// suppose to throttle up
							// setThrottleRate (i, Math.Min (Config.throt_max, Math.Max (Config.throt_min, m_throttleRates [i] - 0.1)));
						} 
						else {
							// disable throttle
							// setThrottleRate (i, Math.Min (Config.throt_max, Math.Max (Config.throt_min, m_throttleRates [i] + 0.1)));
							throttle_rate = Config.default_throttle[i];

						}
						*/
						if (i%4!=0)
							throttle_rate = Config.default_throttle;
						
						setThrottleRate (i, throttle_rate);
					}

					ipc_share = Simulator.stats.insns_persrc_period [i].Count / (Config.slowdown_epoch);
					actual_slowdown = (double)Config.ref_ipc / ipc_share;
					double error_slowdown = (estimated_slowdown - actual_slowdown) / actual_slowdown;
					error_slowdown = (error_slowdown > 0) ? error_slowdown : (-error_slowdown);
					//Simulator.stats.avg_slowdown_error [i].Add (error_slowdown);
					//Simulator.stats.actual_slowdown [i].Add (actual_slowdown);
					//reset
					//Simulator.stats.etimated_slowdown [i].EndPeriod ();
					//Simulator.stats.actual_slowdown [i].EndPeriod ();
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
