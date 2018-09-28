// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;
using Pixel3D.Audio;

namespace Pixel3D.Pipeline.Audio
{
	// Having to use vorbisfile instead of libvorbis directly makes me sad. -AR

	public static class VorbisfileDecoder
	{
		private static readonly Vorbisfile.ov_callbacks StaticCallbacks = new Vorbisfile.ov_callbacks
		{
			read_func = BufferReadFunc,
			seek_func = null,
			close_func = null,
			tell_func = null
		};

		[DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl,
			SetLastError = false)]
		[SuppressUnmanagedCodeSecurity]
		private static extern unsafe void* memcpy(void* dest, void* src, UIntPtr byteCount);

		private static unsafe IntPtr BufferReadFunc(IntPtr ptr, IntPtr size, IntPtr elements, IntPtr datasource)
		{
			var file = (FakeFile*) datasource;

			var s = size.ToInt64();
			var e = elements.ToInt64();

			var bytesToRead = e * s;
			var remainingBytes = file->end - file->position;
			if (bytesToRead > remainingBytes)
				bytesToRead = remainingBytes;

			e = bytesToRead / s;
			bytesToRead = e * s;

			Debug.Assert(bytesToRead >= 0);

			memcpy(ptr.ToPointer(), file->position, (UIntPtr) bytesToRead);
			file->position += bytesToRead;

			return (IntPtr) e;
		}

		public static unsafe SoundEffect Decode(byte* start, byte* end)
		{
			var file = new FakeFile {start = start, position = start, end = end};

			var sampleCount =
				*(int*) file
					.position; // <- We encoded this, before the packets start, because Vorbis doesn't know [not sure if this is still true for Ogg, haven't checked -AR]
			file.position += 4;
			var loopStart = *(int*) file.position;
			file.position += 4;
			var loopLength = *(int*) file.position;
			file.position += 4;

			// TODO: Consider modifying vorbisfile binding so we can stackalloc `vf`
			IntPtr vf;
			Vorbisfile.ov_open_callbacks((IntPtr) (&file), out vf, IntPtr.Zero, IntPtr.Zero, StaticCallbacks);

			var info = Vorbisfile.ov_info(vf, 0);

			var audioData = new byte[sampleCount * info.channels * 2]; // *2 for 16-bit audio (as required by XNA)

			fixed (byte* writeStart = audioData)
			{
				var writePosition = writeStart;
				var writeEnd = writePosition + audioData.Length;

				while (true)
				{
					int currentSection;
					var result = (int) Vorbisfile.ov_read(vf, (IntPtr) writePosition, (int) (writeEnd - writePosition),
						0, 2, 1, out currentSection);

					if (result == 0) // End of file
						break;
					if (result > 0)
						writePosition += result;
					if (writePosition >= writeEnd)
						break;
				}

				Debug.Assert(writePosition ==
				             writeEnd); // <- If this fires, something went terribly wrong. (TODO: Throw exception?)
			}

			Vorbisfile.ov_clear(ref vf);

			return new SoundEffect(audioData, 0, audioData.Length, (int) info.rate, (AudioChannels) info.channels,
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
							Decode(vorbisStart + input.offsets[i], vorbisStart + input.offsets[i + 1]);
					});
			}
		}

		private unsafe struct FakeFile
		{
			public byte* start;
			public byte* position;
			public byte* end;
		}
	}
}