// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Runtime.Serialization;

namespace Pixel3D.P2P
{
	[Serializable]
	public class InsufficientDataToContinueException : Exception
	{
		public InsufficientDataToContinueException()
		{
		}

		public InsufficientDataToContinueException(string message) : base(message)
		{
		}

		public InsufficientDataToContinueException(string message, Exception inner) : base(message, inner)
		{
		}

		protected InsufficientDataToContinueException(
			SerializationInfo info,
			StreamingContext context)
			: base(info, context)
		{
		}
	}
}