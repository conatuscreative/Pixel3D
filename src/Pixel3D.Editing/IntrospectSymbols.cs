// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pixel3D.Editing
{
    public class IntrospectSymbols
    {
        private readonly string[] symbolClassNames;

        public IList<string> Symbols { get; private set; }
        public IEnumerable<Type> Types { get; private set; }

        public IntrospectSymbols(params string[] symbolClassNames)
        {
            this.symbolClassNames = symbolClassNames;
        }

        public IntrospectSymbols() : this("Symbols")
        {

        }

        public void Execute(string introspectionDir, Func<Assembly, bool> introspectAssemblyFilter)
        {
            Types = Introspection.GetExportedTypes(introspectionDir, introspectAssemblyFilter);
            IList<string> symbols;
            Emit(out symbols);
            Symbols = symbols;
        }

        private void Emit(out IList<string> s)
        {
            var symbols = new ConcurrentBag<string>();
            var types = new ConcurrentBag<Type>();
            try
            {
                Parallel.ForEach(Types.Where(type => type.IsSealed && type.IsAbstract).Where(type => symbolClassNames.Contains(type.Name)), type =>
                {
                    foreach (var value in type.GetMembers(BindingFlags.Public | BindingFlags.Static).Select(m => m.Name))
                        symbols.Add(value);
                    types.Add(type);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            s = symbols.Distinct().ToList();
        }
    }
}