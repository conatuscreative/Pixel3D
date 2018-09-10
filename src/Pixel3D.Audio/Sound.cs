// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D.Audio
{
	public class Sound
	{
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