// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.IO;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;
using Pixel3D.StateManagement;

namespace Pixel3D.BuiltIn
{
	public static class SerializeIgnoreStateManagement
	{
		// Don't want to serialize into the "State" object (mostly during type discovery)
		// because it will always be a definition object that is manually created using
		// "AllStateInstances". Instead, just link up the reference by calling "Walk".

		[CustomFieldSerializer]
		public static void SerializeField(SerializeContext context, BinaryWriter bw, StateProvider.State value)
		{
			context.Walk(value);
		}

		[CustomFieldSerializer]
		public static void DeserializeField(DeserializeContext context, BinaryReader br, ref StateProvider.State value)
		{
			context.Walk(ref value);
		}
	}
}
