// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D
{
	/// <summary>Position in world space. X+ is right, Y+ is up, Z+ is backwards (into the screen).</summary>
	public struct Position
	{
		/// <summary>Horizontal position in pixels</summary>
		public int X;

		/// <summary>Vertical position in pixels</summary>
		public int Y;

		/// <summary>Depth position in pixels (into the screen)</summary>
		public int Z;


		public Position(int x, int y, int z = 0)
		{
			X = x;
			Y = y;
			Z = z;
		}

		#region Object Overrides and Equality

		public override string ToString()
		{
			return string.Format("{{X:{0} Y:{1} Z:{2}}}", X, Y, Z);
		}

		public static bool operator ==(Position a, Position b)
		{
			return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
		}

		public static bool operator !=(Position a, Position b)
		{
			return a.X != b.X || a.Y != b.Y || a.Z != b.Z;
		}

		public override bool Equals(object obj)
		{
			if (obj is Position)
				return (Position) obj == this;
			return false;
		}

		public override int GetHashCode()
		{
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
		}

		#endregion

		#region Predefined Positions

		public static Position Zero
		{
			get { return new Position(); }
		}

		public static Position UnitX
		{
			get { return new Position(1, 0, 0); }
		}

		public static Position UnitY
		{
			get { return new Position(0, 1, 0); }
		}

		public static Position UnitZ
		{
			get { return new Position(0, 0, 1); }
		}

		#endregion

		#region Operators

		public static Position operator -(Position a)
		{
			return new Position(-a.X, -a.Y, -a.Z);
		}

		public static Position operator +(Position a, Position b)
		{
			return new Position(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		public static Position operator -(Position a, Position b)
		{
			return new Position(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		}

		public static Position operator *(Position a, int b)
		{
			return new Position(a.X * b, a.Y * b, a.Z * b);
		}

		#endregion

		#region Coordinate System Conversions

		/// <summary>Mirror on the X axis</summary>
		public Position FlipX
		{
			get { return new Position(-X, Y, Z); }
		}

		public Position MaybeFlipX(bool flipX)
		{
			return flipX ? FlipX : this;
		}

		#endregion

		#region Distance and Length

		public int LengthSquared
		{
			get { return X * X + Y * Y + Z * Z; }
		}

		public static int DistanceSquared(Position a, Position b)
		{
			return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z);
		}

		public int ManhattenLength
		{
			get { return Math.Abs(X) + Math.Abs(Y) + Math.Abs(Z); }
		}

		public static int ManhattenDistance(Position a, Position b)
		{
			return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
		}


		/// <param name="bestDistanceSquared">For best results, start with int.MaxValue</param>
		public static bool Closer(ref Position a, ref Position b, ref int bestDistanceSquared)
		{
			var distanceSquared = (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z);
			if (distanceSquared < bestDistanceSquared)
			{
				bestDistanceSquared = distanceSquared;
				return true;
			}

			return false;
		}

		/// <param name="worstDistanceSquared">For best results, start with int.MinValue</param>
		public static bool Further(ref Position a, ref Position b, ref int worstDistanceSquared)
		{
			var distanceSquared = (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z);
			if (distanceSquared > worstDistanceSquared)
			{
				worstDistanceSquared = distanceSquared;
				return true;
			}

			return false;
		}

		#endregion
	}
}