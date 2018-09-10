// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.Audio
{
	/// <summary>
	///     Parallel: all sounds in the cue play at the same time
	///     Serial: all sounds play one after the other
	///     Cycle: first cue plays sound1, second cue plays sound2, repeating etc.
	///     Random: each cue plays randomly one of the sounds in it
	///     RandomCycle: each cue plays randomly but does not replay a sound until all other sounds have been played
	/// </summary>
	public enum CueType : byte
	{
		Parallel,
		Serial,
		Cycle,
		Random,
		RandomCycle
	}
}