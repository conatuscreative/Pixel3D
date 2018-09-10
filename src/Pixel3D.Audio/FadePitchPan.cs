// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D.Audio
{
	/// <summary>Convert an audio position to fade/pitch/pan for playback</summary>
	public struct FadePitchPan
	{
		public float fade, pitch, pan;

		public FadePitchPan(PitchPan pitchPan) : this(pitchPan.pitch, pitchPan.pitch)
		{
		}

		public FadePitchPan(float pitch, float pan)
		{
			var xFade = Math.Abs(pitch).MapFrom(1.05f, 1.3f).Clamp(); // <- assume horizontal is smaller
			var yFade = Math.Abs(pan).MapFrom(1.2f, 1.6f).Clamp();
			fade = ((float) Math.Sqrt(xFade * xFade + yFade * yFade)).Clamp().MapTo(1f, 0f); // <- circular edges

			this.pitch =
				fade.MapFrom(0f, 0.3f).Clamp().MapTo(-0.15f, 0f); // <- subtle pitch-down effect at the edges :)

			if (pitch < 0)
				this.pan = -pan.MapFrom(-0.4f, -1.2f).Clamp();
			else
				this.pan = pan.MapFrom(0.4f, 1.2f).Clamp();
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