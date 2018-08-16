using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Pixel3D.Engine;

namespace Pixel3D.Engine.Levels
{
    public static class CreateThingCache
    {
        delegate Actor CreateThingDelegate(Thing thing, UpdateContext context);
        static Dictionary<string, CreateThingDelegate> cache = new Dictionary<string, CreateThingDelegate>();

        public static void Initialize(params Assembly[] assemblies)
        {
            // This should match CreateThingDelegate
            Type[] constructorTypes = { typeof(Thing), typeof(UpdateContext) };

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
                            DynamicMethod dm = new DynamicMethod("Create_" + type.Name, typeof(Actor), constructorTypes, type);
                            ILGenerator il = dm.GetILGenerator();
                            il.Emit(OpCodes.Ldarg_0); // Thing
                            il.Emit(OpCodes.Ldarg_1); // UpdateContext
                            il.Emit(OpCodes.Newobj, constructor);
                            il.Emit(OpCodes.Ret);

                            cache[type.Name] = (CreateThingDelegate)dm.CreateDelegate(typeof(CreateThingDelegate));
                        }
                        else
                        {
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
    }
}
