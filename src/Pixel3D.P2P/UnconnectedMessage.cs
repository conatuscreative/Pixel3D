// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.P2P
{
	/// <summary>
	///     NOTE: a zero-length unconnected message is used for non-responsive NAT punches
	/// </summary>
	internal enum UnconnectedMessage : byte
	{
		/// <summary>This one is used for forming connections between P2P clients.</summary>
		NATPunchThrough,

		SideChannelVerify,

		SideChannelVerifyResponse
	}
}