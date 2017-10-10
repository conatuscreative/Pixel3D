using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pixel3D
{
    public struct Range
    {
        public Range(int start, int end)
        {
            this.start = start;
            this.end = end;
        }

        /// <summary>Inclusive start</summary>
        public int start;

        /// <summary>Exclusive end</summary>
        public int end;


        public int Size { get { return end - start; } }


        public bool Contains(int position)
        {
            return start <= position && position < end;
        }

        public bool Contains(int otherStart, int otherEnd)
        {
            return !(end <= otherStart || otherEnd <= start);
        }


        /// <summary>Flip the range around the centre of 0 (ie: a range containing only 0 returns the same range)</summary>
        public Range Flipped { get { return new Range(1-end, 1-start); } }

        public Range MaybeFlip(bool flip) { return flip ? Flipped : this; }


        public static Range operator+(Range lhs, int rhs)
        {
            return new Range(lhs.start + rhs, lhs.end + rhs);
        }

        public static Range operator-(Range lhs, int rhs)
        {
            return new Range(lhs.start - rhs, lhs.end - rhs);
        }


        public static bool Overlaps(Range a, Range b)
        {
            return !(a.end <= b.start || b.end <= a.start);
        }


        public Range Clip(int clipStart, int clipEnd)
        {
            return new Range(Math.Max(start, clipStart), Math.Min(end, clipEnd));
        }



        /// <summary>
        /// Given a range that we want data for, return an expanded range that we need to capture data from,
        /// such that a Minkowski expansion operation gives the data for the original range.
        /// </summary>
        public Range ExpandForMinkowski(int addStart, int addEnd)
        {
            return new Range(this.start + addStart, this.end + addEnd - 1); // -1 because we're adding two exclusive ranges that effectivly overlap
        }

    }
}
