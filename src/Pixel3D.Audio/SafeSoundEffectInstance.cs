// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

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

	    public SoundState State { get { return AudioSystem.getSoundState(owner); } }

		#region Wrapper

		public float Volume
		{
		    get
		    {
		        return AudioSystem.getVolume(owner);
		    }
		    set
		    {
		        AudioSystem.setVolume(owner, value);
		    }
		}

		public float Pitch
		{
		    get
		    {
                return AudioSystem.getPitch(owner);
		    }
		    set
		    {
		        AudioSystem.setPitch(owner, value);
		    }
		}

		public float Pan
		{
		    get
		    {
                return AudioSystem.getPan(owner);
		    }
		    set
		    {
		        AudioSystem.setPan(owner, value);
		    }
		}

		public bool IsLooped
		{
		    get
		    {
                return AudioSystem.getIsLooped(owner);
		    }
		    set
		    {
		        AudioSystem.setIsLooped(owner, value);
		    }
		}

		public bool IsDisposed
		{
		    get
		    {
                return AudioSystem.getIsDisposed(owner);
		    }
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
            if(owner != null)
               owner.Dispose();
		}

		#endregion
	}
}