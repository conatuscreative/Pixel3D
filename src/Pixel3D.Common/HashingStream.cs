// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.IO;

namespace Pixel3D
{
	public class HashingStream : Stream
	{
		private long position;

		public HashingStream()
		{
			position = 0;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			for (var i = 0; i < count; i++)
				PushByte(buffer[
					i + offset]); // <- PERF: Could be faster by pushing more than 1 byte at a time (although is alignment an issue?)

			position += count;
		}

		public override void WriteByte(byte value)
		{
			PushByte(value);
			position++;
		}

		#region Hashing

		// Using MurmurHash3, because it's relatively easy to implement, very fast, and has excellent distribution.
		// It also doesn't require tables (contrast: CRC). And it's in the public domain, as per:
		// https://code.google.com/p/smhasher/source/browse/trunk/MurmurHash3.cpp
		//
		// Doing the 128-bit variant, as that is best for data identification (contrast: 32-bit for hash tables)
		// Doing it 32-bit wide, as XNA forces us to run 32-bit, and that is fastest.

		private static uint ROTL32(uint x, int r)
		{
			return (x << r) | (x >> (32 - r));
		}

		private static uint fmix32(uint h)
		{
			h ^= h >> 16;
			h *= 0x85ebca6b;
			h ^= h >> 13;
			h *= 0xc2b2ae35;
			h ^= h >> 16;

			return h;
		}

		private const uint c1 = 0x239b961b;
		private const uint c2 = 0xab0e9789;
		private const uint c3 = 0x38b34ae5;
		private const uint c4 = 0xa1e38b93;


		/// <summary>NOTE: Rewrites hash and clobbers data</summary>
		private static void HashBlock(ref Value128 hash, ref Value128 data)
		{
			data.v1 *= c1;
			data.v1 = ROTL32(data.v1, 15);
			data.v1 *= c2;
			hash.v1 ^= data.v1;

			hash.v1 = ROTL32(hash.v1, 19);
			hash.v1 += hash.v2;
			hash.v1 = hash.v1 * 5 + 0x561ccd1b;

			data.v2 *= c2;
			data.v2 = ROTL32(data.v2, 16);
			data.v2 *= c3;
			hash.v2 ^= data.v2;

			hash.v2 = ROTL32(hash.v2, 17);
			hash.v2 += hash.v3;
			hash.v2 = hash.v2 * 5 + 0x0bcaa747;

			data.v3 *= c3;
			data.v3 = ROTL32(data.v3, 17);
			data.v3 *= c4;
			hash.v3 ^= data.v3;

			hash.v3 = ROTL32(hash.v3, 15);
			hash.v3 += hash.v4;
			hash.v3 = hash.v3 * 5 + 0x96cd1c35;

			data.v4 *= c4;
			data.v4 = ROTL32(data.v4, 18);
			data.v4 *= c1;
			hash.v4 ^= data.v4;

			hash.v4 = ROTL32(hash.v4, 13);
			hash.v4 += hash.v1;
			hash.v4 = hash.v4 * 5 + 0x32ac3b17;
		}


		/// <summary>NOTE: Rewrites hash and clobbers tail</summary>
		private static void HashTailAndFinalize(ref Value128 hash, ref Value128 tail, uint length)
		{
			// NOTE: We're truncating length to 32-bits, because it will almost always be small enough


			//
			// Tail:
			var tailLength = length & 15;
			switch ((tailLength + 3) >> 2) // Number of dwords of data in the tail, rounded up
			{
				case 4:
					tail.v4 *= c4;
					tail.v4 = ROTL32(tail.v4, 18);
					tail.v4 *= c1;
					hash.v4 ^= tail.v4;
					goto case 3;
				case 3:
					tail.v3 *= c3;
					tail.v3 = ROTL32(tail.v3, 17);
					tail.v3 *= c4;
					hash.v3 ^= tail.v3;
					goto case 2;
				case 2:
					tail.v2 *= c2;
					tail.v2 = ROTL32(tail.v2, 16);
					tail.v2 *= c3;
					hash.v2 ^= tail.v2;
					goto case 1;
				case 1:
					tail.v1 *= c1;
					tail.v1 = ROTL32(tail.v1, 15);
					tail.v1 *= c2;
					hash.v1 ^= tail.v1;
					break;
			}


			//
			// Finalization:

			hash.v1 ^= length;
			hash.v2 ^= length;
			hash.v3 ^= length;
			hash.v4 ^= length;

			hash.v1 += hash.v2;
			hash.v1 += hash.v3;
			hash.v1 += hash.v4;
			hash.v2 += hash.v1;
			hash.v3 += hash.v1;
			hash.v4 += hash.v1;

			hash.v1 = fmix32(hash.v1);
			hash.v2 = fmix32(hash.v2);
			hash.v3 = fmix32(hash.v3);
			hash.v4 = fmix32(hash.v4);

			hash.v1 += hash.v2;
			hash.v1 += hash.v3;
			hash.v1 += hash.v4;
			hash.v2 += hash.v1;
			hash.v3 += hash.v1;
			hash.v4 += hash.v1;
		}

		#endregion

		#region Hash State

		private int pendingLength;
		private Value128 pending;
		private Value128 hash;

		private void PushByte(uint value)
		{
			var data = pending;
			unsafe
			{
				((byte*) &data)[pendingLength] = (byte) value;
			}

			if (pendingLength == 15)
			{
				HashBlock(ref hash, ref data);
				pending = default(Value128);
				pendingLength = 0;
			}
			else
			{
				pending = data;
				pendingLength++;
			}
		}

		/// <summary>Get the value of the hash at the current position in the stream</summary>
		public Value128 GetHash()
		{
			// IMPORTANT: Work on copies, because our hashing function clobbers these, and, although we'll probably never need to
			//            write to it again, it'd be nice if getting the hash didn't convert the stream to read-only (or "break" the hash).
			var tempHash = hash;
			var tempTail = pending;

			HashTailAndFinalize(ref hash, ref tempTail, (uint) position);

			return tempHash;
		}

		#endregion

		#region Boring Stream Stuff

	    public override bool CanRead { get { return false; } } 

	    public override bool CanSeek { get { return false; } }

		public override bool CanWrite { get { return true; } }

		public override void Flush() { }

	    public override long Length { get { return position; } }

	    public override long Position
		{
	        get
	        {
	            return position;
	        }
	        set
	        {
	            throw new InvalidOperationException();
	        }
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new InvalidOperationException();
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException();
		}

		#endregion
	}
}