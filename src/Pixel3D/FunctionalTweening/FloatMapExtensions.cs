using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Common
{
	public static class FloatMapExtensions
	{
		#region Queries

		public static bool IsBetween(this float value, float min, float max)
		{
			return value >= min && value <= max;
		}
		
		#endregion


		#region Mapping

		public static float MapFrom(this float value, float min, float max)
		{
			float position = value - min;
			float scale = max - min;
			return position / scale;
		}

		public static float MapTo(this float value, float min, float max)
		{
			return min + (max-min) * value;
		}


		#endregion


		#region Functions

		/// <summary>Unclamped linear mapping of the range 0.0 to 0.5 to 1.0, to the values 0, 1, 0.</summary>
		public static float Triangle(this float x)
		{
			if(x < 0.5f)
				return x * 2f;
			else
				return 2f - (x * 2f);
		}

		public static float StepAt(this float value, float at)
		{
			return (value < at) ? 0f : 1f;
		}

		/// <summary>Returns a value clamped between 0 and 1</summary>
		public static float Clamp(this float value)
		{
			if(value < 0f) return 0f;
			if(value > 1f) return 1f;
			return value;
		}

		/// <summary>Returns a value clamped between min and max.</summary>
		public static float Clamp(this float value, float min, float max)
		{
			Debug.Assert(min <= max);
			if(value < min) return min;
			if(value > max) return max;
			return value;
		}

		public static float Pulse(this float value)
		{
			if(value > 1f || value < 0f) return 0f;
			else return 1f;
		}

		public static float ClampOff(this float value)
		{
			if(value > 1f || value < 0f) return 0f;
			return value;
		}

		public static float Sawtooth(this float value)
		{
			float r = (float)Math.IEEERemainder(value, 1.0);
			if(r < 0f)
				return r+1f;
			else
				return r;
		}

		#endregion

	}
}
