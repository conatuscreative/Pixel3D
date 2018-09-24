// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.IO;
using Lidgren.Network;

namespace Pixel3D.P2P
{
	/// <summary>Interrface to talk to the application layer</summary>
	public interface INetworkApplication
	{
		/// <summary>Called when we are starting the game as the P2P server.</summary>
		void StartOnServer();

		/// <summary>Called when a remote peer joins the game and you are the P2P server.</summary>
		/// <param name="remotePeer">The joining peer.</param>
		/// <param name="joinMessage">
		///     The connection message that you can append user data to, to be received by
		///     <see cref="JoinOnClient" />.
		/// </param>
		/// <param name="connectedMessage">
		///     The connection message that you can append user data to, to be received by
		///     <see cref="ConnectedOnClient" />.
		/// </param>
		void JoinOnServer(RemotePeer remotePeer, NetOutgoingMessage joinMessage, NetOutgoingMessage connectedMessage);

		/// <summary>Called when a remote peer leaves the game and you are the P2P server.</summary>
		/// <param name="remotePeer">The leaving peer.</param>
		/// <param name="message">
		///     The disconnection message that you can append user data to, to be received by
		///     <see cref="LeaveOnClient" />.
		/// </param>
		void LeaveOnServer(RemotePeer remotePeer, NetOutgoingMessage message);

		/// <summary>Called when we have become the server due to host migration.</summary>
		/// <param name="message">The host migration that is received by <see cref="HostMigrationChangeHost" />.</param>
		void HostMigrationBecomeHost(NetOutgoingMessage message);


		/// <summary>Called when a remote peer joins the game and you are a P2P client.</summary>
		/// <param name="remotePeer">The joining peer.</param>
		/// <param name="message">The connection message, positioned at the user data sent by <see cref="JoinOnServer" />.</param>
		/// <remarks>
		///     On error, throw <see cref="NetworkDataException" /> or a derived exception (usually
		///     <see cref="ProtocolError" />)
		/// </remarks>
		void JoinOnClient(RemotePeer remotePeer, NetIncomingMessage message);

		/// <summary>Called when a remote peer leaves the game and you are a P2P client.</summary>
		/// <param name="remotePeer">The leaving peer.</param>
		/// <param name="message">The disconnection message, positioned at the user data sent by <see cref="LeaveOnServer" />.</param>
		/// <remarks>
		///     On error, throw <see cref="NetworkDataException" /> or a derived exception (usually
		///     <see cref="ProtocolError" />)
		/// </remarks>
		void LeaveOnClient(RemotePeer remotePeer, NetIncomingMessage message);

		/// <summary>Called when we have connected to the game as a P2P client.</summary>
		/// <param name="message">The connection message, positioned at the user data sent by <see cref="JoinOnServer" />.</param>
		/// <remarks>
		///     On error, throw <see cref="NetworkDataException" /> or a derived exception (usually
		///     <see cref="ProtocolError" />)
		/// </remarks>
		void ConnectedOnClient(NetIncomingMessage message);

		/// <summary>
		///     Called when the server has changed due to host migration. The P2PNetwork.RemotePeers collection has changed,
		///     and connection IDs may be repeated.
		/// </summary>
		/// <param name="newHost">The host that has become the new server.</param>
		/// <param name="message">The host migration message, sent by <see cref="HostMigrationBecomeHost" />.</param>
		/// <remarks>
		///     On error, throw <see cref="NetworkDataException" /> or a derived exception (usually
		///     <see cref="ProtocolError" />)
		/// </remarks>
		void HostMigrationChangeHost(RemotePeer newHost, NetIncomingMessage message);


		/// <summary>Called when the network is shutting the application down</summary>
		void Shutdown();

		/// <summary>Called on the server when it needs to respond to a discovery request, to provide application-specific data.</summary>
		MemoryStream GetDiscoveryData();
	}
}