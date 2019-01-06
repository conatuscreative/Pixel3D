// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;

namespace Pixel3D.ActorManagement
{
	public struct SmoothingIdentifiers
	{
		// Don't need equality check methods because SmoothingManager does the right thing. (And no one will break it, right?)
		public Type type;
		public int id;
	}
}