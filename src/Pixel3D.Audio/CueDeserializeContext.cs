// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.IO;

namespace Pixel3D.Audio
{
	public class CueDeserializeContext
	{
		public readonly BinaryReader br;

		public CueDeserializeContext(BinaryReader br)
		{
			this.br = br;
			Version = br.ReadInt32();
			if (Version > CueSerializeContext.formatVersion)
				throw new Exception("Tried to load Cue with a version that is too new");
		}

		public int Version { get; private set; }

		public Sound ReadSound()
		{
			return new Sound(this);
		}
	}
}