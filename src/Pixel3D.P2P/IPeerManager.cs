// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Lidgren.Network;

namespace Pixel3D.P2P
{
	internal interface IPeerManager
	{
		/// <summary>Handle a network message (it will be recycled externally).</summary>
		void HandleMessage(NetIncomingMessage message, ref bool recycle);

		void Update();

		void HandleLocalDisconnection();

		void KickDueToNetworkDataError(RemotePeer remotePeer);
	}
}