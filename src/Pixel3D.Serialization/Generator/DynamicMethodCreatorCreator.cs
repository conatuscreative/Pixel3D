// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Serialization.Generator
{
	internal class DynamicMethodCreatorCreator : MethodCreatorCreator
	{
		public override MethodCreator Create(string containingTypeName)
		{
			return new DynamicMethodCreator();
		}
	}
}