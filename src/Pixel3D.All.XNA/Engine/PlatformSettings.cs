// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.IO;

namespace Pixel3D.Engine
{
	public static class PlatformSettings
	{
		public static string GetPlatformSettingsDir(string gameTitlePath)
		{
			return @".";
		}

		public static string CreateAndReturnDir(string dir)
		{
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			return dir;
		}

		public static string GetLoggingDir()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Conatus Creative", "RCRU");
		}
	}
}