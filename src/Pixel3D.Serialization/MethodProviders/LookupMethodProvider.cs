// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Pixel3D.Serialization.MethodProviders
{
	// Generic to support lookups on MethodBuilder
	internal class LookupMethodProvider : MethodProvider
	{
		public readonly Dictionary<Type, MethodInfo> lookup;

		public LookupMethodProvider()
		{
			lookup = new Dictionary<Type, MethodInfo>();
		}

		public LookupMethodProvider(Dictionary<Type, MethodInfo> lookup)
		{
			this.lookup = lookup;
		}

		public override MethodInfo GetMethodForType(Type type)
		{
			MethodInfo method;

			if (lookup.TryGetValue(type, out method))
				return method;

			if (type.IsGenericType)
				if (lookup.TryGetValue(type.GetGenericTypeDefinition(), out method))
				{
					Debug.Assert(method.IsGenericMethodDefinition);
					return method.MakeGenericMethod(type.GetGenericArguments());
				}

			return null;
		}

		public void Add(Type type, MethodInfo method)
		{
			lookup.Add(type, method);
		}
	}
}