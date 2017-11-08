using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;

namespace Pixel3D.Audio
{
    public static class AudioDevice
    {
        public static bool Available { get; private set; }

        static AudioDevice()
        {
            try
            {
                SoundEffect.MasterVolume = 1f;
                // The above line should throw an exception if there is no audio device
                Available = true;
            }
            catch(NoAudioHardwareException)
            {
                Debug.WriteLine("No audio hardware available");
            }
            catch(Exception e)
            {
                Debug.WriteLine("Exception during audio device testing. XNA or something under it doing something dumb.");
                Log.Current.Warn(e, "Exception during audio device testing");
            }
        }
    }
}
