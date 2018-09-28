// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;

namespace Pixel3D
{
	public static class LogLineFormatter
	{
		public static string LogLine(string loglevel, string message, Exception exception, params object[] args)
		{
			var stackTrace = new StackTrace();
			var frame = stackTrace.GetFrame(1);
			var method = frame.GetMethod();
			var logline = string.Format("{0} [{1}]: {2}: {3} {4}",
				DateTime.UtcNow.ToLongDateString(),
				loglevel.ToUpperInvariant(),
				method.DeclaringType == null ? "" : method.DeclaringType.Name,
				string.Format(message, args),
				exception);
			return logline;
		}
	}
}