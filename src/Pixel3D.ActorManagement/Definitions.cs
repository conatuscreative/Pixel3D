// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Pixel3D.Audio;
using Pixel3D.Strings;

namespace Pixel3D.ActorManagement
{
	public abstract class Definitions : IAudioDefinitions
	{
		public StringsProvider stringsProvider;

		#region Sound Effects

		protected OrderedDictionary<string, SafeSoundEffect> soundBank;

		/// <summary>Return a sound effect for the given cue</summary>
		public SafeSoundEffect GetSound(Cue cue, int index)
		{
			if (cue == null || ReferenceEquals(missingCue, cue))
				return null;

			var path = cue.sounds[index].path;
			if (path == null)
				return null;

			SafeSoundEffect result;
			if (soundBank.TryGetValue(path, out result))
				return result;

			return null;
		}

		/// <summary>Return the sound for a given music cue. Not to be used in the simulation (play it immediately).</summary>
		public SafeSoundEffect LocalGetSoundForMusicCue(Cue cue)
		{
			SafeSoundEffect music = null;
			if (ReferenceEquals(cue, missingCue))
			{
#if DEVELOPER
                music = MissingAudio.GetMissingMusicSound();
#endif
			}
			else if (cue != null && cue.SoundCount > 0)
			{
				// Assumption: Music Cue is just a single sound with no variations!
				Debug.Assert(cue.SoundCount == 1);
				music = GetSound(cue, 0);
			}

			return music;
		}


		protected static ReadAudioPackage.Result LoadSoundEffects(byte[] header, string filename)
		{
			var audioPackagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
			return ReadAudioPackage.ReadHeader(audioPackagePath, header);
		}

		#endregion

		#region Cues

		/// <summary>Lookup of names to cues.</summary>
		protected OrderedDictionary<string, Cue> cues;

		public int cuesWithIds;

		/// <summary>Sentinel value for when we cannot find a requested cue</summary>
		public Cue missingCue = new Cue {friendlyName = "[missing cue]"};

		public Cue GetCue(string name, object debugContext)
		{
			if (string.IsNullOrWhiteSpace(name))
				return null;

			Cue result;
			if (cues.TryGetValue(name, out result))
				return result;

#if DEVELOPER
            MissingAudio.ReportMissingCue(name, debugContext); // <- NOTE: This has its own internal no-repeat handling, so it's fine with rollbacks
#endif
			return missingCue;
		}


		protected struct LoadCuesResult
		{
			public OrderedDictionary<string, Cue> cues;
			public int cuesWithIds;
		}

		protected static LoadCuesResult LoadCues(byte[] header, string filename)
		{
			LoadCuesResult result;
			result.cues = new OrderedDictionary<string, Cue>();
			result.cuesWithIds = 0;

			var cuePackagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
			using (var fs = File.OpenRead(cuePackagePath))
			{
				for (var i = 0; i < header.Length; i++)
					if (fs.ReadByte() != header[i])
						throw new Exception("Cues package is corrupt");

				using (var br = new BinaryReader(new GZipStream(fs, CompressionMode.Decompress, false)))
				{
					var count = br.ReadInt32();
					for (var i = 0; i < count; i++)
					{
						var name = br.ReadString();
						var context = new CueDeserializeContext(br);
						var cue = new Cue(context);

						// Post-processing:
						result.cues.Add(name, cue);
						if (cue.type == CueType.Cycle || cue.type == CueType.RandomCycle)
							cue.id = result.cuesWithIds++;
					}
				}
			}

			return result;
		}

		#endregion
	}
}