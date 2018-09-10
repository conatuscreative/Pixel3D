// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Pixel3D.Extensions;
using System.IO;

namespace Pixel3D
{
	public static class AABBExtensions
	{
		public static void Write(this BinaryWriter bw, AABB aabb)
		{
			bw.Write(aabb.min);
			bw.Write(aabb.max);
		}

		public static AABB ReadAABB(this BinaryReader br)
		{
			AABB aabb;
			aabb.min = br.ReadPosition();
			aabb.max = br.ReadPosition();
			return aabb;
		}
	}
}