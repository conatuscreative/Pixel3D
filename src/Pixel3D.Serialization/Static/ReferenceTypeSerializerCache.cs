// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.Serialization.Static
{
	public static class ReferenceTypeSerializerCache<T> where T : class
	{
		public static readonly ReferenceTypeSerializeMethod<T> Serialize =
			Serializer.StaticMethodLookup.GetReferenceTypeSerializeDelegate<T>();
	}
}