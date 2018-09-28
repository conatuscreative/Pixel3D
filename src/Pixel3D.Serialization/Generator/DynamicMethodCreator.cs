// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Pixel3D.Serialization.Generator
{
	internal class DynamicMethodCreator : MethodCreator
	{
		public override MethodInfo CreateMethod(Type owner, string name, Type returnType, Type[] parameterTypes)
		{
			if (owner != null && owner.IsInterface)
				owner = null;
			if (owner == null || owner.IsArray)
				return new DynamicMethod(name, returnType, parameterTypes, true);
			return new DynamicMethod(name, returnType, parameterTypes, owner, true);
		}
	}
}