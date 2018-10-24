// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Pixel3D.Animations;

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

			if (added)
			{
				string c;
				if (debugContext is string s)
					c = s;
				else if (debugContext is IEditorNameProvider provider)
					c = $"{provider.GetType().Name}: {provider.EditorName}";
				else if (debugContext != null)
					c = debugContext.ToString();
				else
					c = "[no context]";

				string message = $"Missing cue \"{name}\" (context: {c})";
				Debug.WriteLine(message);
				Log.Current.Warn(message);
			}
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

			if (added)
			{
				AnimationSet animationSet = args[0] as AnimationSet;
				Animation animation = args[1] as Animation;
				int frame = (int)args[2];

				string format;
				if (animation == null)
					format = "Expected cue for \"{0}\" on AnimationSet = {1}";
				else if (frame == -1)
					format = "Expected cue for \"{0}\" on Animation = {2} (AnimationSet = {1})";
				else
					format = "Expected cue for \"{0}\" on Frame = {3} (AnimationSet = {1}, Animation = {2})";

				string message = string.Format(format, context, animationSet == null ? "???" : animationSet.friendlyName,
					animation == null ? "???" : animation.friendlyName, frame);
				Debug.WriteLine(message);
				Log.Current.Warn(message);
			}
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

		private static SafeSoundEffect missingSoundEffect;
		private static string missingMusic;

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

		public static string GetMissingMusicPath()
		{
			lock (LockObject)
			{
				return missingMusic;
			}
		}

		#endregion
	}
}