// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using Pixel3D.Animations;

namespace Pixel3D
{
	public static class AnimationSetExtensions
	{
		public static AABB AsAABB(this AnimationSet animationSet, Position position, bool facingLeft)
		{
			// TODO: Stop assuming a height, and get a real AABB from the heightmap (requires Heightmap cache its own AABB)
			var heightmapView = new HeightmapView(animationSet.Heightmap, position, facingLeft);
			var heightmapXZ = heightmapView.Bounds;
			const int guessHeight = 50;
			var aabb = new AABB(heightmapXZ.Left, heightmapXZ.Right - 1, position.Y, position.Y + guessHeight, heightmapXZ.Y, heightmapXZ.Y + heightmapXZ.Height - 1);
			return aabb;
		}
	}
}