// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;

namespace Pixel3D.Serialization.BuiltIn.DelegateHandling
{
	public struct InvocationList
	{
		// Invocation list mode
		internal InvocationList(object[] invocationList, int invocationCount) : this()
		{
			this.invocationList = invocationList;
			Count = invocationCount;
			theDelegate = null;
		}

		// Single mode
		internal InvocationList(MulticastDelegate theDelegate) : this()
		{
			invocationList = null;
			Count = 1;
			this.theDelegate = theDelegate;
		}

		private readonly object[] invocationList;
		private readonly Delegate theDelegate;

		public InvocationListEnumerator GetEnumerator()
		{
			return new InvocationListEnumerator(invocationList, Count, theDelegate);
		}

        public int Count { get; private set; }

		public Delegate this[int index]
		{
			get
			{
				if ((uint) index >= (uint) Count) // Also check for values < 0 by wrapping them around with uint
					throw new IndexOutOfRangeException();

				if (invocationList != null)
					return (Delegate) invocationList[index];
				return theDelegate;
			}
		}
	}
}