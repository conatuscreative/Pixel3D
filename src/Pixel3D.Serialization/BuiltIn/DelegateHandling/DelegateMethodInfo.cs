// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Reflection;

namespace Pixel3D.Serialization.BuiltIn.DelegateHandling
{
	internal struct DelegateMethodInfo
	{
		public DelegateMethodInfo(MethodInfo method, bool canHaveTarget)
		{
			this.method = method;
			this.canHaveTarget = canHaveTarget;
		}

		public readonly MethodInfo method;
		public readonly bool canHaveTarget;
	}
}