// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Pixel3D.Serialization.Context;
using Pixel3D.Serialization.MethodProviders;

namespace Pixel3D.Serialization.Discovery
{
	internal static class CustomMethodDiscovery
	{
		public static SerializationMethodProviders Run(IEnumerable<Assembly> assemblies, StreamWriter report,
			StreamWriter errors)
		{
			var customValueTypeSerializeMethods = new LookupMethodProvider();
			var customValueTypeDeserializeMethods = new LookupMethodProvider();
			var customReferenceTypeSerializeMethods = new LookupMethodProvider();
			var customReferenceTypeDeserializeMethods = new LookupMethodProvider();
			var customReferenceFieldSerializeMethods = new LookupMethodProvider();
			var customReferenceFieldDeserializeMethods = new LookupMethodProvider();
			var customReferenceTypeInitializerMethods = new LookupMethodProvider();

			var bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic |
			                   BindingFlags.Static;
			if (errors != null)
				bindingFlags |=
					BindingFlags
						.Instance; // If we're reporting errors, also check instance methods to report them as errors (otherwise skip them for speed)

			var allTypes = assemblies.SelectMany(a => a.GetTypes());
			var allMethods = allTypes.SelectMany(t => t.GetMethods(bindingFlags));

			var customSerializerCandidates = allMethods.Where(m =>
				m.GetCustomAttributes(typeof(CustomSerializerAttribute), false).Length > 0);
			var customInitializerCandidates = allMethods.Where(m =>
				m.GetCustomAttributes(typeof(CustomInitializerAttribute), false).Length > 0);
			var customFieldSerializerCandidates = allMethods.Where(m =>
				m.GetCustomAttributes(typeof(CustomFieldSerializerAttribute), false).Length > 0);

			#region Handle [CustomSerializer]

			var customSerializerParameters = new[] { typeof(SerializeContext), typeof(BinaryWriter), null };
			var customDeserializerParameters = new[] { typeof(DeserializeContext), typeof(BinaryReader), null };

			foreach (var method in customSerializerCandidates)
			{
				var serialize = MatchStaticSignature(method, typeof(void), customSerializerParameters);
				var deserialize = MatchStaticSignature(method, typeof(void), customDeserializerParameters);

				if (!serialize && !deserialize)
				{
					CustomSerializerSignatureError(errors, method);
					continue;
				}

				var type = method.GetParameters()[2].ParameterType;

				if (!ValidateGenericTypeUsage(method, type, errors, "[CustomSerializer]"))
					continue;

				var originalByRef = type.IsByRef;
				if (originalByRef)
					type = type.GetElementType(); // strip off byref

				FixupGenericTypes(ref type);

				if (!(type.IsValueType == originalByRef))
				{
					CustomSerializerSignatureError(errors, method);
					continue;
				}

				if (type.IsInterface)
				{
					if (errors != null)
					{
						errors.WriteLine("ERROR: [CustomSerializer] method is not valid for interfaces: " +
						                 FormatMethodName(method));
						errors.WriteLine("  " + method);
						errors.WriteLine();
					}

					continue;
				}

				LookupMethodProvider methodTable;
				if (type.IsValueType)
					methodTable = serialize ? customValueTypeSerializeMethods : customValueTypeDeserializeMethods;
				else
					methodTable = serialize
						? customReferenceTypeSerializeMethods
						: customReferenceTypeDeserializeMethods;

				if (methodTable.lookup.ContainsKey(type))
				{
					if (errors != null)
					{
						errors.WriteLine("ERROR: Found multiple [CustomSerializer] methods for type " + type);
						errors.WriteLine("  Found: " + FormatMethodName(method));
						errors.WriteLine("  Conflicts with: " + FormatMethodName(methodTable.lookup[type]));
						errors.WriteLine();
					}

					continue;
				}

				methodTable.Add(type, method);
			}

			#endregion

			#region Handle [CustomInitializer]

			foreach (var method in customInitializerCandidates)
			{
				if (!MatchStaticSignature(method, null, Type.EmptyTypes))
				{
					if (errors != null)
					{
						errors.WriteLine("ERROR: [CustomInitializer] invalid signature: " + FormatMethodName(method));
						errors.WriteLine("  Example:");
						errors.WriteLine("  static Class Initialize();");
						errors.WriteLine();
					}

					continue;
				}

				var type = method.ReturnType;

				if (!ValidateGenericTypeUsage(method, type, errors, "[CustomInitializer]"))
					continue;

				if (type.IsValueType)
				{
					if (errors != null)
					{
						errors.WriteLine("ERROR: [CustomInitializer] is not possible for value types: " +
						                 FormatMethodName(method));
						errors.WriteLine();
					}

					continue;
				}

				if (type.IsInterface)
				{
					if (errors != null)
					{
						errors.WriteLine("ERROR: [CustomInitializer] method is not valid for interfaces: " +
						                 FormatMethodName(method));
						errors.WriteLine();
					}

					continue;
				}

				FixupGenericTypes(ref type);
				customReferenceTypeInitializerMethods.Add(type, method);
			}

			#endregion

			#region Handle [CustomFieldSerializer]

			foreach (var method in customFieldSerializerCandidates)
			{
				var serialize = MatchStaticSignature(method, typeof(void), customSerializerParameters);
				var deserialize = MatchStaticSignature(method, typeof(void), customDeserializerParameters);

				if (!serialize && !deserialize)
				{
					CustomFieldSerializerSignatureError(errors, method);
					continue;
				}

				var type = method.GetParameters()[2].ParameterType;

				if (!ValidateGenericTypeUsage(method, type, errors, "[CustomFieldSerializer]"))
					continue;

				if (serialize && type.IsByRef || deserialize && !type.IsByRef)
				{
					CustomFieldSerializerSignatureError(errors, method);
					continue;
				}

				if (type.IsByRef)
					type = type.GetElementType(); // Strip off byref
				FixupGenericTypes(ref type);

				if (type.IsValueType)
				{
					if (errors != null)
					{
						errors.WriteLine(
							"ERROR: [CustomFieldSerializer] cannot be applied to value types (Use a [CustomSerializer] instead): " +
							FormatMethodName(method));
						errors.WriteLine("  " + method);
						errors.WriteLine();
					}

					continue;
				}


				var methodTable =
					serialize ? customReferenceFieldSerializeMethods : customReferenceFieldDeserializeMethods;

				if (methodTable.lookup.ContainsKey(type))
				{
					if (errors != null)
					{
						errors.WriteLine("ERROR: Found multiple [CustomFieldSerializer] methods for type " + type);
						errors.WriteLine("  Found: " + FormatMethodName(method));
						errors.WriteLine("  Conflicts with: " + FormatMethodName(methodTable.lookup[type]));
						errors.WriteLine();
					}

					continue;
				}

				methodTable.Add(type, method);
			}

			#endregion

			#region Checking and Reporting

			DoMatchCheck(customValueTypeSerializeMethods.lookup, customValueTypeDeserializeMethods.lookup, errors);
			DoMatchCheck(customReferenceTypeSerializeMethods.lookup, customReferenceTypeDeserializeMethods.lookup,
				errors);
			DoMatchCheck(customReferenceFieldSerializeMethods.lookup, customReferenceFieldDeserializeMethods.lookup,
				errors);

			if (report != null)
			{
				DoReport(report, "Value Types", customValueTypeSerializeMethods.lookup,
					customValueTypeDeserializeMethods.lookup);
				DoReport(report, "Reference Types", customReferenceTypeSerializeMethods.lookup,
					customReferenceTypeDeserializeMethods.lookup, customReferenceTypeInitializerMethods.lookup);
				DoReport(report, "Reference Fields", customReferenceFieldSerializeMethods.lookup,
					customReferenceFieldDeserializeMethods.lookup);
			}

			#endregion

			return new SerializationMethodProviders(
				customValueTypeSerializeMethods,
				customValueTypeDeserializeMethods,
				customReferenceTypeSerializeMethods,
				customReferenceTypeDeserializeMethods,
				customReferenceFieldSerializeMethods,
				customReferenceFieldDeserializeMethods,
				customReferenceTypeInitializerMethods);
		}

		private static void FixupGenericTypes(ref Type type)
		{
			// Generic parameter types are different to their "real" type!
			// For example, the first parameter in:
			//   void Foo<T>(List<T>)
			// Is *not* the same type as:
			//   List<T>

			Debug.Assert(!type.IsByRef);

			if (type.ContainsGenericParameters)
			{
				type = type.Assembly.GetType(type.Namespace + "." + type.Name);
				Debug.Assert(type != null);
			}
		}

		private static void CustomSerializerSignatureError(StreamWriter errors, MethodInfo method)
		{
			if (errors != null)
			{
				errors.WriteLine("ERROR: [CustomSerializer] has invalid signature: " + FormatMethodName(method));
				errors.WriteLine("  " + method);
				errors.WriteLine("  Valid Examples:");
				errors.WriteLine("  static void Serialize(SerializeContext, BinaryWriter, Class);");
				errors.WriteLine("  static void Serialize(SerializeContext, BinaryWriter, ref Struct);");
				errors.WriteLine("  static void Deserialize(DeserializeContext, BinaryReader, Class);");
				errors.WriteLine("  static void Deserialize(DeserializeContext, BinaryReader, ref Struct);");
				errors.WriteLine();
			}
		}

		private static void CustomFieldSerializerSignatureError(StreamWriter errors, MethodInfo method)
		{
			if (errors != null)
			{
				errors.WriteLine("ERROR: [CustomFieldSerializer] has invalid signature: " + FormatMethodName(method));
				errors.WriteLine("  " + method);
				errors.WriteLine("  Valid Examples:");
				errors.WriteLine("  static void SerializeField(SerializeContext, BinaryWriter, Class);");
				errors.WriteLine("  static void DeserializeField(DeserializeContext, BinaryReader, ref Class);");
				errors.WriteLine();
			}
		}

		private static string FormatMethodName(MethodInfo method)
		{
			return method.DeclaringType + "." + method.Name;
		}

		private static bool ValidateGenericTypeUsage(MethodInfo method, Type subjectType, StreamWriter errors,
			string source)
		{
			if (subjectType.ContainsGenericParameters &&
			    (subjectType.IsArray || subjectType.IsByRef && subjectType.GetElementType().IsArray))
			{
				if (errors != null)
				{
					errors.WriteLine("ERROR: " + source + " generic array type not supported in: " +
					                 FormatMethodName(method));
					errors.WriteLine("  " + method);
					errors.WriteLine();
				}

				return false;
			}

			if (method.ContainsGenericParameters && (!subjectType.ContainsGenericParameters ||
			                                         !method.GetGenericArguments()
				                                         .SequenceEqual(subjectType.GetGenericArguments())))
			{
				if (errors != null)
				{
					errors.WriteLine("ERROR: " + source +
					                 " method's generic arguments do not match the subject's generic arguments: " +
					                 FormatMethodName(method));
					errors.WriteLine("  " + method);
					errors.WriteLine();
				}

				return false;
			}

			return true;
		}

		private static bool MatchStaticSignature(MethodInfo method, Type requestedReturnType,
			Type[] requestedParameterTypes)
		{
			if (!method.IsStatic)
				return false; // Not static

			if (requestedReturnType != null && method.ReturnType != requestedReturnType)
				return false; // Bad return type

			if (requestedParameterTypes != null)
			{
				var parameters = method.GetParameters();
				if (requestedParameterTypes.Length != parameters.Length)
					return false; // Bad parameter length

				for (var i = 0; i < requestedParameterTypes.Length; i++)
					if (requestedParameterTypes[i] != null && parameters[i].ParameterType != requestedParameterTypes[i])
						return false; // Bad parameter
			}

			return true; // Success
		}

		private static void DoMatchCheck(Dictionary<Type, MethodInfo> customSerializeMethods,
			Dictionary<Type, MethodInfo> customDeserializeMethods, StreamWriter errors)
		{
			var notMatchingTypes = new List<Type>();
			foreach (var customSerialize in customSerializeMethods)
				if (!customDeserializeMethods.ContainsKey(customSerialize.Key))
				{
					notMatchingTypes.Add(customSerialize.Key);
					if (errors != null)
					{
						errors.WriteLine("ERROR: Missing matching custom deserializer for type " + customSerialize.Key);
						errors.WriteLine("  Serializer is: " + FormatMethodName(customSerialize.Value));
						errors.WriteLine();
					}
				}

			foreach (var type in notMatchingTypes)
				customSerializeMethods.Remove(type);


			notMatchingTypes.Clear();
			foreach (var customDeserialize in customDeserializeMethods)
				if (!customSerializeMethods.ContainsKey(customDeserialize.Key))
				{
					notMatchingTypes.Add(customDeserialize.Key);
					if (errors != null)
					{
						errors.WriteLine("ERROR: Missing matching custom serializer for type " + customDeserialize.Key);
						errors.WriteLine("  Deserializer is: " + FormatMethodName(customDeserialize.Value));
						errors.WriteLine();
					}
				}

			foreach (var type in notMatchingTypes)
				customDeserializeMethods.Remove(type);
		}

		private static void DoReport(StreamWriter report, string title,
			Dictionary<Type, MethodInfo> customSerializeMethods,
			Dictionary<Type, MethodInfo> customDeserializeMethods,
			Dictionary<Type, MethodInfo> customInitializerMethods = null)
		{
			report.WriteLine("----------------------------");
			report.WriteLine(title);
			report.WriteLine("----------------------------");
			report.WriteLine();

			var allCustomTypes = new HashSet<Type>();

			foreach (var type in customSerializeMethods.Keys)
				allCustomTypes.Add(type);
			foreach (var type in customDeserializeMethods.Keys)
				allCustomTypes.Add(type);

			if (customInitializerMethods != null)
				foreach (var type in customInitializerMethods.Keys)
					allCustomTypes.Add(type);

			foreach (var type in allCustomTypes)
			{
				report.WriteLine(type.ToString());
				if (customSerializeMethods.ContainsKey(type))
					report.WriteLine("  Serialize   = " + FormatMethodName(customSerializeMethods[type]));
				if (customDeserializeMethods.ContainsKey(type))
					report.WriteLine("  Deserialize = " + FormatMethodName(customDeserializeMethods[type]));
				if (customInitializerMethods != null && customInitializerMethods.ContainsKey(type))
					report.WriteLine("  Initialize  = " + FormatMethodName(customInitializerMethods[type]));
				report.WriteLine();
			}
		}


		/// <summary>Any types derived from a type with a custom initializer must itself have a custom initialzier.</summary>
		public static void CheckForDerivedCustomInitializers(MethodProvider initializerMethods,
			HashSet<Type> referenceTypes, StreamWriter errors)
		{
			foreach (var type in referenceTypes)
			{
				Debug.Assert(!type.IsValueType);
				Debug.Assert(!type.IsInterface);

				if (initializerMethods.GetMethodForType(type) != null)
					continue; // Type has a custom initializer

				var baseType = type.BaseType;
				while (baseType != null && baseType != typeof(object))
				{
					if (initializerMethods.GetMethodForType(baseType) != null)
					{
						errors.WriteLine("ERROR: Type " + type + " is drived from type " + baseType +
						                 " with a custom initializer!");
						errors.WriteLine("  Add a [CustomInitializer] for " + type);
						errors.WriteLine();
						goto nextType;
					}

					baseType = baseType.BaseType; // <- walk up inheritance hierarchy
				}

				nextType: ;
			}
		}
	}
}