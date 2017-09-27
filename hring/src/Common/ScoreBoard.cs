using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace ICSimulator
{
	public class ScoreBoard
	{
		public static List<List<ulong>> inFlightFlit = new List<List<ulong>> ();

		public static void createScoreBoard ()
		{
			for (int i = 0; i < Config.N; i++)
				inFlightFlit.Add (new List<ulong>());
		}

		public static void RegPacket (int dstID, ulong pktID) {
			
			if (Config.scoreBoardDisable)
				return;
			
			inFlightFlit [dstID].Add (pktID);
		}

		public static void UnregPacket (int dstID, ulong pktID) {
			
			if (Config.scoreBoardDisable)
				return;
			
			if (inFlightFlit [dstID].Remove (pktID) == false)
				Debug.Assert (false, "ERROR: Packet was not registerred.\n");
			
		}

		public static bool ScoreBoardisClean () {

			bool clean = true;

			foreach (List<ulong> sublist in inFlightFlit) {

				if (sublist.Count != 0) {
					clean = false;
					break;
				} else
					continue;
			}
			return clean; // list is empty?
		}

		public static void ScoreBoardDump () {
			for (int i = 0; i < Config.N; i++) {
				Console.WriteLine ("Unfinished Flit @ Node {0}", i);

				inFlightFlit[i].ForEach(delegate (ulong pktID) {
					Console.Write ("{0}  ", pktID);
				});
				Console.WriteLine ();
			}
		}

	}
}

