// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Lidgren.Network;

namespace Pixel3D.P2P
{
	internal class P2PServer : IPeerManager
	{
		private readonly P2PNetwork owner;

		private readonly Random random = new Random();

		public P2PServer(P2PNetwork owner, string playerName, byte[] playerData, string gameName, bool openToInternet,
			bool sideChannelAuth, ulong sideChannelId)
		{
			this.owner = owner;

			owner.Log("Starting P2P Server");

			// Directly setup info about the current game (it's good to be the server):
			owner.GameInfo = new GameInfo(gameName, openToInternet, sideChannelAuth);

			OpenToConnections(openToInternet, sideChannelAuth);

			var myAddress = owner.GetInternalEndpoint();

			owner.LocalPeerInfo = new PeerInfo
			{
				ConnectionId = nextConnectionId++,
				PlayerName = playerName.FilterName(),
				PlayerData = playerData,
				InputAssignment = AssignInput(),

				InternalEndPoint = myAddress,
				ExternalEndPoint =
					myAddress, // <- we have no way to determine our external IP (except for UPnP, which is unreliable); we could STUN but technically it can change!

				SideChannelId = sideChannelId,

				IsApplicationConnected = true,
				IsServer = true
			};

			owner.Log("I am " + owner.LocalPeerInfo);
		}

		private NetPeer NetPeer => owner.netPeer;


		private void OpenToConnections(bool openToInternet, bool sideChannelAuth)
		{
			// Note: the following settings will be cleared when the server is shutdown:
            if (openToInternet)
            {
#if UPNP
                StartUPnP();
#endif
            }
			if (sideChannelAuth)
			{
				NetPeer.Configuration.EnableMessageType(NetIncomingMessageType.UnconnectedData);
				NetPeer.Configuration.DisableMessageType(NetIncomingMessageType.DiscoveryRequest);
			}
			else
			{
				NetPeer.Configuration.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
			}

			NetPeer.Configuration.AcceptIncomingConnections = true;
		}
        
#region Network Data Errors

		private void NetworkDataError(RemotePeer remotePeer, Exception exception)
		{
			owner.Log("Network data error from " +
			          (remotePeer != null ? remotePeer.PeerInfo.ToString() : "unknown peer"));
			owner.LogException(exception);
			if (remotePeer != null)
				Kick(remotePeer); // buh-bye
		}

#endregion

#region UPnP
#if UPNP
		/// <summary>True if UPnP was *attempted* (because Lidgren and UPnP and routers are all janky)</summary>
		private bool usingUPnP;

		private void StartUPnP()
		{
            if(NetPeer.UPnP != null)
            {
                usingUPnP = true;
                NetPeer.UPnP.ForwardPort(owner.PortNumber, owner.appConfig.AppId);
            }
        }   

        private void EndUPnP()
		{
            if(usingUPnP && NetPeer.UPnP != null)
                NetPeer.UPnP.DeleteForwardingRule(owner.PortNumber);
		}
#endif
#endregion


#region IPeerManager

        public void HandleMessage(NetIncomingMessage message, ref bool recycle)
		{
			Debug.Assert(!didDisconnect);

			RemotePeer senderRemotePeer = null;
			if (message.SenderConnection != null)
				senderRemotePeer = message.SenderConnection.Tag as RemotePeer;

			try
			{
				switch (message.MessageType)
				{
					case NetIncomingMessageType.DiscoveryRequest: // (Clients trying to find LAN games)
					{
						var response = NetPeer.CreateMessage();
						DiscoveredGame.WriteDiscoveryResponse(owner.appConfig, response, owner.GameInfo, IsFull,
							owner.NetworkApplication.GetDiscoveryData());
						NetPeer.SendDiscoveryResponse(response, message.SenderEndPoint);
					}
						break;


					case NetIncomingMessageType.StatusChanged:
						var status = (NetConnectionStatus) message.ReadByte();
						switch (status)
						{
							case NetConnectionStatus.Connected:
								HandleConnection(message.SenderConnection);
								break;
							case NetConnectionStatus.Disconnected:
								HandleDisconnection(message.SenderConnection, message);
								break;
						}

						break;

					case NetIncomingMessageType.UnconnectedData:
						HandleUnconnectedMessage(message);
						break;

					case NetIncomingMessageType.Data:
						HandleNetworkManagementFromClient(message);
						break;
				}
			}
			catch (NetworkDataException exception)
			{
				NetworkDataError(senderRemotePeer, exception);
			}
		}


		private void HandleUnconnectedMessage(NetIncomingMessage message)
		{
			if (message.LengthBits == 0)
				return;

			UnconnectedMessage um;
			try
			{
				um = (UnconnectedMessage) message.ReadByte();
			}
			catch
			{
				return; // Unexpected!
			}

			switch (um)
			{
				case UnconnectedMessage.SideChannelVerify:
					HandleSideChannelVerify(message);
					break;

				default:
					return; // Silently ignore message types we don't recognise
			}
		}


		public void Update()
		{
			Debug.Assert(!didDisconnect);

			var currentTime = NetTime.Now;
			UpdatePendingDisconnects(currentTime);
			UpdatePendingConnections(currentTime);
		}


		private bool didDisconnect;

		public void HandleLocalDisconnection()
		{
			Debug.Assert(!didDisconnect);
			didDisconnect = true;

			NetPeer.Configuration.DisableMessageType(NetIncomingMessageType.DiscoveryRequest);
			NetPeer.Configuration.DisableMessageType(NetIncomingMessageType.UnconnectedData);
			NetPeer.Configuration.AcceptIncomingConnections = false;

#if UPNP
            EndUPnP();
#endif

			foreach (var connection in locallyConnected)
				connection.Disconnect(DisconnectStrings.Shutdown);
		}


		public void KickDueToNetworkDataError(RemotePeer remotePeer)
		{
			Debug.Assert(remotePeer != null);
			Kick(remotePeer); // buh-bye
		}

#endregion


#region Side-Channel Auth

		// NOTE: zero represents an invalid token (connection has been spent, prevent replay)
		private Dictionary<ulong, int> authTokens;

		/// <returns>Random value required to authorise a new connection</returns>
		internal int SideChannelAdd(ulong sideChannelId)
		{
			if (!owner.GameInfo.SideChannelAuth)
			{
				owner.Log("ERROR: SideChannelAdd in non-side-channel game");
				return 0;
			}

			if (authTokens == null)
				authTokens = new Dictionary<ulong, int>();

			int token;
			if (!authTokens.TryGetValue(sideChannelId, out token) || token == 0)
			{
				do
				{
					token = random.Next();
				} while (token == 0);

				authTokens[sideChannelId] = token;
			}

			return token;
		}

		internal void SideChannelRemove(ulong sideChannelId)
		{
			if (!owner.GameInfo.SideChannelAuth)
			{
				owner.Log("ERROR: SideChannelRemove in non-side-channel game");
				return;
			}

			if (authTokens != null)
				authTokens.Remove(sideChannelId);
			var remotePeer = FindRemotePeerBySideChannelId(sideChannelId);
			if (remotePeer != null)
				Kick(remotePeer);
		}

		/// <summary>Call this after host migration, after re-issuing adding authorised users from the side-channel</summary>
		internal void SideChannelValidateAndKick()
		{
			if (!owner.GameInfo.SideChannelAuth)
			{
				owner.Log("ERROR: SideChannelValidateAndKick in non-side-channel game");
				return;
			}

			foreach (var connection in locallyConnected)
			{
				var remotePeer = connection.Tag as RemotePeer;
				Debug.Assert(remotePeer != null);
				if (authTokens == null || !authTokens.ContainsKey(remotePeer.PeerInfo.SideChannelId))
					Kick(remotePeer);
			}
		}


		private void HandleSideChannelVerify(NetIncomingMessage message)
		{
			ulong sideChannelId;
			int token;
			try
			{
				sideChannelId = message.ReadUInt64();
				token = message.ReadInt32();
			}
			catch
			{
				owner.UnconnectedProtocolError("Bad verify request");
				return;
			}

			if (token == 0)
				return;

			int expectedToken;
			if (authTokens != null && authTokens.TryGetValue(sideChannelId, out expectedToken) &&
			    expectedToken == token)
			{
				owner.Log("Sending verify to " + message.SenderEndPoint);

				// TODO: This is open to being spoofed (do we care? -- put verification we connected to the right game in the side-channel?)
				var response = NetPeer.CreateMessage();
				response.Write((byte) UnconnectedMessage.SideChannelVerifyResponse);
				response.Write(sideChannelId);
				response.Write(token);
				NetPeer.SendUnconnectedMessage(response, message.SenderEndPoint);
			}
			else
			{
#if DEBUG
				owner.UnconnectedProtocolError("Bad verify request (not authorised)");
#endif
			}
		}

#endregion


#region Local Connections

		private int nextConnectionId;

		// All items in the locally connected list should have a Tag with a RemotePeer that IsConnected
		public List<NetConnection> locallyConnected = new List<NetConnection>();


		private RemotePeer FindRemotePeerById(int connectionId)
		{
			foreach (var connection in locallyConnected)
			{
				var remotePeer = connection.Tag as RemotePeer;
				Debug.Assert(remotePeer != null);
				if (remotePeer.PeerInfo.ConnectionId == connectionId)
					return remotePeer;
			}

			return null;
		}


		private RemotePeer FindRemotePeerBySideChannelId(ulong sideChannelId)
		{
			foreach (var connection in locallyConnected)
			{
				var remotePeer = connection.Tag as RemotePeer;
				Debug.Assert(remotePeer != null);
				if (remotePeer.PeerInfo.SideChannelId == sideChannelId)
					return remotePeer;
			}

			return null;
		}


		private void HandleConnection(NetConnection connection)
		{
			// Read hail message:
			IPEndPoint internalEndPoint;
			ulong sideChannelId;
			int sideChannelToken;
			string requestedName;
			byte[] remotePlayerData = null;
			try
			{
				var theirAppVersion = connection.RemoteHailMessage.ReadUInt16();
				if (theirAppVersion != owner.appConfig.ApplicationVersion)
				{
					owner.Log("Disconnected client with wrong application version");
					connection.Disconnect(DisconnectStrings.BadGameVersion);
					return;
				}

				var theirAppSignatureLength = (int) connection.RemoteHailMessage.ReadVariableUInt32();
				byte[] theirAppSignature;
				if (theirAppSignatureLength < 0 ||
				    theirAppSignatureLength > NetworkAppConfig.ApplicationSignatureMaximumLength)
					theirAppSignature = null;
				else
					theirAppSignature = connection.RemoteHailMessage.ReadBytes(theirAppSignatureLength);

				if (theirAppSignature == null || !owner.appConfig.ApplicationSignature.SequenceEqual(theirAppSignature))
				{
					owner.Log("Disconnected client with wrong application signature");
					connection.Disconnect(DisconnectStrings.BadGameVersion);
					return;
				}


				internalEndPoint = connection.RemoteHailMessage.ReadIPEndPoint();
				sideChannelId = connection.RemoteHailMessage.ReadUInt64();
				sideChannelToken = connection.RemoteHailMessage.ReadInt32();
				requestedName = connection.RemoteHailMessage.ReadString();
				remotePlayerData = connection.RemoteHailMessage.ReadByteArray();
			}
			catch
			{
				owner.UnconnectedProtocolError("Bad hail message");
				connection.Disconnect(DisconnectStrings.BadHailMessage);
				return;
			}


			if (owner.GameInfo.SideChannelAuth)
			{
				int expectedToken;
				if (sideChannelToken == 0 || authTokens == null ||
				    !authTokens.TryGetValue(sideChannelId, out expectedToken) || expectedToken != sideChannelToken)
				{
					owner.UnconnectedProtocolError("Bad side-channel authentication");
					connection.Disconnect(DisconnectStrings.BadSideChannelAuth);
				}
			}


			requestedName = requestedName.FilterNameNoDuplicates(locallyConnected, owner.LocalPeerInfo.PlayerName);


			// Check whether the server is full:
			// (Could possibly handle this as Approve/Deny, but would then have to track slots for pending connections.)
			if (IsFull)
			{
				owner.Log("Rejecting incoming connection (player name: " + requestedName + "): game is full");
				connection.Disconnect(DisconnectStrings.GameFull);
				return;
			}


			// Setup remote peer:
			var peerInfo = new PeerInfo
			{
				ConnectionId = nextConnectionId++,
				PlayerName = requestedName,
				PlayerData = remotePlayerData,
				InputAssignment = AssignInput(),
				InternalEndPoint = internalEndPoint,
				ExternalEndPoint = connection.RemoteEndPoint,
				SideChannelId = sideChannelId,
				IsApplicationConnected = false,
				IsServer = false
			};


			var remotePeer = new RemotePeer(connection, peerInfo);
			owner.Log("New connection: " + peerInfo);


			// Setup connection tokens:
			var connectionTokens = new int[locallyConnected.Count];
			for (var i = 0; i < connectionTokens.Length; i++)
				connectionTokens[i] = random.Next();


			// Send info to joining peer:
			{
				var message = NetPeer.CreateMessage();
				message.Write(P2PServerMessage.NetworkStartInfo);
				owner.GameInfo.WriteTo(message);
				peerInfo.WriteTo(message,
					true); // Send their peer info (except: they already have their own playerData)
				owner.LocalPeerInfo.WriteTo(message); // Send our peer info

				// Write out a list of who to connect to:
				Debug.Assert(locallyConnected.Count < byte.MaxValue);
				message.Write((byte) locallyConnected.Count);
				for (var i = 0; i < locallyConnected.Count; i++)
				{
					(locallyConnected[i].Tag as RemotePeer).PeerInfo.WriteTo(message);
					message.Write(connectionTokens[i]);
				}

				remotePeer.Connection.SendMessage(message, NetDeliveryMethod.ReliableOrdered, 0);
			}


			// Send connect instructions to other peers:
			for (var i = 0; i < locallyConnected.Count; i++)
			{
				var message = NetPeer.CreateMessage();
				message.Write(P2PServerMessage.PeerJoinedNetwork);
				peerInfo.WriteTo(message);
				message.Write(connectionTokens[i]);

				locallyConnected[i].SendMessage(message, NetDeliveryMethod.ReliableOrdered, 0);
			}


			// Add new connection to the local connection list:
			locallyConnected.Add(connection);

			// Start waiting for this new client to connect to the full network:
			StartPendingConnection(remotePeer);

			// Although the first connection won't need to wait:
			if (locallyConnected.Count == 1)
				ReadyCheckAll();
		}


		private void HandleDisconnection(NetConnection connection, NetIncomingMessage message)
		{
			var remotePeer = connection.Tag as RemotePeer;
			if (remotePeer == null)
			{
				Debug.Assert(!locallyConnected.Contains(connection));
				return; // Already dealt with
			}

			Debug.Assert(remotePeer.Connection == connection);
			Debug.Assert(locallyConnected.Contains(remotePeer.Connection));

			var reason = message.ReadString();

			owner.Log("Disconnected: " + remotePeer.PeerInfo + " (supplied reason: " + reason + ")");

			locallyConnected.Remove(remotePeer.Connection);
			remotePeer.WasDisconnected();
			RemoveFromNetwork(remotePeer);


			CheckDisconnectionForHostMigrationInvalidation(reason);
		}


		public void Kick(RemotePeer remotePeer)
		{
			Debug.Assert(remotePeer.IsConnected == locallyConnected.Contains(remotePeer.Connection));

			if (!remotePeer.IsConnected)
				return; // Already removed

			owner.Log("Kicking " + remotePeer.PeerInfo);

			locallyConnected.Remove(remotePeer.Connection);
			remotePeer.Disconnect(DisconnectStrings.Kicked);
			RemoveFromNetwork(remotePeer);
		}


		private void RemoveFromNetwork(RemotePeer remotePeer)
		{
			ReleaseInputAssignment(remotePeer.PeerInfo.InputAssignment);

			// Clean up pending:
			RemovePendingDisconnectsFor(remotePeer.PeerInfo.ConnectionId);
			RemovePendingConnectionsFor(remotePeer.PeerInfo.ConnectionId);

			if (!remotePeer.PeerInfo.IsApplicationConnected)
			{
				// Wasn't application connected yet (just send the notification)
				var message = NetPeer.CreateMessage();
				message.Write(P2PServerMessage.PeerLeftNetwork);
				message.Write(remotePeer.PeerInfo.ConnectionId);
				NetPeer.SendMessage(message, locallyConnected, NetDeliveryMethod.ReliableOrdered, 0);
			}
			else
			{
				BecomeApplicationDisconnected(remotePeer);
			}

			// Possible that connecting clients no longer need to connect to this one:
			ReadyCheckAll();
		}

#endregion


#region Input Assignment

		private InputAssignment assignedInputs;

		private bool IsFull => assignedInputs == InputAssignment.Full;

		private InputAssignment AssignInput()
		{
			var next = assignedInputs.GetNextAssignment();
			Debug.Assert(next != 0);
			assignedInputs |= next;
			return next;
		}

		private void ReleaseInputAssignment(InputAssignment assignment)
		{
			assignedInputs &= ~assignment;
		}

#endregion


#region Network Management (from client)

		private void HandleNetworkManagementFromClient(NetIncomingMessage message)
		{
			if (message.SenderConnection == null)
			{
				Debug.Assert(false); // This should never happen, unless Lidgren does something weird
				return;
			}

			var remotePeer = message.SenderConnection.Tag as RemotePeer;
			if (remotePeer == null)
			{
				// Should never get here... unless Lidgren is being dumb (race condition?). At least assert our own state:
				Debug.Assert(!locallyConnected.Contains(message.SenderConnection));
				return;
			}

			if (message.DeliveryMethod != NetDeliveryMethod.ReliableOrdered || message.SequenceChannel != 0)
			{
				Debug.Assert(false); // <- Message should have been handled elsewhere
				return;
			}

			var messageType = message.TryReadP2PClientMessage();
			switch (messageType)
			{
				case P2PClientMessage.Connected:
					HandleReportConnected(message);
					break;

				case P2PClientMessage.Disconnected:
					HandleReportDisconnected(message);
					break;

				case P2PClientMessage.DisputeDisconnection:
					HandleReportDisputeDisconnection(message);
					break;

				case P2PClientMessage.RTTInitialised:
					MarkRemoteInitialisedRTT(remotePeer);
					break;

				case P2PClientMessage.AcceptHostMigration:
					AcceptHostMigration(remotePeer);
					break;

				default:
					throw new ProtocolException("Bad network client message: " + messageType);
			}
		}


		private void HandleReportConnected(NetIncomingMessage message)
		{
			var reporter = message.SenderConnection.Tag as RemotePeer;

			int connectedId;
			try
			{
				connectedId = message.ReadInt32();
			}
			catch (Exception e)
			{
				throw new ProtocolException("Bad connected report", e);
			}

			var connected = FindRemotePeerById(connectedId);
			if (connected == null)
				return; // was disconnected (or bad ID)

			owner.Log(reporter.PeerInfo + " connected to " + connected.PeerInfo);
			MarkConnectionMade(reporter.PeerInfo.ConnectionId, connectedId);
		}

		private void HandleReportDisconnected(NetIncomingMessage message)
		{
			var reporter = message.SenderConnection.Tag as RemotePeer;
			int disconnectedId;
			try
			{
				disconnectedId = message.ReadInt32();
			}
			catch (Exception e)
			{
				throw new ProtocolException("Bad disconnected report", e);
			}


			var disconnected = FindRemotePeerById(disconnectedId);
			if (disconnected == null)
				return; // already disconnected (or bad ID)

			// The network trust model says that we can't tell which client is misbehaving (or has a misbehaving link).
			// If one claims to be disconnected from another, unless we involve other clients (over-complicated), we can't tell who is misbehaving.
			// The network requires all clients are connected. So we just have to pick one to disconnect.
			// Prefer to disconnect the "newer" client. But if they reported first, check whether the older client is actually alive.

			if (reporter.PeerInfo.ConnectionId < disconnectedId) // Older client disconnected from newer client
			{
				owner.Log(disconnected.PeerInfo + " was disconnected by " + reporter.PeerInfo);
				Kick(disconnected);
			}
			else if (reporter.PeerInfo.ConnectionId == disconnectedId) // They disconnected themselves! (oooookay...)
			{
				owner.Log(reporter.PeerInfo + " disconnected from self (weird...)");
				Kick(reporter); // You're the boss...
			}
			else // Newer client disconnected from older client
			{
				owner.Log(disconnected.PeerInfo + " was disconnected by " + reporter.PeerInfo +
				          " and has a chance to dispute.");

				// Inform the older client:
				var msg = NetPeer.CreateMessage();
				msg.Write(P2PServerMessage.YouWereDisconnectedBy);
				msg.Write(reporter.PeerInfo.ConnectionId);
				disconnected.Connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);

				// And wait for them to respond:
				AddPendingDisconnect(reporter.PeerInfo.ConnectionId, disconnectedId);
			}
		}

		private void HandleReportDisputeDisconnection(NetIncomingMessage message)
		{
			var disputer = message.SenderConnection.Tag as RemotePeer;
			int reporterId;
			try
			{
				reporterId = message.ReadInt32();
			}
			catch (Exception e)
			{
				throw new ProtocolException("Bad disconnection dispute", e);
			}


			// Try and find the matching pending disconnect:
			for (var i = pendingDisconnects.Count - 1; i >= 0; i--)
				if (pendingDisconnects[i].failedConnectFrom == reporterId &&
				    pendingDisconnects[i].failedConnectTo == disputer.PeerInfo.ConnectionId)
				{
					var reporter = FindRemotePeerById(reporterId);
					owner.Log(disputer.PeerInfo + " successfully disputed disconnection by " + reporter.PeerInfo);
					Kick(reporter);
				}

			// If we get to here, there was no matching pending disconnect. But this could be because they were already disconnected.
		}

#endregion


#region Disconnect Resolution

		private struct PendingDisconnect
		{
			public double timeOut;

			// A newer client ("From"; higher ID) is reporting that it cannot connect to an older client ("To").
			// Give the older client a chance to respond before kicking it.
			// (Basically: prefer to keep older clients on the network.)
			public int failedConnectFrom;
			public int failedConnectTo;
		}

		// This is a queue (FIFO), but using List<T> so things can be removed from the middle if they become invalid.
		private readonly List<PendingDisconnect> pendingDisconnects = new List<PendingDisconnect>();

		/// <summary>
		///     How long a disconnected client has to respond (prove they are alive) before they get kicked. Actual value is
		///     arbitrary.
		/// </summary>
		private const double
			pendingDisconnectTimeout = 8; // seconds (8 seconds == 2x max RTT, due to lidgren 4sec RTT bug)


		private void AddPendingDisconnect(int failedConnectFrom, int failedConnectTo)
		{
			Debug.Assert(failedConnectFrom > failedConnectTo); // Repoter should be the newer client
			pendingDisconnects.Add(new PendingDisconnect
			{
				failedConnectFrom = failedConnectFrom,
				failedConnectTo = failedConnectTo,
				timeOut = NetTime.Now + pendingDisconnectTimeout
			});
		}

		private void RemovePendingDisconnectsFor(int connectionId)
		{
			for (var i = pendingDisconnects.Count - 1; i >= 0; i--)
				if (pendingDisconnects[i].failedConnectFrom == connectionId ||
				    pendingDisconnects[i].failedConnectTo == connectionId)
					pendingDisconnects.RemoveAt(i);
		}

		private void UpdatePendingDisconnects(double currentTime)
		{
			var now = NetTime.Now;

			// Only take things from the front of the queue (because when something gets kicked, the pendingDisconnects list can change)
			while (pendingDisconnects.Count > 0 && pendingDisconnects[0].timeOut < currentTime)
			{
				// Timed out:
				var pd = pendingDisconnects[0];

				var remotePeer = FindRemotePeerById(pd.failedConnectTo);
				owner.Log("Timed out after disconnect: " + remotePeer.PeerInfo);
				Kick(remotePeer);
			}
		}

#endregion


#region Connection Tracking

		private class PendingConnection
		{
			// List of IDs we've connected to, and who have connected to us
			// All IDs should be less than (older) than ours
			// (newer connections manage their own status)
			public readonly SortedSet<int> incommingConnections;
			public readonly SortedSet<int> outgoingConnections;

			public readonly RemotePeer remotePeer;

			public readonly double timeOut;

			public bool remoteInitialisedRTT;

			public PendingConnection(double timeOut, RemotePeer remotePeer)
			{
				this.timeOut = timeOut;
				this.remotePeer = remotePeer;
				incommingConnections = new SortedSet<int>();
				outgoingConnections = new SortedSet<int>();
			}
		}

		/// <summary>Dictionary of pending connections by connection ID (which also makes it a FIFO queue)</summary>
		private readonly SortedList<int, PendingConnection> pendingConnections =
			new SortedList<int, PendingConnection>();

		/// <summary>How long a connecting client has to form all P2P network connections.</summary>
		private const double pendingConnectionTimeout = 60; // seconds (calculated in "P2P Network.pptx")


		private void StartPendingConnection(RemotePeer remotePeer)
		{
			Debug.Assert(!pendingConnections.ContainsKey(remotePeer.PeerInfo.ConnectionId));

			pendingConnections.Add(remotePeer.PeerInfo.ConnectionId,
				new PendingConnection(NetTime.Now + pendingConnectionTimeout, remotePeer));
		}


		private void RemovePendingConnectionsFor(int connectionId)
		{
			pendingConnections.Remove(connectionId);
		}


		private void MarkRemoteInitialisedRTT(RemotePeer remotePeer)
		{
			PendingConnection pc;
			if (pendingConnections.TryGetValue(remotePeer.PeerInfo.ConnectionId, out pc))
			{
				pc.remoteInitialisedRTT = true;
				ReadyCheck(pc);
			}
			else
			{
				throw new ProtocolException("Unexpected RTT initialised report from " + remotePeer.PeerInfo);
			}
		}


		private void MarkConnectionMade(int fromConnectionId, int toConnectionId)
		{
			PendingConnection from, to;
			pendingConnections.TryGetValue(fromConnectionId, out from);
			pendingConnections.TryGetValue(toConnectionId, out to);

			if (from == null && to == null)
				throw new ProtocolException("Unexpected connection report from #" + fromConnectionId + " to #" +
				                            toConnectionId);

			if (from != null)
			{
				from.outgoingConnections.Add(toConnectionId);
				ReadyCheck(from);
			}

			if (to != null)
			{
				to.incommingConnections.Add(fromConnectionId);
				ReadyCheck(to);
			}
		}


		private bool IsReady(PendingConnection pendingConnection)
		{
			if (!pendingConnection.remoteInitialisedRTT)
				return false;

			var pendingConnectionId = pendingConnection.remotePeer.PeerInfo.ConnectionId;

			// Check that a pending connection is connected (in both directions) to all clients that connected before it:
			// (Note: the server is not in the "locallyConnected" list, but it doesn't get reported either)
			for (var i = 0; i < locallyConnected.Count; i++)
			{
				var otherConnectionId = (locallyConnected[i].Tag as RemotePeer).PeerInfo.ConnectionId;
				if (otherConnectionId < pendingConnectionId)
				{
					if (!pendingConnection.incommingConnections.Contains(otherConnectionId))
						return false;
					if (!pendingConnection.outgoingConnections.Contains(otherConnectionId))
						return false;
				}
			}

			return true;
		}


		private void ReadyCheck(PendingConnection pendingConnection)
		{
			if (IsReady(pendingConnection))
			{
				BecomeApplicationConnected(pendingConnection.remotePeer);
				pendingConnections.Remove(pendingConnection.remotePeer.PeerInfo.ConnectionId);
			}
		}


		private void ReadyCheckAll()
		{
			for (var i = pendingConnections.Values.Count - 1;
				i >= 0;
				i--) // <- reverse iterate, as ReadyCheck might remove stuff
				ReadyCheck(pendingConnections.Values[i]);
		}


		private void UpdatePendingConnections(double currentTime)
		{
			while (pendingConnections.Count > 0 && pendingConnections.Values[0].timeOut < currentTime)
			{
				// Timed out:
				var pendingConnection = pendingConnections.Values[0];

				Debug.Assert(!IsReady(pendingConnection)); // <- did we miss a ready check?

				owner.Log("Timed out waiting for P2P setup to finish: " + pendingConnection.remotePeer.PeerInfo);
				Kick(pendingConnection.remotePeer);
			}
		}

#endregion


#region Application Connected Peers

		// Used to send different join/leave messages to different connections based on their state
		private readonly List<NetConnection> broadcastTemp = new List<NetConnection>();

		private List<NetConnection> ListNotApplicationConnected(NetConnection except)
		{
			broadcastTemp.Clear();
			foreach (var connection in locallyConnected)
			{
				Debug.Assert(connection.Tag != null);
				if (connection == except)
					continue;
				if (!(connection.Tag as RemotePeer).PeerInfo.IsApplicationConnected)
					broadcastTemp.Add(connection);
			}

			return broadcastTemp;
		}

		private List<NetConnection> ListApplicationConnected(NetConnection except)
		{
			broadcastTemp.Clear();
			foreach (var connection in locallyConnected)
			{
				Debug.Assert(connection.Tag != null);
				if (connection == except)
					continue;
				if ((connection.Tag as RemotePeer).PeerInfo.IsApplicationConnected)
					broadcastTemp.Add(connection);
			}

			return broadcastTemp;
		}


		private void BecomeApplicationConnected(RemotePeer remotePeer)
		{
			Debug.Assert(!remotePeer.PeerInfo.IsApplicationConnected);
			remotePeer.PeerInfo.IsApplicationConnected = true;

			owner.Log("Application Connected: " + remotePeer.PeerInfo);
			owner.AddApplicationConnection(remotePeer);


			var listNotApplicationConnected = ListNotApplicationConnected(remotePeer.Connection);
			if (listNotApplicationConnected.Count > 0)
			{
				var notAppConnectedMessage = NetPeer.CreateMessage();
				notAppConnectedMessage.Write(P2PServerMessage.PeerBecameApplicationConnected);
				notAppConnectedMessage.Write(remotePeer.PeerInfo.ConnectionId);
				NetPeer.SendMessage(notAppConnectedMessage, listNotApplicationConnected,
					NetDeliveryMethod.ReliableOrdered, 0);
			}


			var joinMessage = NetPeer.CreateMessage();
			joinMessage.Write(P2PServerMessage.PeerBecameApplicationConnected);
			joinMessage.Write(remotePeer.PeerInfo.ConnectionId);
			var connectedMessage = NetPeer.CreateMessage();
			connectedMessage.Write(P2PServerMessage.PeerBecameApplicationConnected);
			connectedMessage.Write(remotePeer.PeerInfo.ConnectionId);

			owner.NetworkApplication.JoinOnServer(remotePeer, joinMessage, connectedMessage);

			NetPeer.SendMessage(joinMessage, ListApplicationConnected(remotePeer.Connection),
				NetDeliveryMethod.ReliableOrdered, 0);
			NetPeer.SendMessage(connectedMessage, remotePeer.Connection, NetDeliveryMethod.ReliableOrdered, 0);
		}

		private void BecomeApplicationDisconnected(RemotePeer remotePeer)
		{
			Debug.Assert(remotePeer.PeerInfo.IsApplicationConnected);
			remotePeer.PeerInfo.IsApplicationConnected = false;

			owner.Log("Application Disconnected: " + remotePeer.PeerInfo);
			owner.RemoveApplicationConnection(remotePeer);


			var listNotApplicationConnected = ListNotApplicationConnected(remotePeer.Connection);
			if (listNotApplicationConnected.Count > 0)
			{
				var notAppConnectedMessage = NetPeer.CreateMessage();
				notAppConnectedMessage.Write(P2PServerMessage.PeerLeftNetwork);
				notAppConnectedMessage.Write(remotePeer.PeerInfo.ConnectionId);
				NetPeer.SendMessage(notAppConnectedMessage, listNotApplicationConnected,
					NetDeliveryMethod.ReliableOrdered, 0);
			}


			var message = NetPeer.CreateMessage();
			message.Write(P2PServerMessage.PeerLeftNetwork);
			message.Write(remotePeer.PeerInfo.ConnectionId);

			owner.NetworkApplication.LeaveOnServer(remotePeer, message);

			NetPeer.SendMessage(message, ListApplicationConnected(remotePeer.Connection),
				NetDeliveryMethod.ReliableOrdered, 0);
		}

#endregion


#region Host Migration - Transition from Client

		/// <summary>Construct for host migration only!</summary>
		internal P2PServer(P2PNetwork owner, bool hostMigrationValidatedByServer, int maxConnectionId)
		{
			this.owner = owner;
			owner.Log("Becoming P2P Server (host migration)");


			hostMigrationTime = NetTime.Now;
			clientDisconnections = new ClientDisconnections();

			if (hostMigrationValidatedByServer)
				ValidateHostMigration();


			owner.LocalPeerInfo.IsServer = true;


			// Recover information from application-connected peer list:
			var leavingPeers = new List<RemotePeer>();
			foreach (var remotePeer in owner.RemotePeers)
			{
				Debug.Assert(remotePeer.PeerInfo.IsApplicationConnected);
				Debug.Assert(remotePeer.PeerInfo.ConnectionId <= maxConnectionId);

				Debug.Assert(remotePeer.PeerInfo.InputAssignment != 0);
				Debug.Assert((assignedInputs & remotePeer.PeerInfo.InputAssignment) == 0);
				assignedInputs |= remotePeer.PeerInfo.InputAssignment;

				Debug.Assert(!(remotePeer.PeerInfo.IsServer &&
				               remotePeer.IsConnected)); // old server should not still be connected

				if (remotePeer.IsConnected) locallyConnected.Add(remotePeer.Connection);
				// Anyone not connected will get removed from the game in CompleteHostMigration

				remotePeer.PeerInfo.IsServer = false; // Un-server-ify the original server
			}

			// And also from ourself:
			Debug.Assert(owner.LocalPeerInfo.IsApplicationConnected);
			Debug.Assert(owner.LocalPeerInfo.ConnectionId <= maxConnectionId);
			Debug.Assert(owner.LocalPeerInfo.InputAssignment != 0);
			Debug.Assert((assignedInputs & owner.LocalPeerInfo.InputAssignment) == 0);
			assignedInputs |= owner.LocalPeerInfo.InputAssignment;


			// This should be safe, as host migration should remove all traces peers beyond this point (including in the app layer) and is atomic
			nextConnectionId = maxConnectionId + 1;
		}

		// Continues on from the host-migration constructor (this is so P2PNetwork gets the IPeerManager change)
		internal void CompleteHostMigration()
		{
			// Send host-migration packet:
			{
				var message = NetPeer.CreateMessage();
				message.Write(P2PServerMessage.HostMigration);

				// Write out the list of application-connected peers to re-sync the clients:
				Debug.Assert(owner.RemotePeers.Count < byte.MaxValue);
				message.Write((byte) owner.RemotePeers.Count);
				for (var i = 0; i < owner.RemotePeers.Count; i++) owner.RemotePeers[i].PeerInfo.WriteTo(message);

				owner.NetworkApplication.HostMigrationBecomeHost(message);

				NetPeer.SendMessage(message, locallyConnected, NetDeliveryMethod.ReliableOrdered, 0);
			}


			// Remove any remaining peers without connections (this will send leaving packets and interact with application-layer appropriately)
			var leavingPeers = new List<RemotePeer>();
			foreach (var remotePeer in owner.RemotePeers)
				if (!remotePeer.IsConnected)
					leavingPeers.Add(remotePeer);
			foreach (var leavingPeer in leavingPeers)
				// Note: we don't have to disconnect them or remove them from locallyConnected, because they weren't connected in the first place
				RemoveFromNetwork(leavingPeer);
		}

#endregion


#region Host Migration - Validation

		/// <summary>Non-null if a host migration has not yet been validated</summary>
		private ClientDisconnections clientDisconnections;

		private readonly double hostMigrationTime;
		private const double hostMigrationGraceTime = 40; // seconds


		private void ValidateHostMigration()
		{
			if (clientDisconnections == null)
				return; // Already host-migrated

			owner.Log("Host migration validated");

			clientDisconnections = null;
			OpenToConnections(owner.GameInfo.IsInternetGame, owner.GameInfo.SideChannelAuth);
		}


		private void UpdateHostMigration()
		{
			if (clientDisconnections == null)
				return; // Indicates that we are done host migrating (or never were)

			if (NetTime.Now > hostMigrationTime + hostMigrationGraceTime)
			{
				ValidateHostMigration();
				return;
			}

			if (locallyConnected.Count == 0) // If no one is connected, no one can invalidate us
			{
				ValidateHostMigration();
			}
		}


		private void AcceptHostMigration(RemotePeer remotePeer)
		{
			owner.Log("Host migration accepted by: " + remotePeer.PeerInfo);
			ValidateHostMigration();
		}


		private void CheckDisconnectionForHostMigrationInvalidation(string disconnectReason)
		{
			if (clientDisconnections != null)
			{
				clientDisconnections.AddDisconnection(disconnectReason);

				// Check invalidation conditions:
				if (clientDisconnections.HasDisconnectedByServerDisconnects
				    || clientDisconnections.HasTimedOutDisconnects && locallyConnected.Count == 0)
				{
					owner.Log("Host migration invalidated");
					owner.DisconnectAndThrow(UserVisibleStrings.ServerTimedOut); // Treat as original time-out
				}
			}
		}

#endregion
	}
}