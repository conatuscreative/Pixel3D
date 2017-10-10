using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace Common
{
	public static class FloatEaseExtensions
	{
		public static float SmoothStep(this float x)
		{
			return x * x * (3 - 2*x);
		}

		public static float EaseInQuad(this float x) { return x * x; }
		public static float EaseOutQuad(this float x) { return -(x * (x-2)); }
		public static float EaseInOutQuad(this float x)
		{
			return 0.5f * ((x *= 2f) < 1 ? (x*x) : -(((x-1)) * (x-3) - 1));
		}
	}
}
