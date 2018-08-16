using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Pixel3D.Engine.Audio
{
    public static class MusicManager
    {
        // Public methods are thread-safe (must lock)
        private static object lockObject = new object();


        const float fadeTime = 0.8f; // seconds
        const float sufficientFadeOutToStart = 0.2f;
        const float sufficientFadeOutToFadeIn = 0.75f;

        public const float DefaultVolume = 0.7f;
        private static float _volume = DefaultVolume; // <- good default (keeps the sound effects above the music)
        public static float Volume
        {
            get { lock(lockObject) return _volume; }
            set
            {
                lock(lockObject)
                {
                    _volume = value;
                    if(!AudioDevice.Available)
                        return;

                    foreach(var a in activeMusic)
                        if(a.instance != null)
                            a.instance.Volume = a.fade.StepNES() * _volume;

                    foreach(var f in fadingOutMusic)
                        f.instance.Volume = f.fade.StepNES() * _volume;
                }
            }
        }


        /// <summary>Volume stepping like a NES (16 levels)</summary>
        public static float StepNES(this float value)
        {
            return (float)(Math.Round(value * 16.0) / 16.0);
        }


        #region Active Music

        /// <param name="synchronise">Start music immediately, don't wait for fade</param>
        public static void SetMenuMusic(SafeSoundEffect music, bool loop = true, bool synchronise = false)
        {
            lock(lockObject)
            {
                SetMusic(music, 0, loop, synchronise);
            }
        }

        /// <param name="synchronise">Start music immediately, don't wait for fade</param>
        public static void SetGameMusic(SafeSoundEffect music, bool loop = true, bool synchronise = false)
        {
            lock(lockObject)
            {
                SetMusic(music, 1, loop, synchronise);
            }
        }


        private struct ActiveMusic
        {
            public SoundEffect music; // <- used to indicate "exists" ("has value")
            public SoundEffectInstance instance;
            public float fade;
            public bool loop;
            public bool synchronise;
        }

        // Active music at different priorities (lower index is higher priority)
        private static ActiveMusic[] activeMusic = new ActiveMusic[2];

        private static int BestPriority
        {
            get
            {
                int i;
                for(i = 0; i < activeMusic.Length; i++)
                    if(activeMusic[i].music != null)
                        break;
                return i;
            }
        }



        private static void SetMusic(SafeSoundEffect safeMusic, int priority, bool loop, bool synchronise)
        {
            if(!AudioDevice.Available)
                return;
            SoundEffect music = safeMusic != null ? safeMusic.soundEffect : null;

            if(activeMusic[priority].music == music)
                return; // Already playing this song

            // Get rid of music currently set at this level (possibly with a fade-out, if it is still playing)
            if(activeMusic[priority].music != null)
            {
                if(activeMusic[priority].instance != null)
                {
                    if(activeMusic[priority].fade == 0)
                        activeMusic[priority].instance.Dispose();
                    else
                        fadingOutMusic.Add(new FadingOutMusic { fade = activeMusic[priority].fade, instance = activeMusic[priority].instance });
                }

                activeMusic[priority] = default(ActiveMusic);
            }

            if(music == null)
                return; // Nothing to play

            activeMusic[priority].music = music;
            activeMusic[priority].loop = loop;
            activeMusic[priority].synchronise = synchronise;


            if(synchronise && priority >= BestPriority)
                DoFastFade(); // Make the most of the synced music opening

            if(synchronise)
                StartPlaying(priority, priority >= BestPriority ? 1f : 0f); // <- synced music starts immediately (even if it is at zero volume)
            else if(CanStartSong(priority, sufficientFadeOutToStart))
                StartPlaying(priority);
        }


        private static void StartPlaying(int priority, float fade = 1f)
        {
            var instance = activeMusic[priority].music.CreateInstance();
            activeMusic[priority].instance = instance;
            activeMusic[priority].fade = fade;

            instance.Volume = _volume * fade;
            instance.IsLooped = activeMusic[priority].loop;
            instance.Play();
        }


        private static bool CanStartSong(int priority, float sufficientFade)
        {
            if(fadingOutMusic.Count > 0 && fadingOutMusic[fadingOutMusic.Count-1].fade > sufficientFade)
                return false;

            for(int i = 0; i < activeMusic.Length; i++)
            {
                if(activeMusic[i].music != null)
                {
                    if(i < priority)
                        return false; // never start playing something when we have a higher priority to play
                    else if(i > priority)
                    {
                        if(activeMusic[i].fade > sufficientFade)
                            return false; // waiting to fade out
                    }
                }
            }

            return true;
        }



        public static void Update(TimeSpan elapsedTime)
        {
            if(!AudioDevice.Available)
                return;

            lock(lockObject)
            {
                float seconds = (float)elapsedTime.TotalSeconds;

                UpdateFadeOuts(seconds);

                // Fade in/out the active music:
                int bestPriority = activeMusic.Length;
                for(int i = 0; i < activeMusic.Length; i++) // highest to lowest priority
                {
                    if(activeMusic[i].music != null)
                    {

                        if(i < bestPriority)
                            bestPriority = i;


                        if(i == bestPriority)
                        {
                            if(activeMusic[i].instance == null)
                            {
                                // NOTE: synced music does not get a chance to restart (should never happen anyway)
                                if(!activeMusic[i].synchronise && CanStartSong(i, sufficientFadeOutToStart))
                                    StartPlaying(i);
                            }
                            else if(activeMusic[i].fade < 1f)
                            {
                                if(activeMusic[i].fade == 0f)
                                {
                                    if(CanStartSong(i, sufficientFadeOutToFadeIn))
                                    {
                                        if(activeMusic[i].loop) // NOTE: only looping music gets paused when silent
                                            activeMusic[i].instance.Play(); // unpause
                                    }
                                    else
                                        continue;
                                }

                                activeMusic[i].fade += (seconds / fadeTime); // fade in
                                if(activeMusic[i].fade > 1f)
                                    activeMusic[i].fade = 1f;
                                activeMusic[i].instance.Volume = _volume * activeMusic[i].fade.StepNES();
                            }
                        }


                        if(i > bestPriority)
                        {
                            if(activeMusic[i].instance != null)
                            {
                                if(activeMusic[i].fade > 0f)
                                {
                                    activeMusic[i].fade -= (seconds / fadeTime); // fade out
                                    if(activeMusic[i].fade <= 0f)
                                    {
                                        activeMusic[i].fade = 0f;
                                        activeMusic[i].instance.Volume = 0f;
                                        if(activeMusic[i].loop) // NOTE: only looping music gets paused when silent
                                            activeMusic[i].instance.Pause();
                                    }
                                    else
                                    {
                                        activeMusic[i].instance.Volume = _volume * activeMusic[i].fade.StepNES();
                                    }
                                }
                            }
                        }

                    }
                } // <- end for each active music

            }
        }

        #endregion




        #region Fading Out Music

        private struct FadingOutMusic
        {
            public float fade;
            public SoundEffectInstance instance;
            public bool fastFade;
        }

        // This acts as a queue
        private static List<FadingOutMusic> fadingOutMusic = new List<FadingOutMusic>();

        private static void DoFastFade()
        {
            for(int i = 0; i < fadingOutMusic.Count; i++)
            {
                var f = fadingOutMusic[i];
                f.fastFade = true;
                fadingOutMusic[i] = f;
            }
        }


        private static void UpdateFadeOuts(float seconds)
        {
            for(int i = 0; i < fadingOutMusic.Count;) // NOTE: in-loop removal
            {
                FadingOutMusic f = fadingOutMusic[i];

                float fadeOutBy = (seconds / fadeTime);
                if(f.fastFade)
                    fadeOutBy *= 3;
                f.fade -= fadeOutBy;

                if(f.fade <= 0)
                {
                    f.instance.Dispose(); // stops the sound
                    fadingOutMusic.RemoveAt(i);
                    continue;
                }
                else
                {
                    f.instance.Volume = _volume * f.fade.StepNES();
                    fadingOutMusic[i] = f;
                }

                i++;
            }
        }

        #endregion


    }
}
