using System.IO;
using Pixel3D.Extensions;

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