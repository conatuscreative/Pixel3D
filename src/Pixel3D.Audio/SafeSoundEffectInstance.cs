using System;

namespace Pixel3D.Audio
{
	public class SafeSoundEffectInstance : IDisposable
	{
		/// <summary>The underlying sound effect instance (cannot be null)</summary>
		private readonly IDisposable owner;

		public SafeSoundEffectInstance(IDisposable owner)
		{
			this.owner = owner;
		}

		public SoundState State => AudioSystem.getSoundState(owner);

		#region Wrapper

		public float Volume
		{
			get => AudioSystem.getVolume(owner);
			set => AudioSystem.setVolume(owner, value);
		}

		public float Pitch
		{
			get => AudioSystem.getPitch(owner);
			set => AudioSystem.setPitch(owner, value);
		}

		public float Pan
		{
			get => AudioSystem.getPan(owner);
			set => AudioSystem.setPan(owner, value);
		}

		public bool IsLooped
		{
			get => AudioSystem.getIsLooped(owner);
			set => AudioSystem.setIsLooped(owner, value);
		}

		public void Play()
		{
			AudioSystem.playSoundEffectInstance(owner);
		}

		public void Stop()
		{
			AudioSystem.stopSoundEffectInstance(owner);
		}

		public void Pause()
		{
			AudioSystem.pauseSoundEffectInstance(owner);
		}

		public void Dispose()
		{
			owner?.Dispose();
		}

		#endregion
	}
}