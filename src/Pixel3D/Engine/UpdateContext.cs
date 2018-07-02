using System.Collections.Generic;
using System.Diagnostics;
using Common;
using Pixel3D.Animations;
using Pixel3D.Audio;
using Pixel3D.Strings;

namespace Pixel3D.Engine
{
    public class UpdateContext : ILocalizationProvider
    {
        protected readonly SoundRollbackManager soundRollbackManager;

        /// <summary>Not network-safe!</summary>
        public int localPlayerBits = int.MaxValue;

        public UpdateContext(Camera audioCamera, SoundRollbackManager soundRollbackManager)
        {
            this.soundRollbackManager = soundRollbackManager;
            AudioCamera = audioCamera;
        }

        /// <summary>
        /// The context must not retain data across frames, because it is not part of the networked state.
        /// So, for safety, reset it every time it is used.
        /// </summary>
        public virtual void Reset()
        {
            random = null;
            activeActorIndex = -1;
            initialAnimationStates.Clear();
            physics.Reset();
            deferredAttachments.Clear();
            actorsToRemove.Clear();
            deferredSpawn.Clear();
        }

        public virtual bool DebugNetworkUnsafeHasLocalSettings { get { return false; } }

        #region Non-Retained Data

        // Things that must not be retained across frames (clear in Reset method!)

        public XorShift random; // <- here so that no one uses it outside of network update methods

        /// <summary>The index in the actor list of the actor currently being updated (or -1 when outside the normal actor update loop)</summary>
        public int activeActorIndex = -1;

        // Kinda hacky?
        public List<AnimationPlayer> initialAnimationStates = new List<AnimationPlayer>();

        public GamePhysics physics = new GamePhysics();

        public Dictionary<Actor, DeferredAttachment> deferredAttachments = new Dictionary<Actor, DeferredAttachment>(ReferenceEqualityComparer<Actor>.Instance);

        public List<Actor> actorsToRemove = new List<Actor>();
        public List<Actor> deferredSpawn = new List<Actor>();

        #endregion

        #region Audio

        /// <summary>For positioning audio in world space. May be null.</summary>
        protected Camera AudioCamera { get; set; }

        /// <summary>Play a cue in a world-space position (relative to the camera)</summary>
        public void PlayCueWorld(Cue cue, Actor source)
        {
            var parameters = PlayCueParameters.GetParameters(Definitions, cue, random, GameState.cueStates);
            // ^^^^ Affects gameplay || Local-only vvvv
            if (soundRollbackManager != null && AudioCamera != null)
            {
                var fpp = new FadePitchPan(AudioCamera.WorldToAudio(source.position));
#if DEVELOPER
                soundRollbackManager.PlayCue(Definitions, cue, source.position, fpp, parameters, playsLocally: true);
#else
                soundRollbackManager.PlayCueSkipMissingCheck(Definitions, cue, source.position, fpp, parameters, playsLocally: true);
#endif

            }
        }

        // Set-only (because simulation should not access it)
        public void SetFirstTimeSimulated(bool firstTimeSimulated)
        {
            this.firstTimeSimulated = firstTimeSimulated;
        }

        /// <summary>True if not running a rollback (can play audio, do effects, etc). Do not access from simulation.</summary>
        protected bool firstTimeSimulated;

        /// <summary>Play a sound without any position (always plays centred)</summary>
        public void PlayCueGlobal(string symbol, Actor source = null) // <- keeping source around, in case it is useful information (will become useful for rollback)
        {
            PlayCueGlobal(Definitions.GetCue(symbol, source), source);
        }

        /// <summary>Play a cue in a world-space position (relative to the camera)</summary>
        public void PlayCueWorld(string symbol, Actor source)
        {
            PlayCueWorld(Definitions.GetCue(symbol, source), source);
        }

        /// <summary>Play a cue in a world-space position (relative to the camera)</summary>
        public void PlayCueWorld(string symbol, Position source)
        {
            PlayCueWorld(Definitions.GetCue(symbol, source), source);
        }

        /// <summary>Play a cue only for a specific player</summary>
        public void PlayCueUI(string symbol, int playerIndex, bool useHudStereo)
        {
            PlayCueUI(Definitions.GetCue(symbol, "PlayCueUI"), playerIndex, useHudStereo);
        }


        /// <summary>Play a sound without any position (always plays centred)</summary>
        public void PlayCueGlobal(Cue cue, Actor source = null) // <- keeping source around, in case it is useful information (will become useful for rollback)
        {
            var parameters = PlayCueParameters.GetParameters(Definitions, cue, random, GameState.cueStates);
            // ^^^^ Affects gameplay || Local-only vvvv
            if (soundRollbackManager != null)
            {
                var fpp = new FadePitchPan(1f);
#if DEVELOPER
                soundRollbackManager.PlayCue(Definitions, cue, null, fpp, parameters, playsLocally: true);
#else
                soundRollbackManager.PlayCueSkipMissingCheck(Definitions, cue, null, fpp, parameters, playsLocally: true);
#endif
            }
        }

        /// <summary>Play a cue in a world-space position (relative to the camera)</summary>
        public void PlayCueWorld(Cue cue, Position source)
        {
            var parameters = PlayCueParameters.GetParameters(Definitions, cue, random, GameState.cueStates);
            // ^^^^ Affects gameplay || Local-only vvvv
            if (soundRollbackManager != null && AudioCamera != null)
            {
                var fpp = new FadePitchPan(AudioCamera.WorldToAudio(source));

#if DEVELOPER
                soundRollbackManager.PlayCue(Definitions, cue, source, fpp, parameters, playsLocally: true);
#else
                soundRollbackManager.PlayCueSkipMissingCheck(Definitions, cue, source, fpp, parameters, playsLocally: true);
#endif
            }
        }
        

        public void PlayCueUI(Cue cue, int playerIndex, bool useHudStereo)
        {
            var parameters = PlayCueParameters.GetParameters(Definitions, cue, random, GameState.cueStates);
            // ^^^^ Affects gameplay || Local-only vvvv
            if (soundRollbackManager != null)
            {
                FadePitchPan fpp = new FadePitchPan(1f);

                if (useHudStereo) // TODO: Consider not using stereo if only one player is in game?
                    fpp.pan = -0.6f + (0.4f * playerIndex); // NOTE: Not using "audio space" because we don't want normal camera effects for HUD audio

                bool playLocally = ((localPlayerBits & (1 << playerIndex)) != 0);

                // NOTE: Cheat and use an impossible world position so it (really) can't interfere with in-game sounds
#if DEVELOPER
                soundRollbackManager.PlayCue(Definitions, cue, new Position(-6000 + 4000 * playerIndex, -100000, 0), fpp, parameters, playLocally);
#else
                soundRollbackManager.PlayCueSkipMissingCheck(Definitions, cue, new Position(-6000 + 4000 * playerIndex, -100000, 0), fpp, parameters, playLocally);
#endif
            }
        }



        // TODO: This is a problem for rollback (note how non-menu music is implemented) -AR
        public void PlayMenuMusic(string symbol, bool loop = true, bool synchronise = false)
        {
            if (soundRollbackManager == null) // <- Don't try to play music if we're muted!
                return;

            var music = Definitions.LocalGetSoundForMusicCue(Definitions.GetCue(symbol, "PlayMenuMusic"));
            MusicManager.SetMenuMusic(music, loop, synchronise);
        }

        #endregion

        #region Localization

        #region By TagSet

        public string GetSingleString(TagSet tagSet)
        {
            return Definitions.GetSingleString(tagSet, GameState.language);
        }

        public string GetSingleStringUppercase(TagSet tagSet)
        {
            return Definitions.GetSingleStringUppercase(tagSet, GameState.language);
        }

        public StringList GetStrings(TagSet tagSet)
        {
            return Definitions.GetStrings(tagSet, GameState.language);
        }

        public string GetRandomString(TagSet tagSet)
        {
            return Definitions.GetRandomString(tagSet, GameState.language, random.Next());
        }

        public string GetRandomStringUppercase(TagSet tagSet)
        {
            return Definitions.GetRandomStringUppercase(tagSet, GameState.language, random.Next());
        }

        #endregion

        #region By String

        public string GetSingleString(string tagSet)
        {
            return Definitions.GetSingleString(tagSet, GameState.language);
        }

        public string GetSingleStringUppercase(string tagSet)
        {
            return Definitions.GetSingleStringUppercase(tagSet, GameState.language);
        }

        public StringList GetStrings(string tagSet)
        {
            return Definitions.GetStrings(tagSet, GameState.language);
        }

        public string GetRandomString(string tagSet)
        {
            return Definitions.GetRandomString(tagSet, GameState.language, random.Next());
        }

        public string GetRandomStringUppercase(string tagSet)
        {
            return Definitions.GetRandomStringUppercase(tagSet, GameState.language, random.Next());
        }

        #endregion

        #endregion

        #region Helpers

        public GameState GameState { get; set; }

        public Definitions Definitions { get { return GameState.Definitions; } }

        /// <summary>Spawn an actor (at the end of this frame)</summary>
        public void Spawn(Actor actor)
        {
            deferredSpawn.Add(actor);
        }

        /// <summary>Spawn an actor and anything they are holding (at the end of this frame)</summary>
        public void SpawnRecursive(Actor actor)
        {
            while (actor != null)
            {
                Spawn(actor);
                actor = actor.OutgoingConnection;
            }
        }

        /// <summary>Remove the given actor from the game state (NOTE: players go to purgatory)</summary>
        public void Destroy(Actor actor)
        {
            Debug.Assert(actor != null);
            if (!actorsToRemove.Contains(actor))
                actorsToRemove.Add(actor);
        }

        public bool TimeElapsed(int startedAtTick, int durationTicks)
        {
            if (startedAtTick == -1)
                return false;
            return GameState.frameCounter >= startedAtTick + durationTicks;
        }

        public bool DutyCycle(int ticks)
        {
            return ticks == 0 || (GameState.frameCounter % ticks == 0 && GameState.frameCounter != 0);
        }

        #endregion
    }
}