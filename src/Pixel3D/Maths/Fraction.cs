using System.Diagnostics;

namespace Pixel3D.Maths
{
    public struct Fraction
    {
        public int numerator, denominator;

        public Fraction(int numerator, int denominator)
        {
            this.numerator = numerator;
            this.denominator = denominator;
        }


        public static Fraction operator +(Fraction f)
        {
            return f;
        }

        public static Fraction operator -(Fraction f)
        {
            f.numerator = -f.numerator;
            return f;
        }


        public static int MultiplyDivideTruncate(int value, Fraction multiply, Fraction divide)
        {
            // Hoping this doesn't overflow, obviously...
            return (value * multiply.numerator * divide.denominator) / (multiply.denominator * divide.numerator);
        }

        public static int MultiplyTruncate(int value, Fraction multiply)
        {
            return (value * multiply.numerator) / multiply.denominator;
        }

        public int MultiplyTruncate(int value)
        {
            return (value * numerator) / denominator;
        }

        public int InverseMultiplyTruncate(int value)
        {
            return (value * denominator) / numerator;
        }



        // Only works for positive fractions
        public int GetPositiveWholeCeiling()
        {
            Debug.Assert(numerator >= 0 && denominator > 0);
            return (numerator + denominator - 1) / denominator;
        }
    }
}
