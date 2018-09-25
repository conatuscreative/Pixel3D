// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.


namespace Pixel3D.UI
{
	public interface IDrawableGameMenuHost : IGameMenuHost
	{
		Position Position { get; }
		int Width { get; }
		int Height { get; }
		bool DeferLayout { get; }
	}
}