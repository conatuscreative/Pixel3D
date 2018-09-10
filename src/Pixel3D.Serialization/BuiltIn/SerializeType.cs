// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.IO;
using Pixel3D.Serialization.Context;
using Pixel3D.Serialization.Static;

namespace Pixel3D.Serialization.BuiltIn
{
	internal static class SerializeType
	{
		// NOTE: This only support serializing basic types (not arrays, closed constructed generic types, etc)
		//       and only supports types in the module table (filled at serializer generation time)

		[CustomFieldSerializer]
		public static void SerializeField(SerializeContext context, BinaryWriter bw, Type type)
		{
			if (type == null)
			{
				bw.Write(-1);
				return;
			}

			int moduleId;
			if (!StaticModuleTable.moduleToId.TryGetValue(type.Module, out moduleId))
				throw new InvalidOperationException("Module not available for type " + type);

			bw.Write(moduleId);
			bw.Write(type.MetadataToken);
		}

		[CustomFieldSerializer]
		public static void DeserializeField(DeserializeContext context, BinaryReader br, ref Type type)
		{
			var moduleId = br.ReadInt32();
			if (moduleId == -1)
			{
				type = null;
				return;
			}

			var module = StaticModuleTable.idToModule[moduleId];

			var typeId = br.ReadInt32();
			type = module.ResolveType(typeId);
			if (type == null)
				throw new InvalidOperationException("Failed to find type " + typeId + " in module " + moduleId);
		}
	}
}