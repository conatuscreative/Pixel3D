// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Pixel3D.Serialization.Generator
{
	internal class MethodBuilderCreatorCreator : MethodCreatorCreator
	{
		private readonly ModuleBuilder moduleBuilder;
		private readonly string @namespace;


		private readonly List<TypeBuilder> typeBuilders = new List<TypeBuilder>();

		public MethodBuilderCreatorCreator(ModuleBuilder moduleBuilder, string @namespace)
		{
			this.moduleBuilder = moduleBuilder;
			this.@namespace = @namespace;
		}

		public void Finish()
		{
			foreach (var typeBuilder in typeBuilders)
			{
#if NET40
				typeBuilder.CreateType();
#else
	            typeBuilder.CreateTypeInfo();
#endif
			}
		}


		public override MethodCreator Create(string containingTypeName)
		{
			var typeBuilder = moduleBuilder.DefineType(@namespace + "." + containingTypeName);
			typeBuilders.Add(typeBuilder);
			return new MethodBuilderCreator(typeBuilder);
		}
	}
}