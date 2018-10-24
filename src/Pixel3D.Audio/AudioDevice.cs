// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;

namespace Pixel3D.Audio
{
	public static class AudioDevice
	{
		private static bool? _available;

		public static bool Available
		{
			get
			{
				if (_available.HasValue)
					return _available.Value;
				_available = AudioDeviceCheck();
				return _available.GetValueOrDefault();
			}
		}

		private static bool AudioDeviceCheck()
		{
			try
			{
				SoundEffect.MasterVolume = 1f;
				// The above line should throw an exception if there is no audio device
				return true;
			}
			catch (NoAudioHardwareException)
			{
				Debug.WriteLine("No audio hardware available");
				return false;
			}
			catch (Exception e)
			{
				Debug.WriteLine("Exception during audio device testing. XNA or something under it doing something dumb.");
				Log.Current.WarnException("Exception during audio device testing", e);
				return false;
			}
		}
	}
}