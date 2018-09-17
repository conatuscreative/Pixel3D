// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D.Serialization
{
	/// <summary>
	///     Indicates to serialization generation that this is a root type to begin searching from.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
	public sealed class SerializationRootAttribute : Attribute
	{
	}


	// TODO: Document attributes


	// TODO: Make this work for auto-properties?

	// TODO: This attribute is not yet used:
	///// <summary>Apply to types that can be serialized as their System.Type</summary>
	//[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface
	//        | AttributeTargets.Delegate | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
	//public sealed class SerializedAsTypeAttribute : Attribute
	//{
	//}
}