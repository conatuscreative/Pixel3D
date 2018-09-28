// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Audio
{
	internal static class FloatMapExtensions
	{
		#region Functions

		/// <summary>Returns a value clamped between 0 and 1</summary>
		public static float Clamp(this float value)
		{
			if (value < 0f) return 0f;
			if (value > 1f) return 1f;
			return value;
		}

		#endregion

		#region Mapping

		public static float MapFrom(this float value, float min, float max)
		{
			var position = value - min;
			var scale = max - min;
			return position / scale;
		}

		public static float MapTo(this float value, float min, float max)
		{
			return min + (max - min) * value;
		}

		#endregion
	}
}