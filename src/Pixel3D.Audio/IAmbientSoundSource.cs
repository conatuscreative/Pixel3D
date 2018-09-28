// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Audio
{
	/// <summary>
	///     Objects that can play back ambient audio
	/// </summary>
	public interface IAmbientSoundSource
	{
		AmbientSound AmbientSound { get; }
		Position Position { get; }
		bool FacingLeft { get; }
		AABB? Bounds { get; }
	}
}