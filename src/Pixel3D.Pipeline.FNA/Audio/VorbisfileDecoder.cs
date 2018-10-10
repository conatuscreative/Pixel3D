// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;
using Pixel3D.Audio;

namespace Pixel3D.Pipeline.Audio
{
	// TODO: Move out of Pipeline (this is used at runtime, not asset build time)
	// TODO: Once these are fixed, don't reference this assembly from engine
	// TODO: Fix up usage of DLLs relating to Vorbisfile

	public static class VorbisfileDecoder
	{
		public static unsafe SoundEffect DecodePackageVorbis(byte* start, byte* end)
		{
			var expectedSampleCount = *(int*)start; // <- Encoded ourselves, to save a vorbis seek (stb_vorbis_stream_length_in_samples)
			start += 4;
			var loopStart = *(int*)start;
			start += 4;
			var loopLength = *(int*)start;
			start += 4;

			int error;
			var vorbis = FAudio.stb_vorbis_open_memory((IntPtr)start, (int)(end - start), out error, IntPtr.Zero);

			Debug.Assert(vorbis != IntPtr.Zero);
			if(vorbis == IntPtr.Zero)
				return null;

			var info = FAudio.stb_vorbis_get_info(vorbis);
			var audioDataFloat = new float[expectedSampleCount * info.channels];

			// A cusory read of stb_vorbis suggests that it will never partially fill our buffer
			int readSamples = FAudio.stb_vorbis_get_samples_float_interleaved(vorbis, info.channels, audioDataFloat, audioDataFloat.Length);
			Debug.Assert(readSamples == expectedSampleCount); // <- If this fires, the package is corrupt somehow (in release builds, just silently fail)


			// Annoying conversion:
			var audioDataShort = new byte[readSamples * info.channels * 2]; // *2 for 16-bit audio

			// This is taken from stb_vorbis FAST_SCALED_FLOAT_TO_INT / copy_samples
			// (Because the FAudio version of stb_vorbis is compiled without an integer conversion, we'll do it ourselves)
			const int shift = 15;
			const float magic = (1.5f * (1 << (23-shift)) + 0.5f/(1 << shift));
			const int addend = (((150-shift) << 23) + (1 << 22));

			fixed (float* sourcePinned = audioDataFloat)
			{
				fixed (byte* destinationPinnedBytes = audioDataShort)
				{
					float* source = sourcePinned;
					short* destination = (short*)destinationPinnedBytes;
					
					int length = readSamples * info.channels;
					for(int i = 0; i < length; i++)
					{
						int temp;
						*(float*)&temp = source[i] + magic;
						temp -= addend;
						if((uint)(temp + 32768) > 65535) // <- Clamp
							temp = (temp < 0) ? -32768 : 32767;

						destination[i] = (short)temp;
					}
				}
			}

			return new SoundEffect(audioDataShort, 0, audioDataShort.Length, (int)info.sample_rate, (AudioChannels)info.channels,
					loopStart, loopLength);
		}


		// This is split out so that it can run late in the loading process (because it saturates the CPU)
		public static unsafe void DecodeVorbisData(ReadAudioPackage.Result input)
		{
			if (!AudioDevice.Available)
				return; // <- No sound!

			// IMPORTANT: This is lock-free, because each entry only writes to its own slot (everything else is read-only)
			fixed (byte* data = input.audioPackageBytes)
			{
				var vorbisStart = data + input.vorbisOffset;

				var count = input.sounds.Length;
				//for (int i = 0; i < count; i++)
				Parallel.ForEach(Enumerable.Range(0, count),
					i =>
					{
						input.sounds[i].owner =
							DecodePackageVorbis(vorbisStart + input.offsets[i], vorbisStart + input.offsets[i + 1]);
					});
			}
		}
	}
}