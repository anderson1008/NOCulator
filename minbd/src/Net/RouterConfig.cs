using System.Reflection;
namespace ICSimulator
{
    public enum TrafficPattern
    {
        uniformRandom,
        transpose,
        bitComplement,
        meshTornado,
        singleHotSpot,
        doubleHotSpot,
        applicationTraces,
        crosspath,
        starvespot
    }

	public enum ConnectorAlgorithm
	{
		DIRECT_LINK,
		RING_LINK
	}

    public enum RouterAlgorithm
    {
        OLDEST_FIRST_DO_ROUTER,
        ROUND_ROBIN_DO_ROUTER,
        MIN_AD_OLDEST_FIRST_DO_ROUTER,
        MIN_AD_ROUND_ROBIN_DO_ROUTER,
        ROMM_OLDEST_FIRST_DO_ROUTER,
        ROMM_ROUND_ROBIN_DO_ROUTER,
        ROMM_MIN_AD_OLDEST_FIRST_DO_ROUTER,
        ROMM_MIN_AD_ROUND_ROBIN_DO_ROUTER,

        STC_DO_ROUTER,

        DR_FLIT_SWITCHED_CLOSEST_FIRST,
        DR_FLIT_SWITCHED_OLDEST_FIRST,
        DR_FLIT_SWITCHED_FURTHEST_FIRST,
        DR_FLIT_SWITCHED_MOSTDEFS_FIRST,
        DR_FLIT_SWITCHED_ROUNDROBIN,
        DR_FLIT_SWITCHED_MIXED_RR_OLDESTFIRST,

        DR_FLIT_SWITCHED_CTLR,
        
        // DR_BUFFERLESS_PURE_WORMHOLE_CLOSEST_FIRST,
        // DR_BUFFERLESS_PURE_WORMHOLE_OLDEST_FIRST,
        DR_TRUNCATION_WORMHOLE_CLOSEST_FIRST,
        DR_TRUNCATION_WORMHOLE_OLDEST_FIRST,
        DR_TRUNCATION_WORMHOLE_FURTHEST_FIRST,
        DR_TRUNCATION_WORMHOLE_MOSTDEFS_FIRST,
        DR_TRUNCATION_WORMHOLE_ROUNDROBIN,
        DR_TRUNCATION_WORMHOLE_MIXED_RR_OLDESTFIRST,
        MOST_DEFLECTIONS_FIRST_OLS_DR,
        FURTHEST_FIRST_ROMM_OLS_DR,
        MOST_DEFLECTIONS_FIRST_ROMM_OLS_DR,

        DR_SCARAB,

        OF_DO_BUFFERED,
        RR_DO_BUFFERED,

        DR_FLIT_SWITCHED_GP,
        DR_FLIT_SWITCHED_RANDOM,
        DR_FLIT_SWITCHED_RRFLUSH,

        DR_FLIT_SWITCHED_CALF,
        DR_FLIT_SWITCHED_CALF_OF,

        ROUTER_FLIT_EXHAUSTIVE,

        DR_AFC,

        ROR_RANDOM,
        ROR_OLDEST_FIRST,
        ROR_CLOSEST_FIRST,
        ROR_GP,
        
        RINGROUTER_SIMPLE,

        NEW_CF,
        NEW_OF,
        NEW_GP
    }

    public class RouterConfig : ConfigGroup
    {
        public TrafficPattern pattern = TrafficPattern.uniformRandom;
        public bool idealNetwork = false; //summary>Packets bypass the network.</summary>
        public int addrPacketSize = 1;
        public int dataPacketSize = 4;//8;
        public int maxPacketSize  = 4;//8;
		public RouterAlgorithm algorithm = RouterAlgorithm.DR_FLIT_SWITCHED_GP;//RouterAlgorithm.DR_FLIT_SWITCHED_CTLR;
        public string options = "";
        public double throttleparam = 1.0;
        public int extraLatency = 0;
        public int linkLatency = 2;
        public int nrVCperPC = 4;
        public int nrVCperPC_DR = 1;
        public int sizeOfVCBuffer = 8;
        public int sizeOfVCBuffer_DR = 1;
        public int sizeOfRxBuf = 0;
        public bool ejectMultipleCheat = false; //labelled a cheat b/c it should only be used for legacy testing.

		/* Ring */
		public ConnectorAlgorithm connectionalgorithm = ConnectorAlgorithm.DIRECT_LINK;

        public ulong packetExpirationThreshold = 250;

        public bool useCreditBasedControl = false;

        public int ejectionPipeline_extraLatency = 2;

        protected override bool setSpecialParameter(string param, string val)
        {
            return false;
        }
        public override void finalize()
        {
            if (idealNetwork)
                Config.ignore_livelock = true;
        }

        
    }
}
