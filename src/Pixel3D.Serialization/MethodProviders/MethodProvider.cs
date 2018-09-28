// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Reflection;

namespace Pixel3D.Serialization.MethodProviders
{
	internal abstract class MethodProvider
	{
	    public MethodInfo this[Type type]
	    {
	        get { return GetMethodForType(type); }
	    }

	    public abstract MethodInfo GetMethodForType(Type type);
	}
}