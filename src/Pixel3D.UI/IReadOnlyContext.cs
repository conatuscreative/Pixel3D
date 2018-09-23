using Pixel3D.Audio;

namespace Pixel3D.UI
{
	public interface IReadOnlyContext
	{
		bool TryGetAudioPlayer(out IAudioPlayer audioPlayer);
	}
}