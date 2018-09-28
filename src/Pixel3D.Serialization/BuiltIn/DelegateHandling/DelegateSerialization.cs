// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Pixel3D.Serialization.Context;
using Pixel3D.Serialization.MethodProviders;
using Pixel3D.Serialization.Static;

namespace Pixel3D.Serialization.BuiltIn.DelegateHandling
{
	internal static class DelegateSerialization
	{
		// NOTE: Do *not* mark these as [CustomFieldSerializer]
		//       They are accessed directly by the serializer and generator (via DelegateFieldMethodProvider)


		public static void SerializeDelegateField<T>(SerializeContext context, BinaryWriter bw, T d)
			where T : class // MulticastDelegate constraint not allowed by C#
		{
			SerializeDelegateField(context, bw, d as MulticastDelegate, typeof(T));
		}

		public static void DeserializeDelegateField<T>(DeserializeContext context, BinaryReader br, ref T d)
			where T : class // MulticastDelegate constraint not allowed by C#
		{
			var mulitcastDelegate = d as MulticastDelegate;
			DeserializeDelegateField(context, br, ref mulitcastDelegate, typeof(T));
			d = mulitcastDelegate as T;
		}


		internal static void SerializeDelegateField(SerializeContext context, BinaryWriter bw, MulticastDelegate d,
			Type delegateType)
		{
			Debug.Assert(delegateType.BaseType == typeof(MulticastDelegate));

			if (d == null)
			{
				bw.Write(0);
				return;
			}

			var invocationList = d.GetInvocationListDirect();
			bw.WriteSmallInt32(invocationList.Count);

			foreach (var invocation in invocationList) SerializeDelegate(context, bw, invocation, delegateType);
		}

		internal static void DeserializeDelegateField(DeserializeContext context, BinaryReader br,
			ref MulticastDelegate d, Type delegateType)
		{
			Debug.Assert(delegateType.BaseType == typeof(MulticastDelegate));

			var multicastCount = br.ReadSmallInt32();

			if (multicastCount == 0)
			{
				d = null;
			}
			else if (multicastCount == 1)
			{
				Delegate singleDelegate = null;
				DeserializeDelegate(context, br, ref singleDelegate, delegateType);
				d = (MulticastDelegate) singleDelegate;
			}
			else
			{
				var multicastList = new Delegate[multicastCount]; // TODO: no allocation version
				for (var i = 0; i < multicastList.Length; i++)
					DeserializeDelegate(context, br, ref multicastList[i], delegateType);
				d = (MulticastDelegate) Delegate.Combine(multicastList);
			}
		}


		private static void SerializeDelegate(SerializeContext context, BinaryWriter bw, Delegate d, Type delegateType)
		{
			Debug.Assert(d != null);
			Debug.Assert(StaticDelegateTable.delegateTypeTable.ContainsKey(delegateType));

			var delegateTypeInfo = StaticDelegateTable.delegateTypeTable[delegateType];

			var methodId = delegateTypeInfo.GetIdForMethod(d.Method);
			bw.Write(methodId);
			var delegateMethodInfo = delegateTypeInfo.GetMethodInfoForId(methodId);

			if (delegateMethodInfo.canHaveTarget)
			{
				var target = d.Target;
				if (context.Walk(target))
					StaticDispatchTable.SerializationDispatcher(context, bw, target);
			}
			else
			{
				Debug.Assert(d.Target == null);
			}
		}


		private static void DeserializeDelegate(DeserializeContext context, BinaryReader br, ref Delegate d,
			Type delegateType)
		{
			Debug.Assert(StaticDelegateTable.delegateTypeTable.ContainsKey(delegateType));

			var delegateTypeInfo = StaticDelegateTable.delegateTypeTable[delegateType];
			var delegateMethodInfo = delegateTypeInfo.GetMethodInfoForId(br.ReadInt32());

			object target = null;
			if (delegateMethodInfo.canHaveTarget)
				if (context.Walk(ref target))
					target = StaticDispatchTable.DeserializationDispatcher(context,
						br); // (see comment in the SerializeDelegate method)

			d = Delegate.CreateDelegate(delegateType, target, delegateMethodInfo.method);
		}


		#region Method Provider

		public static SerializationMethodProviders CreateSerializationMethodProviders()
		{
			return new SerializationMethodProviders(
				new EmptyMethodProvider(),
				new EmptyMethodProvider(),
				new EmptyMethodProvider(),
				new EmptyMethodProvider(),
				new DelegateFieldMethodProvider(typeof(DelegateSerialization).GetMethod("SerializeDelegateField")),
				new DelegateFieldMethodProvider(typeof(DelegateSerialization).GetMethod("DeserializeDelegateField")),
				new EmptyMethodProvider());
		}

		internal class DelegateFieldMethodProvider : MethodProvider
		{
			private readonly MethodInfo serializeMethod;

			// Expects the methods in SerializeArray, sorted by array rank:
			internal DelegateFieldMethodProvider(MethodInfo serializeMethod)
			{
				this.serializeMethod = serializeMethod;
			}

			public override MethodInfo GetMethodForType(Type type)
			{
				if (type.BaseType != typeof(MulticastDelegate))
					return null;
				return serializeMethod.MakeGenericMethod(type);
			}
		}

		#endregion
	}
}