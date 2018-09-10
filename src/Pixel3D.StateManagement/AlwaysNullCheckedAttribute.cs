// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D.StateManagement
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class AlwaysNullCheckedAttribute : Attribute
	{
	}
}