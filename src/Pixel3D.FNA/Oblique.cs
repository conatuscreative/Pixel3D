using System;
using System.IO;

namespace Pixel3D
{
    // NOTE: This enum has special meaning when cast to int
    public enum Oblique
    {
        /// <summary>Oblique projection extending backwards and to the left at 45 degrees</summary>
        Left = -1, // (for each Y += 1, X -= 1)

        /// <summary>Oblique projection extending directly backwards</summary>
        Straight = 0,

        /// <summary>Oblique projection extending backwards and to the right at 45 degrees</summary>
        Right = 1, // (for each Y += 1, X += 1)
    }


    public static class ObliqueExtensions
    {
        public static void Write(this BinaryWriter bw, Oblique oblique)
        {
            bw.Write((sbyte)oblique);
        }

        public static Oblique ReadOblique(this BinaryReader br)
        {
            return (Oblique)br.ReadSByte();
        }

        public static string ToFancyString(this Oblique oblique, bool straightMeansFlat = false)
        {
            switch (oblique)
            {
                case Oblique.Left:
                    return "Left ↖";
                case Oblique.Straight:
                    return straightMeansFlat ? "Flat ↑" : "Straight ↑";
                case Oblique.Right:
                    return "Right ↗";
                default:
                    throw new ArgumentOutOfRangeException("oblique");
            }
        }
    }

}
