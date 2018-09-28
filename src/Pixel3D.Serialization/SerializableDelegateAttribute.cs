// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;

namespace Pixel3D.Serialization
{
	/// <summary>All delegate types that can be serialized must be tagged with this attribute.</summary>
	[AttributeUsage(AttributeTargets.Delegate)]
	public sealed class SerializableDelegateAttribute : Attribute
	{
	}
}