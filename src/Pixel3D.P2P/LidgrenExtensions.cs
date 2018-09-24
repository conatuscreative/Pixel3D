// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Diagnostics;
using System.IO;
using Lidgren.Network;

namespace Pixel3D.P2P
{
	public static class LidgrenExtensions
	{
		/// <summary>Write a byte array, converting an empty array to null</summary>
		public static void WriteByteArray(this NetOutgoingMessage message, byte[] data)
		{
			message.WritePadBits();
			if (data == null || data.Length == 0)
			{
				message.WriteVariableUInt32(0);
			}
			else
			{
				message.WriteVariableUInt32((uint) data.Length);
				message.Write(data);
			}
		}

		/// <summary>Write a memory stream in a format readable by ReadByteArray. Resets the memory stream position to zero.</summary>
		public static void WriteMemoryStreamAsByteArray(this NetOutgoingMessage message, MemoryStream ms)
		{
			Debug.Assert(ms == null || ms.Length <= 1024 * 1024 * 1024); // 1GB is stupidly large

			message.WritePadBits();
			if (ms == null || ms.Length == 0)
			{
				message.WriteVariableUInt32(0);
			}
			else
			{
				message.WriteVariableUInt32((uint) ms.Length);

				// Directly write into message buffer:
				ms.Position = 0;
				message.EnsureBufferSize((message.LengthBytes + (int) ms.Length) * 8);
				ms.Read(message.Data, message.LengthBytes, (int) ms.Length);
				message.LengthBytes += (int) ms.Length;
			}
		}


		/// <summary>Read a byte array, converting an empty array to null</summary>
		public static byte[] ReadByteArray(this NetIncomingMessage message)
		{
			message.SkipPadBits();
			var length = (int) message.ReadVariableUInt32();
			if (length > 0)
				return message.ReadBytes(length);
			return null;
		}
	}
}