// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Runtime.InteropServices;

namespace Pixel3D.Network.Rollback
{
	internal static class RollbackNative
	{
		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int memcmp(byte[] data1, byte[] data2, UIntPtr bytes);

		/// <returns>True if the buffers are the same</returns>
		public static bool CompareBuffers(byte[] buffer1, byte[] buffer2)
		{
			if (buffer1.Length != buffer2.Length)
				return false;

			return memcmp(buffer1, buffer2, (UIntPtr) buffer1.Length) == 0;
		}
	}
}