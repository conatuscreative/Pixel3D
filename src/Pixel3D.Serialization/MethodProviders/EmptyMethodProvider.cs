// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Reflection;

namespace Pixel3D.Serialization.MethodProviders
{
	internal class EmptyMethodProvider : MethodProvider
	{
		public override MethodInfo GetMethodForType(Type type)
		{
			return null;
		}
	}
}