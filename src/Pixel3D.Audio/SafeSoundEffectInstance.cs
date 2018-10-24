// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using Microsoft.Xna.Framework.Audio;

namespace Pixel3D.Audio
{
	public class SafeSoundEffectInstance : IDisposable
	{
		/// <summary>The underlying sound effect instance (cannot be null)</summary>
		private readonly SoundEffectInstance inner;

		public SafeSoundEffectInstance(SoundEffectInstance inner)
		{
			this.inner = inner;
		}

	    public SoundState State
	    {
		    get { return inner.State; }
	    }

		#region Wrapper

		public float Volume
		{
		    get { return inner.Volume; }
		    set { inner.Volume = value; }
		}

		public float Pitch
		{
		    get { return inner.Pitch; }
		    set { inner.Pitch = value; }
		}

		public float Pan
		{
		    get { return inner.Pan; }
		    set { inner.Pan = value; }
		}

		public bool IsLooped
		{
		    get { return inner.IsLooped; }
		    set { inner.IsLooped = value; }
		}

		public bool IsDisposed
		{
		    get { return inner.IsDisposed; }
		}

		public void Play()
		{
			inner.Play();
		}

		public void Stop()
		{
			inner.Stop();
		}

		public void Pause()
		{
			inner.Pause();
		}

		public void Dispose()
		{
            if(inner != null)
               inner.Dispose();
		}

		#endregion
	}
}