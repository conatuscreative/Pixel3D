// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Diagnostics;
using Lidgren.Network;

namespace Pixel3D.P2P
{
	internal class UnconnectedRemotePeer
	{
		public readonly int connectionToken;
		public readonly bool initiateConnection;
		private readonly P2PClient owner;

		public readonly RemotePeer remotePeer;


		public UnconnectedRemotePeer(P2PClient owner, RemotePeer remotePeer, int connectionToken,
			bool initiateConnection)
		{
			this.owner = owner;

			this.remotePeer = remotePeer;
			this.connectionToken = connectionToken;
			this.initiateConnection = initiateConnection;

			Debug.Assert(!remotePeer.IsConnected); // <- our job is to wait until we get this connection


			// Timing:
			var now = NetTime.Now;
			nextTick = now + tickRate;

			// The time-out times are calculated in the design document ("P2P Network.pptx")
			// They are dependent on the current Lidgren config, some Lidgren internals, and the punch-through settings (below)
			if (initiateConnection)
				timeOutAt = now + 14; // seconds to receive punch-through message from other end so we can start initiating connection
			else
				timeOutAt = now + 32; // seconds to receive "Connect" (as "Accept") from other end initiating connection


			// Send first punch-through:
			SendNatPunchThrough();
		}

		private NetPeer NetPeer => owner.NetPeer;

		public PeerInfo PeerInfo => remotePeer.PeerInfo;


		#region Updating

		// NOTE: changing these values will change what the time-outs should be
		private const double tickRate = 2; // seconds per tick
		private const int natPunchThroughRetries = 5;

		private int tickCount;
		private double nextTick;
		private readonly double timeOutAt;

		/// <param name="now">The current NetTime in seconds</param>
		/// <returns>True if still valid, false if timed out.</returns>
		internal bool Update(double now)
		{
			if (nextTick < now)
			{
				if (tickCount < natPunchThroughRetries) SendNatPunchThrough();

				tickCount++;
				nextTick += tickRate;
			}

			// Sanity-check: are we getting all the punch attempts out within the time-out?
			Debug.Assert(now < timeOutAt || tickCount >= natPunchThroughRetries);

			return now < timeOutAt; // False if timed out
		}

		#endregion


		#region Connection Messages

		internal static void ReadConnectionMessage(NetIncomingMessage message, out int localId, out int remoteId,
			out int connectionToken)
		{
			localId = message.ReadInt32(); // Gets written as remote ID (swapped at each end)
			remoteId = message.ReadInt32();
			connectionToken = message.ReadInt32();
		}

		internal static void WriteConnectionMessage(NetOutgoingMessage message, int localId, int remoteId,
			int connectionToken)
		{
			message.Write(remoteId); // Gets read as local ID (swapped at each end)
			message.Write(localId);
			message.Write(connectionToken);
		}


		private NetOutgoingMessage CreatePunchMessage()
		{
			var message = NetPeer.CreateMessage();
			message.Write((byte) UnconnectedMessage.NATPunchThrough);
			WriteConnectionMessage(message, owner.owner.LocalPeerInfo.ConnectionId, PeerInfo.ConnectionId,
				connectionToken);
			return message;
		}

		private void SendNatPunchThrough()
		{
			NetPeer.SendUnconnectedMessage(CreatePunchMessage(), PeerInfo.InternalEndPoint);
			NetPeer.SendUnconnectedMessage(CreatePunchMessage(), PeerInfo.ExternalEndPoint);
		}

		#endregion
	}
}