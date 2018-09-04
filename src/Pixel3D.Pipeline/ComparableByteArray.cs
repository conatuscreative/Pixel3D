using System;
using System.Runtime.InteropServices;

namespace Pixel3D.Pipeline
{
	// Basically copied from ComparableImageData
	public unsafe struct ComparableByteArray : IEquatable<ComparableByteArray>
	{
		public ComparableByteArray(byte[] source)
		{
			this.data = source;

			if(data == null)
			{
				hash = 0;
				return;
			}

			// https://en.wikipedia.org/wiki/Jenkins_hash_function
			hash = 0;
			int uintLength = data.Length / 4;
			fixed(byte* ptr = source)
			{
				uint* uintPtr = (uint*)ptr;
				for(int i = 0; i < uintLength; i++)
				{
					hash += uintPtr[i];
					hash += (hash << 10);
					hash ^= (hash >> 6);
				}
			}

			// Fill up the last few bytes:
			int remaining = source.Length % 4;
			uint final = 0;
			switch(remaining)
			{
				case 3: final |= ((uint)source[uintLength*4 + 2] << 24); goto case 2;
				case 2: final |= ((uint)source[uintLength*4 + 1] << 16); goto case 1;
				case 1: final |= ((uint)source[uintLength*4]);
				{
					hash += final;
					hash += (hash << 10);
					hash ^= (hash >> 6);
				}
					break;
			}

			hash += (hash << 3);
			hash ^= (hash >> 11);
			hash += (hash << 15);
		}


		public byte[] data;
		public uint hash;

		public override int GetHashCode()
		{
			return (int)hash;
		}

		public override bool Equals(object obj)
		{
			if(obj is ComparableByteArray)
				return Equals((ComparableByteArray)obj);
			else
				return false;
		}


		[DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
		private static unsafe extern int memcmp(byte[] data1, byte[] data2, UIntPtr bytes);

		public bool Equals(ComparableByteArray other) // from IEquatable<ComparableByteArray>
		{
			if(this.hash != other.hash)
				return false;

			if(this.data == null && other.data == null)
				return true;
			if(this.data == null || other.data == null)
				return false;

			if(this.data.Length != other.data.Length)
				return false;

			return 0 == memcmp(this.data, other.data, (UIntPtr)this.data.Length);
		}
	}
}