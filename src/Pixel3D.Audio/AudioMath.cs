namespace Pixel3D.Audio
{
	public static class AudioMath
	{
		//  Microsoft.Xna.Framework.MathHelpers.Lerp
		public static float Lerp(float value1, float value2, float amount)
		{
			return value1 + (value2 - value1) * amount;
		}

		//  Microsoft.Xna.Framework.MathHelpers.Lerp
		public static float Clamp(float value, float min, float max)
		{
			// First we check to see if we're greater than the max.
			value = (value > max) ? max : value;

			// Then we check to see if we're less than the min.
			value = (value < min) ? min : value;

			// There's no check to see if min > max.
			return value;
		}
	}
}