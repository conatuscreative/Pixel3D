// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Lidgren.Network;

namespace Pixel3D.P2P
{
	internal static class P2PServerMessageExtensions
	{
		public static P2PServerMessage Validate(this P2PServerMessage m)
		{
			if (m < 0 || m > P2PServerMessage.Unknown)
				return P2PServerMessage.Unknown;
			return m;
		}

		public static void Write(this NetOutgoingMessage message, P2PServerMessage m)
		{
			message.Write((byte) m);
		}

		public static P2PServerMessage TryReadP2PServerMessage(this NetIncomingMessage message)
		{
			try
			{
				return ((P2PServerMessage) message.ReadByte()).Validate();
			}
			catch
			{
				return P2PServerMessage.Unknown;
			}
		}

		public static P2PServerMessage TryPeekP2PServerMessage(this NetIncomingMessage message)
		{
			try
			{
				return ((P2PServerMessage) message.PeekByte()).Validate();
			}
			catch
			{
				return P2PServerMessage.Unknown;
			}
		}
	}
}