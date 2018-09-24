// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lidgren.Network;

namespace Pixel3D.P2P
{
	public class RemotePeer
	{
		#region Host Migration Queue

		internal Queue<NetIncomingMessage> hostMigrationQueue;

		#endregion


		internal RemotePeer(NetConnection connection, PeerInfo peerInfo)
		{
			PeerInfo = peerInfo;

			if (connection != null) AddConnection(connection);
		}

		public PeerInfo PeerInfo { get; private set; }


		#region Connection Info

		public float AverageRoundtripTime => Connection.AverageRoundtripTime;

		#endregion

		public override string ToString()
		{
			Debug.Assert(false); // should be accessing peer info
			return PeerInfo.ToString();
		}


		// Special use (for setting the server's peer info, and for host migration)
		internal void SetPeerInfo(PeerInfo peerInfo)
		{
			PeerInfo = peerInfo;
		}


		#region Outgoing Messages

		public void Send(NetOutgoingMessage message, NetDeliveryMethod deliveryMethod, int sequenceChannel)
		{
			if (deliveryMethod == NetDeliveryMethod.ReliableOrdered && sequenceChannel == 0)
				throw new ArgumentException("ReliableOrdered channel 0 is reserved for P2P network management.");

			if (Connection != null)
				Connection.SendMessage(message, deliveryMethod, sequenceChannel);
		}

		#endregion


		#region Network Connection

		/// <summary>
		///     The remote peer's network connection. A null value represents a lack of connection.
		///     Because Lidgren's connection state is not threadsafe (between main and network threads),
		///     this is set to null when a disconnection has been processed on the main thread.
		///     It can also be null if a connection has not yet been made for this RemotePeer (on the client).
		/// </summary>
		internal NetConnection Connection { get; private set; }

		public bool IsConnected => Connection != null;


		// RemotePeers cannot reform connections
		private bool hadConnection;


		internal void AddConnection(NetConnection connection)
		{
			Debug.Assert(connection != null);

			Debug.Assert(!hadConnection);
			hadConnection = true;

			Debug.Assert(Connection == null);
			Connection = connection;
			Debug.Assert(connection.Tag == null);
			connection.Tag = this;
		}

		internal void Disconnect(string reason)
		{
			if (Connection != null)
			{
				Connection.Disconnect(reason);
				Connection.Tag = null;
				Connection = null;
			}
		}

		internal void WasDisconnected()
		{
			if (Connection != null)
			{
				Debug.Assert(Connection.Status == NetConnectionStatus.Disconnected ||
				             Connection.Status == NetConnectionStatus.Disconnecting);
				Connection.Tag = null;
				Connection = null;
			}
		}

		#endregion


		#region Incoming Messages

		private readonly Queue<NetIncomingMessage> incomingMessageQueue = new Queue<NetIncomingMessage>();

		internal void QueueMessage(NetIncomingMessage message, ref bool recycle)
		{
			incomingMessageQueue.Enqueue(message);
			recycle = false;
		}

		internal void ClearMessageQueue()
		{
			if (Connection != null)
				Connection.Peer.Recycle(incomingMessageQueue);
			incomingMessageQueue.Clear();
		}

		/// <summary>Read a message from this remote client. Or return null if there are no messages to read.</summary>
		public NetIncomingMessage ReadMessage()
		{
			if (incomingMessageQueue.Count > 0)
				return incomingMessageQueue.Dequeue();

			return null;
		}

		public void Recycle(NetIncomingMessage message)
		{
			if (Connection != null)
				Connection.Peer.Recycle(message);
		}

		#endregion
	}
}