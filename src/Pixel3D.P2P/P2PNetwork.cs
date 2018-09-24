// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lidgren.Network;
using Pixel3D.P2P.Diagnostics;

namespace Pixel3D.P2P
{
	public class P2PNetwork
	{
		internal readonly NetworkAppConfig appConfig;

		public P2PNetwork(NetworkAppConfig appConfig, NetworkLogHandler networkLogHandler = null,
			BadNetworkSimulation badNetworkSimulation = null)
		{
			if (appConfig == null)
				throw new ArgumentNullException("appConfig");

			this.appConfig = appConfig;
			NetworkLogHandler = networkLogHandler;

			Log("Configuration:");
			Log("  AppId = " + appConfig.AppId);
			Log("  Version = " + appConfig.ApplicationVersion);
			Log("  Signature = " + BitConverter.ToString(appConfig.ApplicationSignature));

			SetupSocket(badNetworkSimulation);

			LocalisedDisconnectReason = null;
		}


		#region Update

		/// <summary>
		///     Update network state, and poll for and handle messages.
		/// </summary>
		public void Update()
		{
			try
			{
				// Kick anyone who was queued up to be kicked
				if (peerManager != null)
					foreach (var remotePeer in networkDataErrorKickList)
						peerManager.KickDueToNetworkDataError(remotePeer);
				networkDataErrorKickList.Clear();


				NetIncomingMessage message;
				while ((message = netPeer.ReadMessage()) != null)
				{
					var recycle = true;

					LogNetMessage(message);

					switch (message.MessageType)
					{
						// Discovery:
						case NetIncomingMessageType.DiscoveryResponse:
							if (Discovery != null)
								Discovery.ReceiveDiscoveryResponse(message);
							break;

						// Endpoint requests
						case NetIncomingMessageType.EndpointRequestResult:
							if (OnExternalEndpointDiscovered != null)
								OnExternalEndpointDiscovered(message.SenderEndPoint);
							break;

						// Application-layer data:
						case NetIncomingMessageType.Data:
							if (message.DeliveryMethod == NetDeliveryMethod.ReliableOrdered &&
							    message.SequenceChannel == 0) // ... except network management
							{
								if (peerManager != null)
									peerManager.HandleMessage(message, ref recycle);
							}
							else
							{
								var remotePeer = message.SenderConnection.Tag as RemotePeer;
								if (remotePeer != null)
									remotePeer.QueueMessage(message, ref recycle);
								else
									Debug.Assert(message.SenderConnection.Status == NetConnectionStatus.Disconnected
									             || message.SenderConnection.Status ==
									             NetConnectionStatus.Disconnecting);
							}

							break;

						// All other messages are presumed to be for the peer manager (or garbage):
						default:
							if (peerManager != null)
								peerManager.HandleMessage(message, ref recycle);
							break;
					}

					if (recycle)
						netPeer.Recycle(message);
				}

				if (peerManager != null)
					peerManager.Update();
			}
			catch (NetworkDisconnectionException)
			{
				Debug.Assert(peerManager == null); // <- we did actually get disconnected, right?
			}


			if (Discovery != null)
				Discovery.Update();
		}

		#endregion


		#region NAT Punch

		/// <summary>Send a generic punch message, with no expectation of response</summary>
		public void GenericPunch(IPEndPoint endpoint)
		{
			if (netPeer != null && netPeer.Status == NetPeerStatus.Running)
			{
				var message = netPeer.CreateMessage();
				// Deliberately zero-length message
				netPeer.SendUnconnectedMessage(message, endpoint);
			}
		}

		#endregion


		#region Logging

		public NetworkLogHandler NetworkLogHandler { get; set; }

		public void Log(string message)
		{
			if (NetworkLogHandler != null)
				NetworkLogHandler.HandleMessage(message);
		}

		internal void LogException(Exception exception)
		{
			if (exception != null)
			{
				Log("Exception:");
				Log(exception.ToString());
			}
		}

		private void LogNetMessage(NetIncomingMessage message)
		{
			if (NetworkLogHandler != null)
				NetworkLogHandler.InternalReceiveMessage(message);
		}

		#endregion


		#region Network Data / Protocol Error Handling

		private readonly List<RemotePeer> networkDataErrorKickList = new List<RemotePeer>();

		// NOTE: It's not always strictly a data error. So this method could use a better name.
		/// <summary>
		///     Call this when a RemotePeer provides invalid network data or otherwise needs to be disconnected.
		///     If the remote peer is a client, they will stop providing network messages and will be
		///     disconnected/kicked (depending on our authority) at the next call to Update.
		///     If the remote peer is the server, we will disconnect from the network and a
		///     <see cref="NetworkDisconnectionException" /> will be thrown.
		/// </summary>
		/// <remarks>
		///     Application layer should call this when it cannot throw back into the network system
		///     (from its event handlers) to get an orderly disconnection (ie: outside of Update).
		/// </remarks>
		public void NetworkDataError(RemotePeer remotePeer, Exception exception)
		{
			if (remotePeer == null)
				throw new ArgumentNullException("remotePeer");
			if (peerManager == null)
				throw new InvalidOperationException("Network not active");
			// (really we should be checking whether the remote peer is our's)

			Log("Network data error from " + remotePeer.PeerInfo);
			LogException(exception);

			// Stop any more messages from being read, until we can disconnect them properly!
			remotePeer.ClearMessageQueue();

			if (remotePeer.PeerInfo.IsServer)
				DisconnectAndThrow(UserVisibleStrings.ErrorInDataFromServer);
			else
				networkDataErrorKickList.Add(remotePeer);
		}


		private int unconnectedProtocolErrorCount;

		internal void UnconnectedProtocolError(string message)
		{
			Debug.Assert(false); // Alert

			const int maxLogCount = 50;

			unconnectedProtocolErrorCount++;
			if (unconnectedProtocolErrorCount <= maxLogCount)
			{
				Log("Unconnected Protocol Error: " + message);
				if (unconnectedProtocolErrorCount == maxLogCount)
					Log("Stopping logging of unconnected protocol errors.");
			}
		}

		#endregion


		#region Lidgren Socket Management (NetPeer)

		internal NetPeer netPeer;

		public int PortNumber => netPeer.Port;
		public bool UsingKnownPort => PortIsKnown(netPeer.Port);

		public bool PortIsKnown(int port)
		{
			return appConfig.KnownPorts.Contains(port);
		}

		private void SetupSocket(BadNetworkSimulation badNetworkSimulation)
		{
			// Try each known port to see if we can open a socket...
			for (var i = 0; i < appConfig.KnownPorts.Length; i++)
				try
				{
					var config = CreateNetPeerConfiguration(badNetworkSimulation);
					config.Port = appConfig.KnownPorts[i];
					netPeer = new NetPeer(config);
					netPeer.Start();

					Log("Socket opened on port " + PortNumber);
					return;
				}
				catch (SocketException socketException) // Probably port unavailable
				{
					if (socketException.SocketErrorCode != SocketError.AddressAlreadyInUse)
						throw; // Actually, it was something else
				}


			Log("Known port unavailable, auto-assigning port...");
			{
				// Try again with auto-assigned port
				var config = CreateNetPeerConfiguration(badNetworkSimulation);
				config.DisableMessageType(NetIncomingMessageType.DiscoveryRequest); // <- will enable when we need it

				config.Port = 0;
				netPeer = new NetPeer(config);
				netPeer.Start();

				Log("Socket opened on port " + PortNumber);
			}
		}


		private NetPeerConfiguration CreateNetPeerConfiguration(BadNetworkSimulation badNetworkSimulation)
		{
			var config = new NetPeerConfiguration(appConfig.AppId);
			if (badNetworkSimulation != null)
				badNetworkSimulation.ApplySettingsToConfig(config);

			// TODO: Re-enable UPnP... or, better yet, replace it with an implementation that doesn't block the network thread!
			//config.EnableUPnP = true; // <- Lidgren locks this property, so it must be set here

			// NOTE: Additional configuration options are enabled/disabled as required:
			//       These NetIncomingMessageTypes: DiscoveryRequest, DiscoveryResponse, UnconnectedData (for NAT punch through)
			//       Also: AcceptIncomingConnections


			// NOTE: Hard-coded time-outs in the P2P layer (see "P2P Network.pptx") depend on the
			//       configuration using these default values.
			//       (Ideally the time-outs should not be hard-coded, but calculated from these values)
			Debug.Assert(config.ConnectionTimeout == 25.0f);
			Debug.Assert(config.PingInterval == 4.0f);
			Debug.Assert(config.ResendHandshakeInterval == 3.0f);
			Debug.Assert(config.MaximumHandshakeAttempts == 5);

			return config;
		}


		public ShutdownWaiter Shutdown()
		{
			netPeer.Shutdown(DisconnectStrings.Shutdown);
			return new ShutdownWaiter(netPeer);
		}


		public void RequestExternalEndpoint()
		{
			Log("Requesting external endpoint");
			netPeer.RequestExternalEndpoint();
		}

		public event Action<IPEndPoint> OnExternalEndpointDiscovered;

		public IPEndPoint GetInternalEndpoint()
		{
			IPAddress subnetMask;
			return new IPEndPoint(NetUtility.GetMyAddress(out subnetMask), netPeer.Port);
		}

		#endregion


		#region Discovery

		public void StartDiscovery()
		{
			if (Discovery == null)
			{
				Log("Starting discovery");
				Discovery = new Discovery(this, appConfig.KnownPorts);
			}
		}

		public void StopDiscovery()
		{
			if (Discovery != null)
			{
				Log("Stopping discovery");
				Discovery.Stop();
				Discovery = null;
			}
		}

		public Discovery Discovery { get; private set; }

		#endregion


		#region Network Connect / Start

		/// <summary>
		///     Parse a hostname or ip address with an optional port to an IPEndPoint.
		///     Throws on error.
		/// </summary>
		public IPEndPoint ParseAndResolveEndpoint(string ipOrHostOptionalPort)
		{
			var ipOrHost = ipOrHostOptionalPort;
			var port = appConfig.KnownPorts[0]; // Could eventually add support for trying multiple known ports.

			if (ipOrHostOptionalPort.Contains(':'))
			{
				var split = ipOrHostOptionalPort.Split(':');
				if (split.Length != 2)
					throw new Exception("Parse error");
				ipOrHost = split[0];
				port = int.Parse(split[1]);
			}

			return NetUtility.Resolve(ipOrHost, port);
		}


		/// <summary>
		///     Connect to a network game (connect to the game's P2P server).
		/// </summary>
		/// <param name="p2pServerEndPoint">Pass null to delay connection and use side-channel verification</param>
		public void ConnectToGame(INetworkApplication networkApplication, string playerName, byte[] playerData,
			IPEndPoint p2pServerEndPoint,
			ulong sideChannelId, int sideChannelToken)
		{
			if (networkApplication == null)
				throw new ArgumentNullException("networkApplication");

			if (peerManager != null)
				throw new InvalidOperationException("Already running");

			LocalisedDisconnectReason = null;

			NetworkApplication = networkApplication;
			var p2pClient = new P2PClient(this, playerName, playerData, sideChannelId, sideChannelToken);
			if (p2pServerEndPoint != null)
				p2pClient.ConnectImmediate(p2pServerEndPoint);
			peerManager = p2pClient;
		}


		/// <summary>
		///     Start a network game (as the P2P server).
		/// </summary>
		/// <param name="gameName">The user-visible name of the game.</param>
		/// <param name="respondToDiscovery">Set to true if on LAN, otherwise don't care.</param>
		/// <param name="openToInternet">Set to false if on LAN, true if on Internet.</param>
		public void StartGame(INetworkApplication networkApplication, string playerName, byte[] playerData,
			string gameName, bool openToInternet,
			bool sideChannelAuth, ulong sideChannelId)
		{
			if (networkApplication == null)
				throw new ArgumentNullException("networkApplication");

			if (peerManager != null)
				throw new InvalidOperationException("Already running");

			LocalisedDisconnectReason = null;

			NetworkApplication = networkApplication;
			peerManager = new P2PServer(this, playerName, playerData, gameName, openToInternet, sideChannelAuth,
				sideChannelId);

			networkApplication.StartOnServer();
		}

		#endregion


		#region Network Disconnect

		// TODO: This is not very nice. Should have an event to raise (probably queued and from Update) when we lose network connection.
		/// <summary>The disconnection reason. Or null if not disconnected.</summary>
		public string LocalisedDisconnectReason { get; private set; }

		/// <summary>
		///     Disconnect and reset for starting or connnecting to a new game.
		///     NOTE: Pass "null" to disconnect without error - otherwise the passed reason is considered an error (TODO: This is
		///     ugly and should get fixed!).
		/// </summary>
		public void Disconnect(string localisedReason)
		{
			if (localisedReason == null)
				Log("Disconnecting from network.");
			else
				Log("Disconnecting from network because: " + localisedReason);
			LocalisedDisconnectReason = localisedReason;


			if (peerManager != null)
				peerManager.HandleLocalDisconnection();
			peerManager = null;

			if (NetworkApplication != null)
				NetworkApplication.Shutdown();

			LocalPeerInfo = null;
			GameInfo = null;
			NetworkApplication = null;

			ClearApplicationConnections();


			unconnectedProtocolErrorCount = 0;
		}

		/// <summary>
		///     Disconnect and raise a <see cref="NetworkDisconnectionException" />. Must be inside something that will catch
		///     it!
		/// </summary>
		internal void DisconnectAndThrow(string localisedReason)
		{
			Disconnect(localisedReason);
			throw new NetworkDisconnectionException();
		}

		#endregion


		#region P2P Network / Network Game

		internal IPeerManager peerManager;
		internal INetworkApplication NetworkApplication { get; private set; }


		/// <summary>Information about the game we are currently connected to.</summary>
		public GameInfo GameInfo { get; internal set; }

		/// <summary>Information about ourselves, according to the P2P server. Can be null.</summary>
		public PeerInfo LocalPeerInfo { get; internal set; }


		public bool IsActive => peerManager != null;
		public bool IsServer => peerManager != null && peerManager is P2PServer;
		public bool IsApplicationConnected => IsActive && LocalPeerInfo != null && LocalPeerInfo.IsApplicationConnected;

		#endregion


		#region Side-Channel Auth

		public int SideChannelAdd(ulong ownerId)
		{
			var server = peerManager as P2PServer;
			if (server == null)
			{
				Log("ERROR: SideChannelAdd when not the server");
				return 0;
			}

			return server.SideChannelAdd(ownerId);
		}

		public void SideChannelRemove(ulong ownerId)
		{
			var server = peerManager as P2PServer;
			if (server == null)
			{
				Log("ERROR: SideChannelRemove when not the server");
				return;
			}

			server.SideChannelRemove(ownerId);
		}

		/// <summary>Following host-migration, call this after re-adding all authorised users, once we have become the server</summary>
		internal void SideChannelValidateAndKick()
		{
			var server = peerManager as P2PServer;
			if (server == null)
			{
				Log("ERROR: SideChannelValidateAndKick when not the server");
				return;
			}

			server.SideChannelValidateAndKick();
		}


		/// <summary>Caller should limit the number of endpoints we attempt to join</summary>
		public void SideChannelTryVerifyAndConnect(IPEndPoint potentialEndpoint)
		{
			var p2pClient = peerManager as P2PClient;
			if (p2pClient != null) p2pClient.TryToVerifyAndConnect(potentialEndpoint);
		}

		#endregion


		#region Application-Layer Connections

		/// <remarks>
		///     This list can contain "stale" (disconnected) connections. Just send to them anyway and let Lidgren deal with it.
		/// </remarks>
		private readonly List<NetConnection> broadcastList = new List<NetConnection>();

		private readonly List<RemotePeer> remotePeerList = new List<RemotePeer>();
		public ReadOnlyList<RemotePeer> RemotePeers => new ReadOnlyList<RemotePeer>(remotePeerList);


		internal void AddApplicationConnection(RemotePeer remotePeer)
		{
			Debug.Assert(remotePeer.PeerInfo.IsApplicationConnected);

			Debug.Assert(!remotePeerList.Contains(remotePeer));
			Debug.Assert(!broadcastList.Contains(remotePeer.Connection));

			remotePeerList.Add(remotePeer);
			if (remotePeer.Connection != null)
				broadcastList.Add(remotePeer.Connection);
		}

		internal void RemoveApplicationConnection(RemotePeer remotePeer)
		{
			Debug.Assert(remotePeerList.Contains(remotePeer));

			remotePeerList.Remove(remotePeer);
			broadcastList.Remove(remotePeer.Connection);
		}

		internal void ClearApplicationConnections()
		{
			remotePeerList.Clear();
			broadcastList.Clear();
		}


		public NetOutgoingMessage CreateMessage()
		{
			var message = netPeer.CreateMessage();
			return message;
		}

		public void Broadcast(NetOutgoingMessage message, NetDeliveryMethod deliveryMethod, int sequenceChannel)
		{
			if (deliveryMethod == NetDeliveryMethod.ReliableOrdered && sequenceChannel == 0)
				throw new ArgumentException("ReliableOrdered channel 0 is reserved for P2P network management.");

			netPeer.SendMessage(message, broadcastList, deliveryMethod, sequenceChannel);
		}

		#endregion
	}
}