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
        }

        #region Non-Retained Data

        // Things that must not be retained across frames (clear in Reset method!)

        public XorShift random; // <- here so that no one uses it outside of network update methods

        #endregion

        #region Audio

        /// <summary>For positioning audio in world space. May be null.</summary>
        protected Camera AudioCamera { get; set; }


        #endregion
    }
}