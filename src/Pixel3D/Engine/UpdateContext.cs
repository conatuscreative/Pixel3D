using System.Collections.Generic;
using Pixel3D.Animations;
using Pixel3D.Audio;

namespace Pixel3D.Engine
{
    public class UpdateContext : IUpdateContext
    {
        protected readonly SoundRollbackManager soundRollbackManager;

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
        }

        #region Non-Retained Data

        // Things that must not be retained across frames (clear in Reset method!)

        /// <summary>The index in the actor list of the actor currently being updated (or -1 when outside the normal actor update loop)</summary>
        public int activeActorIndex = -1;

        public XorShift random; // <- here so that no one uses it outside of network update methods

        // Kinda hacky?
        public List<AnimationPlayer> initialAnimationStates = new List<AnimationPlayer>();

        #endregion

        #region Audio

        /// <summary>For positioning audio in world space. May be null.</summary>
        protected Camera AudioCamera { get; set; }


        #endregion

        #region Helpers


        #endregion
    }
}