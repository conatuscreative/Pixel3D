// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Net;
using Lidgren.Network;

namespace Pixel3D.P2P
{
	public class PeerInfo
	{
		public int ConnectionId { get; internal set; }

		public string PlayerName { get; internal set; }
		public byte[] PlayerData { get; internal set; }

		public InputAssignment InputAssignment { get; internal set; }

		public IPEndPoint InternalEndPoint { get; internal set; }
		public IPEndPoint ExternalEndPoint { get; internal set; }

		public ulong SideChannelId { get; internal set; }

		/// <summary>True if this peer is marked as connected to all other peers on the P2P network</summary>
		public bool IsApplicationConnected { get; internal set; }

		/// <summary>True if this peer is the "Server" on the P2P network</summary>
		public bool IsServer { get; internal set; }


		public override string ToString()
		{
			return "#" + ConnectionId + " \"" + PlayerName + "\"";
		}


		#region Construction and Serialisation

		internal PeerInfo()
		{
		} // <- construct manually

		internal PeerInfo(NetIncomingMessage message, bool useExistingPlayerData = false,
			byte[] existingPlayerData = null)
		{
			ConnectionId = message.ReadInt32();
			InternalEndPoint = message.ReadIPEndPoint();
			ExternalEndPoint = message.ReadIPEndPoint();
			SideChannelId = message.ReadUInt64();
			IsApplicationConnected = message.ReadBoolean();
			IsServer = message.ReadBoolean();
			InputAssignment = message.ReadInputAssignment();
			message.SkipPadBits();
			PlayerName = message.ReadString();

			if (useExistingPlayerData)
				PlayerData = existingPlayerData; // NOTE: can be null
			else
				PlayerData = message.ReadByteArray();
		}

		internal void WriteTo(NetOutgoingMessage message, bool skipPlayerData = false)
		{
			message.Write(ConnectionId);
			message.Write(InternalEndPoint);
			message.Write(ExternalEndPoint);
			message.Write(SideChannelId);
			message.Write(IsApplicationConnected);
			message.Write(IsServer);
			message.Write(InputAssignment);
			message.WritePadBits();
			message.Write(PlayerName);

			if (!skipPlayerData)
				message.WriteByteArray(PlayerData);
		}

		#endregion
	}
}