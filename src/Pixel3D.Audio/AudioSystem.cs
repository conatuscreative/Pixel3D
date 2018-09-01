namespace Pixel3D.Audio
{
	public static class AudioSystem
	{
		// 
		// Sound Effect Instance:
		public static CreateSoundEffectInstance createSoundEffectInstance;

		//
		// Sound Effect:
		public static CreateSoundEffectFromStream createSoundEffectFromStream;
		public static CreateSoundEffectFromFile createSoundEffectFromFile;
		public static PlaySoundEffect playSoundEffect;

		public static IsAudioDeviceAvailable audioDeviceCheck;
		public static GetSingle getMasterVolume;
		public static SetSingle setMasterVolume;


		public static PlaySoundEffectInstance playSoundEffectInstance;
		public static StopSoundEffectInstance stopSoundEffectInstance;
		public static PauseSoundEffectInstance pauseSoundEffectInstance;

		public static GetString getName;
		public static SetString setName;

		public static GetBoolean getIsLooped;
		public static SetBoolean setIsLooped;

		public static GetSingle getVolume;
		public static SetSingle setVolume;

		public static GetSingle getPitch;
		public static SetSingle setPitch;

		public static GetSingle getPan;
		public static SetSingle setPan;

		public static GetTimeSpan getDuration;

		public static GetState getState;
		
		public static SetFadePitchPan setFadePitchPan;
	}
}