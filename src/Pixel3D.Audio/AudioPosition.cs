using System;

namespace Pixel3D.Audio
{
	public struct AudioPosition : IEquatable<AudioPosition>
	{
		public int X;
		public int Y;
		public int Z;

		public AudioPosition(int x, int y, int z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}

		#region Predefined Positions

		public static AudioPosition Zero { get { return new AudioPosition(); } }
		public static AudioPosition UnitX { get { return new AudioPosition(1, 0, 0); } }
		public static AudioPosition UnitY { get { return new AudioPosition(0, 1, 0); } }
		public static AudioPosition UnitZ { get { return new AudioPosition(0, 0, 1); } }

		#endregion

		public static int DistanceSquared(AudioPosition a, AudioPosition b)
		{
			return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z);
		}

		public override bool Equals(object obj)
		{
			return obj is AudioPosition position && Equals(position);
		}

		public bool Equals(AudioPosition other)
		{
			return X == other.X &&
				   Y == other.Y &&
				   Z == other.Z;
		}

		public override int GetHashCode()
		{
			var hashCode = 373119288;
			hashCode = hashCode * -1521134295 + X.GetHashCode();
			hashCode = hashCode * -1521134295 + Y.GetHashCode();
			hashCode = hashCode * -1521134295 + Z.GetHashCode();
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