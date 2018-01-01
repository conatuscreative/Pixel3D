using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Pixel3D.Collections;
using Pixel3D.Engine;
using Pixel3D.Engine.Levels;

namespace Pixel3D.Levels
{
    public class CreateLevelBehaviourCache
    {
        delegate LevelBehaviour CreateLevelBehaviourDelegate(Level level, UpdateContext context);
        delegate ILevelSubBehaviour CreateLevelSubBehaviourDelegate();

        static Dictionary<string, CreateLevelBehaviourDelegate> levelCache = new Dictionary<string, CreateLevelBehaviourDelegate>();
        static Dictionary<string, CreateLevelSubBehaviourDelegate> levelSubCache = new Dictionary<string, CreateLevelSubBehaviourDelegate>();
        static Dictionary<string, CreateLevelSubBehaviourDelegate> globalSubCache = new Dictionary<string, CreateLevelSubBehaviourDelegate>();

        public static void Initialize(Assembly[] scanAssemblies, params Type[] globalSubBehaviours)
        {
            Type[] delegateArgumentTypes = { typeof(Level), typeof(UpdateContext) };

            // Possible constructors in priority order:
            Type[][] parameterTypeSets = {
                    new[] { typeof(Level), typeof(UpdateContext) },
                    new[] { typeof(UpdateContext) },
                    new[] { typeof(Level) },
                    Type.EmptyTypes,
            };

            //
            // Add all global sub-behaviours; in the future, these can be managed through better tools
            //
            foreach (var subBehaviour in globalSubBehaviours)
            {
                RegisterSubBehaviourType(globalSubCache, subBehaviour);
            }

            //
            // Look in all assemblies, as we may have content scattered across multiple WADs...
            //

            foreach (var assembly in scanAssemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(ILevelSubBehaviour).IsAssignableFrom(type) && type != typeof(LevelSubBehaviour) && !type.IsAbstract)
                    {
                        RegisterSubBehaviourType(levelSubCache, type);
                    }

                    if (typeof(LevelBehaviour).IsAssignableFrom(type) && type != typeof(LevelBehaviour) && !type.IsAbstract)
                    {
                        foreach (var parameterTypes in parameterTypeSets)
                        {
                            var constructor = type.GetConstructor(parameterTypes);
                            if (constructor != null)
                            {
                                // No way to convert a constructor to a delegate directly. And we want to select parameters. To IL we go!
                                DynamicMethod dm = new DynamicMethod("Create_" + type.Name, typeof(LevelBehaviour), delegateArgumentTypes, type);
                                ILGenerator il = dm.GetILGenerator();

                                // Map delegate parameters to constructor parameters
                                foreach (var parameterType in parameterTypes)
                                {
                                    if (parameterType == typeof(Level))
                                        il.Emit(OpCodes.Ldarg_0); // <- depends on order of delegate arguments!
                                    else if (parameterType == typeof(UpdateContext))
                                        il.Emit(OpCodes.Ldarg_1); // <- depends on order of delegate arguments!
                                    else
                                        throw new InvalidOperationException(); // <- should be impossible
                                }

                                il.Emit(OpCodes.Newobj, constructor);
                                il.Emit(OpCodes.Ret);

                                levelCache[type.Name] = (CreateLevelBehaviourDelegate)dm.CreateDelegate(typeof(CreateLevelBehaviourDelegate));
                                goto foundValidConstructor;
                            }
                        }

                        Debug.WriteLine("WARNING: No valid constructor to create level behaviour: " + type);

                    foundValidConstructor:
                        ; // done
                    }
                }
            }
        }

        private static void RegisterSubBehaviourType(IDictionary<string, CreateLevelSubBehaviourDelegate> cache, Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                DynamicMethod dm = new DynamicMethod("Create_" + type.Name, type, Type.EmptyTypes);
                ILGenerator il = dm.GetILGenerator();
                il.Emit(OpCodes.Newobj, constructor);
                il.Emit(OpCodes.Ret);

                string key = type.Name.Replace("SubBehaviour", string.Empty);
                cache[key] = (CreateLevelSubBehaviourDelegate)dm.CreateDelegate(typeof(CreateLevelSubBehaviourDelegate));
            }
        }

        private static readonly char[] CommaSeparator = { ',' };

        public static LevelBehaviour CreateLevelBehaviour(string behaviour, Level level, UpdateContext context)
        {
            if (behaviour == null)
                return new LevelBehaviour();

            CreateLevelBehaviourDelegate createMethod; 
            if (levelCache.TryGetValue(behaviour, out createMethod))
            {
                LevelBehaviour levelBehaviour = createMethod(level, context);

                InjectSubBehaviours(level, levelBehaviour);
                
                return levelBehaviour;
            }
            
            Debug.WriteLine("Missing LevelBehaviour called \"" + behaviour + "\"");
            return new LevelBehaviour();
        }

        private static readonly ReadOnlyList<ILevelSubBehaviour> NoSubBehaviours = new ReadOnlyList<ILevelSubBehaviour>(new List<ILevelSubBehaviour>(0));

        private static void InjectSubBehaviours(Level level, LevelBehaviour levelBehaviour)
        {
            bool hasGlobalSubBehaviours = globalSubCache != null && globalSubCache.Count > 0;
            
            var subBehaviourString = level.properties.GetString(Symbols.SubBehaviours);
            if (!hasGlobalSubBehaviours && subBehaviourString == null)
            {
                levelBehaviour.subBehaviours = NoSubBehaviours;
                return;
            }

            string[] subBehaviours = null;
            if (subBehaviourString != null)
            {
                subBehaviours = subBehaviourString.Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (!hasGlobalSubBehaviours && subBehaviours.Length == 0)
                {
                    levelBehaviour.subBehaviours = NoSubBehaviours;
                    return;
                }    
            }
            
            var subList = new List<ILevelSubBehaviour>();

            if (hasGlobalSubBehaviours)
            {
                foreach (var globalSubBehaviour in globalSubCache)
                {
                    subList.Add(globalSubBehaviour.Value());
                }    
            }

            if (subBehaviours != null)
            {
                foreach (var subBehaviour in subBehaviours)
                {
                    CreateLevelSubBehaviourDelegate createSubMethod;
                    if (levelSubCache.TryGetValue(subBehaviour, out createSubMethod))
                    {
                        subList.Add(createSubMethod());
                    }
                }
            }
            
            levelBehaviour.subBehaviours = new ReadOnlyList<ILevelSubBehaviour>(subList);
        }
    }
}
