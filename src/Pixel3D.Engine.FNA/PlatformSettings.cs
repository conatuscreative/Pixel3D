// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.IO;
using SDL2;

namespace Pixel3D.Engine
{
	public static class PlatformSettings
	{
		public static string GetPlatformSettingsDir(string gameTitlePath)
		{
			string os = SDL.SDL_GetPlatform();
			if (os.Equals("Linux"))
			{
				string osDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
				if (string.IsNullOrEmpty(osDir))
				{
					osDir = Environment.GetEnvironmentVariable("HOME");
					if (string.IsNullOrEmpty(osDir))
					{
						return @"."; // Oh well.
					}

					return CreateAndReturnDir(Path.Combine(osDir, ".config/" + gameTitlePath));
				}

				return CreateAndReturnDir(Path.Combine(osDir, gameTitlePath));
			}

			if (os.Equals("Mac OS X"))
			{
				string osDir = Environment.GetEnvironmentVariable("HOME");
				if (string.IsNullOrEmpty(osDir))
				{
					return @"."; // Oh well.
				}

				return CreateAndReturnDir(Path.Combine(osDir,
					"Library/Application Support/" + gameTitlePath));
			}

			if (!os.Equals("Windows"))
			{
				throw new NotSupportedException("Unhandled SDL2 platform!");
			}

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
			string os = SDL.SDL_GetPlatform();
			if (os.Equals("Linux"))
			{
				string osDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
				if (string.IsNullOrEmpty(osDir))
				{
					osDir = Environment.GetEnvironmentVariable("HOME");
					if (string.IsNullOrEmpty(osDir))
					{
						return @"."; // Oh well.
					}
					return CreateAndReturnDir(Path.Combine(osDir, ".config/RiverCityRansomUnderground"));
				}
				return CreateAndReturnDir(Path.Combine(osDir, "RiverCityRansomUnderground"));
			}
			if (os.Equals("Mac OS X"))
			{
				string osDir = Environment.GetEnvironmentVariable("HOME");
				if (string.IsNullOrEmpty(osDir))
				{
					return @"."; // Oh well.
				}
				return CreateAndReturnDir(Path.Combine(osDir, "Library/Application Support/RiverCityRansomUnderground"));
			}
			if (!os.Equals("Windows"))
			{
				throw new NotSupportedException("Unhandled SDL2 platform!");
			}
			
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Conatus Creative", "RCRU");
		}
	}
}