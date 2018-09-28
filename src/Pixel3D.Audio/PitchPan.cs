// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Audio
{
	public struct PitchPan
	{
		public float pitch;
		public float pan;

		public PitchPan(float pitch, float pan)
		{
			this.pitch = pitch;
			this.pan = pan;
		}
	}
}