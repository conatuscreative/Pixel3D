// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pixel3D.Editing
{
    public abstract class IntrospectBehaviours<T>
    {
        public IEnumerable<string> Behaviours { get; private set; }
        public IEnumerable<Type> BehaviourTypes { get; private set; }

        public void Execute(string introspectionDir, Func<Assembly, bool> introspectAssemblyFilter)
        {
            BehaviourTypes = Introspection.GetExportedTypes(introspectionDir, introspectAssemblyFilter) ?? Enumerable.Empty<Type>();
            IEnumerable<string> names;
            Emit(out names);
            Behaviours = names ?? Enumerable.Empty<string>();
        }

        private void Emit(out IEnumerable<string> n)
        {
            var names = new ConcurrentBag<string>();
            var visitedTypes = new ConcurrentBag<Type>();
            var available = BehaviourTypes.Where(type =>
            {
                if (type.IsAbstract)
                    return false;

                // there are many implementations of the same types based on platform,
                // and the Editor is only aware the types built against XNA and combined source,
                // so we will match based on key rather than a true type, in the introspection
                // folder types

                var superType = BehaviourTypes.FirstOrDefault(x => x.Namespace == typeof(T).Namespace && x.Name == typeof(T).Name);
                if (superType == null)
                    return false;

                return type.IsSubclassOf(superType) && !type.IsAbstract;
            }).ToList();
            if (available.Count == 0)
            {
                n = names;
                return;
            }
            foreach (var type in available)
            {
                names.Add(type.Name);
                visitedTypes.Add(type);
            }
            n = names.Distinct().ToList();
        }
    }
}