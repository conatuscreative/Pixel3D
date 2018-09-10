// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace Pixel3D.LoopRecorder
{
	public sealed class NaturalStringComparer : IComparer<string>
	{
		public int Compare(string a, string b)
		{
			return StrCmpLogicalW(a, b);
		}

		[DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
		private static extern int StrCmpLogicalW(string psz1, string psz2);
	}
}