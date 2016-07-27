#define DEBUG

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Diagnostics;


namespace ICSimulator
{
	enum DIR {N, E, S, W, L};


	public class Router_BLESS_MC: Router_Flit
	{
		protected Flit m_injectSlot;
		Queue<Flit>	[] ejectBuffer;
		protected int [,] mcMask;

		public Router_BLESS_MC(Coord myCoord)
			: base(myCoord)
		{
			m_injectSlot = null;
			ejectBuffer = new Queue<Flit>[4];
			for (int n = 0; n < 4; n++)
				ejectBuffer[n] = new Queue<Flit>();
			mcMask = new int[4,Config.N]; // Clockwise 0->3 map to N->E->S->W
			for (int i = 0; i < 4; i++)
				for (int j = 0; j < Config.N; j++)
					mcMask [i, j] = 0;
			SetupMask ();
		}
        

		protected void SetupMask () {
			Coord dstCoord; // coord is the coordinate of the current node.
			//DIR dir;

			for (int j=0; j<Config.N; j++){
				if (j == coord.ID)
					continue;
				dstCoord = new Coord (j);
				if (coord.x <= dstCoord.x && coord.y < dstCoord.y)
					mcMask [(int)DIR.N, j] = 1;
				else if (coord.x < dstCoord.x && coord.y >= dstCoord.y)
					mcMask [(int)DIR.E, j] = 1;
				else if (coord.x >= dstCoord.x && coord.y > dstCoord.y)
					mcMask [(int)DIR.S, j] = 1;
				else if (coord.x > dstCoord.x && coord.y <= dstCoord.y)
					mcMask [(int)DIR.W, j] = 1;
				else
					Debug.Assert (false, "ERROR: Mask Setup\n");
			}
		    
		}

		public void PrintMask () {
			Console.WriteLine ("0 1 2 3 4 5 6 7 8 9 A B C D E F");
			
			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < Config.N; j++) 		
					Console.Write ("{0} ", mcMask [i, j]);	
				Console.WriteLine ();
			}
		}

		//protected override void _doStep(){} // called from Network
		//public override bool canInjectFlit(Flit f){ return true;} // called from Processor
		//public override void InjectFlit(Flit f){} // called from Processor

	}
}

