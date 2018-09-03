using Pixel3D.Animations;

namespace Pixel3D
{
	public static class AnimationSetExtensions
	{
		public static AABB AsAudioAABB(this AnimationSet animationSet, Position position, bool facingLeft)
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