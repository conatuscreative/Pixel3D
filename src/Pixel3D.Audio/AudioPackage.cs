// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;

namespace Pixel3D.Audio
{
	public unsafe sealed class AudioPackage : IDisposable
	{
		MemoryMappedFile file;
		MemoryMappedViewAccessor view;
		byte* filePointer;
		byte* vorbisPointer;

		public int vorbisOffset;
		public int[] offsets;
		public OrderedDictionary<string, int> lookup;

		public int Count { get { return lookup.Count; } }


		private static void ThrowError()
		{
			throw new Exception("Audio Package Corrupt");
		}



		public AudioPackage(string path, byte[] magicNumber)
		{
#if !WINDOWS
			path = path.Replace('\\', '/');
#endif
			file = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
			view = file.CreateViewAccessor();
			view.SafeMemoryMappedViewHandle.AcquirePointer(ref filePointer);

			//
			// Magic Number:
			for(int i = 0; i < magicNumber.Length; i++)
				if(filePointer[i] != magicNumber[i])
					ThrowError();

			//
			// Audio File Table:
			int tableLength = *(int*)(filePointer + magicNumber.Length);
			int tableStart = magicNumber.Length + 4;
			vorbisPointer = filePointer + tableStart + tableLength;

			using(var stream = file.CreateViewStream(tableStart, tableLength))
			{
				using(BinaryReader br = new BinaryReader(new GZipStream(stream, CompressionMode.Decompress, true)))
				{
					int count = br.ReadInt32();
					offsets = new int[count+1]; // <- For simplicity, offsets[0] = 0 (start of first sound)
					lookup = new OrderedDictionary<string, int>(count);
					for(int i = 0; i < count; i++)
					{
						lookup.Add(br.ReadString(), i);
						offsets[i+1] = br.ReadInt32();
					}
				}
			}
		}

		public void Dispose()
		{
			if(file != null)
			{
				if(view != null)
				{
					if(filePointer != null)
						view.SafeMemoryMappedViewHandle.ReleasePointer();
					filePointer = null;
					vorbisPointer = null;

					view.Dispose();
				}
				view = null;

				file.Dispose();
			}
			file = null;
		}


		internal unsafe struct Entry
		{
			public byte* start;
			public byte* end;

			public bool Valid { get { return start != null; } }

			public int ExpectedSamples { get { return *(int*)start; } } // <- Encoded ourselves, to save a vorbis seek (stb_vorbis_stream_length_in_samples)
			public int LoopStart { get { return *(int*)(start+4); } }
			public int LoopLength { get { return *(int*)(start+8); } }
			public byte* VorbisStart { get { return start + 12; } }

			public byte* VorbisEnd { get { return end; } }
		}

		internal Entry GetEntryByIndex(int i)
		{
			Debug.Assert((uint)i < Count);

			Entry entry;
			entry.start = vorbisPointer + offsets[i];
			entry.end = vorbisPointer + offsets[i+1];
			return entry;
		}

		internal Entry GetEntryByPath(string path)
		{
			int index;
			if(!lookup.TryGetValue(path, out index))
				return default(Entry);
			else
				return GetEntryByIndex(index);
		}


		// This is split out so that it can run late in the loading process (because it saturates the CPU)
		public void FillSoundEffectArray(SafeSoundEffect[] sounds)
		{
			if(!AudioDevice.Available)
				return;

			if(vorbisPointer == null)
				throw new ObjectDisposedException(typeof(AudioPackage).Name);
			
			// IMPORTANT: This is lock-free, because each entry only writes to its own slot (everything else is read-only)
			int count = sounds.Length;
			//for (int i = 0; i < count; i++)
			Parallel.ForEach(Enumerable.Range(0, count), i =>
			{
				var entry = GetEntryByIndex(i);

				sounds[i].owner = AudioSystem.createSoundEffectFromVorbisMemory(entry.VorbisStart, entry.VorbisEnd,
						entry.ExpectedSamples, entry.LoopStart, entry.LoopLength);
			});
		}


		public bool Contains(string musicPath)
		{
			return lookup.ContainsKey(musicPath);
		}
	}
}