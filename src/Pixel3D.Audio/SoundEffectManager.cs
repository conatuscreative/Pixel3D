using System;
using System.Diagnostics;

namespace Pixel3D.Audio
{
    public static class SoundEffectManager
    {
        // Public static methods are thread safe!
        private static readonly object lockObject = new object();



        public const float rollOffTime = 0.7f; // seconds

        /// <summary>IMPORTANT: Don't use this from inside game-code, it doesn't handle rollback properly!</summary>
        public static void RollOffCurrentSounds()
        {
            lock(lockObject)
            {
                rollOffSoundCount = playingSoundCount;
            }
        }



        /// <summary>How many live sound effects we allow at once (fake a maximum number of playable sounds at once)</summary>
        public const int maxSlots = 2;

        public static int UsedSlots { get; private set; }

        /// <summary>Number of frames before the end of a sound that we consider it "done"</summary>
        const int soundTailThresholdFrames = 5;
        public const float slotFadeTime = 0.15f; // seconds


        public static void Update()
        {
            lock(lockObject)
            {
                UpdatePlayingSounds();
            }
        }



        public static void PlayCue(IAudioDefinitions definitions, Cue cue, PlayCueParameters parameters, FadePitchPan fpp, bool loop = false)
        {
            if(parameters.soundIndex == PlayCueParameters.MISSING_CUE)
            {
                MissingAudio.TriedToPlayMissingCue(fpp);
                return;
            }

			if (parameters.soundIndex < 0)
                return;

            PlayCueSkipMissingCheck(definitions, cue, parameters, fpp, loop);
        }


        /// <summary>Call this only when PlayCueParameters have been validated</summary>
        public static void PlayCueSkipMissingCheck(IAudioDefinitions definitions, Cue cue, PlayCueParameters parameters, FadePitchPan fpp, bool loop = false)
        {
            fpp.fade = AudioMath.Clamp(fpp.fade * cue.volume, 0, 1);
            fpp.pitch = AudioMath.Clamp(fpp.pitch + parameters.cuePitch, -1, 1);
            fpp.pan = AudioMath.Clamp(fpp.pan + cue.pan, -1, 1);

            if(fpp.fade < 0.01f)
                return; // too quiet to care about

            if(cue.SoundCount == 0)
                return; // <- nothing to play

            lock(lockObject)
            {
                switch(cue.type)
                {
                    case CueType.Parallel:
                        {
                            for(int i = 0; i < cue.SoundCount; i++)
                                AddAndStartPlaying(definitions.GetSound(cue, i), fpp, loop);
                        }
                        break;

                    case CueType.Serial:
                        {
                            // Build the queue for the cue:
                            int queueHead = -1;
                            for(int i = cue.SoundCount - 1; i >= 1; i--)
                            {
                                int q = AllocateQueuedSound();
                                queuedSounds[q].sound = definitions.GetSound(cue, i);
                                queuedSounds[q].next = queueHead;
                                queueHead = q;
                            }

                            AddAndStartPlaying(definitions.GetSound(cue, 0), fpp, loop, queueHead);
                        }
                        break;

                    default:
                        {
                            AddAndStartPlaying(definitions.GetSound(cue, parameters.soundIndex), fpp, loop);
                        }
                        break;
                }
            }
        }




        #region Playing Sounds

        struct PlayingSound
        {
            public SafeSoundEffect sound;
            public SafeSoundEffectInstance instance; // <- never null, for valid indicies of `playingSounds`
            public FadePitchPan fpp;
            public float fade; // <- handles global roll-off AND ducking for channel-limit simulation
            public int frameCount;
            public int queue; // <- for serial cues
            public bool linkToNext; // <- sounds on the same frame (esp. parallel cues) get linked together
        }

        static int rollOffSoundCount = 0;
        static int playingSoundCount = 0;
        static PlayingSound[] playingSounds = new PlayingSound[64];


        private static void ReleasePlayingSoundAt(int index)
        {
            Debug.Assert(index < playingSoundCount);

            // Release the sound instance:
            if(playingSounds[index].instance.IsLooped) // <- cannot pool looped instances
            {
                playingSounds[index].instance.Dispose();
            }
            else
            {
                playingSounds[index].instance.Stop();
                playingSounds[index].sound.SoundEffectManager_ReturnInstance(playingSounds[index].instance);
            }

            // Ensure a parallel linkage into us does not bleed over:
            if(index > 0 && playingSounds[index-1].linkToNext && !playingSounds[index].linkToNext)
                playingSounds[index-1].linkToNext = false;
            
            // Ensure that pending sounds are released:
            int queue = playingSounds[index].queue;
            while(queue != -1)
                queue = FreeQueuedSound(queue);

            // Actually remove it from the list:
            Array.Copy(playingSounds, index+1, playingSounds, index, playingSoundCount - (index+1));
            playingSoundCount--;
            playingSounds[playingSoundCount] = default(PlayingSound); // <- so the garbage collector can't see the references

            // Keep the roll-off count consistent:
            if(index < rollOffSoundCount)
                rollOffSoundCount--;
        }

        private static void FinishedPlayingSoundAt(int index)
        {
            if(index >= rollOffSoundCount && playingSounds[index].queue != -1)
            {
                int queue = playingSounds[index].queue;
                var sound = queuedSounds[queue].sound;
                queue = FreeQueuedSound(queue);
                playingSounds[index].queue = -1; // <- so we don't deallocate it again!

                AddAndStartPlaying(sound, playingSounds[index].fpp, false, queue);
            }

            ReleasePlayingSoundAt(index);
        }


        private static void UpdatePlayingSounds()
        {
            // At the update point, stop linking sounds together for the frame:
            if(playingSoundCount > 0)
                playingSounds[playingSoundCount-1].linkToNext = false;

            // Update all live sounds:
            bool lastSoundGotASlot = true;
            UsedSlots = 0;
            for(int i = playingSoundCount - 1; i >= 0; i--)
            {
                // Handle sounds that have stopped playing
                if(playingSounds[i].instance.State == SoundState.Stopped)
                {
                    if(i < rollOffSoundCount)
                        ReleasePlayingSoundAt(i);
                    else
                        FinishedPlayingSoundAt(i);
                    continue;
                }

                // Count how long the sound has been playing for:
                int framesLeft = playingSounds[i].sound.DurationInFrames(playingSounds[i].fpp.pitch) - playingSounds[i].frameCount;
                bool nearlyDone = (framesLeft < soundTailThresholdFrames && !playingSounds[i].instance.IsLooped);

                // See if this sound gets a slot:
                bool allocateSlot = (UsedSlots < maxSlots || playingSounds[i].linkToNext && lastSoundGotASlot);
                if(allocateSlot)
                    if(!nearlyDone) // <- sounds that are nearly done don't count towards a slot (so another sound may fade in over the top)
                        UsedSlots++;

                lastSoundGotASlot = allocateSlot;

                // Handle fading:
                float fadeAmount = (allocateSlot ? (1f / (slotFadeTime * 60f)) : -(1f / (slotFadeTime * 60f)));
                if(i < rollOffSoundCount)
                    fadeAmount = Math.Min(fadeAmount, -(1f / (rollOffTime * 60f)));

                playingSounds[i].fade = AudioMath.Clamp(playingSounds[i].fade + fadeAmount, 0, 1);
                if(i < rollOffSoundCount && playingSounds[i].fade == 0)
                {
                    ReleasePlayingSoundAt(i);
                    continue;
                }

                playingSounds[i].fpp.ApplyTo(playingSounds[i].instance, playingSounds[i].fade * SafeSoundEffect.SoundEffectVolume);
                playingSounds[i].frameCount++;
            }
        }


        private static void AddAndStartPlaying(SafeSoundEffect sound, FadePitchPan fpp, bool loop = false, int queue = -1)
        {
            Debug.Assert(playingSoundCount <= playingSounds.Length);
            if(playingSoundCount == playingSounds.Length)
                Array.Resize(ref playingSounds, playingSounds.Length * 2);

            Debug.Assert(playingSounds[playingSoundCount].sound == null); // <- got cleared properly
            Debug.Assert(playingSounds[playingSoundCount].instance == null); // <- got cleared properly
            Debug.Assert(playingSounds[playingSoundCount].frameCount == 0); // <- got cleared properly


            // If we are about to play multiple identical sounds at about the same time, stop them from overlapping:
            float quashFade = 1f;
            for(int i = playingSoundCount-1; i >= 0; i--)
            {
                if(playingSounds[i].frameCount >= 3)
                    break; // <- Reaching sounds that are too old
                if(ReferenceEquals(playingSounds[i].sound, sound))
                    quashFade -= (1f - ((float)playingSounds[i].frameCount / 3f));
            }
            // TODO: The following is ugly, because it kills sequential sounds (but odds are they would be killed anyway - and because we just use `fpp.fade`, below, they'd get killed anyway)
            // If a sound would be quashed completely, just don't play it -- this is required because otherwise the quashed sounds would be taking up simulated channels
            if(quashFade < 0.1f)
            {
                while(queue != -1) // Don't leak the queue, if any
                    queue = FreeQueuedSound(queue);
                return;
            }
            // TODO: This is ugly because sequential sounds will inherit this fade level
            fpp.fade *= AudioMath.Clamp(quashFade, 0f, 1f);



            if(loop)
                playingSounds[playingSoundCount].instance = sound.CreateInstance();
            else
                playingSounds[playingSoundCount].instance = sound.SoundEffectManager_GetInstance();

            if(playingSounds[playingSoundCount].instance == null)
            {
                while(queue != -1) // Don't leak the queue, if any
                    queue = FreeQueuedSound(queue);
                return; // Failed to create sound instance... oh well.
            }

            Debug.Assert(playingSounds[playingSoundCount].instance.IsLooped == false); // <- instance was properly cleared
            if(loop) // <- Cannot set on used instances (even to the same value)
                playingSounds[playingSoundCount].instance.IsLooped = true;

            playingSounds[playingSoundCount].sound = sound;
            playingSounds[playingSoundCount].fpp = fpp;
            playingSounds[playingSoundCount].queue = queue;
            playingSounds[playingSoundCount].linkToNext = true; // <- all sounds on a given frame get linked!
            playingSounds[playingSoundCount].fade = 1f; // <- NOTE: assumed by channel ducking code
            playingSounds[playingSoundCount].frameCount = 0;

            fpp.ApplyTo(playingSounds[playingSoundCount].instance, SafeSoundEffect.SoundEffectVolume);
            playingSounds[playingSoundCount].instance.Play();

            playingSoundCount++;
        }

        #endregion



        #region Queued Sounds (for serial cues)

        struct QueuedSound
        {
            public SafeSoundEffect sound;
            public int next;
        }

        static int queuedSoundFree = -1; // <- free chain
        static int queuedSoundUsed = 0;

        static QueuedSound[] queuedSounds = new QueuedSound[8];

        static int AllocateQueuedSound()
        {
            Debug.Assert(queuedSoundUsed <= queuedSounds.Length);

            if(queuedSoundFree != -1)
            {
                int result = queuedSoundFree;
                queuedSoundFree = queuedSounds[result].next;
                queuedSounds[result].next = -1;
                return result;
            }
            else
            {
                if(queuedSoundUsed == queuedSounds.Length)
                    Array.Resize(ref queuedSounds, queuedSounds.Length * 2);

                int result = queuedSoundUsed;
                queuedSounds[result].next = -1;

                queuedSoundUsed++;

                return result;
            }
        }

        static int AllocateQueuedSound(int insertAfter)
        {
            int result = AllocateQueuedSound();
            queuedSounds[result].next = queuedSounds[insertAfter].next;
            queuedSounds[insertAfter].next = result;

            return result;
        }


        /// <returns>Returns the next item in the queue</returns>
        static int FreeQueuedSound(int index)
        {
            int result = queuedSounds[index].next;

            queuedSounds[index].sound = null;
            queuedSounds[index].next = queuedSoundFree;
            queuedSoundFree = index;

            return result;
        }

        #endregion

    }
}
