using System;
using System.Diagnostics;

namespace CRTSim
{
	internal static class IntegerExtensions
	{
		/// <summary>Clamp a value between min and max, NOTE: Inclusive!</summary>
		public static int Clamp(this int v, int min, int max)
		{
			Debug.Assert(min <= max);
			return Math.Max(min, Math.Min(max, v));
		}
	}
}