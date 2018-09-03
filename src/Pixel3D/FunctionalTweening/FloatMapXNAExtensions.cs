using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Common
{
	public static class FloatMapXNAExtensions
	{
		/// <summary>Map a float between two colours</summary>
		public static Color MapTo(this float value, Color zero, Color one)
		{
			return Color.Lerp(zero, one, value);
		}

		/// <summary>Map a float between two Vector2s</summary>
		public static Vector2 MapTo(this float value, Vector2 zero, Vector2 one)
		{
			return Vector2.Lerp(zero, one, value);
		}
	}
}
