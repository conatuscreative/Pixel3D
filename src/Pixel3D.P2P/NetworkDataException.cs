// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Runtime.Serialization;

namespace Pixel3D.P2P
{
	/// <summary>
	///     Represents an unrecoverable error that occurs when reading a NetIncomingMessage.
	///     Any method that dispatches NetIncomingMessages, whether it is directly from Lidgren
	///     or a message queued in a RemotePeer, should catch and handle this exception.
	/// </summary>
	[Serializable]
	public class NetworkDataException : Exception
	{
		public NetworkDataException()
		{
		}

		public NetworkDataException(string message) : base(message)
		{
		}

		public NetworkDataException(string message, Exception inner) : base(message, inner)
		{
		}

		protected NetworkDataException(
			SerializationInfo info,
			StreamingContext context)
			: base(info, context)
		{
		}
	}
}