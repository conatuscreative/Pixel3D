using System;
using Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Pixel3D
{
    /// <summary>Convert an audio position to fade/pitch/pan for playback</summary>
    public struct FadePitchPan
    {
        public float fade, pitch, pan;

        public FadePitchPan(Vector2 audioPosition)
        {
            float xFade = Math.Abs(audioPosition.X).MapFrom(1.05f, 1.3f).Clamp(); // <- assume horizontal is smaller
            float yFade = Math.Abs(audioPosition.Y).MapFrom(1.2f, 1.6f).Clamp();
            fade = ((float)Math.Sqrt(xFade * xFade + yFade * yFade)).Clamp().MapTo(1f, 0f); // <- circular edges


            pitch = fade.MapFrom(0f, 0.3f).Clamp().MapTo(-0.15f, 0f); // <- subtle pitch-down effect at the edges :)


            if(audioPosition.X < 0)
                pan = -(audioPosition.X.MapFrom(-0.4f, -1.2f).Clamp());
            else
                pan =  (audioPosition.X.MapFrom( 0.4f,  1.2f).Clamp());
        }

        public FadePitchPan(float fade)
        {
            this.fade = fade;
            this.pitch = 0;
            this.pan = 0;
        }


        public void ApplyTo(SoundEffectInstance soundEffectInstance, float mixVolume)
        {
            soundEffectInstance.Volume = fade * mixVolume;
            soundEffectInstance.Pitch = pitch;
            soundEffectInstance.Pan = pan;
        }
    }
}
