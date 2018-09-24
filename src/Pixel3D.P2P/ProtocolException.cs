// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Pixel3D.P2P
{
	/// <summary>
	///     Represents a protocol error (ie: badly formed packet).
	///     In debug mode, with trusted peers, should never happen unless there's a programming error - so break into the
	///     debugger.
	///     But could theoretically be caused by a remote client with a modified game.
	/// </summary>
	[Serializable]
	public class ProtocolException : NetworkDataException
	{
		public ProtocolException()
		{
			Debug.Assert(false);
		}

		public ProtocolException(string message) : base(message)
		{
			Debug.Assert(false);
		}

		public ProtocolException(string message, Exception inner) : base(message, inner)
		{
			Debug.Assert(false);
		}

		protected ProtocolException(
			SerializationInfo info,
			StreamingContext context)
			: base(info, context)
		{
			Debug.Assert(false);
		}
	}
}