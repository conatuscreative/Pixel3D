// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Pixel3D.Serialization.Context;
using Pixel3D.Serialization.Discovery;
using Pixel3D.Serialization.Generator.ILWriting;
using Pixel3D.Serialization.MethodProviders;

namespace Pixel3D.Serialization.Generator
{
	//
	//
	// Type serializer (reference or value type)
	//   |
	//   |     { => Primitive Serializers (and other special handling)
	//  Fields { => Value Type Serializer
	//         { => Field Serializer
	//              |
	//              +--> Dynamic Dispatch --> Reference Type serializer
	//              +-----------------------> Reference Type serializer
	//
	//


	internal class SerializerMethodGenerator
	{
		private readonly SerializationMethodProviders customMethods;


		private MethodInfo deserializationDispatcherMethod;
		private MethodProvider fieldDeserializeMethods;


		// For serializing fields (both reference and value type)
		private MethodProvider fieldSerializeMethods;
		private MethodProvider referenceTypeDeserializeMethods;

		// For dispatching to and for calling base-type serializers
		private MethodProvider referenceTypeSerializeMethods;
		private readonly IEnumerable<Type> suplementalDispatchTypes; // can be null
		private readonly TypeClassifier typeClassifier;

		public SerializerMethodGenerator(TypeClassifier typeClassifier, SerializationMethodProviders customMethods,
			IEnumerable<Type> suplementalDispatchTypes)
		{
			this.typeClassifier = typeClassifier;
			this.customMethods = customMethods;
			this.suplementalDispatchTypes = suplementalDispatchTypes;
		}


		public GeneratorResult Generate(MethodCreatorCreator methodCreatorCreator)
		{
			#region Declare Value Type Serializer Methods

			var valueTypeSerializerType = methodCreatorCreator.Create("ValueTypeSerializer");
			var valueTypeDeserializerType = methodCreatorCreator.Create("ValueTypeDeserializer");

			var generatedValueTypeSerializeMethods = new Dictionary<Type, MethodInfo>();
			var generatedValueTypeDeserializeMethods = new Dictionary<Type, MethodInfo>();

			foreach (var valueType in typeClassifier.ValueTypes)
			{
				if (customMethods.HasTypeSerializer(valueType))
					continue; // Don't generate serializers where custom methods exist

				var serializeMethod = valueTypeSerializerType.CreateMethod(valueType, "ValueTypeSerialize",
					typeof(void), new[] {typeof(SerializeContext), typeof(BinaryWriter), valueType.MakeByRefType()});
				generatedValueTypeSerializeMethods.Add(valueType, serializeMethod);

				var deserializeMethod = valueTypeDeserializerType.CreateMethod(valueType, "ValueTypeDeserialize",
					typeof(void), new[] {typeof(DeserializeContext), typeof(BinaryReader), valueType.MakeByRefType()});
				generatedValueTypeDeserializeMethods.Add(valueType, deserializeMethod);
			}

			#endregion


			#region Declare Reference Type Serializer Methods

			// Note: These methods have no type check/dispatch - fields that could reference derived types must go through the field methods instead of these!

			var referenceTypeSerializerType = methodCreatorCreator.Create("ReferenceTypeSerializer");
			var referenceTypeDeserializerType = methodCreatorCreator.Create("ReferenceTypeDeserializer");

			var generatedReferenceTypeSerializeMethods = new Dictionary<Type, MethodInfo>();
			var generatedReferenceTypeDeserializeMethods = new Dictionary<Type, MethodInfo>();

			foreach (var referenceType in typeClassifier.ReferenceTypes)
			{
				Debug.Assert(!referenceType.IsArray);
				if (customMethods.HasTypeSerializer(referenceType))
					continue; // Don't generate serializers where custom methods exist

				var serializeMethod = referenceTypeSerializerType.CreateMethod(referenceType, "ReferenceTypeSerialize",
					typeof(void), new[] {typeof(SerializeContext), typeof(BinaryWriter), referenceType});
				generatedReferenceTypeSerializeMethods.Add(referenceType, serializeMethod);

				var deserializeMethod = referenceTypeDeserializerType.CreateMethod(referenceType,
					"ReferenceTypeDeserialize", typeof(void),
					new[] {typeof(DeserializeContext), typeof(BinaryReader), referenceType});
				generatedReferenceTypeDeserializeMethods.Add(referenceType, deserializeMethod);
			}

			#endregion


			#region Declare Field Serializer Methods

			var fieldSerializerType = methodCreatorCreator.Create("FieldSerializer");
			var fieldDeserializerType = methodCreatorCreator.Create("FieldDeserializer");

			var generatedReferenceFieldSerializeMethods = new Dictionary<Type, MethodInfo>();
			var generatedReferenceFieldDeserializeMethods = new Dictionary<Type, MethodInfo>();

			foreach (var fieldType in typeClassifier.fieldSerializerDispatchLookup.Keys)
			{
				if (customMethods.HasFieldSerializer(fieldType))
					continue; // Don't create field serializers where custom ones exist

				var serializeMethod = fieldSerializerType.CreateMethod(fieldType, "FieldSerialize", typeof(void),
					new[] {typeof(SerializeContext), typeof(BinaryWriter), fieldType});
				generatedReferenceFieldSerializeMethods.Add(fieldType, serializeMethod);

				var deserializeMethod = fieldDeserializerType.CreateMethod(fieldType, "FieldDeserialize", typeof(void),
					new[] {typeof(DeserializeContext), typeof(BinaryReader), fieldType.MakeByRefType()});
				generatedReferenceFieldDeserializeMethods.Add(fieldType, deserializeMethod);
			}

			#endregion


			#region Declare Dispatch Methods

			var serializerDispatchType = methodCreatorCreator.Create("SerializerDispatch");
			var deserializerDispatchType = methodCreatorCreator.Create("DeserializerDispatch");

			// These are dispatched by ID#, so this is a lookup by that ID#
			var dynamicDispatchMethods = new List<SerializationMethods>(typeClassifier.dispatchableTypes.Count);

			// Dispatch to types that require actual dispatch (from type classification) as well as any suplemental types
			// (Suplemental types come from delegates, at the moment, a few of which require dynamic dispatch (see DelegateSerialization.SerializeDelegate)
			// and I haven't written a direct-dispatch path yet)
			HashSet<Type> dispatchTypes;
			if (suplementalDispatchTypes == null)
				dispatchTypes = typeClassifier.dispatchableTypes;
			else
				dispatchTypes = new HashSet<Type>(typeClassifier.dispatchableTypes.Concat(suplementalDispatchTypes));


			foreach (var type in dispatchTypes.NetworkOrder(t => t.Module.Name + t.FullName))
			{
				var idString = dynamicDispatchMethods.Count.ToString("0000");
				SerializationMethods sm;
				sm.type = type;

				sm.serializer = serializerDispatchType.CreateMethod(type, "SerializeWithId_" + idString, typeof(void),
					new[] {typeof(SerializeContext), typeof(BinaryWriter), typeof(object)});
				sm.deserializer = deserializerDispatchType.CreateMethod(type, "DeserializeFromId_" + idString, type,
					new[] {typeof(DeserializeContext), typeof(BinaryReader)});
				dynamicDispatchMethods.Add(sm); // Defines the ID# 
			}

			// Deserialization dispatch method (this could reasonably use a table like serialization, but it was original a giant switch statement - so let's keep it that way)
			deserializationDispatcherMethod = deserializerDispatchType.CreateMethod(null, "DeserializationDispatcher",
				typeof(object), new[] {typeof(DeserializeContext), typeof(BinaryReader)});

			#endregion


			#region Lookup Hookup

			// These are all used to do lookup during IL generation (should probably be switched to a SerializationMethodProviders)

			fieldSerializeMethods = FallbackMethodProvider.Combine(customMethods.ValueTypeSerializeMethods,
				customMethods.ReferenceFieldSerializeMethods,
				new LookupMethodProvider(generatedValueTypeSerializeMethods),
				new LookupMethodProvider(generatedReferenceFieldSerializeMethods));

			fieldDeserializeMethods = FallbackMethodProvider.Combine(
				customMethods.ValueTypeDeserializeMethods,
				customMethods.ReferenceFieldDeserializeMethods,
				new LookupMethodProvider(generatedValueTypeDeserializeMethods),
				new LookupMethodProvider(generatedReferenceFieldDeserializeMethods));

			referenceTypeSerializeMethods = FallbackMethodProvider.Combine(
				customMethods.ReferenceTypeSerializeMethods,
				new LookupMethodProvider(generatedReferenceTypeSerializeMethods));

			referenceTypeDeserializeMethods = FallbackMethodProvider.Combine(
				customMethods.ReferenceTypeDeserializeMethods,
				new LookupMethodProvider(generatedReferenceTypeDeserializeMethods));

			var serializeContext =
				new ILGenContext(Direction.Serialize, fieldSerializeMethods, referenceTypeSerializeMethods);
			var deserializeContext = new ILGenContext(Direction.Deserialize, fieldDeserializeMethods,
				referenceTypeDeserializeMethods);

			#endregion


			#region Generate Dynamic Dispatch Method IL

			for (var i = 0; i < dynamicDispatchMethods.Count; i++)
			{
				// SerializeWithId Method:

				#region Generate IL

				{
					var il = dynamicDispatchMethods[i].serializer.GetILGenerator();

					// br.Write('i');
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldc_I4, i);
					il.Emit(OpCodes.Callvirt, Methods.BinaryWriter_WriteInt32);

					// ReferenceTypeSerializer.Serialize(context, bw, (Class)obj);
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldarg_2);
					il.Emit(OpCodes.Castclass, dynamicDispatchMethods[i].type);
					il.Emit(OpCodes.Call, referenceTypeSerializeMethods[dynamicDispatchMethods[i].type]);

					il.Emit(OpCodes.Ret);
				}

				#endregion


				// DeserializeFromId Method:

				#region Generate IL

				{
					var il = dynamicDispatchMethods[i].deserializer.GetILGenerator();
					il.DeclareLocal(dynamicDispatchMethods[i].type); // Class obj

					// Class obj = /* create object */
					GenerateTypeCreation(il, dynamicDispatchMethods[i].type);
					il.Emit(OpCodes.Stloc_0);

					// ReferenceTypeDeserializer.Deserialize(context, br, (Class)obj);
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldloc_0);
					il.Emit(OpCodes.Call, referenceTypeDeserializeMethods[dynamicDispatchMethods[i].type]);

					// return obj;
					il.Emit(OpCodes.Ldloc_0);
					il.Emit(OpCodes.Ret);
				}

				#endregion
			}

			#endregion


			#region Generate Reference Field Serializer Method IL

			foreach (var fieldDispatch in typeClassifier.fieldSerializerDispatchLookup)
			{
				var fieldType = fieldDispatch.Key;
				var dispatchToTypes = fieldDispatch.Value;

				var serializeMethod = generatedReferenceFieldSerializeMethods[fieldType];
				var deserializeMethod = generatedReferenceFieldDeserializeMethods[fieldType];

				GenerateReferenceFieldSerializer(serializeMethod.GetILGenerator(), fieldType, dispatchToTypes);
				GenerateReferenceFieldDeserializer(deserializeMethod.GetILGenerator(), fieldType, dispatchToTypes);
			}

			#endregion


			#region Generate Reference and Value Type Serializers

			foreach (var sm in generatedValueTypeSerializeMethods)
				TypeSerializeILGeneration.GenerateValueTypeSerializationMethod(sm.Key, sm.Value.GetILGenerator(),
					serializeContext);
			foreach (var sm in generatedValueTypeDeserializeMethods)
				TypeSerializeILGeneration.GenerateValueTypeSerializationMethod(sm.Key, sm.Value.GetILGenerator(),
					deserializeContext);
			foreach (var sm in generatedReferenceTypeSerializeMethods)
				TypeSerializeILGeneration.GenerateReferenceTypeSerializationMethod(sm.Key, sm.Value.GetILGenerator(),
					serializeContext);
			foreach (var sm in generatedReferenceTypeDeserializeMethods)
				TypeSerializeILGeneration.GenerateReferenceTypeSerializationMethod(sm.Key, sm.Value.GetILGenerator(),
					deserializeContext);

			#endregion


			#region Generate Deserialization Dispatcher (IL)

			{
				var il = deserializationDispatcherMethod.GetILGenerator();

				var dispatchLabels = new Label[dynamicDispatchMethods.Count];
				for (var i = 0; i < dispatchLabels.Length; i++)
					dispatchLabels[i] = il.DefineLabel();
				var failLabel = il.DefineLabel();

				// br.ReadInt32()
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Callvirt,
					typeof(BinaryReader)
						.GetMethod("ReadInt32")); // NOTE: C# compiler loads this into two locals for no reason

				// switch(...)
				il.Emit(OpCodes.Switch, dispatchLabels);
				il.Emit(OpCodes.Br,
					failLabel); // default case // NOTE: C# compiler emits br.s, but I bet it's not short with our huge table!

				for (var i = 0; i < dynamicDispatchMethods.Count; i++)
				{
					// case 'i':
					il.MarkLabel(dispatchLabels[i]);
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Call, dynamicDispatchMethods[i].deserializer);
					il.Emit(OpCodes.Ret);
				}

				// throw new Exception("...");
				il.MarkLabel(failLabel);
				il.Emit(OpCodes.Ldstr, "Unknown Type");
				il.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor(new[] {typeof(string)}));
				il.Emit(OpCodes.Throw);
			}

			#endregion


			#region Fill Serialization Dispatch Table

			// We used to generate a table with IL in the serializer assembly (fancy). Now we can just do it directly.
			// (But we do have to do it last, so the methods are actually ready to convert to delegates)

			var dispatchTable = new Dictionary<Type, SerializeDispatchDelegate>();

			// Only dynamic methods can be converted to delegates (because the dynamic assembly we create in assembly-generating mode is save-only)
			if (methodCreatorCreator is DynamicMethodCreatorCreator)
				for (var i = 0; i < dynamicDispatchMethods.Count; i++)
				{
					var serializerMethod = dynamicDispatchMethods[i].serializer as DynamicMethod;
					Debug.Assert(serializerMethod != null);
					var serializerDelegate =
						(SerializeDispatchDelegate) serializerMethod.CreateDelegate(typeof(SerializeDispatchDelegate));
					dispatchTable.Add(dynamicDispatchMethods[i].type, serializerDelegate);
				}

			#endregion


			#region Return

			var generatedMethodProviders = new SerializationMethodProviders(
				new LookupMethodProvider(generatedValueTypeSerializeMethods),
				new LookupMethodProvider(generatedValueTypeDeserializeMethods),
				new LookupMethodProvider(generatedReferenceTypeSerializeMethods),
				new LookupMethodProvider(generatedReferenceTypeDeserializeMethods),
				new LookupMethodProvider(generatedReferenceFieldSerializeMethods),
				new LookupMethodProvider(generatedReferenceFieldDeserializeMethods),
				new EmptyMethodProvider());


			// Note: custom methods come before generated methods:
			var combinedMethodProviders = SerializationMethodProviders
				.Combine(customMethods, generatedMethodProviders);


			DeserializeDispatchDelegate deserializeDispatchDelegate = null;
			if (methodCreatorCreator is DynamicMethodCreatorCreator
			) // Only dynamic methods can be converted to delegates (our dynamic assembly is save-only)
				deserializeDispatchDelegate =
					(DeserializeDispatchDelegate) (deserializationDispatcherMethod as DynamicMethod).CreateDelegate(
						typeof(DeserializeDispatchDelegate));


			return new GeneratorResult(combinedMethodProviders, dispatchTable, deserializeDispatchDelegate,
				null); // module table gets filled later

			#endregion
		}

		// Just a helper:
		private struct SerializationMethods
		{
			public Type type;
			public MethodInfo serializer;
			public MethodInfo deserializer;
		}


		#region IL Generation - Serialize Fields

		private void GenerateReferenceFieldSerializer(ILGenerator il, Type fieldType, HashSet<Type> dispatchToTypes)
		{
			//
			// WALK:
			//
			GenerateReferenceFieldSerializerWalk(il, fieldType);

			//
			// DISPATCH:
			//
			if (dispatchToTypes.Count == 0)
			{
				// Attempt to serialize directly on the field type, if that is possible
				// (The field is probably of an abstract or interface type, with no concrete types - meaning we can't deserialize it!)
				// TODO: Should this case be handled some other way?? (currently only used for fwd.GameSystem)
				var serializeMethod = referenceTypeSerializeMethods[fieldType];
				if (serializeMethod != null)
					GenerateReferenceFieldSerializerDispatchCall(il, serializeMethod);
				else
					il.Emit(OpCodes.Ret); // Probably an interface type - do nothing
			}
			else if (dispatchToTypes.Count == 1)
			{
				// If we're doing direct dispatch from an interface or abstract type, cast it down to the concrete type we expect
				var directDispatchType = dispatchToTypes.First();
				var castDown = directDispatchType.IsAssignableFrom(fieldType) ? null : directDispatchType;
				GenerateReferenceFieldSerializerDispatchCall(il, referenceTypeSerializeMethods[directDispatchType],
					castDown);
			}
			else // Multiple dispatch targets
			{
				// Convert CLR type to type ID via lookup table (writes type ID and dispatches)
				GenerateReferenceFieldSerializerDispatchCall(il, Methods.StaticDispatchTable_SerializationDispatcher,
					null);
			}
		}


		private void GenerateReferenceFieldSerializerWalk(ILGenerator il, Type fieldType)
		{
			var checkPassLabel = il.DefineLabel();

			// if(!context.Walk(obj)) return;
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, Methods.SerializeContext_Walk);
			il.Emit(OpCodes.Brtrue_S, checkPassLabel);

			il.Emit(OpCodes.Ret);
			il.MarkLabel(checkPassLabel);
		}


		private void GenerateReferenceFieldSerializerDispatchCall(ILGenerator il, MethodInfo targetMethod,
			Type castDown = null)
		{
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);

			if (castDown != null)
				il.Emit(OpCodes.Castclass, castDown);

			il.Emit(OpCodes.Call, targetMethod);
			il.Emit(OpCodes.Ret);
		}

		#endregion


		#region IL Generation - Deserialize Fields

		private void GenerateReferenceFieldDeserializer(ILGenerator il, Type fieldType, HashSet<Type> dispatchToTypes)
		{
			//
			// WALK:
			//
			// if(!context.Walk(obj)) return;
			var checkPassLabel = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, typeof(DeserializeContext).GetMethod("Walk").MakeGenericMethod(fieldType));
			il.Emit(OpCodes.Brtrue_S, checkPassLabel);

			il.Emit(OpCodes.Ret);
			il.MarkLabel(checkPassLabel);


			//
			// DISPATCH:
			//
			if (dispatchToTypes.Count == 0) // Nothing to dispatch to (throws exception)
			{
				// obj = null;
				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Stind_Ref);

				// throw new NotSupportedException("..." + typeof(...));
				il.Emit(OpCodes.Ldstr, "Cannot deserialize type ");
				il.Emit(OpCodes.Ldtoken, fieldType);
				il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle);
				il.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new[] {typeof(object), typeof(object)}));
				il.Emit(OpCodes.Newobj, typeof(NotSupportedException).GetConstructor(new[] {typeof(string)}));
				il.Emit(OpCodes.Throw);
			}
			else if (dispatchToTypes.Count == 1) // One dispatch target: Do direct dispatch
			{
				var directDispatchType = dispatchToTypes.First();

				// obj = /* create object */
				il.Emit(OpCodes.Ldarg_2);
				GenerateTypeCreation(il, directDispatchType);
				il.Emit(OpCodes.Stind_Ref);

				// DeserializeReferenceType(context, bw, obj);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldind_Ref);
				if (!directDispatchType.IsAssignableFrom(fieldType)
				) // Direct dispatch where the field is an abstract type, dispatching to a single concrete type (requires a cast)
					il.Emit(OpCodes.Castclass, directDispatchType);
				il.Emit(OpCodes.Call, referenceTypeDeserializeMethods[directDispatchType]);
				il.Emit(OpCodes.Ret);
			}
			else // Multiple dispatch targets: Do dynamic dispatch
			{
				// obj = (Class)DeserializationDispatcher(context, br); // <- target is responsible for object init
				il.Emit(OpCodes.Ldarg_2); // load 'obj' reference
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Call,
					deserializationDispatcherMethod); // <- will read object type ID and dispatch on it (will create and deserialize the object)
				il.Emit(OpCodes.Castclass, fieldType);
				il.Emit(OpCodes.Stind_Ref); // store into 'obj' reference
				il.Emit(OpCodes.Ret);
			}
		}


		private void GenerateTypeCreation(ILGenerator il, Type type)
		{
			var customInitializeMethod = customMethods.ReferenceTypeInitializeMethods[type];
			if (customInitializeMethod != null)
			{
				// Call custom initializer

				if (customInitializeMethod.GetParameters().Length != 0)
					throw new Exception("Internal Error: Bad custom initializer parameters"); // Should never happen!
				if (type != customInitializeMethod.ReturnType)
					throw new Exception(
						"Internal Error: Bad custom initializer return type"); // Stick a cast class in here if this ever becomes possible

				il.Emit(OpCodes.Call, customInitializeMethod);
			}
			else
			{
				// (Class)FormatterServices.GetUninitializedObject(typeof(Class));
				il.Emit(OpCodes.Ldtoken, type);
				il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle);
				il.Emit(OpCodes.Call, Methods.FormatterServices_GetUninitializedObject);
				il.Emit(OpCodes.Castclass, type);
			}
		}

		#endregion
	}
}