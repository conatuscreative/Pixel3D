// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Pixel3D.Audio
{
	public struct AudioPackage
	{
		public byte[] audioPackageBytes;
		public int vorbisOffset;
		public int[] offsets;
		public OrderedDictionary<string, int> lookup;

		public int Count { get { return lookup.Count; } }


		private static void ThrowError()
		{
			throw new Exception("Audio Package Corrupt");
		}

		public static AudioPackage Read(string path, byte[] header)
		{
			AudioPackage result;

#if !WINDOWS
			path = path.Replace('\\', '/');
#endif
			result.audioPackageBytes = File.ReadAllBytes(path);
			var ms = new MemoryStream(result.audioPackageBytes);

			//
			// Magic Number:
			if (result.audioPackageBytes.Length < header.Length)
				ThrowError();
			for (var i = 0; i < header.Length; i++)
				if (ms.ReadByte() != header[i])
					ThrowError();

			//
			// Audio File Table:
			var integerReadBuffer = new byte[4];
			if (ms.Read(integerReadBuffer, 0, 4) != 4)
				ThrowError();
			var indexLength = BitConverter.ToInt32(integerReadBuffer, 0);
			result.vorbisOffset = indexLength + (int) ms.Position;

			using (var br = new BinaryReader(new GZipStream(ms, CompressionMode.Decompress, true)))
			{
				var count = br.ReadInt32();
				result.offsets = new int[count + 1]; // <- For simplicity, offsets[0] = 0 (start of first sound)
				result.lookup = new OrderedDictionary<string, int>(count);
				for (var i = 0; i < count; i++)
				{
					result.lookup.Add(br.ReadString(), i);
					result.offsets[i + 1] = br.ReadInt32();
				}
			}

			return result;
		}

	}
}