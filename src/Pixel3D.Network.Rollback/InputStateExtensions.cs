// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Diagnostics;
using Lidgren.Network;

namespace Pixel3D.Network.Rollback
{
	internal static class InputStateExtensions
	{
		#region Network Read/Write

		public static void WriteInputState(this NetOutgoingMessage message, InputState inputState, int bits)
		{
			// If this assert triggers, you've specified too few bits to use for inputs
			Debug.Assert(((uint) inputState & ((1u << bits) - 1u)) == (uint) inputState);

			message.Write((uint) inputState, bits);
		}

		public static InputState ReadInputState(this NetIncomingMessage message, int bits)
		{
			return (InputState) message.ReadUInt32(bits);
		}

		#endregion
	}
}