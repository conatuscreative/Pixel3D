using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Pixel3D.Audio
{
    /// <summary>
    /// Rollback-aware one-shot sound playback manager.
    /// IMPORTANT: Do not mix non-rollback (UI) sounds with rollback-able sounds (gameplay)
    /// </summary>
    public class SoundRollbackManager
    {
        /// <summary>Number of frames earlier or later a sound may be played than its actual time</summary>
        public const int MaximumSoundShift = 12;

        /// <summary>Maximum number of frames of sound we bother tracking</summary>
        // Allows a tracked sound to exist long enough to kill a sound (shifting it backwards) that would otherwise be shifted forward to play immediately
        public const int DontCareLimit = MaximumSoundShift * 2;






        /// <param name="random">IMPORTANT: This may be part of the game state</param>
        /// <param name="cueStates">IMPORTANT: This may be part of the game state</param>
        /// <param name="playLocally">True if this sound should be played, false if the sound is for a remote player (not for us). Allows for local-only UI sounds.</param>
        public void PlayCue(IAudioDefinitions definitions, Cue cue, Position? worldPosition, FadePitchPan fpp, PlayCueParameters parameters, bool playsLocally)
        {
#if DEVELOPER
            if(parameters.soundIndex == PlayCueParameters.MISSING_CUE)
            {
                if(!doingPrediction) // <- poor man's "first time simulated" (Nelson doesn't get rollback-aware sound handling)
                    MissingAudio.TriedToPlayMissingCue(fpp);
                return;
            }
#endif
            if(parameters.soundIndex < 0)
                return;
            

            if(!playsLocally || !AudioDevice.Available)
                return; // <- nothing to do!

            if(doingPrediction) // Re-prediction following rollback
            {
                if(activeFrame >= liveFrame - DontCareLimit) // <- new enough that we could still be tracking it
                {
                    if(!TryKillCueExact(cue, activeFrame, worldPosition))
                    {
                        if(activeFrame >= liveFrame - MaximumSoundShift) // <- new enough that we may play it
                        {
                            PendingCue pending;
                            pending.cue = cue;
                            pending.parameters = parameters;
                            pending.frame = activeFrame;
                            pending.position = worldPosition;
                            pending.fpp = fpp;

                            pendingCues.Add(pending);
                        }
                    }
                }
            }
            else // Standard playback
            {
                Debug.Assert(activeFrame == liveFrame);

                if(!rollbackAware || !TryKillCueFuzzy(cue, activeFrame, worldPosition))
                {
                    if(!doingStartupPrediction)
                        SoundEffectManager.PlayCueSkipMissingCheck(definitions, cue, parameters, fpp);
                    AddLiveCueNow(cue, worldPosition);
                }
            }
        }




        /// <summary>Prevent any sounds from playing while doing startup prediction</summary>
        bool doingStartupPrediction = false;
        bool rollbackAware = false;
        bool doingPrediction = false;

        /// <summary>Frame that cues are being played on</summary>
        int activeFrame = 0;
        /// <summary>The most recent frame to be simulated</summary>
        int liveFrame = 0;


        /// <param name="frame">Networked frame number</param>
        public void BeforeRollbackAwareFrame(int frame, bool startupPrediction)
        {
            Debug.Assert(!doingPrediction || frame > activeFrame); // <- time advances during prediction (time going backwards indicates start of prediction)

            if(rollbackAware == false)
            {
                rollbackAware = true;
                this.activeFrame = frame; // <- ensure this here, in case we are launching directly into rollback somehow
                this.liveFrame = frame;
            }

            doingStartupPrediction = startupPrediction;

            activeFrame = frame;
            if(activeFrame <= liveFrame) // <- we got sent back in time - we are doing prediction!
            {
                doingPrediction = true;

                // Because we have been rolled-back to this frame, all sounds on and after this frame have not yet been played
                for(int i = 0; i < liveCues.Count; )
                {
                    if(liveCues[i].simulationFrame >= frame)
                    {
                        liveUnmatched.Add(liveCues[i]);
                        liveCues.RemoveAtUnordered(i);
                        continue;
                    }
                    i++;
                }
            }
            else // <- Normal updating
            {
                Debug.Assert(doingPrediction == false); // <- set by AfterRollbackAwareFrame
                liveFrame = frame;

                CleanupLiveCueList(liveUnmatched, frame - DontCareLimit);
                CleanupLiveCueList(liveCues, frame - DontCareLimit);
            }
        }


        public void AfterRollbackAwareFrame(IAudioDefinitions definitions)
        {
            Debug.Assert(rollbackAware);

            if(doingPrediction)
            {
                if(activeFrame == liveFrame) // Completed re-simulation of rollback frames
                {
                    doingPrediction = false;

                    foreach(var pending in pendingCues)
                    {
                        if(!TryKillCueFuzzy(pending.cue, activeFrame, pending.position))
                        {
                            if(!doingStartupPrediction)
                                SoundEffectManager.PlayCueSkipMissingCheck(definitions, pending.cue, pending.parameters, pending.fpp);
                            AddLiveCueNow(pending.cue, pending.position);
                        }
                    }

                    pendingCues.Clear();
                }
            }
        }
        

        public void StopBeingRollbackAware()
        {
            rollbackAware = false;
            doingStartupPrediction = false;
            activeFrame = 0;
            liveFrame = 0;

            liveCues.Clear();
            liveUnmatched.Clear();
            pendingCues.Clear();
        }




        #region Played Cue Lists
        
        struct LiveCue
        {
            public Cue cue;

            public int playedFrame; // <- for fuzzy matching
            public int simulationFrame; // <- for simulation matching
            public Position? playedPosition;
            public Position? simulationPosition;
        }

        /// <summary>Sounds that are live, but haven't actually been played by the simulation.</summary>
        readonly List<LiveCue> liveUnmatched = new List<LiveCue>();
        readonly List<LiveCue> liveCues = new List<LiveCue>();


        static void CleanupLiveCueList(List<LiveCue> list, int keepFrom)
        {
            for(int i = 0; i < list.Count;)
            {
                if(list[i].playedFrame < keepFrom && list[i].simulationFrame < keepFrom)
                {
                    list.RemoveAtUnordered(i);
                    continue;
                }
                i++;
            }
        }


        bool TryKillCueExact(Cue cue, int frame, Position? position)
        {
            for(int i = 0; i < liveUnmatched.Count; i++)
            {
                var runningCue = liveUnmatched[i];

                if(ReferenceEquals(cue, runningCue.cue)
                        && frame == runningCue.simulationFrame
                        && position == runningCue.simulationPosition)
                {
                    liveCues.Add(runningCue);
                    liveUnmatched.RemoveAtUnordered(i);
                    return true;
                }
            }

            return false;
        }

        bool TryKillCueFuzzy(Cue cue, int frame, Position? position)
        {
            const int frameDistanceSqFactor = 5*5;
            const int worldDistanceThresholdSq = 60*60; // <- really quite wide

            int bestDistanceSq = int.MaxValue;
            int bestIndex = -1;

            for(int i = 0; i < liveUnmatched.Count; i++)
            {
                if(ReferenceEquals(cue, liveUnmatched[i].cue) && position.HasValue == liveUnmatched[i].playedPosition.HasValue)
                {
                    int distanceSq = 0;
                    if(position.HasValue)
                    {
                        distanceSq = Position.DistanceSquared(position.GetValueOrDefault(), liveUnmatched[i].playedPosition.GetValueOrDefault());
                        if(distanceSq > worldDistanceThresholdSq)
                            continue; // <- too far away
                    }

                    distanceSq += Math.Abs(frame - liveUnmatched[i].playedFrame) * frameDistanceSqFactor;

                    if(distanceSq < bestDistanceSq)
                    {
                        bestDistanceSq = distanceSq;
                        bestIndex = i;
                    }
                }
            }

            if(bestIndex != -1)
            {
                LiveCue runningCue = liveUnmatched[bestIndex];
                // Keep these updated:
                runningCue.simulationFrame = frame;
                runningCue.simulationPosition = position;

                liveCues.Add(runningCue);
                liveUnmatched.RemoveAtUnordered(bestIndex);
                return true;
            }
            else
                return false;
        }


        void AddLiveCueNow(Cue cue, Position? position)
        {
            if(rollbackAware)
            {
                LiveCue runningCue;
                runningCue.cue = cue;
                runningCue.playedFrame = liveFrame;
                runningCue.simulationFrame = activeFrame;
                runningCue.playedPosition = position;
                runningCue.simulationPosition = position;

                liveCues.Add(runningCue);
            }
        }
        
        #endregion



        #region Pending Cue list (for rollback)

        struct PendingCue
        {
            public Cue cue;

            public int frame;
            public Position? position;

            public PlayCueParameters parameters;
            public FadePitchPan fpp;
        }

        /// <summary>Cues from rollback that did not find exact matches</summary>
        readonly List<PendingCue> pendingCues = new List<PendingCue>();

        #endregion



    }
}

