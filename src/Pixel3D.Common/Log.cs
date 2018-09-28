// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D
{
	public static class Log
	{
		private static ILogger logger;

		public static ILogger Current
		{
			get { return logger ?? (logger = new TraceLogger()); }
		}

		public static void FallbackToMemory()
		{
			logger = new MemoryLogger();
		}
	}
}