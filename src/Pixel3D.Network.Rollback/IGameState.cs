// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.IO;

namespace Pixel3D.Network.Rollback
{
	public interface IGameState
	{
		/// <param name="startupPrediction">Frame is being executed during network startup, to catch up the syncronised clock time</param>
		void BeforeRollbackAwareFrame(int frame, bool startupPrediction);

		void AfterRollbackAwareFrame();

		void RollbackDriverDetach();


		/// <param name="firstTimeSimulated">
		///     This is the first time this frame has been simulated (play sound effects, advance
		///     smoothing, etc)
		/// </param>
		/// <param name="displayToUser">This update is expected to be displayed to the user</param>
		void Update(MultiInputState input, bool firstTimeSimulated);


		// TODO: Add "player grouping bits" to this method (to support "multiple players per network host")
		//       (Right now we are faking it in RCRU's GameStateManager.)
		void PlayerJoin(int playerIndex, string playerName, byte[] playerData, bool firstTimeSimulated);
		void PlayerLeave(int playerIndex, bool firstTimeSimulated);


		/// <summary>Use to capture data for smoothing</summary>
		void BeforePrediction();

		/// <summary>Use to apply captured smoothing data</summary>
		void AfterPrediction();


		/// <summary>Write out game-specific discovery data</summary>
		/// <param name="bw">A shared binary writer (don't keep it)</param>
		void WriteDiscoveryData(BinaryWriter bw);


		// TODO: change this to something better than a byte array
		byte[] Serialize();

		// Implementors note: If deserialization fails, throw an exception!
		void Deserialize(byte[] data);
	}
}