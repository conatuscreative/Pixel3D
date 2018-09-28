// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.Reflection;
using Pixel3D.Serialization.BuiltIn.DelegateHandling;

namespace Pixel3D.Serialization.Generator
{
	public class GeneratorResult
	{
		internal readonly DeserializeDispatchDelegate deserializeDispatch;

		internal readonly SerializationMethodProviders serializationMethods;

		internal readonly Dictionary<Type, SerializeDispatchDelegate> serializeDispatchTable;

		internal Dictionary<Type, DelegateTypeInfo> delegateTypeTable;

		// Valid modules for serializing System.Type
		internal List<Module> moduleTable;

		internal GeneratorResult(SerializationMethodProviders serializationMethods,
			Dictionary<Type, SerializeDispatchDelegate> dispatchTable,
			DeserializeDispatchDelegate deserializeDispatch,
			List<Module> moduleTable)
		{
			this.serializationMethods = serializationMethods;
			serializeDispatchTable = dispatchTable;
			this.deserializeDispatch = deserializeDispatch;
			this.moduleTable = moduleTable;
		}
	}
}