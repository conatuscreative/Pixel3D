// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Serialization.Static
{
	// These classes offload the caching of serialization methods onto the CLR
	// (They will be filled when the CLR JITs any method that references them)
	// 
	// IMPORTANT: Do not add static constructors to these types, as this will remove their "beforefieldinit" flag
	//            (which, in turn, will cause an initialization check to occur every time they are accessed)

	public static class FieldSerializerCache<T>
	{
		public static readonly FieldSerializeMethod<T> Serialize =
			Serializer.StaticMethodLookup.GetFieldSerializeDelegate<T>();
	}
}