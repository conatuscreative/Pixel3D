// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D.Serialization
{
	/// <summary>
	///     The serializer should ignore any delegates instantiated by this method (the delegates created should never be
	///     stored in a serialized field).
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
	public sealed class SerializationIgnoreDelegatesAttribute : Attribute
	{
	}
}