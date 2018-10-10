// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pixel3D.Audio
{
	public struct SoundBank
	{
		public OrderedDictionary<string, int> lookup;
		public SafeSoundEffect[] sounds;

		public int Count { get { return sounds.Length; } }


		public SafeSoundEffect this[string name]
		{
			get
			{
				int i;
				if(lookup.TryGetValue(name, out i))
					return sounds[i];
				else
					return null;
			}
		}

		public static SoundBank CreateEmpty(AudioPackage input)
		{
			// IMPORTANT: The sound bank is definition data. We must return the same result no matter whether or not we have an audio device!
			//            (SafeSoundEffect serializes as reference only, so we don't need to care about its contents.)

			SoundBank result;
			result.lookup = input.lookup;
			result.sounds = new SafeSoundEffect[input.Count];
			for(int i = 0; i < result.sounds.Length; i++)
			{
				result.sounds[i] = new SafeSoundEffect();
			}

			return result;
		}


		// This is split out so that it can run late in the loading process (because it saturates the CPU)
		public static unsafe void DecodeVorbisData(AudioPackage source, SoundBank destination)
		{
			Debug.Assert(source.Count == destination.Count);
			Debug.Assert(ReferenceEquals(source.lookup, destination.lookup));

			if(!AudioDevice.Available)
				return;

			// IMPORTANT: This is lock-free, because each entry only writes to its own slot (everything else is read-only)
			fixed (byte* data = source.audioPackageBytes)
			{
				byte* vorbisStart = data + source.vorbisOffset;

				int count = destination.sounds.Length;
				//for (int i = 0; i < count; i++)
				Parallel.ForEach(Enumerable.Range(0, count), i =>
				{
					byte* start = vorbisStart + source.offsets[i];
					byte* end = vorbisStart + source.offsets[i+1];

					var expectedSampleCount = *(int*)start; // <- Encoded ourselves, to save a vorbis seek (stb_vorbis_stream_length_in_samples)
					start += 4;
					var loopStart = *(int*)start;
					start += 4;
					var loopLength = *(int*)start;
					start += 4;

					destination.sounds[i].owner = AudioSystem.createSoundEffectFromVorbisMemory(start, end, expectedSampleCount, loopStart, loopLength);
				});
			}
		}
		
	}


}
