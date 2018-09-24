// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Pixel3D.P2P;

namespace Pixel3D.Network
{
	public static class FilterName
	{
		// Basically exists to expose this out of Pixel3D.Network.P2P, which Engine doesn't reference
		// TODO: Fiddle around with the architecture so that the string filter function is passed up to the network layer
		//       (Because why does the network know about what characters we can render?)
		public static string Filter(string name)
		{
			return name.FilterName();
		}
	}
}