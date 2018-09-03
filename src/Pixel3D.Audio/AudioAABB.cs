namespace Pixel3D.Audio
{
	public struct AudioAABB
	{
		public AudioAABB(Position min, Position max)
		{
			this.min = min;
			this.max = max;
		}

		public AudioAABB(int left, int right, int bottom, int top, int front, int back)
		{
			this.min = new Position(left, bottom, front);
			this.max = new Position(right, top, back);
		}

		public Position min, max;

		#region Faces

		public int Left { get { return min.X; } }
		public int Right { get { return max.X; } }
		public int Bottom { get { return min.Y; } }
		public int Top { get { return max.Y; } }
		public int Front { get { return min.Z; } }
		public int Back { get { return max.Z; } }

		#endregion
		
		public int DistanceSquaredTo(Position position)
		{
			int xDistance = 0;
			if (position.X < min.X)
				xDistance = min.X - position.X;
			else if (position.X > max.X)
				xDistance = position.X - max.X;

			int yDistance = 0;
			if (position.Y < min.Y)
				yDistance = min.Y - position.Y;
			else if (position.Y > max.Y)
				yDistance = position.Y - max.Y;

			int zDistance = 0;
			if (position.Z < min.Z)
				zDistance = min.Z - position.Z;
			else if (position.Z > max.Z)
				zDistance = position.Z - max.Z;

			return xDistance * xDistance + yDistance * yDistance + zDistance * zDistance;
		}
	}
}