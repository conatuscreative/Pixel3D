// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;

namespace Pixel3D
{
	public class MemoryLogger : ILogger
	{
		public MemoryLogger(int limit = 100)
		{
			Limit = limit;
			Logs = new Queue<string>();
		}

		public int Limit { get; set; }

		public Queue<string> Logs { get; private set; }

		public void Trace(string message, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("trace", message, null, args);
			Write(logline);
		}

		public void Info(string message, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("info", message, null, args);
			Write(logline);
		}

		public void Warn(string message, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("warn", message, null, args);
			Write(logline);
		}

		public void Error(string message, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("error", message, null, args);
			Write(logline);
		}

		public void Fatal(string message, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("fatal", message, null, args);
			Write(logline);
		}

		public void WarnException(string message, Exception exception, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("warn", message, exception, args);
			Write(logline);
		}

		public void ErrorException(string message, Exception exception, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("error", message, exception, args);
			Write(logline);
		}

		public void FatalException(string message, Exception exception, params object[] args)
		{
			var logline = LogLineFormatter.LogLine("fatal", message, exception, args);
			Write(logline);
		}

		private void Write(string logline)
		{
			Logs.Enqueue(logline);
			while (Logs.Count > Limit)
				Logs.Dequeue();
		}
	}
}