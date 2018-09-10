// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.Serialization.Context
{
	internal static class Constants
	{
		internal const int DefinitionSerializationId = -1;

		// Visited object table index flat format:
		// 0xFFFFFFFF  = null
		// 0xFFFFFFFE  = first visit (read into object)
		// 0x00000000+ = index into visited object table
		// 0x80000000+ = index into definition object table (mask out high bit)
		internal const uint DefinitionVisitFlag = 1u << 31;
		internal const uint VisitNull = uint.MaxValue;
		internal const uint FirstVisit = uint.MaxValue - 1;
	}
}