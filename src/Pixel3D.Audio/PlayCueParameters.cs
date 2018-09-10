// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Diagnostics;

namespace Pixel3D.Audio
{
	/// <summary>
	///     Holds the gameplay-affecting logic for playing a cue
	/// </summary>
	public struct PlayCueParameters
	{
		public int soundIndex;
		public float cuePitch;

		public const int NO_SOUND = -1;
		public const int MISSING_CUE = -2;


		/// <summary>Potentially gameplay-mutating logic for cue playback (modifies `random` and `cueStates`)</summary>
		public static PlayCueParameters GetParameters(Cue cue, XorShift random, ushort[] cueStates)
		{
			if (cue == null)
				return new PlayCueParameters {soundIndex = NO_SOUND};
			if (ReferenceEquals(cue, Cue.missingCue))
				return new PlayCueParameters {soundIndex = MISSING_CUE};
			if (cue.SoundCount == 0)
				return new PlayCueParameters {soundIndex = NO_SOUND};

			PlayCueParameters result;
			result.cuePitch = cue.SelectPitch(random);
			result.soundIndex = cue.SelectSound(random, cueStates);

			Debug.Assert(result.soundIndex >= 0);

			return result;
		}
	}
}