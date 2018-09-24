using System.IO;

namespace Microsoft.Xna.Framework // Put these methods in the same namespace of their types
{
    public static class BinaryReadWriteXNAExtensions
    {
        public static Color ReadColor(this BinaryReader br)
        {
            Color c = new Color();
            c.PackedValue = br.ReadUInt32();
            return c;
        }

        public static void Write(this BinaryWriter bw, Color c)
        {
            bw.Write(c.PackedValue);
        }



        public static Point ReadPoint(this BinaryReader br)
        {
            Point p;
            p.X = br.ReadInt32();
            p.Y = br.ReadInt32();
            return p;
        }

        public static void Write(this BinaryWriter bw, Point p)
        {
            bw.Write(p.X);
            bw.Write(p.Y);
        }


        public static Rectangle ReadRectangle(this BinaryReader br)
        {
            Rectangle r;
            r.X = br.ReadInt32();
            r.Y = br.ReadInt32();
            r.Width = br.ReadInt32();
            r.Height = br.ReadInt32();
            return r;
        }

        public static void Write(this BinaryWriter bw, Rectangle r)
        {
            bw.Write(r.X);
            bw.Write(r.Y);
            bw.Write(r.Width);
            bw.Write(r.Height);
        }

    }
}
