using System;
using Pixel3D.Maths;

namespace Pixel3D.Physics
{
    public struct ThreeDVelocity
    {
        public PackedVelocity X, Y, Z;
    }


    public static class Ballistics
    {
        // Could modify this to take up and down gravity
        internal static ThreeDVelocity GetVelocityToHit(Position position, Position goal, int clearanceHeight, int gravity256)
        {
            // There is possibly a minor performance argument for doing this in 24.8 instead of 16.16 (mostly due to sqrt), but oh well...

            if(clearanceHeight == 0 && position.Y == goal.Y) // TODO: specify a maximum speed and solve for an arc in the no-clearance case?
                clearanceHeight = 10; // <- Prevents division by zero in the case that there is no Y distance to travel (kinda hacky)

            int topOfArc = Math.Max(position.Y, goal.Y) + clearanceHeight;
            int distanceUp = topOfArc - position.Y;
            int distanceDown = goal.Y - topOfArc;

            Fixed65536 gravity = new Fixed65536(gravity256 << 8);
            Fixed65536 velocityY = Fixed65536.Sqrt((-2 * distanceUp) * gravity);

            Fixed65536 upwardsTime = -velocityY / gravity;
            Fixed65536 downwardsTime = Fixed65536.Sqrt((2 * distanceDown) / gravity);
            Fixed65536 totalTime = upwardsTime + downwardsTime;

            Fixed65536 velocityX = (goal.X - position.X) / totalTime;
            Fixed65536 velocityZ = (goal.Z - position.Z) / totalTime; // TODO: Expensive division (although not relative to sqrt) is not necessary if our caller ignores the result


            ThreeDVelocity result;
            result.X.packedValue = velocityX.value65536 & ~((1<<8)-1); // <- NOTE: We know about the format of `packedValue`
            result.Y.packedValue = velocityY.value65536 & ~((1<<8)-1);
            result.Z.packedValue = velocityZ.value65536 & ~((1<<8)-1);

            return result;
        }

    }
}
