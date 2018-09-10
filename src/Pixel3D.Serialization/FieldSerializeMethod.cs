// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization
{
	// Field methods that work for both value and reference types (so they can be used successfully in generics)

	public delegate void FieldSerializeMethod<T>(SerializeContext context, BinaryWriter bw, ref T obj);

	// These are the actual method signatures used by the serializer:
}