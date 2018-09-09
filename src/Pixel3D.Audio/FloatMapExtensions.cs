namespace Pixel3D.Audio
{
	internal static class FloatMapExtensions
	{
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

		/// <summary>Returns a value clamped between 0 and 1</summary>
		public static float Clamp(this float value)
		{
			if(value < 0f) return 0f;
			if(value > 1f) return 1f;
			return value;
		}
		
		#endregion
	}
}
