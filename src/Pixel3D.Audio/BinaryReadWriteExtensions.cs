using System.IO;

namespace Pixel3D.Audio
{
	internal static class BinaryReadWriteExtensions
	{
		public static float? ReadNullableSingle(this BinaryReader br)
		{
			if (br.ReadBoolean())
				return br.ReadSingle();
			else
				return null;
		}

		public static void WriteNullableSingle(this BinaryWriter bw, float? value)
		{
			if (bw.WriteBoolean(value.HasValue))
				bw.Write(value.Value);
		}

		public static string ReadNullableString(this BinaryReader br)
		{
			if (br.ReadBoolean())
				return br.ReadString();
			else
				return null;
		}

		public static void WriteNullableStringNonBlank(this BinaryWriter bw, string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				value = null;

			if (bw.WriteBoolean(value != null))
				bw.Write(value);
		}

		public static bool WriteBoolean(this BinaryWriter bw, bool value)
		{
			bw.Write(value);
			return value; // Returning the written value allows for easy null checks
		}
	}
}