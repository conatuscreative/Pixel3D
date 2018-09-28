// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Pixel3D.Serialization.Discovery;

namespace Pixel3D.Serialization.Generator.ILWriting
{
	internal static class TypeSerializeILGeneration
	{
		//
		// Serialization of a Type:
		//

		// .NET and Mono have different opinions on this name... -flibit
		private static readonly string HAS_VALUE_FIELD = GetHasValueField();

		private static string GetHasValueField()
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT) return "hasValue";
			return "has_value";
		}

		// Value type entry point:
		public static void GenerateValueTypeSerializationMethod(Type type, ILGenerator il, ILGenContext context)
		{
			Debug.Assert(type.IsValueType);

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				GenerateNullableTypeSerializeMethod(type, il, context);
			}
			else
			{
				GenerateSerializeAllTypeFields(type, il, context);
				il.Emit(OpCodes.Ret);
			}
		}

		// Reference type entry point:
		public static void GenerateReferenceTypeSerializationMethod(Type type, ILGenerator il, ILGenContext context)
		{
			Debug.Assert(!type.IsValueType);

			if (context.Serialize
			) // Serialize visits and leaves objects like a stack (including up the inheritance hierarchy within a single object instance)
				GenerateReferenceTypeVisitObject(type, il, context);

			var baseTypeSerializer = context.referenceTypeSerializationMethods[type.BaseType];
			if (baseTypeSerializer != null)
			{
				// Serialize the base type:
				// ReferenceType[De]Serializer(context, bw, (BaseType)obj);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Call, baseTypeSerializer);
			}
			else if (!context.Serialize
			) // Deserialize visits objects once only to get their reference (so only visit the base)
			{
				GenerateReferenceTypeVisitObject(type, il, context);
			}

			GenerateSerializeAllTypeFields(type, il, context);

			GenerateReferenceTypeLeaveObject(type, il, context);
			il.Emit(OpCodes.Ret);
		}


		private static void GenerateReferenceTypeVisitObject(Type type, ILGenerator il, ILGenContext context)
		{
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt,
				context.Serialize ? Methods.SerializeContext_VisitObject : Methods.DeserializeContext_VisitObject);
		}

		private static void GenerateReferenceTypeLeaveObject(Type type, ILGenerator il, ILGenContext context)
		{
			if (context.Serialize) // Deserialize doesn't have LeaveObject
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Callvirt, Methods.SerializeContext_LeaveObject);
			}
		}


		[Conditional("DEBUG")]
		private static void GenerateDebugTrace(string traceString, ILGenerator il, ILGenContext context)
		{
#if DEBUG
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldstr, traceString);
			il.Emit(OpCodes.Callvirt,
				context.Serialize ? Methods.SerializeContext_DebugTrace : Methods.DeserializeContext_DebugTrace);
#endif
		}


		private static void GenerateNullableTypeSerializeMethod(Type type, ILGenerator il, ILGenContext context)
		{
			var fieldHasValue = type.GetField(HAS_VALUE_FIELD, TypeDiscovery.allInstanceDeclared);
			var fieldValue = type.GetField("value", TypeDiscovery.allInstanceDeclared);

			// Serialize obj.hasValue
			GenerateSerializePrimitiveField(type, fieldHasValue, il, context);

			// if(obj.hasValue)
			var end = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldfld, fieldHasValue);
			il.Emit(OpCodes.Brfalse, end);

			//     Serialize obj.value
			GenerateSerializeField(type, fieldValue, il, context);

			if (context.Deserialize) // Return before the else statement (serialize can just drop through)
				il.Emit(OpCodes.Ret);

			// else
			il.MarkLabel(end);
			if (context.Deserialize) GenerateClearField(type, fieldValue, il, context);

			il.Emit(OpCodes.Ret);
		}


		/// <summary>Emit the shortest constant load available</summary>
		/// <remarks>Oddly enough, ILGenerator doesn't emit the short form automatically.</remarks>
		private static void EmitLdc_I4(this ILGenerator il, int value)
		{
			switch (value)
			{
				case -1:
					il.Emit(OpCodes.Ldc_I4_M1);
					break;
				case 0:
					il.Emit(OpCodes.Ldc_I4_0);
					break;
				case 1:
					il.Emit(OpCodes.Ldc_I4_1);
					break;
				case 2:
					il.Emit(OpCodes.Ldc_I4_2);
					break;
				case 3:
					il.Emit(OpCodes.Ldc_I4_3);
					break;
				case 4:
					il.Emit(OpCodes.Ldc_I4_4);
					break;
				case 5:
					il.Emit(OpCodes.Ldc_I4_5);
					break;
				case 6:
					il.Emit(OpCodes.Ldc_I4_6);
					break;
				case 7:
					il.Emit(OpCodes.Ldc_I4_7);
					break;
				case 8:
					il.Emit(OpCodes.Ldc_I4_8);
					break;

				default:
					if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
						il.Emit(OpCodes.Ldc_I4_S, value);
					else
						il.Emit(OpCodes.Ldc_I4, value);
					break;
			}
		}

		private static void GenerateSerializeAllTypeFields(Type type, ILGenerator il, ILGenContext context)
		{
			//
			// Pack all bool fields into bytes:

			var boolFields = type.GetFields(TypeDiscovery.allInstanceDeclared)
				.Where(f => f.FieldType == typeof(bool))
				.Where(f => !IsIgnoredField(f))
				.NetworkOrder(fi => fi.Name).ToArray();

			if (boolFields.Length > 0)
			{
				var bytesRequired = (boolFields.Length + 7) / 8;

				if (context.Serialize)
					for (var i = 0; i < bytesRequired; i++)
					{
						var bitsRequired = Math.Min(8, boolFields.Length - i * 8);
						Debug.Assert(bitsRequired > 0);

#if DEBUG
						var traceString = "Boolean fields (" + i + "): " + string.Join(", ",
							                  boolFields.Select(f => f.Name).ToArray(), i * 8, bitsRequired);
						GenerateDebugTrace(traceString, il, context);
#endif

						il.Emit(OpCodes.Ldarg_1); // bw.(...)

						for (var j = 0; j < bitsRequired; j++)
						{
							// const x = (1 << j);
							// (obj.value ? x : 0u)
							il.Emit(OpCodes.Ldarg_2);
							il.Emit(OpCodes.Ldfld, boolFields[i * 8 + j]);
							var labelTrue = il.DefineLabel();
							il.Emit(OpCodes.Brtrue_S, labelTrue);
							il.Emit(OpCodes.Ldc_I4_0);
							var labelEnd = il.DefineLabel();
							il.Emit(OpCodes.Br_S, labelEnd);
							il.MarkLabel(labelTrue);
							il.EmitLdc_I4(1 << j);
							il.MarkLabel(labelEnd);

							// (...) | (...)
							if (j != 0)
								il.Emit(OpCodes.Or);
						}

						// bw.Write((byte)(...));
						il.Emit(OpCodes.Conv_U1);
						il.Emit(OpCodes.Callvirt, Methods.BinaryWriter_WriteByte);
					}
				else
					for (var i = 0; i < bytesRequired; i++)
					{
						var bitsRequired = Math.Min(8, boolFields.Length - i * 8);
						Debug.Assert(bitsRequired > 0);

#if DEBUG
						var traceString = "Boolean fields (" + i + "): " + string.Join(", ",
							                  boolFields.Select(f => f.Name).ToArray(), i * 8, bitsRequired);
						GenerateDebugTrace(traceString, il, context);
#endif

						// uint data;
						var localValue = il.DeclareLocal(typeof(uint));

						// data = br.ReadByte();
						il.Emit(OpCodes.Ldarg_1);
						il.Emit(OpCodes.Callvirt, Methods.BinaryReader_ReadByte);
						il.Emit(OpCodes.Stloc, localValue); // <- NOTE: Emits short form

						for (var j = 0; j < bitsRequired; j++)
						{
							// const x = (1 << j);
							// obj.value = (data & x) > 0;
							il.Emit(OpCodes.Ldarg_2);
							il.Emit(OpCodes.Ldloc, localValue); // <- NOTE: Emits short form
							il.EmitLdc_I4(1 << j);
							il.Emit(OpCodes.And);
							il.Emit(OpCodes.Ldc_I4_0);
							il.Emit(OpCodes.Cgt_Un);
							il.Emit(OpCodes.Stfld, boolFields[i * 8 + j]);
						}
					}
			}


			//
			// Then handle all other fields:

			// NOTE: I have tried sorting this for cache coherency (primatives, then value types, then refrence types)
			//       and it is difficult to tell if it makes a real difference (due to measurement error),
			//       but if it does it seems slightly worse than just having the fields in alphabetical order.
			//       (Speculation: it might be better to mix in a couple of value-type writes every time we come back
			//        to write a pointer for a reference type. Best case may be to duplicate the field order from
			//        the CLR (can sort by MethodInfo.MetadataToken, apparently) - but this defeats the
			//        purpose of NetworkOrder, because that that is compile-time dependent.)
			//       -AR
			var nonBoolFields = type.GetFields(TypeDiscovery.allInstanceDeclared)
				.Where(f => f.FieldType != typeof(bool)).NetworkOrder(f => f.Name);

			foreach (var field in nonBoolFields)
			{
				GenerateDebugTrace(field.Name, il, context);
				GenerateSerializeField(type, field, il, context);
			}
		}

		//
		// Serialization of Fields within a Type:
		//

		private static bool IsIgnoredField(FieldInfo field)
		{
			return TypeDiscovery.IsIgnoredType(field.FieldType) ||
			       field.GetCustomAttributes(typeof(SerializationIgnoreAttribute), true).Length != 0 ||
			       field.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length != 0;
		}

		private static void GenerateSerializeField(Type type, FieldInfo field, ILGenerator il, ILGenContext context)
		{
			// NOTE: Delegate and array fields are handled by built-in custom field methods (via custom method providers)
			//       (so get handled as reference field types)


			if (IsIgnoredField(field)) return;


			if (field.FieldType.IsPrimitive)
			{
				GenerateSerializePrimitiveField(type, field, il, context); // Inline serialize
				return;
			}

			if (field.FieldType.IsPointer)
				return; // Do nothing (should explode?)

			if (field.FieldType.IsValueType)
				GenerateSerializeValueTypeField(type, field, il, context);
			else
				GenerateSerializeReferenceTypeField(type, field, il, context);
		}

		private static void GenerateSerializePrimitiveField(Type type, FieldInfo field, ILGenerator il,
			ILGenContext context)
		{
			if (context.Serialize)
			{
				il.Emit(OpCodes.Ldarg_1); // bw
				il.Emit(OpCodes.Ldarg_2); // obj
				il.Emit(OpCodes.Ldfld, field); // obj.'field'
				il.Emit(OpCodes.Callvirt, Methods.BinaryWriterPrimitive[field.FieldType]);
			}
			else
			{
				il.Emit(OpCodes.Ldarg_2); // obj
				il.Emit(OpCodes.Ldarg_1); // br
				il.Emit(OpCodes.Callvirt, Methods.BinaryReaderPrimitive[field.FieldType]);
				il.Emit(OpCodes.Stfld, field); // obj.'field' = ...
			}
		}

		private static void GenerateClearField(Type type, FieldInfo field, ILGenerator il, ILGenContext context)
		{
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldflda, field);
			il.Emit(OpCodes.Initobj, field.FieldType);
		}

		private static void GenerateSerializeValueTypeField(Type type, FieldInfo field, ILGenerator il,
			ILGenContext context)
		{
			Debug.Assert(field.FieldType.IsValueType);
			il.Emit(OpCodes.Ldarg_0); // context
			il.Emit(OpCodes.Ldarg_1); // bw
			il.Emit(OpCodes.Ldarg_2); // obj
			il.Emit(OpCodes.Ldflda, field); // obj.'field' (byref)
			il.Emit(OpCodes.Call, context.fieldSerializationMethods[field.FieldType]);
		}

		private static void GenerateSerializeReferenceTypeField(Type type, FieldInfo field, ILGenerator il,
			ILGenContext context)
		{
			Debug.Assert(!field.FieldType.IsValueType);
			il.Emit(OpCodes.Ldarg_0); // context
			il.Emit(OpCodes.Ldarg_1); // bw
			il.Emit(OpCodes.Ldarg_2); // obj
			il.Emit(context.Serialize ? OpCodes.Ldfld : OpCodes.Ldflda,
				field); // obj.'field' (byref for deserialize only!)
			il.Emit(OpCodes.Call, context.fieldSerializationMethods[field.FieldType]);
		}
	}
}