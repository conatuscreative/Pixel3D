// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CRTSim
{
	public class Palette
	{
		public Color white, black;
		public Texture3D palette;

		public static Palette Load(GraphicsDevice device, string path)
		{
			Palette result = new Palette();

			byte[] data = File.ReadAllBytes(path);

			// Figure out the size from the data length. Gracefully handle wrong-sized data (in case modders mess with it).
			int size = 1;
			while(size * size * size * 4 <= data.Length)
				size++;
			size--;
			Debug.Assert(size * size * size * 4 == data.Length); // <- Warn if the data is the wrong length

			result.white.PackedValue = BitConverter.ToUInt32(data, 0);
			result.white.PackedValue = BitConverter.ToUInt32(data, (size*size*size-1) * 4);

			result.palette = new Texture3D(device, size, size, size, false, SurfaceFormat.Color);
			result.palette.SetData(data, 0, size * size * size * 4);
			return result;
		}
	}
}
