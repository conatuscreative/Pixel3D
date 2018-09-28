// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.Generator
{
	internal class MethodBuilderCreator : MethodCreator
	{
		private readonly TypeBuilder typeBuilder;

		public MethodBuilderCreator(TypeBuilder typeBuilder)
		{
			this.typeBuilder = typeBuilder;
		}

		public override MethodInfo CreateMethod(Type owner, string name, Type returnType, Type[] parameterTypes)
		{
			const MethodAttributes staticMethod =
				MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
			var mb = typeBuilder.DefineMethod(name, staticMethod, returnType, parameterTypes);

			// Take a rough guess at parameter names:
			// (Note: parameter 0 for DefineParameter is 'this', even for static methods)
			if (parameterTypes.Length > 0 && (parameterTypes[0] == typeof(SerializeContext) ||
			                                  parameterTypes[0] == typeof(DeserializeContext)))
				mb.DefineParameter(1, 0, "context");
			if (parameterTypes.Length > 1)
			{
				if (parameterTypes[1] == typeof(BinaryWriter))
					mb.DefineParameter(2, 0, "bw");
				else if (parameterTypes[1] == typeof(BinaryReader))
					mb.DefineParameter(2, 0, "br");
			}

			if (parameterTypes.Length > 2) mb.DefineParameter(3, 0, "obj"); // The subject, probably.

			return mb;
		}
	}
}