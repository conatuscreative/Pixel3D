// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Animations
{
	public struct FrontBack
	{
		public byte front, back;

		// NOTE: Hacky mechanisim for storing "on top" status without requiring more data (we're nicely 16-bits)
		//       If we are "on top", then only the back bounds contains usable data
		bool IsOnTop { get { return front > back; } }
	}
}