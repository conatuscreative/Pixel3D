using System.IO;

namespace Pixel3D
{
	public static class PositionExtensions
	{
		public static void Write(this BinaryWriter bw, Position position)
		{
			bw.Write(position.X);
			bw.Write(position.Y);
			bw.Write(position.Z);
		}

		public static Position ReadPosition(this BinaryReader br)
		{
			Position p;
			p.X = br.ReadInt32();
			p.Y = br.ReadInt32();
			p.Z = br.ReadInt32();
			return p;
		}
	}
}