// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Pixel3D.Audio;
using Pixel3D.FNA.PostProcessing;

namespace Pixel3D.UI
{
	public interface INetworkSafeContext
	{
		IPostProcessProvider PostProcessProvider { get; }
		bool TryGetAudioPlayer(out IAudioPlayer audioPlayer);
	}
}