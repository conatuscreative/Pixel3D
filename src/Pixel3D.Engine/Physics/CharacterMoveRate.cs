using System;
using System.Diagnostics;
using System.IO;
using Pixel3D.Engine.Maths;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Engine.Physics
{
	public class CharacterMoveRate
	{
		public Fraction walkSpeedX, walkSpeedZ, runSpeedX, runSpeedZ;


		public Fraction GetFastestX()
		{
			// Determine fastest move speed on each axis (for heuristic calculation)
			if (walkSpeedX.numerator * runSpeedX.denominator > runSpeedX.numerator * walkSpeedX.denominator)
			{
				return walkSpeedX;
			}
			else
			{
				return runSpeedX;
			}
		}

		public Fraction GetFastestZ()
		{
			// Determine fastest move speed on each axis (for heuristic calculation)
			if (walkSpeedZ.numerator * runSpeedZ.denominator > runSpeedZ.numerator * walkSpeedZ.denominator)
			{
				return walkSpeedZ;
			}
			else
			{
				return runSpeedZ;
			}
		}


		public Fraction GetSlowestX()
		{
			// Determine fastest move speed on each axis (for heuristic calculation)
			if (walkSpeedX.numerator * runSpeedX.denominator < runSpeedX.numerator * walkSpeedX.denominator)
			{
				return walkSpeedX;
			}
			else
			{
				return runSpeedX;
			}
		}

		public Fraction GetSlowestZ()
		{
			// Determine fastest move speed on each axis (for heuristic calculation)
			if (walkSpeedZ.numerator * runSpeedZ.denominator < runSpeedZ.numerator * walkSpeedZ.denominator)
			{
				return walkSpeedZ;
			}
			else
			{
				return runSpeedZ;
			}
		}


		public int GetRunSlideVelocity256()
		{
			// NOTE: When converting to use CharacterMoveRate, retained what appears to be a *2 factor from the actual run speed. Intentional? (should maybe lower friction instead?)
			return (runSpeedX.numerator * (256 * 2)) / runSpeedX.denominator;
		}


		

	}
}