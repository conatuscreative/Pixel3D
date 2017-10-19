using System;

namespace Pixel3D.Core
{
	/// <summary>
    /// Random number generator that requires significantly less state than Random
    /// </summary>
    public class XorShift
    {
        public XorShift() : this((UInt64)Environment.TickCount) { }

        /// <summary>Copy the seed from another RNG, without mutating it</summary>
        public XorShift(XorShift readSeedFrom) : this((UInt64)readSeedFrom.x) { }

        public XorShift(UInt64 seed)
        {
            if(seed == 0)
                seed = 1234567890; // <- cannot be zero

            this.x = seed;

            // Not sure how many times we need to do this (if at all) -AR:
            NextUInt64();
            NextUInt64();
            NextUInt64();
            NextUInt64();
        }


        // 64-bit RNG. We could do 32-bit, but I can't find the magic values for it (or info on just how much quality is lost by going 32-bit) -AR
        UInt64 x;

        public UInt64 NextUInt64()
        {
            x ^= (x << 21);
            x ^= (x >> 35);
            x ^= (x << 4);
            return x;
        }


        #region Next methods

        public Int64 NextInt64()
        {
            return (long)(NextUInt64() & Int64.MaxValue);
        }

        /// <summary>Return a random integer in the range [0 .. Int32.MaxValue]</summary>
        public Int32 Next()
        {
            return (int)(NextUInt64() & Int32.MaxValue);
        }

        /// <summary>Return a random integer in the range [0 .. maxValue-1]</summary>
        public Int32 Next(int maxValue)
        {
            if(maxValue == 0)
                return 0;
            if(maxValue < 0)
                throw new ArgumentOutOfRangeException("maxValue");

            if(maxValue > 0x7FFFFF) // <- guess at a good value -AR
                return InternalWideNextInt32(0, maxValue);

            return Next() % maxValue; // Do this in 32 bits in the common case
        }

        /// <summary>Return a random integer in the range [minValue .. maxValue-1]</summary>
        public Int32 Next(int minValue, int maxValue)
        {
            if(maxValue < minValue)
                throw new ArgumentOutOfRangeException();

            uint range = (uint)((long)maxValue - (long)minValue);
            if(range == 0)
                return 0;
            if(range > 0x7FFFFFu) // <- guess at a good value -AR
                return InternalWideNextInt32(minValue, maxValue);

            return (Next() % (int)range) + minValue; // Do this in 32 bits in the common case
        }

        /// <summary>Return a random integer in the range [minValue .. maxValue-1], or minValue if the range is invalid</summary>
        public Int32 NextFailSafe(int minValue, int maxValue)
        {
            if(maxValue < minValue)
                return minValue;
            return Next(minValue, maxValue);
        }

        private Int32 InternalWideNextInt32(int minValue, int maxValue)
        {
            long range = (long)maxValue - (long)minValue;
            return (int)((NextInt64() % range) + (long)minValue);
        }

        /// <summary>Return a random boolean with a 50% chance of being true</summary>
        public bool NextBoolean()
        {
            return ((uint)NextUInt64() & 1) != 0;
        }

        /// <summary>Return a random boolean with a N% chance (0-100) of being true</summary>
        public bool NextBoolean(int n)
        {
            return Next() % 100 < n;
        }

        /// <summary>Return a random boolean with a N/255 chance of being true</summary>
        public bool NextBool255(int n)
        {
            return (((int)NextUInt64()) & 0xFF) < n;
        }

        /// <summary>Get a single-precision floating-point number between 0 and 1. IMPORTANT: the only safe time to use this is for direct local output (ie: immediately playing a sound)</summary>
        public float _NetworkUnsafe_UseMeForAudioOnly_NextSingle()
        {
            return (float)((double)Next() / (double)Int32.MaxValue);
        }

        #endregion

    }
}
