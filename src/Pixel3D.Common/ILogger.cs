// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D
{
	public interface ILogger
	{
		void Trace(string message, params object[] args);
		void Info(string message, params object[] args);
		void Warn(string message, params object[] args);
		void Error(string message, params object[] args);
		void Fatal(string message, params object[] args);

		void WarnException(string message, Exception exception, params object[] args);
		void ErrorException(string message, Exception exception, params object[] args);
		void FatalException(string message, Exception exception, params object[] args);
	}
}