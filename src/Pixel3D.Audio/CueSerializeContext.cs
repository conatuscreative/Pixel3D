// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.IO;

namespace Pixel3D.Audio
{
	public class CueSerializeContext
	{
		public readonly BinaryWriter bw;

		public CueSerializeContext(BinaryWriter bw) : this(bw, formatVersion)
		{
		} // Default to writing current version

		public CueSerializeContext(BinaryWriter bw, int version)
		{
			this.bw = bw;
			Version = version;

			bw.Write(Version);
		}

		public void WriteSound(Sound sound)
		{
			sound.Serialize(this);
		}

		#region Version

		/// <summary>Increment this number when anything we serialize changes</summary>
		public const int formatVersion = 4;

		public int Version { get; private set; }

		#endregion
	}
}