using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace Pixel3D.LoopRecorder
{
    public sealed class NaturalStringComparer : IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        public int Compare(string a, string b)
        {
            return StrCmpLogicalW(a, b);
        }
    }
}