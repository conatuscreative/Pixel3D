// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D
{
	public class TraceLogger : ILogger
	{
		public void Trace(string message, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("trace", message, null, args);
			System.Diagnostics.Trace.TraceInformation(logline);
		}

		public void Info(string message, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("info", message, null, args);
			System.Diagnostics.Trace.TraceInformation(logline);
		}

		public void Warn(string message, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("warn", message, null, args);
			System.Diagnostics.Trace.TraceWarning(logline);
		}

		public void Error(string message, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("error", message, null, args);
			System.Diagnostics.Trace.TraceError(logline);
		}

		public void Fatal(string message, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("fatal", message, null, args);
			System.Diagnostics.Trace.TraceError(logline);
		}

		public void WarnException(string message, Exception exception, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("warn", message, exception, args);
			System.Diagnostics.Trace.TraceWarning(logline);
		}

		public void ErrorException(string message, Exception exception, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("error", message, exception, args);
			System.Diagnostics.Trace.TraceError(logline);
		}

		public void FatalException(string message, Exception exception, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("fatal", message, exception, args);
			System.Diagnostics.Trace.TraceError(logline);
		}
	}
}