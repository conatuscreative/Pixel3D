// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.P2P
{
	/// <summary>Messages from the P2P server to the P2P client</summary>
	internal enum P2PServerMessage
	{
		/// <summary>
		///     Initial data for <see cref="P2PClient" /> to connect to the P2P network
		/// </summary>
		NetworkStartInfo,

		/// <summary>
		///     Another client has joined the network. Perform NAT punchthrough to that peer and allow them to connect.
		/// </summary>
		PeerJoinedNetwork,

		/// <summary>
		///     Another client has left the network. Disconnect them.
		/// </summary>
		PeerLeftNetwork,

		/// <summary>
		///     Another client on the network reported you disconnected. Are you still alive?
		/// </summary>
		YouWereDisconnectedBy,

		/// <summary>
		///     A peer just became connected to the network, connect them at the application layer.
		/// </summary>
		PeerBecameApplicationConnected,

		/// <summary>
		///     We just became the server due to host migration
		/// </summary>
		HostMigration,


		Unknown
	}
}