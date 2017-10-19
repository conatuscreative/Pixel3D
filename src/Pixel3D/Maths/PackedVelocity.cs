using System.Diagnostics;

namespace Pixel3D.Maths
{
    [DebuggerDisplay("{_RealVelocity}")]
    public struct PackedVelocity
    {
        public PackedVelocity(int velocity256)
        {
            packedValue = velocity256 << velocityShift;
        }

        public void Reset(int velocity256)
        {
            packedValue = velocity256 << velocityShift;
        }


        /// <summary>
        /// Packed value containing 16 bits of the whole part of the velocity (signed),
        /// followed by 8 bits of the fractional part of the velocity, followed by 8 bits
        /// of the fractional part of the position.
        /// </summary>
        public int packedValue;

        const int velocityMask = unchecked((int)0xFFFFFF00u);
        const int velocityShift = 8;
        const int wholeVelocityMask = unchecked((int)0xFFFF0000u);
        const int wholeVelocityShift = 16;
        const int fractionalPositionMask = 0xFF;


        /// <summary>Get the velocity in 24.8 fixed-point format, in pixels/frame</summary>
        public int Velocity256
        {
            get { return packedValue >> velocityShift; }
            set
            {
                Debug.Assert((value << velocityShift) >> velocityShift == value);
                packedValue = (packedValue & fractionalPositionMask) | (value << velocityShift);
            }
        }

        /// <summary>Get the whole-number part of the velocity, in pixels/frame</summary>
        public int WholeVelocity
        {
            get { return packedValue >> wholeVelocityShift; }
            set
            {
                Debug.Assert((value << wholeVelocityShift) >> wholeVelocityShift == value);
                packedValue = (packedValue & fractionalPositionMask) | (value << wholeVelocityShift);
            }
        }


        /// <summary>For debug display only</summary>
        private double _RealVelocity
        {
            get { return Velocity256 / 256.0; }
        }


        /// <summary>Reset the fractional position value</summary>
        public void PositionReset()
        {
            packedValue &= ~fractionalPositionMask;
        }

        public void Reset()
        {
            packedValue = 0;
        }



        /// <summary>Multiply the velocity by a .8 fixed-point value</summary>
        public void Scale256(int scale256)
        {
            int original = Velocity256;
            int scaled = (original * scale256) >> 8; // Fixed point multiply
            if(original == scaled) // Deal with precision break-down at small velocities causing constant velocity under fractional scaling
                scaled -= System.Math.Sign(scaled);
            Velocity256 = scaled;
        }
        
       

        /// <summary>
        /// Tick by one frame, with the given acceleration
        /// </summary>
        /// <param name="originalPosition">The original position</param>
        /// <param name="acceleration">Acceleration in 24.8 fixed-point, in pixels/frame^2</param>
        /// <returns>The updated position</returns>
        public int Update(int position, int acceleration)
        {
            Debug.Assert((position << velocityShift) >> velocityShift == position);

            // Do all of the maths in 24.8 fixed-point:
            position = (position << 8) | (packedValue & fractionalPositionMask);
            int velocity = packedValue >> velocityShift;

            // NOTE: By fixing our time delta at 1 frame, and measuring rates as "per frame",
            //       the 't' and 't^2' values just become 1 and can be elided.

            // p  =  0.5 a t^2  +  v t  + p0
            // NOTE: Doing right shift in place of division by two, to get round-towards negative-infinity.
            position += (acceleration >> 1) + velocity;

            // v = a t + v0
            velocity += acceleration;

            packedValue = (velocity << velocityShift) | (position & fractionalPositionMask);
            return position >> 8;
        }


        /// <summary>
        /// Tick by one frame, with zero acceleration
        /// </summary>
        /// <param name="originalPosition">The original position</param>
        /// <returns>The updated position</returns>
        public int Update(int position)
        {
            Debug.Assert((position << velocityShift) >> velocityShift == position);

            // Do all of the maths in 24.8 fixed-point:
            position = (position << 8) | (packedValue & fractionalPositionMask);
            int velocity = packedValue >> velocityShift;

            // NOTE: By fixing our time delta at 1 frame, and measuring rates as "per frame",
            //       the 't' and 't^2' values just become 1 and can be elided.

            // p  =  v t  + p0
            // NOTE: Doing right shift in place of division by two, to get round-towards negative-infinity.
            position += velocity;

            packedValue = (velocity << velocityShift) | (position & fractionalPositionMask);
            return position >> 8;
        }




        public static PackedVelocity ToReachHeight(int height, int gravity256)
        {
            // v^2 = u^2 + 2as
            // v = 0 at top of jump
            // 0 = u^2 + 2as
            // u = sqrt(-2as)

            // For fixed-point gravity and velocity (this is the old version):
            //
            // u*256 = sqrt(-2 (a*256)/256 s) * 256
            // u*256 = sqrt(-2 (a*256) s) * 16

            // Using fixed-point sqrt:
            //
            // u*256 = sqrt(-2 (a*256)/256 s) * 256
            // u*256 = sqrt(-2 (a*256) s) * 16
            // u*256 = FPSR(-2 (a*256) s)/65536 * 16
            // u*256 = FPSR(-2 (a*256) s)/65536 * 16
            // u*256 = FPSR(-2 (a*256) s) / 4096
            // u*256 = FPSR(-2 (a*256) s) >> 12

            int velocity256 = Fixed65536.Sqrt(-2 * gravity256 * height).value65536 >> 12;

            // NOTE: TEMP: Validate that my maths is more-or-less correct (matches old code) -AR
            Debug.Assert(System.Math.Abs(velocity256 - (int)System.Math.Round(System.Math.Sqrt(-2.0 * gravity256 * height) * 16)) < 10);

            return new PackedVelocity(velocity256);
        }

    }
}