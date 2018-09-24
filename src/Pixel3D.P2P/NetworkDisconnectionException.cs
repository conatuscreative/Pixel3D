// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Runtime.Serialization;

namespace Pixel3D.P2P
{
	/// <summary>
	///     Thrown when the network gets disconnected
	/// </summary>
	[Serializable]
	public class NetworkDisconnectionException : Exception
	{
		public NetworkDisconnectionException()
		{
		}

		public NetworkDisconnectionException(string message) : base(message)
		{
		}

		public NetworkDisconnectionException(string message, Exception inner) : base(message, inner)
		{
		}

		protected NetworkDisconnectionException(
			SerializationInfo info,
			StreamingContext context)
			: base(info, context)
		{
		}
	}
}