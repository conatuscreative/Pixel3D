// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using Pixel3D.P2P.Diagnostics;

namespace Pixel3D.Network.Test
{
	internal class ConsoleLogHandler : NetworkLogHandler
	{
		public override void HandleLidgrenMessage(string message)
		{
			var fgc = Console.ForegroundColor;
			var bgc = Console.BackgroundColor;

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.BackgroundColor = ConsoleColor.DarkBlue;
			Console.WriteLine(message);

			Console.ForegroundColor = fgc;
			Console.BackgroundColor = bgc;
		}

		public override void HandleMessage(string message)
		{
			var fgc = Console.ForegroundColor;
			var bgc = Console.BackgroundColor;

			Console.ForegroundColor = ConsoleColor.Gray;
			Console.BackgroundColor = ConsoleColor.DarkGreen;
			Console.WriteLine(message);

			Console.ForegroundColor = fgc;
			Console.BackgroundColor = bgc;
		}
	}
}