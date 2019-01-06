// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using Pixel3D.Animations;

namespace Pixel3D.ActorManagement
{
	/// <summary>
	///     Provides various accessors for handling "object recipes" that transfer from
	///     an external source (like an editor, or level data), and are instantiated by the
	///     engine. This interface should not be used on actual game actors.
	/// </summary>
	public interface IGameObjectDefinition : IHasDrawableFrame
	{
		AnimationSet AnimationSet { get; }
		Position Position { get; set; }
		bool FacingLeft { get; }
	}
}