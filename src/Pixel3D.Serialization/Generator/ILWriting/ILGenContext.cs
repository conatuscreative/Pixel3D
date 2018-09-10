// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Pixel3D.Serialization.MethodProviders;

namespace Pixel3D.Serialization.Generator.ILWriting
{
	// This class exists to allow for the generator code to be bi-directional (cutting down on code duplication in the generator)
	internal class ILGenContext
	{
		public readonly Direction direction;
		public readonly MethodProvider fieldSerializationMethods;
		public readonly MethodProvider referenceTypeSerializationMethods;

		public ILGenContext(Direction direction, MethodProvider fieldSerializeMethods,
			MethodProvider referenceTypeSerializeMethods)
		{
			this.direction = direction;
			fieldSerializationMethods = fieldSerializeMethods;
			referenceTypeSerializationMethods = referenceTypeSerializeMethods;
		}

	    public bool Serialize
	    {
	        get { return direction == Direction.Serialize; }
	    }

	    public bool Deserialize
	    {
	        get { return direction != Direction.Serialize; }
	    }
	}
}