// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Reflection;

namespace Pixel3D.Serialization.Discovery
{
	/// <summary>Represents the instantiation of a delegate.</summary>
	internal struct DelegateUsage
	{
		/// <summary>The static field type of the delegate Target at the instantiation site, or null for no target</summary>
		public Type targetType;

		public MethodInfo delegateMethod;
		public Type delegateType;
	}
}