namespace Pixel3D.Engine.Audio
{
    // TODO: Replace me with Cue
    public class AmbientSound
    {
        public AmbientSound(SafeSoundEffect sound, int radius, float volume, float pitch, float pan)
        {
            this.soundEffect = sound;
            this.radius = radius;
            this.volume = volume;
            this.pitch = pitch;
            this.pan = pan;
        }
        public SafeSoundEffect soundEffect;
        public int radius;
        public readonly float volume;
        public readonly float pitch;
        public readonly float pan;
    }
}
