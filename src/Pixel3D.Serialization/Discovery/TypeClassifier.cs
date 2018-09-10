// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pixel3D.Serialization.Discovery
{
	internal class TypeClassifier
	{
		// May as well use a shared dispatcher (the runtime can do our type-checks)
		public HashSet<Type> dispatchableTypes = new HashSet<Type>();


		// Find what types a given field serializer could dispatch to (if it's just one, we can dispatch directly)
		public Dictionary<Type, HashSet<Type>> fieldSerializerDispatchLookup = new Dictionary<Type, HashSet<Type>>();
		private readonly TypeDiscovery typeDiscovery;

		public TypeClassifier(TypeDiscovery typeDiscovery)
		{
			this.typeDiscovery = typeDiscovery;
		}


		public HashSet<Type> ValueTypes => typeDiscovery.valueTypes;
		public HashSet<Type> ReferenceTypes => typeDiscovery.referenceTypes;


		public void RunClassification()
		{
			foreach (var fieldType in typeDiscovery.referenceFieldTypes)
			{
				var dispatchList = new HashSet<Type>(typeDiscovery.referenceTypes
					.Where(type => !type.IsAbstract) // <- no point in dispatching to a type that cannot be created!
					.Where(type => fieldType.IsAssignableFrom(type)));

				fieldSerializerDispatchLookup[fieldType] = dispatchList;
			}

			foreach (var fieldDispatch in fieldSerializerDispatchLookup)
				if (fieldDispatch.Value.Count > 1
				) // More than one type that we can dispatch to (otherwise will do direct dispatch)
					foreach (var type in fieldDispatch.Value) // Need to include all those types in the dispatcher
						dispatchableTypes.Add(type);
		}


		public void WriteReport(StreamWriter report, StreamWriter errors)
		{
			report.WriteLine("Value Types (" + ValueTypes.Count + ")");
			foreach (var type in ValueTypes)
				report.WriteLine("  " + type);
			report.WriteLine();

			report.WriteLine("Reference Types (" + ReferenceTypes.Count + ")");
			foreach (var type in ReferenceTypes)
				report.WriteLine("  " + type);
			report.WriteLine();


			var noDispatch = fieldSerializerDispatchLookup.Where(d => d.Value.Count == 0).Select(d => d.Key);
			report.WriteLine("Reference Field Types with No Dispatch (" + noDispatch.Count() + ")");
			foreach (var type in noDispatch)
			{
				report.WriteLine("  " + type);

				// Also an error:
				errors.WriteLine("ERROR: No non-abstract types assignable to field type " + type);
				if (type.IsInterface)
					errors.WriteLine("  Fields will be skipped during serialization, and cannot be deserialized");
				else
					errors.WriteLine("  Fields will be serialized directly as " + type +
					                 " (even if that is not the true type), and cannot be deserialized");
				errors.WriteLine();
			}

			report.WriteLine();


			var directDispatch = fieldSerializerDispatchLookup.Where(d => d.Value.Count == 1).Select(d => d.Key);
			report.WriteLine("Reference Field Types with Direct Dispatch (" + directDispatch.Count() + ")");
			foreach (var type in directDispatch)
				report.WriteLine("  " + type);
			report.WriteLine();

			var dynamicDispatch = fieldSerializerDispatchLookup.Where(d => d.Value.Count > 1).Select(d => d.Key);
			report.WriteLine("Reference Field Types with Dynamic Dispatch (" + dynamicDispatch.Count() + ")");
			foreach (var type in dynamicDispatch)
				report.WriteLine("  " + type);
			report.WriteLine();

			var interfaceFields = fieldSerializerDispatchLookup.Keys.Where(t => t.IsInterface);
			report.WriteLine("Interface Fields Types (" + interfaceFields.Count() + ")");
			foreach (var type in interfaceFields)
				report.WriteLine("  " + type);
			report.WriteLine();

			report.WriteLine("Dynamic Dispatch Target Types (" + dispatchableTypes.Count + ")");
			foreach (var type in dispatchableTypes)
				report.WriteLine("  " + type);
			report.WriteLine();

			report.WriteLine("Delegate Field Types (" + typeDiscovery.delegateFieldTypes.Count + ")");
			foreach (var type in typeDiscovery.delegateFieldTypes)
				report.WriteLine("  " + type);
			report.WriteLine();
		}
	}
}