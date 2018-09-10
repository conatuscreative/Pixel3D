// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Pixel3D.Audio
{
	// IMPORTANT: This class is thread-safe.
	public static class MissingAudio
	{
		#region Reporting

		private static readonly HashSet<string> MissingCues = new HashSet<string>();

		public static void ReportMissingCue(string name, object debugContext)
		{
			bool added;
			lock (MissingCues)
			{
				added = MissingCues.Add(name);
			}

			if (added && AudioSystem.reportMissingCue != null)
			    AudioSystem.reportMissingCue.Invoke(name, debugContext);
		}

		private class ExpectedCueInfo
		{
			public object[] args;
			public string context;

			public override int GetHashCode()
			{
				var hashCode = context.GetHashCode();
				foreach (var arg in args)
					hashCode ^= RuntimeHelpers.GetHashCode(arg);
				return hashCode;
			}

			public override bool Equals(object obj)
			{
			    var other = obj as ExpectedCueInfo;
                if (other == null)
					return false;

				if (args.Length != other.args.Length)
					return false;

				var equals = context == other.context;
				for (var i = 0; i < args.Length; i++)
					equals &= ReferenceEquals(args[i], other.args[i]);

				return equals;
			}
		}

		private static readonly HashSet<ExpectedCueInfo> ExpectedCues = new HashSet<ExpectedCueInfo>();

		public static void ReportExpectedCue(string context, params object[] args)
		{
			var eci = new ExpectedCueInfo
			{
				context = context,
				args = args
			};
			bool added;
			lock (ExpectedCues)
			{
				added = ExpectedCues.Add(eci);
			}

			if (added && AudioSystem.reportExpectedCue != null)
				AudioSystem.reportExpectedCue.Invoke(context, args);
		}

		#endregion

		#region Nonsense Sounds (DEVELOPER only)

		private static readonly object LockObject = new object();

		private static bool initialized;

		public static void ActivateAudibleMissingSounds()
		{
			if (!AudioDevice.Available)
				return; // Don't even bother with setup, if we have no device

			lock (LockObject)
			{
				if (initialized)
					return;
				initialized = true;
			}

			Debug.WriteLine("Enabling Developer Audio");
			var thread = new Thread(InternalInitializeMissingSounds);
			thread.Start();
		}

		private static void InternalInitializeMissingSounds()
		{
#if false // TODO: Add the missing sound sounds back in (need to decode through vorbisfile)
            {
                string nelsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Nelson.ogg");
                if(!File.Exists(nelsonPath))
                    Debug.WriteLine("Couldn't find Nelson!");
                else
                {
                    var sound = new SafeSoundEffect(OggVorbis.OggVorbisDecoder.TryDecode(nelsonPath));
                    if(sound.soundEffect == null)
                        Debug.WriteLine("Couldn't load Nelson!");
                    else
                        lock(lockObject)
                            missingSoundEffect = sound;
                }
            }
            
            {
                string howardPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Howard.ogg");
                if(!File.Exists(howardPath))
                    Debug.WriteLine("Couldn't find Howard!");
                else
                {
                    var sound = new SafeSoundEffect(OggVorbis.OggVorbisDecoder.TryDecode(howardPath));
                    if(sound.soundEffect == null)
                        Debug.WriteLine("Couldn't load Howard!");
                    else
                        lock(lockObject)
                            missingMusic = sound;
                }
            }
#endif
		}

		private static SafeSoundEffect missingSoundEffect, missingMusic;

		public static void TriedToPlayMissingCue(FadePitchPan fpp)
		{
			SafeSoundEffect sound;
			lock (LockObject)
			{
				sound = missingSoundEffect;
			}

            if(sound != null)
			    sound.Play(fpp.fade, fpp.pitch, fpp.pan);
		}

		public static SafeSoundEffect GetMissingMusicSound()
		{
			lock (LockObject)
			{
				return missingMusic;
			}
		}

		#endregion
	}
}