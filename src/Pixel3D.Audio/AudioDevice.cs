// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

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
				_available = AudioSystem.audioDeviceCheck();
				return _available.GetValueOrDefault();
			}
		}
	}
}