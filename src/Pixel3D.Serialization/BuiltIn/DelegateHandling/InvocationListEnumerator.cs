// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D.Serialization.BuiltIn.DelegateHandling
{
	public struct InvocationListEnumerator
	{
		internal InvocationListEnumerator(object[] invocationList, int invocationCount, Delegate theDelegate) : this()
		{
			this.invocationList = invocationList;
			this.invocationCount = invocationCount;
			Current = theDelegate;
			index = 0;
		}

		private readonly object[] invocationList;
		private readonly int invocationCount;
		private int index;

		public Delegate Current { get; private set; }

		public bool MoveNext()
		{
			if (invocationList == null)
			{
				if (index == 0)
				{
					index++;
					return true;
				}
			}
			else if (index < invocationCount)
			{
				Current = (Delegate) invocationList[index];
				index++;
				return true;
			}

			return false;
		}
	}
}