using System;

namespace Pixel3D.Audio
{
	public struct AudioPosition : IEquatable<AudioPosition>
	{
		public int x;
		public int y;
		public int z;

		public AudioPosition(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static int DistanceSquared(AudioPosition a, AudioPosition b)
		{
			return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z);
		}

		public override bool Equals(object obj)
		{
			return obj is AudioPosition && Equals((AudioPosition)obj);
		}

		public bool Equals(AudioPosition other)
		{
			return x == other.x &&
				   y == other.y &&
				   z == other.z;
		}

		public override int GetHashCode()
		{
			var hashCode = 373119288;
			hashCode = hashCode * -1521134295 + x.GetHashCode();
			hashCode = hashCode * -1521134295 + y.GetHashCode();
			hashCode = hashCode * -1521134295 + z.GetHashCode();
			return hashCode;
		}

		public static bool operator ==(AudioPosition position1, AudioPosition position2)
		{
			return position1.Equals(position2);
		}

		public static bool operator !=(AudioPosition position1, AudioPosition position2)
		{
			return !(position1 == position2);
		}
	}
}