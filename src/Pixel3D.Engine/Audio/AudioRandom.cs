using Pixel3D.Audio;

namespace Pixel3D.Engine.Audio
{
	public struct AudioRandom : IAudioRandomizer
	{
		private readonly XorShift random;

		public AudioRandom(XorShift random)
		{
			this.random = random;
		}

		public float _NetworkUnsafe_UseMeForAudioOnly_NextSingle()
		{
			return random._NetworkUnsafe_UseMeForAudioOnly_NextSingle();
		}

		public int Next(int soundCount)
		{
			return random.Next(soundCount);
		}
	}
}