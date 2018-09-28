// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Pixel3D.Serialization.Context;
using Pixel3D.Serialization.MethodProviders;

namespace Pixel3D.Serialization
{
	internal class SerializationMethodProviders
	{
		#region Method Providers

        public MethodProvider ValueTypeSerializeMethods { get; private set; }
        public MethodProvider ValueTypeDeserializeMethods { get; private set; }
        public MethodProvider ReferenceTypeSerializeMethods { get; private set; }
        public MethodProvider ReferenceTypeDeserializeMethods { get; private set; }
        public MethodProvider ReferenceFieldSerializeMethods { get; private set; }
        public MethodProvider ReferenceFieldDeserializeMethods { get; private set; }
        public MethodProvider ReferenceTypeInitializeMethods { get; private set; }


		public SerializationMethodProviders(
			MethodProvider valueTypeSerializeMethods,
			MethodProvider valueTypeDeserializeMethods,
			MethodProvider referenceTypeSerializeMethods,
			MethodProvider referenceTypeDeserializeMethods,
			MethodProvider referenceFieldSerializeMethods,
			MethodProvider referenceFieldDeserializeMethods,
			MethodProvider referenceTypeInitializeMethods)
		{
			ValueTypeSerializeMethods = valueTypeSerializeMethods;
			ValueTypeDeserializeMethods = valueTypeDeserializeMethods;
			ReferenceTypeSerializeMethods = referenceTypeSerializeMethods;
			ReferenceTypeDeserializeMethods = referenceTypeDeserializeMethods;
			ReferenceFieldSerializeMethods = referenceFieldSerializeMethods;
			ReferenceFieldDeserializeMethods = referenceFieldDeserializeMethods;
			ReferenceTypeInitializeMethods = referenceTypeInitializeMethods;
		}


		public static SerializationMethodProviders Combine(SerializationMethodProviders first,
			SerializationMethodProviders second)
		{
			return new SerializationMethodProviders(
				FallbackMethodProvider.Combine(first.ValueTypeSerializeMethods, second.ValueTypeSerializeMethods),
				FallbackMethodProvider.Combine(first.ValueTypeDeserializeMethods, second.ValueTypeDeserializeMethods),
				FallbackMethodProvider.Combine(first.ReferenceTypeSerializeMethods,
					second.ReferenceTypeSerializeMethods),
				FallbackMethodProvider.Combine(first.ReferenceTypeDeserializeMethods,
					second.ReferenceTypeDeserializeMethods),
				FallbackMethodProvider.Combine(first.ReferenceFieldSerializeMethods,
					second.ReferenceFieldSerializeMethods),
				FallbackMethodProvider.Combine(first.ReferenceFieldDeserializeMethods,
					second.ReferenceFieldDeserializeMethods),
				FallbackMethodProvider.Combine(first.ReferenceTypeInitializeMethods,
					second.ReferenceTypeInitializeMethods));
		}

		#endregion

		#region Delegate Creation

		/// <summary>Helper method that can create a delegate from both RuntimeMethodInfo and DynamicMethod</summary>
		private static Delegate CreateDelegate(Type delegateType, MethodInfo method)
		{
			Debug.Assert(!(method is MethodBuilder)); // Check that nothing is sneaking past

			var dynamicMethod = method as DynamicMethod;
			if (dynamicMethod != null)
				return dynamicMethod.CreateDelegate(delegateType);

			return Delegate.CreateDelegate(delegateType, method);
		}


		public ReferenceTypeSerializeMethod<T> GetReferenceTypeSerializeDelegate<T>() where T : class
		{
			return (ReferenceTypeSerializeMethod<T>) CreateDelegate(typeof(ReferenceTypeSerializeMethod<T>),
				ReferenceTypeSerializeMethods.GetMethodForType(typeof(T)));
		}

		public ReferenceTypeDeserializeMethod<T> GetReferenceTypeDeserializeDelegate<T>() where T : class
		{
			return (ReferenceTypeDeserializeMethod<T>) CreateDelegate(typeof(ReferenceTypeDeserializeMethod<T>),
				ReferenceTypeDeserializeMethods.GetMethodForType(typeof(T)));
		}


		// Field serializers are always "byref" so they will always have the same signature, taking a "ref T", for both reference and value types.
		//   Value-type serializers continue to work as-is as field serializers (always byref)
		//   Reference field deserialize is already taking a reference type byref
		//   Reference field serialize just takes a reference, so must be wrapped so the delegate can take a reference byref

		public FieldSerializeMethod<T> GetFieldSerializeDelegate<T>()
		{
			if (typeof(T).IsValueType)
			{
				var method = ValueTypeSerializeMethods.GetMethodForType(typeof(T));
				Debug.Assert(method != null);
				return (FieldSerializeMethod<T>) CreateDelegate(typeof(FieldSerializeMethod<T>), method);
			}
			else
			{
				var method = ReferenceFieldSerializeMethods.GetMethodForType(typeof(T));
				Debug.Assert(method != null);

				// Normal reference field serializers don't take a 'byref' parameter, so we must create a wrapper so the external signature matches:
				var serializeField =
					(Action<SerializeContext, BinaryWriter, T>) CreateDelegate(
						typeof(Action<SerializeContext, BinaryWriter, T>), method);

				FieldSerializeMethod<T> wrapper = (SerializeContext context, BinaryWriter bw, ref T obj) =>
					serializeField(context, bw, obj);
				return wrapper;
			}
		}

		public FieldDeserializeMethod<T> GetFieldDeserializeDelegate<T>()
		{
			if (typeof(T).IsValueType)
			{
				var method = ValueTypeDeserializeMethods.GetMethodForType(typeof(T));
				Debug.Assert(method != null);
				return (FieldDeserializeMethod<T>) CreateDelegate(typeof(FieldDeserializeMethod<T>), method);
			}
			else
			{
				var method = ReferenceFieldDeserializeMethods.GetMethodForType(typeof(T));
				Debug.Assert(method != null);
				return (FieldDeserializeMethod<T>) CreateDelegate(typeof(FieldDeserializeMethod<T>), method);
			}
		}

		#endregion

		#region Queries

		public bool HasTypeSerializer(Type type)
		{
			if (type.IsValueType)
			{
				var hasSerializeMethod = ValueTypeSerializeMethods.GetMethodForType(type) != null;
				Debug.Assert(hasSerializeMethod ==
				             (ValueTypeDeserializeMethods.GetMethodForType(type) !=
				              null)); // <- debug check for matching deserializer
				return hasSerializeMethod;
			}
			else
			{
				var hasSerializeMethod = ReferenceTypeSerializeMethods.GetMethodForType(type) != null;
				Debug.Assert(hasSerializeMethod ==
				             (ReferenceTypeDeserializeMethods.GetMethodForType(type) !=
				              null)); // <- debug check for matching deserializer
				return hasSerializeMethod;
			}
		}

		public bool HasFieldSerializer(Type type)
		{
			if (type.IsValueType)
			{
				var hasSerializeMethod = ValueTypeSerializeMethods.GetMethodForType(type) != null;
				Debug.Assert(hasSerializeMethod ==
				             (ValueTypeDeserializeMethods.GetMethodForType(type) !=
				              null)); // <- debug check for matching deserializer
				return hasSerializeMethod;
			}
			else
			{
				var hasSerializeMethod = ReferenceFieldSerializeMethods.GetMethodForType(type) != null;
				Debug.Assert(hasSerializeMethod ==
				             (ReferenceFieldDeserializeMethods.GetMethodForType(type) !=
				              null)); // <- debug check for matching deserializer
				return hasSerializeMethod;
			}
		}

		#endregion
	}
}