// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Pixel3D.Audio
{
	public class Cue
	{
		/// <summary>Sentinel value for when we cannot find a requested cue</summary>
		public static Cue missingCue = new Cue {friendlyName = "[missing cue]"};

		public readonly List<Sound> sounds = new List<Sound>();

		/// <summary>IMPORTANT: Do not use in gameplay code (not network safe)</summary>
		[NonSerialized] public string friendlyName;

		public int id;
		public float? maxPitch;
		public float? minPitch;
		public float pan;
		public float pitch;
		public int radius;

		public CueType type;
		public float volume;

	    public string EditorName
	    {
	        get { return friendlyName; }
	    }

	    public int SoundCount
	    {
	        get { return sounds.Count; }
	    }

	    #region Serialization

		public void Serialize(CueSerializeContext context)
		{
			context.bw.WriteNullableStringNonBlank(friendlyName);
			context.bw.Write((byte) type);
			context.bw.Write(radius);
			context.bw.Write(pitch);
			context.bw.Write(pan);
			context.bw.Write(volume);
			context.bw.Write(sounds.Count);
			foreach (var sound in sounds)
				context.WriteSound(sound);
			if (context.Version >= 2)
			{
				context.bw.WriteNullableSingle(minPitch);
				context.bw.WriteNullableSingle(maxPitch);
			}
		}

		public Cue()
		{
			radius = 100; // assumed default
		}

		/// <summary>Deserialize into new object instance</summary>
		public Cue(CueDeserializeContext context)
		{
			friendlyName = context.br.ReadNullableString();
			type = (CueType) context.br.ReadByte();
			radius = context.br.ReadInt32();
			pitch = context.br.ReadSingle();
			pan = context.br.ReadSingle();
			volume = context.br.ReadSingle();
			var soundsCount = context.br.ReadInt32();
			for (var i = 0; i < soundsCount; i++)
				sounds.Add(context.ReadSound());
			if (context.Version >= 2)
			{
				minPitch = context.br.ReadNullableSingle();
				maxPitch = context.br.ReadNullableSingle();
			}

			if (context.Version >= 3)
				if (radius == 0)
					radius = 100; // new default
		}

		/// <summary>Check that an Cue round-trips through serialization cleanly</summary>
		public void RoundTripCheck()
		{
			// Serialize a first time
			var firstMemoryStream = new MemoryStream();
			var firstSerializeContext = new CueSerializeContext(new BinaryWriter(firstMemoryStream));
			Serialize(firstSerializeContext);
			var originalData = firstMemoryStream.ToArray();

			// Then deserialize that data
			var br = new BinaryReader(new MemoryStream(originalData));
			var deserializeContext = new CueDeserializeContext(br);
			var deserialized = new Cue(deserializeContext);

			// Then serialize that deserialized data and see if it matches
			var secondMemoryStream = new MemoryCompareStream(originalData);
			var secondSerializeContext = new CueSerializeContext(new BinaryWriter(secondMemoryStream));
			deserialized.Serialize(secondSerializeContext);
		}

		#endregion

		#region File Read/Write

		public void WriteToFile(string path)
		{
			using (var stream = File.Create(path))
			{
				using (var zip = new GZipStream(stream, CompressionMode.Compress, true))
				{
					using (var bw = new BinaryWriter(zip))
					{
						Serialize(new CueSerializeContext(bw));
					}
				}
			}
		}

		public static Cue ReadFromFile(string path)
		{
			using (var stream = File.OpenRead(path))
			{
				using (var unzip = new GZipStream(stream, CompressionMode.Decompress, true))
				{
					using (var br = new BinaryReader(unzip))
					{
						var deserializeContext = new CueDeserializeContext(br);
						return new Cue(deserializeContext);
					}
				}
			}
		}

		#endregion

		#region Simulation Helpers

		/// <param name="random">This parameter will be mutated. Be aware of network-safety!</param>
		public float SelectPitch(XorShift random)
		{
			// Pitch variance:
			var cuePitch = pitch;
			if (minPitch.HasValue && maxPitch.HasValue)
			{
				// NOTE: This is wrong because it is non-linear. But I can't be bothered fixing it at this point. -AR
				var p = 1 + cuePitch; // XNA normalizes on 0.0
				var min = p * minPitch.GetValueOrDefault();
				var max = p * maxPitch.GetValueOrDefault();
				var randomValue = random._NetworkUnsafe_UseMeForAudioOnly_NextSingle();
				AudioMath.Lerp(min, max, randomValue);
				cuePitch = pitch - 1;
			}

			return cuePitch;
		}

		/// <param name="random">This parameter will be mutated. Be aware of network-safety!</param>
		/// <param name="cueStates">This parameter will be mutated. Be aware of network-safety!</param>
		public int SelectSound(XorShift random, ushort[] cueStates)
		{
			switch (type)
			{
				case CueType.Parallel:
					return 0; // <- should be ignored by playback code
				case CueType.Random:
					return random.Next(SoundCount);
				case CueType.Serial:
					return 0; // <- should be ignored by playback code
				case CueType.Cycle:
				{
					Debug.Assert(SoundCount <= 16); // Sound storage is 16 bits!
					var safeSoundCount = Math.Min(16, SoundCount);

					if (cueStates[id] == 0)
						cueStates[id] = (ushort) ((1u << safeSoundCount) - 1); // Reset

					// Find first bit:
					for (var i = 0; i < safeSoundCount; i++)
						if ((cueStates[id] & (1u << i)) != 0) // If set
						{
							cueStates[id] &= (ushort) ~(1u << i); // Clear
							return i;
						}

					// This should never happen:
					Debug.Assert(false);
					return 0;
				}
				case CueType.RandomCycle:
				{
					Debug.Assert(SoundCount <= 16); // Sound storage is 16 bits!
					var safeSoundCount = Math.Min(16, SoundCount);

					int bitCount;
					if (cueStates[id] == 0)
					{
						bitCount = safeSoundCount;
						cueStates[id] = (ushort) ((1u << safeSoundCount) - 1); // Reset
					}
					else
					{
						bitCount = 0;
						int s = cueStates[id];
						do
						{
							bitCount += s & 1;
							s >>= 1;
						} while (s != 0);
					}

					// Find selected bit:
					var choice = random.Next(bitCount);
					for (var i = 0; i < safeSoundCount; i++)
						if ((cueStates[id] & (1u << i)) != 0) // If set
						{
							if (choice == 0)
							{
								cueStates[id] &= (ushort) ~(1u << i); // Clear
								return i;
							}

							choice--;
						}

					// This should never happen:
					Debug.Assert(false);
					return 0;
				}

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion
	}
}