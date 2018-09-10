// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Pixel3D.Serialization.Generator
{
	internal static class MethodInfoExtensions
	{
		// Let's pretend that MethodInfo provides GetILGenerator as an abstract method...
		public static ILGenerator GetILGenerator(this MethodInfo methodInfo)
		{
			var dynamicMethod = methodInfo as DynamicMethod;
			if (dynamicMethod != null)
				return dynamicMethod.GetILGenerator();

			var methodBuilder = methodInfo as MethodBuilder;
			if (methodBuilder != null)
				return methodBuilder.GetILGenerator();

			throw new InvalidOperationException("Cannot generate IL for fixed MethodInfo");
		}
	}
}