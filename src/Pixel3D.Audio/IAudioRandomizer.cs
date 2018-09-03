namespace Pixel3D.Audio
{
	public interface IAudioRandomizer
	{
		float _NetworkUnsafe_UseMeForAudioOnly_NextSingle();
		int Next(int soundCount);
	}
}