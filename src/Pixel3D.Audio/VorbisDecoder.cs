// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;

namespace Pixel3D.Engine
{
	static class VorbisDecoder
	{
		public static unsafe SoundEffect DecodeVorbis(byte* start, byte* end, int expectedSampleCount, int loopStart, int loopLength)
		{
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

			FAudio.stb_vorbis_close(vorbis);

			return new SoundEffect(audioDataShort, 0, audioDataShort.Length, (int)info.sample_rate, (AudioChannels)info.channels,
					loopStart, loopLength);
		}
	}
}
