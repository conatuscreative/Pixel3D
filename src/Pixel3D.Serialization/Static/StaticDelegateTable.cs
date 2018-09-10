// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using Pixel3D.Serialization.BuiltIn.DelegateHandling;

namespace Pixel3D.Serialization.Static
{
	internal static class StaticDelegateTable
	{
		internal static Dictionary<Type, DelegateTypeInfo> delegateTypeTable;
	}
}