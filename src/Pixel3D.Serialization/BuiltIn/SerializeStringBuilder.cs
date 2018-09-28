// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.IO;
using System.Text;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.BuiltIn
{
	internal class SerializeStringBuilder
	{
		[CustomFieldSerializer]
		public static void SerializeField(SerializeContext context, BinaryWriter bw, StringBuilder stringBuilder)
		{
			if (!context.Walk(stringBuilder)) // null check
				return;

			context.VisitObject(stringBuilder);

			bw.WriteSmallInt32(stringBuilder.Length);
			for (var i = 0; i < stringBuilder.Length; i++)
				bw.Write(stringBuilder[i]);

			context.LeaveObject();
		}

		[CustomFieldSerializer]
		public static void DeserializeField(DeserializeContext context, BinaryReader br,
			ref StringBuilder stringBuilder)
		{
			if (!context.Walk(ref stringBuilder))
				return;

			var length = br.ReadSmallInt32();
			stringBuilder = new StringBuilder(length);
			stringBuilder.Length = length;
			context.VisitObject(stringBuilder);
			for (var i = 0; i < length; i++)
				stringBuilder[i] = br.ReadChar();
		}
	}
}