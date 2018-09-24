// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Microsoft.Xna.Framework;
using Pixel3D.P2P.Diagnostics;

namespace Pixel3D.Network
{
	public class SimpleConsoleWriter : NetworkLogHandler
	{
		private readonly SimpleConsole target;

		public SimpleConsoleWriter(SimpleConsole target)
		{
			this.target = target;
		}

		public override void HandleLidgrenMessage(string message)
		{
			target.WriteLine(message, Color.DarkBlue);
		}

		public override void HandleMessage(string message)
		{
			target.WriteLine(message, Color.DarkGreen);
		}
	}
}