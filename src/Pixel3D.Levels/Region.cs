// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Pixel3D.ActorManagement;
using Pixel3D.Engine;
using Pixel3D.Physics;

namespace Pixel3D.Levels
{
	public class Region
	{
		public int endY = WorldPhysics.MaximumHeight;

		/// <summary>The region in the XZ (floor) plane where the mask is active</summary>
		public MaskData mask;

		/// <summary>Index into navigation info for the given region (expect serialization to set these correctly)</summary>
		public int regionIndex;

		public int startY;

		// Provided to allow parameterless construction (due to presence of deserialization constructor)

		public bool Contains(Actor subject)
		{
			return Contains(subject.position);
		}

		public bool Contains(Position position)
		{
			return position.Y >= startY && position.Y < endY && mask.GetOrDefault(position.X, position.Z);
		}

		public bool ContainsXZ(int x, int z)
		{
			if (mask.GetOrDefault(x, z)) return true;
			return false;
		}
	}
}