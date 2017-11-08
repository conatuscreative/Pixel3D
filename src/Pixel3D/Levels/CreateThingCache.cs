using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Pixel3D.Engine;
using Pixel3D.Levels;

namespace RCRU.Engine.Levels
{
    public static class CreateThingCache
    {
        delegate IActor CreateThingDelegate(Thing thing, IUpdateContext context);
        static Dictionary<string, CreateThingDelegate> cache = new Dictionary<string, CreateThingDelegate>();

        public static void Initialize(Assembly assembly)
        {
            // This should match CreateThingDelegate
            Type[] constructorTypes = { typeof(Thing), typeof(IUpdateContext) };

            foreach (var type in assembly.GetTypes())
            {
                if (typeof(IActor).IsAssignableFrom(type))
                {
                    var constructor = GetConstructor(type);
                    if (constructor != null)
                    {
                        // No way to convert a constructor to a delegate directly. To IL we go!
                        DynamicMethod dm = new DynamicMethod("Create_" + type.Name, typeof(IActor), constructorTypes, type);
                        ILGenerator il = dm.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0); // Thing
                        il.Emit(OpCodes.Ldarg_1); // IUpdateContext
                        il.Emit(OpCodes.Newobj, constructor);
                        il.Emit(OpCodes.Ret);

                        cache[type.Name] = (CreateThingDelegate)dm.CreateDelegate(typeof(CreateThingDelegate));
                    }
                    else
                    {
                        // Debug.WriteLine("Warning: No 'Thing' constructor for " + type);
                    }
                }
            }
        }

        private static ConstructorInfo GetConstructor(Type type)
        {
            return type.GetConstructors()
                .Where(ci =>
                {
                    var parameters = ci.GetParameters();
                    return parameters.Length == 2 && parameters[0].ParameterType == typeof(Thing);
                })
                .Where(ci =>
                {
                    var parameters = ci.GetParameters();
                    return typeof(IUpdateContext).IsAssignableFrom(parameters[1].ParameterType);
                }).FirstOrDefault();
        }

        public static IActor CreateThing(string behaviour, Thing thing, IUpdateContext context)
        {
            return cache[behaviour](thing, context);
        }
    }
}
