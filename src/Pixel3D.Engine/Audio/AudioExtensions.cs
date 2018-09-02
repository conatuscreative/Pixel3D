using Pixel3D.Animations;
using Pixel3D.Audio;

namespace Pixel3D.Engine.Audio
{
	public static class AudioExtensions
	{
		public static AudioPosition AsAudioPosition(this Position position)
		{
			return new AudioPosition { X = position.X, Y = position.Y, Z = position.Z };
		}

		public static AudioAABB AsAudioAABB(this AABB aabb)
		{
			return new AudioAABB(aabb.Left, aabb.Right, aabb.Bottom, aabb.Top, aabb.Front, aabb.Back);
		}

		public static Position AsPosition(this AudioPosition position)
		{
			return new Position { X = position.X, Y = position.Y, Z = position.Z };
		}

		public static AudioAABB AsAudioAABB(this AnimationSet animationSet, Position position, bool facingLeft)
		{
			// TODO: Stop assuming a height, and get a real AABB from the heightmap (requires Heightmap cache its own AABB)
			var heightmapView = new HeightmapView(animationSet.Heightmap, position, facingLeft);
			var heightmapXZ = heightmapView.Bounds;
			const int guessHeight = 50;
			var aabb = new AudioAABB(heightmapXZ.Left, heightmapXZ.Right - 1, position.Y, position.Y + guessHeight, heightmapXZ.Y, heightmapXZ.Y + heightmapXZ.Height - 1);
			return aabb;
		}
	}
}