// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Lidgren.Network;

namespace Pixel3D.P2P
{
	internal static class P2PClientMessageExtensions
	{
		public static void Write(this NetOutgoingMessage message, P2PClientMessage m)
		{
			message.Write((byte) m);
		}

		public static P2PClientMessage TryReadP2PClientMessage(this NetIncomingMessage message)
		{
			try
			{
				return (P2PClientMessage) message.ReadByte();
			}
			catch
			{
				return P2PClientMessage.Unknown;
			}
		}
	}
}