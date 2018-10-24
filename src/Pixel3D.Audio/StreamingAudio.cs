// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Microsoft.Xna.Framework.Audio;
using Pixel3D.Audio;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Pixel3D.Engine
{
	class StreamingAudio : IStreamingAudio
	{
		public bool IsLooped { get; set; }

		float internalVolume;
		public float Volume
		{
			get { return internalVolume; }
			set
			{
				internalVolume = value;
				if(instance != null)
					instance.Volume = value;
			}
		}


		int loopStart;
		
		IntPtr vorbis;
		const int bufferSamples = 2048;
		const int maxChannels = 2;
		float[] audioBuffer;

		int channels;
		DynamicSoundEffectInstance instance;


		public unsafe void Open(byte* vorbisStart, byte* vorbisEnd, int loopStart = 0)
		{
			if(vorbis != IntPtr.Zero)
				throw new Exception("StreamingAudio mismatched Open");

			int error; // <- TODO: Do something useful with this?
			vorbis = FAudio.stb_vorbis_open_memory((IntPtr)vorbisStart, (int)(vorbisEnd - vorbisStart), out error, IntPtr.Zero);
			if(vorbis == IntPtr.Zero)
				return; // <- Does nothing

			this.loopStart = loopStart;

			var info = FAudio.stb_vorbis_get_info(vorbis);
			channels = Math.Min(maxChannels, info.channels);
			audioBuffer = new float[bufferSamples * channels];

			instance = new DynamicSoundEffectInstance((int)info.sample_rate, (AudioChannels)channels);
			instance.Volume = internalVolume;
			instance.BufferNeeded += FillBuffer;
			FillBuffer(null, EventArgs.Empty);
		}

		// NOTE: We need our own binding so we can pass a position part-way into the buffer.
		[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
		public static unsafe extern int stb_vorbis_get_samples_float_interleaved(
			IntPtr f,
			int channels,
			float* buffer,
			int num_floats
		);

		private unsafe void FillBuffer(object sender, EventArgs e)
		{
			int totalSamples = 0;
			
			if(vorbis != IntPtr.Zero)
			{
				fixed (float* audioBufferPinned = audioBuffer)
				{
					int previousSamples = -1; // <- Just silence if we cannot read anything even after seeking
					do
					{
						int samples = stb_vorbis_get_samples_float_interleaved(vorbis, channels,
								audioBufferPinned + totalSamples * channels,
								audioBuffer.Length - totalSamples * channels);
						Debug.Assert(samples >= 0); // <- stb_vorbis should never return negative samples!
						totalSamples += samples;

						if(samples == 0)
						{
							if(IsLooped && previousSamples != 0)
							{
								if(loopStart == 0)
									FAudio.stb_vorbis_seek_start(vorbis);
								else
									FAudio.stb_vorbis_seek(vorbis, (uint)loopStart);
							}
							else
							{
								break;
							}
						}

						previousSamples = samples;
					}
					while(totalSamples < bufferSamples);
				}
			}

			Debug.Assert(totalSamples <= bufferSamples);
			if(totalSamples < bufferSamples)
			{
				Array.Clear(audioBuffer, totalSamples * channels, audioBuffer.Length - totalSamples * channels);
				totalSamples = bufferSamples;
			}
			
			instance.SubmitFloatBufferEXT(audioBuffer, 0, totalSamples * channels);
		}


		public void Close()
		{
			if(instance != null)
			{
				instance.BufferNeeded -= FillBuffer;
				instance.Dispose();
				instance = null;
			}

			if(vorbis != IntPtr.Zero)
				FAudio.stb_vorbis_close(vorbis);
			vorbis = IntPtr.Zero;
		}

		
		public void Play()
		{
			instance.Play();
		}

		public void Pause()
		{
			instance.Pause();
		}

	}
}
