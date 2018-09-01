using Microsoft.Xna.Framework;
using Pixel3D.Audio;

namespace Pixel3D.Engine.Audio
{
	internal static class AudioExtensions
	{
		public static AudioPosition AsAudioPosition(this Vector2 position)
		{
			return new AudioPosition { x = position.X, y = position.Y};
		}
	}
}