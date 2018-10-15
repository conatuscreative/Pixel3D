// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.Audio
{
	public interface IStreamingAudio
	{
		float Volume { get; set; }
		bool IsLooped { get; set; }

		unsafe void Open(byte* vorbisStart, byte* vorbisEnd, int loopStart = 0);
		void Close();

		void Play();
		void Pause();
	}
}
