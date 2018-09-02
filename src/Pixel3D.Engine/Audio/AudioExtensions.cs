using Pixel3D.Audio;

namespace Pixel3D.Engine.Audio
{
	internal static class AudioExtensions
	{
		public static AudioPosition AsAudioPosition(this Position position)
		{
			return new AudioPosition { x = position.X, y = position.Y, z = position.Z };
		}
	}
}