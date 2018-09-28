// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Pixel3D.ActorManagement;

namespace Pixel3D.Levels
{
	public static class CreateThingCache
	{
		private static readonly Dictionary<string, CreateThingDelegate> cache =
			new Dictionary<string, CreateThingDelegate>();

		public static void Initialize(params Assembly[] assemblies)
		{
			// This should match CreateThingDelegate (explicit definition of Thing)
			Type[] constructorTypes = {typeof(Thing), typeof(UpdateContext)};

			foreach (var assembly in assemblies)
			{
				foreach (var type in assembly.GetTypes())
				{
					if (typeof(Actor).IsAssignableFrom(type))
					{
						var constructor = type.GetConstructor(constructorTypes);
						if (constructor != null)
						{
							// No way to convert a constructor to a delegate directly. To IL we go!
							var dm = new DynamicMethod("Create_" + type.Name, typeof(Actor), constructorTypes, type);
							var il = dm.GetILGenerator();
							il.Emit(OpCodes.Ldarg_0); // Thing
							il.Emit(OpCodes.Ldarg_1); // UpdateContext
							il.Emit(OpCodes.Newobj, constructor);
							il.Emit(OpCodes.Ret);

							cache[type.Name] = (CreateThingDelegate) dm.CreateDelegate(typeof(CreateThingDelegate));
						}
						else
						{
							if (type.IsAbstract || typeof(ISuppressThingWarning).IsAssignableFrom(type))
								continue;

							Debug.WriteLine("Warning: No 'Thing' constructor for " + type);
						}
					}
				}
			}
		}

		public static Actor CreateThing(string behaviour, Thing thing, UpdateContext context)
		{
			return cache[behaviour](thing, context);
		}

		private delegate Actor CreateThingDelegate(Thing thing, UpdateContext context);
	}
}