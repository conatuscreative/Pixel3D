// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Reflection;

namespace Pixel3D.Serialization.Generator
{
	internal abstract class MethodCreator
	{
		public abstract MethodInfo CreateMethod(Type owner, string name, Type returnType, Type[] parameterTypes);
	}
}