// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D.Audio
{
	public class Sound
	{
		#region REMOVEME

		// required by legacy editor

		// TODO: None of these seem to be used? (Old "AUF" thing?)
		[Obsolete]
		public float? pitch;

		[Obsolete]
		public float? pan;

		[Obsolete]
		public float? volume;

		/// <summary>TODO: Remove me: this was never actually used properly (it's marked on a few Cues that become Ambient sounds)</summary>
		[Obsolete]
		public bool isLooped;

		#endregion

		#region Editor

		public bool muted;

		#endregion

		public string path;

		#region Serialization

		public void Serialize(CueSerializeContext context)
		{
			if (context.Version >= 4)
				context.bw.Write(path);
			else
				throw new NotSupportedException("Legacy formats are not supported.");
		}

		/// <summary>Deserialize into new object instance</summary>
		public Sound(CueDeserializeContext context)
		{
			path = context.br.ReadString();
			if (context.Version < 4)
			{
				context.br.ReadNullableSingle(); // pitch
				context.br.ReadNullableSingle(); // pan
				context.br.ReadNullableSingle(); // volume
				context.br.ReadBoolean(); // isLooped
			}
		}

		public Sound()
		{
		}

		#endregion
	}
}