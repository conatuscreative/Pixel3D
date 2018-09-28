// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Audio
{
	// TODO: Replace me with Cue
	public class AmbientSound
	{
		public readonly float pan;
		public readonly float pitch;
		public readonly float volume;
		public int radius;
		public SafeSoundEffect soundEffect;

		public AmbientSound(SafeSoundEffect sound, int radius, float volume, float pitch, float pan)
		{
			soundEffect = sound;
			this.radius = radius;
			this.volume = volume;
			this.pitch = pitch;
			this.pan = pan;
		}
	}
}