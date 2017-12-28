using Pixel3D.Engine;

namespace Pixel3D.Audio
{
    public interface IAudioPlayer
    {
        /// <summary>Play a sound without any position (always plays centred)</summary>
        void PlayCueGlobal(string symbol, Actor source = null); // <- keeping source around, in case it is useful information (will become useful for rollback)
        
        /// <summary>Play a sound without any position (always plays centred)</summary>
        void PlayCueGlobal(Cue cue, Actor source = null); // <- keeping source around, in case it is useful information (will become useful for rollback)

        void PlayMenuMusic(string symbol, bool loop = true, bool synchronise = false);
    }
}