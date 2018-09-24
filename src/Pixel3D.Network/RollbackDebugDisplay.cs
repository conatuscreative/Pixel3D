// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Microsoft.Xna.Framework;
using Pixel3D.Network.Common;
using Pixel3D.Network.Rollback;

namespace Pixel3D.Network
{
	public static class RollbackDebugDisplay
	{
		public static void Draw(DisplayText dt, RollbackDriver rollbackDriver)
		{
			dt.SetPosition(new Vector2(2, 2));

			if (rollbackDriver == null)
			{
				dt.Begin();
				dt.WriteLine("Waiting to start...", Color.DarkRed);
				dt.End();
				return;
			}


			dt.Begin();
			dt.WriteLine("Current = " + rollbackDriver.CurrentFrame + "   LNCF = " + rollbackDriver.LocalNCF +
			             "   SNCF = " + rollbackDriver.ServerNCF);
			dt.WriteLine("LNCF Offset = " + (rollbackDriver.CurrentFrame - rollbackDriver.LocalNCF));
			dt.WriteLine("SNCF Offset = " + (rollbackDriver.CurrentFrame - rollbackDriver.ServerNCF));
			dt.WriteLine("JLE Buffer Count = " + rollbackDriver.JLEBufferCount);
			dt.WriteLine();
			dt.WriteLine("Local Delay = " + rollbackDriver.LocalFrameDelay);
			dt.WriteLine();

			for (var i = 0; i < 4; i++)
			{
				var ifmf = rollbackDriver.InputFirstMissingFrame(i);
				if (ifmf.HasValue)
					dt.WriteLine("Input " + i + " offset = " + (rollbackDriver.CurrentFrame - (ifmf.Value - 1)));
				else
					dt.WriteLine("Input " + i + " is offline");
			}


			if (rollbackDriver.HasClockSyncInfo) // Only the client has interesting timer sync values:
			{
				dt.WriteLine();
				dt.WriteLine("Packet time offset = " + rollbackDriver.PacketTimeOffset.ToString("0.000"));
				dt.WriteLine("Timer correction rate = " + rollbackDriver.TimerCorrectionRate.ToString("0.00000"));
				dt.WriteLine("Sync clock offset = " + rollbackDriver.SynchronisedClockFrameOffset.ToString("0.000"));
			}


			if (rollbackDriver.debugDisableInputBroadcast)
			{
				dt.WriteLine();
				dt.WriteLine("Input Packet Send Disabled!!!", Color.Red);
			}

			dt.End();
		}
	}
}