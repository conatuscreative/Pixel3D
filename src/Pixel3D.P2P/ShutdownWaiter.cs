// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Diagnostics;
using System.Threading;
using Lidgren.Network;

namespace Pixel3D.P2P
{
	/// <summary>
	///     This is a dirty hack, because Lidgren marks its network threads as background.
	///     So suddenly exiting the main thread does not give the network threads a chance to
	///     exit cleanly (which includes sending disconnection notification to the remote,
	///     which will cause that end to time-out instead). So give them a chance to exit.
	/// </summary>
	public struct ShutdownWaiter
	{
		internal NetPeer netPeer;

		internal ShutdownWaiter(NetPeer netPeer)
		{
			this.netPeer = netPeer;
		}

		public void Wait(int milliseconds)
		{
			const int tickRate = 20; // ms
			for (var i = 0; i < milliseconds; i += tickRate)
			{
				if (netPeer.Status == NetPeerStatus.NotRunning)
					return; // Done
				Thread.Sleep(tickRate);
			}

			// If we get here, it didn't shutdown cleanly. Exit anyway.
			Debug.WriteLine("Did not shutdown cleanly!");
		}
	}
}