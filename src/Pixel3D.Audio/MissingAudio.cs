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

		private static readonly HashSet<string> missingCues = new HashSet<string>();

	    public static void ReportMissingCue(string name, object debugContext)
        {
	        bool added;
	        lock (missingCues)
		        added = missingCues.Add(name);

	        if (added)
	        {
		        string c;
		        if (debugContext is string)
			        c = (string)debugContext;
		        else if (debugContext is IEditorNameProvider)
			        c = string.Format("{0}: {1}", debugContext.GetType().Name, ((IEditorNameProvider)debugContext).EditorName);
		        else if (debugContext != null)
			        c = debugContext.ToString();
		        else
			        c = "[no context]";

		        string message = string.Format("Missing cue \"{0}\" (context: {1})", name, c);
		        Debug.WriteLine(message);
		        Log.Current.Warn(message);
	        }
		}

        class ExpectedCueInfo
        {
            public string context;
            public AnimationSet animationSet;
            public Animation animation;
            public int frame;

            public override int GetHashCode()
            {
                return context.GetHashCode() ^ RuntimeHelpers.GetHashCode(animationSet) ^ RuntimeHelpers.GetHashCode(animation) ^ frame.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                ExpectedCueInfo other = obj as ExpectedCueInfo;
                if(other == null)
                    return false;

                return context == other.context
                    && ReferenceEquals(animationSet, other.animationSet)
                    && ReferenceEquals(animationSet, other.animationSet)
                    && frame == other.frame;
            }
        }

        private static readonly HashSet<ExpectedCueInfo> expectedCues = new HashSet<ExpectedCueInfo>();

        public static void ReportExpectedCue(string context, AnimationSet animationSet, Animation animation = null, int frame = -1)
        {
	        ExpectedCueInfo eci = new ExpectedCueInfo { context = context, animationSet = animationSet, animation = animation, frame = frame };
	        bool added;
	        lock (expectedCues)
		        added = expectedCues.Add(eci);

	        if (added)
	        {
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

        private static readonly object lockObject = new object();

        private static bool initialized = false;

        public static void ActivateAudibleMissingSounds()
        {
            if(!AudioDevice.Available)
                return; // Don't even bother with setup, if we have no device

            lock(lockObject)
            {
                if(initialized)
                    return;
                initialized = true;
            }

            Debug.WriteLine("Enabling Developer Audio");

            Thread thread = new Thread(InternalInitializeMissingSounds);
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
            lock(lockObject)
                sound = missingSoundEffect;

            if(sound != null)
                sound.Play(fpp.fade, fpp.pitch, fpp.pan);
        }

		public static SafeSoundEffect GetMissingMusicSound()
        {
            lock(lockObject)
                return missingMusic;
        }

        #endregion
    }
}
