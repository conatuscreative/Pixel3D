using System;

namespace Pixel3D.Audio
{
	/// <summary>Convert an audio position to fade/pitch/pan for playback</summary>
    public struct FadePitchPan
    {
        public float fade, pitch, pan;

        public FadePitchPan(float x, float y)
        {
            var xFade = Math.Abs(x).MapFrom(1.05f, 1.3f).Clamp(); // <- assume horizontal is smaller
            var yFade = Math.Abs(y).MapFrom(1.2f, 1.6f).Clamp();
            fade = ((float)Math.Sqrt(xFade * xFade + yFade * yFade)).Clamp().MapTo(1f, 0f); // <- circular edges
			
            pitch = fade.MapFrom(0f, 0.3f).Clamp().MapTo(-0.15f, 0f); // <- subtle pitch-down effect at the edges :)
			
			if(x < 0)
                pan = -y.MapFrom(-0.4f, -1.2f).Clamp();
            else
                pan =  y.MapFrom( 0.4f,  1.2f).Clamp();
        }

        public FadePitchPan(float fade)
        {
            this.fade = fade;
            pitch = 0;
            pan = 0;
        }
		
        public void ApplyTo(SafeSoundEffectInstance soundEffectInstance, float mixVolume)
        {
	        AudioSystem.setFadePitchPan(soundEffectInstance, fade * mixVolume, pitch, pan);
        }
    }
}