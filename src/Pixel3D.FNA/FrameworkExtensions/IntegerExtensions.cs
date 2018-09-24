using System;
using System.Diagnostics;

namespace Pixel3D.FrameworkExtensions // <- include with Int32 and friends
{
    public static class IntegerExtensions
    {
        /// <summary>Clamp a value between min and max, NOTE: Inclusive!</summary>
        public static int Clamp(this int v, int min, int max)
        {
            Debug.Assert(min <= max);
            return Math.Max(min, Math.Min(max, v));
        }

        /// <summary>Clamp a value between min and max, NOTE: Inclusive!</summary>
        public static uint Clamp(this uint v, uint min, uint max)
        {
            Debug.Assert(min <= max);
            return Math.Max(min, Math.Min(max, v));
        }


        /// <summary>Count the number of set bits in an integer</summary>
        public static uint CountBitsSet(this uint v)
        {
            // https://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel
            v = v - ((v >> 1) & 0x55555555u);                        // reuse input as temporary
            v = (v & 0x33333333u) + ((v >> 2) & 0x33333333u);        // temp
            return ((v + (v >> 4) & 0xF0F0F0Fu) * 0x1010101u) >> 24; // count
        }

        /// <summary>Count the number of set bits in an integer</summary>
        public static int CountBitsSet(this int v)
        {
            return (int)CountBitsSet((uint)v);
        }


        /// <summary>Round up for positive integers</summary>
        public static int PositiveRoundUp(this int v, int toNearest)
        {
            return ((v + toNearest - 1) / toNearest) * toNearest;
        }


    }
}
