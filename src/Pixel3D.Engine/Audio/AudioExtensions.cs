using Pixel3D.Audio;

namespace Pixel3D.Engine.Audio
{
	public static class AudioExtensions
	{
		public static AudioPosition AsAudioPosition(this Position position)
		{
			return new AudioPosition { X = position.X, Y = position.Y, Z = position.Z };
		}

		public static Position AsPosition(this AudioPosition position)
		{
			return new Position { X = position.X, Y = position.Y, Z = position.Z };
		}
	}
}