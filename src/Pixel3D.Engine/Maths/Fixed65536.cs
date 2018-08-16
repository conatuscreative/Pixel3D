using System;

namespace Pixel3D.Engine.Maths
{
    /// <summary>A signed 16.16 fixed-point number</summary>
    public struct Fixed65536
    {
        public int value65536;

        public int WholePart { get { return value65536 >> 16; } }
        public int Fraction65536 { get { return value65536 & ((1 << 16)-1); } }

        public int WholePartRoundedUp { get { return (value65536 + 65535) >> 16; } }


        #region Construction

        /// <summary>Create from a value already in 16.16 fixed-point format</summary>
        public Fixed65536(int value65536)
        {
            this.value65536 = value65536;
        }

        public Fixed65536(int wholePart, uint fraction65536)
        {
            value65536 = (wholePart << 16) + (int)fraction65536;
        }

        public static implicit operator Fixed65536(int v)
        {
            return new Fixed65536 { value65536 = v << 16 };
        }

        public static Fixed65536 FromDivision(long numerator, long denominator)
        {
            return new Fixed65536((int)((numerator << 32) / (denominator << 16)));
        }

        public static Fixed65536 FromPercentage(int percent100)
        {
            return new Fixed65536(((1 << 16) * percent100) / 100);
        }

        public static Fixed65536 FromPermille(int permille1000)
        {
            return new Fixed65536(((1 << 16) * permille1000) / 1000);
        }

        public static Fixed65536 FromPermyriad(int permyriad10000)
        {
            return new Fixed65536(((1 << 16) * permyriad10000) / 10000);
        }

        #endregion



        #region Object overrides

        // Mostly for debug output:
        public override string ToString()
        {
            return ((double)value65536 / 65536.0).ToString();
        }

        public override int GetHashCode()
        {
            return value65536.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if(obj is Fixed65536)
                return ((Fixed65536)obj).value65536 == this.value65536;
            return false;
        }

        #endregion



        #region Arithmatic

        public static Fixed65536 operator +(Fixed65536 x)
        {
            return x;
        }

        public static Fixed65536 operator -(Fixed65536 x)
        {
            return new Fixed65536(-x.value65536);
        }

        public static Fixed65536 operator +(Fixed65536 x, Fixed65536 y)
        {
            return new Fixed65536(x.value65536 + y.value65536);
        }

        public static Fixed65536 operator -(Fixed65536 x, Fixed65536 y)
        {
            return new Fixed65536(x.value65536 - y.value65536);
        }

        public static Fixed65536 operator *(Fixed65536 x, Fixed65536 y)
        {
            return new Fixed65536((int)(((long)x.value65536 * (long)y.value65536) >> 16));
        }

        public static Fixed65536 operator *(Fixed65536 x, int y)
        {
            return new Fixed65536(x.value65536 * y);
        }

        public static Fixed65536 operator *(int x, Fixed65536 y)
        {
            return new Fixed65536(x * y.value65536);
        }

        public static Fixed65536 operator /(Fixed65536 x, Fixed65536 y)
        {
            return new Fixed65536((int)((((long)x.value65536 << 16) / (long)y.value65536)));
        }

        public static Fixed65536 operator >>(Fixed65536 x, int shift)
        {
            return new Fixed65536(x.value65536 >> shift);
        }

        public static Fixed65536 operator <<(Fixed65536 x, int shift)
        {
            return new Fixed65536(x.value65536 << shift);
        }

        #endregion



        #region Square Root

        // Based on https://github.com/chmike/fpsqrt

        public static Fixed65536 Sqrt(Fixed65536 v)
        {
            if(v.value65536 < 0)
                throw new InvalidOperationException();

            uint r = (uint)v.value65536;
            uint b = 0x40000000;
            uint q = 0;
            while(b > 0x40)
            {
                uint t = q + b;
                if(r >= t)
                {
                    r -= t;
                    q = t + b; // equivalent to q += 2*b
                }
                r <<= 1;
                b >>= 1;
            }
            q >>= 8;
            return new Fixed65536((int)q);
        }

        public static Fixed65536 Sqrt(int v)
        {
            if(v < 0)
                throw new ArgumentOutOfRangeException();
            if(v == 0)
                return 0;

            uint r = (uint)v;
            uint b = 0x40000000;
            uint q = 0;
            while(b > 0)
            {
                uint t = q + b;
                if(r >= t)
                {
                    r -= t;
                    q = t + b; // equivalent to q += 2*b
                }
                r <<= 1;
                b >>= 1;
            }
            if(r >= q) ++q;
            return new Fixed65536((int)q);
        }

        #endregion



        #region Comparison

        public static bool operator ==(Fixed65536 x, Fixed65536 y)
        {
            return x.value65536 == y.value65536;
        }

        public static bool operator !=(Fixed65536 x, Fixed65536 y)
        {
            return x.value65536 != y.value65536;
        }

        public static bool operator >(Fixed65536 x, Fixed65536 y)
        {
            return x.value65536 > y.value65536;
        }

        public static bool operator <(Fixed65536 x, Fixed65536 y)
        {
            return x.value65536 < y.value65536;
        }

        public static bool operator >=(Fixed65536 x, Fixed65536 y)
        {
            return x.value65536 >= y.value65536;
        }

        public static bool operator <=(Fixed65536 x, Fixed65536 y)
        {
            return x.value65536 <= y.value65536;
        }

        #endregion



        #region Trig

        // 16 values is one cache line (assuming we are aligned, which we probably aren't) -- unfortunately we need 17
        /// <summary>Angles for the first quarter period of sine, in 16.16 fixed-point</summary>
        private static int[] trigTable = { 0, 6423, 12785, 19024, 25079, 30893, 36409, 41575, 46340, 50660, 54491, 57797, 60547, 62714, 64276, 65220, 65536 };

        /// <param name="angle">Angle specified as an integer multiple of Tau/64 (or Pi/32)</param>
        public static Fixed65536 Sin64thTau(int angle)
        {
            int result = ((angle & 16) == 0)
                    ? trigTable[angle & 15]
                    : trigTable[16 - (angle & 15)];

            if((angle & 32) != 0)
                result = -result;

            return new Fixed65536(result);
        }

        /// <param name="angle">Angle specified as an integer multiple of Tau/64 (or Pi/32)</param>
        public static Fixed65536 Cos64thTau(int angle)
        {
            return Sin64thTau((angle & 63) + 16);
        }

        #endregion



        #region Rounding

        /// <summary>Round up for positive values</summary>
        public Fixed65536 PositiveRoundUp(Fixed65536 toNearest)
        {
            int tn = toNearest.value65536;
            return new Fixed65536(((value65536 + tn - 1) / tn) * tn);
        }

        #endregion


    }
}
