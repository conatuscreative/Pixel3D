// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization
{
	public delegate void FieldDeserializeMethod<T>(DeserializeContext context, BinaryReader br, ref T obj);
}