using System;

namespace Pixel3D.Animations
{
	public class DepthSlice : IEquatable<DepthSlice>
	{
		public int xOffset;
		public int zOffset;
		public FrontBack[] depths;

		public int Width { get { return depths.Length; } }

		public static DepthSlice CreateBlank(int xOffset, int zOffset)
		{
			return new DepthSlice { xOffset = xOffset, zOffset = zOffset, depths = new FrontBack[1] };
		}


		public bool Equals(DepthSlice other)
		{
			if(xOffset != other.xOffset || zOffset != other.zOffset || depths.Length != other.depths.Length)
			{
				return false;
			}

			// Too lazy to import memcmp...
			for(int i = 0; i < depths.Length; i++)
				if(depths[i].front != other.depths[i].front || depths[i].back != other.depths[i].back)
					return false;

			return true;
		}

	}
}