// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.BuiltIn
{
	internal static class SerializeString
	{
		[CustomFieldSerializer]
		public static void SerializeField(SerializeContext context, BinaryWriter bw, string value)
		{
			if (context.WalkString(value) == null) // null check
				return;

			context.VisitObject(value);
			bw.Write(value);
			context.LeaveObject();
		}

		[CustomFieldSerializer]
		public static void DeserializeField(DeserializeContext context, BinaryReader br, ref string value)
		{
			if (!context.Walk(ref value))
				return;

			value = br.ReadString(); // TODO: one day we might like to not allocate here.
			context.VisitObject(value);
		}
	}
}