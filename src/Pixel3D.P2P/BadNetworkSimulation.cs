// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Lidgren.Network;

namespace Pixel3D.P2P
{
	public class BadNetworkSimulation
	{
		// NOTE: use of DEBUG here matches its use in the Lidgren library (assume both are compiled with same settings)

#if DEBUG
		public const bool Available = true;
#else
        public const bool Available = false;
#endif


		public string name;
		public float loss, duplicate, lag, randomLag;


		internal void ApplySettingsToConfig(NetPeerConfiguration config)
		{
#if DEBUG
			config.SimulatedLoss = loss;
			config.SimulatedDuplicatesChance = duplicate;
			config.SimulatedMinimumLatency = lag;
			config.SimulatedRandomLatency = randomLag;
#endif
		}

		public override string ToString()
		{
#if DEBUG
			return "Simulation: " + lag * 1000 + " - " + (lag + randomLag) * 1000 + "ms one-way lag; "
			       + loss * 100 + "% loss; " + duplicate * 100 + "% dup; \"" + name + "\"";
#else
            return "Bad network simulation unavailable";
#endif
		}


		#region Presets

		private static readonly BadNetworkSimulation[] presets =
		{
			new BadNetworkSimulation {loss = 0f, duplicate = 0f, lag = 0f, randomLag = 0f, name = "Disabled"},
#if DEBUG
			new BadNetworkSimulation {loss = 0.05f, duplicate = 0.01f, lag = 0.05f, randomLag = 0.006f, name = "Good"},
			new BadNetworkSimulation {loss = 0.1f, duplicate = 0.02f, lag = 0.1f, randomLag = 0.02f, name = "OK"},
			new BadNetworkSimulation {loss = 0.2f, duplicate = 0.05f, lag = 0.2f, randomLag = 0.05f, name = "Bad"},
			new BadNetworkSimulation {loss = 0.3f, duplicate = 0.1f, lag = 2f, randomLag = 1f, name = "Terrible"},
			new BadNetworkSimulation {loss = 0.5f, duplicate = 0.2f, lag = 3f, randomLag = 2f, name = "Horrific"},
#endif
		};


		public static BadNetworkSimulation GetPreset(int preset)
		{
			return presets[preset];
		}

		public int PresetCount => presets.Length;


		public static int NextPreset(int preset)
		{
			return (preset + 1) % presets.Length;
		}

		public static int PreviousPreset(int preset)
		{
			return (preset + presets.Length - 1) % presets.Length;
		}

		#endregion
	}
}