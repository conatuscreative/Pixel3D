// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.Network.Rollback
{
	internal static class FastHash
	{
		private static uint ROTL32(uint x, int r)
		{
			return (x << r) | (x >> (32 - r));
		}


		public static unsafe uint Hash(byte[] buffer)
		{
			// Using MurmurHash3, because it's relatively easy to implement, very fast, and has excellent distribution.
			// It also doesn't require tables (contrast: CRC). And it's in the public domain, as per:
			// https://code.google.com/p/smhasher/source/browse/trunk/MurmurHash3.cpp
			//
			// Doing the 32-bit variant, because we want to be very fast (and I don't want to mess with passing 128-bit values around)

			const uint c1 = 0xcc9e2d51u;
			const uint c2 = 0x1b873593u;
			const uint seed = 0; // <- uhmn...

			var h1 = seed;

			var blockCount = buffer.Length / 4;

			fixed (byte* data = buffer) // In theory this is at least 4 and probably 8-byte aligned
			{
				var position = (uint*) data;
				var end = position + buffer.Length / 4;

				// Body:
				while (position != end)
				{
					var k1 = *position;

					k1 *= c1;
					k1 = ROTL32(k1, 15);
					k1 *= c2;

					h1 ^= k1;
					h1 = ROTL32(h1, 13);
					h1 = h1 * 5 + 0xe6546b64;

					position += 1;
				}

				// Tail:
				var tail = (byte*) end;

				{
					uint k1 = 0;
					switch (buffer.Length & 3)
					{
						case 3:
							k1 ^= (uint) tail[2] << 16;
							goto case 2;
						case 2:
							k1 ^= (uint) tail[1] << 8;
							goto case 1;
						case 1:
							k1 ^= tail[0];
							k1 *= c1;
							k1 = ROTL32(k1, 15);
							k1 *= c2;
							h1 ^= k1;
							break;
					}
				}

				// Finalize:
				h1 ^= (uint) buffer.Length;

				// Finalization mix - force all bits of a hash block to avalanche
				h1 ^= h1 >> 16;
				h1 *= 0x85ebca6b;
				h1 ^= h1 >> 13;
				h1 *= 0xc2b2ae35;
				h1 ^= h1 >> 16;

				return h1;
			}
		}
	}
}