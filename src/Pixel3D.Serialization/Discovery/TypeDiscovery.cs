// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Pixel3D.Serialization.Discovery
{
	internal class TypeDiscovery
	{
		public const BindingFlags allInstanceDeclared =
			BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;


		// Fields of these types will always be set to null
		private static readonly Type[] ignoredTypeList =
		{
			typeof(object), // <- Causes type-explosion
			typeof(Stopwatch), // <- Farseer has some of these as fields, which can be ignored
			typeof(Delegate),
			typeof(MulticastDelegate),
			typeof(IntPtr), // <---- Architecture issues and likelyhood of pointing at runtime structures
			typeof(UIntPtr) // <--'
		};

		private readonly HashSet<Assembly> _assemblies = new HashSet<Assembly>();

		private readonly List<Type> allPotentialDerivedTypes = new List<Type>(2048);


		private readonly SerializationMethodProviders customMethods;

		// Fields that can store delegates:
		public HashSet<Type> delegateFieldTypes = new HashSet<Type>();

		// For field types, check for any derived types that can be stored in that field
		private readonly Queue<Type> pendingDerivedClassSearches = new Queue<Type>();


		// Do a breadth-first search, because that gives more readable output
		private readonly Queue<Type> pendingTypes = new Queue<Type>();

		// Field types for fields that can store references:
		public HashSet<Type> referenceFieldTypes = new HashSet<Type>();

		// Reference types to generate a serializer for:
		public HashSet<Type> referenceTypes = new HashSet<Type>();


		// Value types to generate a serializer for:
		public HashSet<Type> valueTypes = new HashSet<Type>();

		public TypeDiscovery(SerializationMethodProviders customMethods, IEnumerable<Assembly> initialAssemblies)
		{
			this.customMethods = customMethods;
			foreach (var a in initialAssemblies)
				AddAssembly(a); // Pre-fill with initial assemblies
		}

	    public IEnumerable<Assembly> Assemblies
	    {
	        get { return _assemblies; }
	    }


	    public bool FoundDelegates { get; private set; }

		private void AddAssembly(Assembly assembly)
		{
			if (_assemblies.Add(assembly))
				foreach (var type in assembly.GetTypes())
				{
					if (type.IsValueType) // No inheritance from value types
						continue;
					if (type.IsAbstract) // Cannot be instanced (interfaces are also abstract)
						continue;

					allPotentialDerivedTypes.Add(type);
				}
		}

		public static bool IsIgnoredType(Type type)
		{
			return ignoredTypeList.Contains(type);
		}


		private void FoundType(Type type, bool fromField, HashSet<Type> foundTypes, Context context)
		{
			Debug.Assert(type != null);

			if (type.IsPrimitive)
				return; // Gets handled normally

			if (IsIgnoredType(type))
			{
				context.WriteError((fromField ? "WARNING" : "ERROR") + ": Serializer ignores type " + type +
				                   (fromField ? " (field will be cleared on deserialize)" : ""));
				return;
			}

			if (type.BaseType == typeof(MulticastDelegate))
			{
				if (type.GetCustomAttributes(typeof(SerializableDelegateAttribute), true).Length == 0)
				{
					context.WriteError("ERROR: Ignoring delegate who's type is not marked [SerializableDelegate]: " +
					                   type);
					return;
				}

				FoundDelegates = true;
			}

			if (type.IsPointer || type.IsByRef)
			{
				context.WriteError("ERROR: Serializer cannot handle pointers: " + type);
				return;
			}

			if (type.ContainsGenericParameters)
			{
				context.WriteError("ERROR: Serializer cannot handle open-constructed type: " + type);
				return;
			}

			if (type.IsArray)
			{
				// Unwrap array types (in the generator arrays are passed through custom methods)
				// (Note: before we check for custom methods, which will reject arrays, due to afforementioned custom handler)
				FoundType(type.GetElementType(), fromField, foundTypes, context);
				return;
			}

			if (fromField && customMethods.HasFieldSerializer(type))
			{
				// Don't walk into fields with custom serializers, but do assume that we will be serializing any generics
				// TODO: Add support for finding user-defined set of types from custom field serializer methods
				if (type.IsGenericType)
					foreach (var userDefinedFieldType in type.GetGenericArguments())
						FoundType(userDefinedFieldType, fromField, foundTypes, context);
				return;
			}

			if (foundTypes.Contains(type))
				return;


			// Types we discover need to be serialized (along with all their fields)
			if (!type.IsInterface && type.BaseType != typeof(MulticastDelegate))
			{
				var added = type.IsValueType ? valueTypes.Add(type) : referenceTypes.Add(type);
				if (added)
					pendingTypes.Enqueue(type);
			}

			// Fields we discover, that can store reference types, may point to a derived class
			if (fromField && !type.IsValueType && type.BaseType != typeof(MulticastDelegate))
				if (referenceFieldTypes.Add(type))
					pendingDerivedClassSearches.Enqueue(type);

			// Special handling for delegate fields:
			if (type.BaseType == typeof(MulticastDelegate))
			{
				Debug.Assert(fromField);
				delegateFieldTypes.Add(type);
			}


			foundTypes.Add(type);
		}


		// Calling this method is equivalent to generating a serializer for the given type
		private void ConsiderType(Type type, StreamWriter report, StreamWriter errors)
		{
			Debug.Assert(!type.ContainsGenericParameters);

			AddAssembly(type.Assembly);

			var typeHasCustomSerializer = customMethods.HasTypeSerializer(type);

			var foundBaseType = new HashSet<Type>();
			if (!type.IsValueType && !type.IsArray && type.BaseType != null && type.BaseType != typeof(object) &&
			    !typeHasCustomSerializer)
				FoundType(type.BaseType, false, foundBaseType, new Context(errors, "base type of", type));

			var foundFieldTypes = new HashSet<Type>();
			if (typeHasCustomSerializer)
			{
				// TODO: Add support for finding user-defined set of fields from custom serializer methods
				// At the moment, we just assume any generic parameters are used as fields:
				if (type.IsGenericType)
					foreach (var userDefinedFieldType in type.GetGenericArguments())
						FoundType(userDefinedFieldType, true, foundFieldTypes,
							new Context(errors, "type (with custom serializer)", type));
			}
			else
			{
				foreach (var fieldInfo in type.GetFields(allInstanceDeclared))
					if (fieldInfo.GetCustomAttributes(typeof(SerializationIgnoreAttribute), true).Length == 0 &&
					    fieldInfo.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
						FoundType(fieldInfo.FieldType, true, foundFieldTypes,
							new Context(errors, "type", type, "field", fieldInfo));
			}


			if (report != null)
			{
				report.WriteLine(type + (type.IsValueType ? " #" : string.Empty));
				foreach (var t in foundBaseType)
					report.WriteLine("+ " + t);
				foreach (var t in foundFieldTypes)
					report.WriteLine("  " + t + (t.IsValueType ? " #" : string.Empty));
				report.WriteLine();
			}
		}


		private void SearchForDerivedTypes(Type[] baseTypes, StreamWriter report, StreamWriter errors)
		{
			foreach (var baseType in baseTypes)
			{
				Debug.Assert(baseType != null);
				Debug.Assert(!baseType.IsValueType);
				Debug.Assert(!baseType.ContainsGenericParameters);
				Debug.Assert(baseType.BaseType != typeof(MulticastDelegate));
			}

			var foundDerivedTypes = new HashSet<Type>();

			foreach (var type in allPotentialDerivedTypes)
			foreach (var baseType in baseTypes)
			{
				if (type == baseType)
					continue; // Same type as the field type in question (which is handled by processing for the field type)

				if (baseType.IsAssignableFrom(type))
					FoundType(type, false, foundDerivedTypes, new Context(errors, "assignable to", baseType));
			}

			if (report != null && foundDerivedTypes.Count > 0)
			{
				foreach (var t in foundDerivedTypes)
					report.WriteLine("> " + t);
				report.WriteLine();
			}
		}


		public void DiscoverFromRoots(IEnumerable<Type> rootTypes, StreamWriter report, StreamWriter errors)
		{
			var foundInitialTypes = new HashSet<Type>();
			foreach (var type in rootTypes)
				// Assuming all root types are field types, because we'd like to be able to serialize them directly
				FoundType(type, true, foundInitialTypes, new Context(errors, "external", "root type"));

			if (report != null)
			{
				report.WriteLine("DISCOVERY ROOTS:");
				foreach (var t in foundInitialTypes)
					report.WriteLine("% " + t);
				report.WriteLine();
			}


			while (pendingTypes.Count > 0 || pendingDerivedClassSearches.Count > 0)
			{
				// Note: ConsiderType will fill out the assemblies list that SearchForDerivedTypes depends on.
				//       Pre-filling with all known assemblies would be more correct (or could re-search if
				//       more assemblies get added) ... but oh well.

				while (pendingTypes.Count > 0)
					ConsiderType(pendingTypes.Dequeue(), report, errors);

				if (pendingDerivedClassSearches.Count > 0)
					SearchForDerivedTypes(pendingDerivedClassSearches.ToArray(), report, errors);
				pendingDerivedClassSearches.Clear();
			}
		}


		// Context for error messages:
		private struct Context
		{
			public Context(StreamWriter errors, string contextName0, object context0, string contextName1 = null,
				object context1 = null)
			{
				this.errors = errors;
				this.contextName0 = contextName0;
				this.contextName1 = contextName1;
				this.context0 = context0;
				this.context1 = context1;
			}

			private readonly StreamWriter errors;

			public readonly string contextName0;
			public readonly string contextName1;
			public readonly object context0;
			public readonly object context1;

			public void WriteError(string message, bool warning = false)
			{
				if (errors == null)
					return;

				errors.WriteLine(message);
				if (contextName0 != null)
					errors.WriteLine("  " + contextName0 + ": " + context0);
				if (contextName1 != null)
					errors.WriteLine("  " + contextName1 + ": " + context1);
				errors.WriteLine();
			}
		}
	}
}