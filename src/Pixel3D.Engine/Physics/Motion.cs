using Pixel3D.Physics;

namespace Pixel3D.Engine.Physics
{
	public static class Motion
    {
        public static MotionResult PhysicsStepVertical(ref CharacterPhysicsInfo cpi,
                ref Position position, ref ThreeDVelocity velocity,
                ref bool onGround,
                int coefficientOfRestitution256,
                int gravityUp256, int gravityDown256, bool lockVertical,
                WorldPhysics physics, bool groundIsAPit)
        {
            if(lockVertical)
            {
                velocity.Y.Reset();
            }
            else
            {
                if(velocity.Y.Velocity256 > 0) // Moving upwards
                {
                    onGround = false;

                    int startY = position.Y + cpi.height;
                    position.Y = velocity.Y.Update(position.Y, gravityUp256);
                    int endY = position.Y + cpi.height;

                    if(endY > startY)
                    {
                        int ceiling = physics.GetCeilingHeightInXRange(position.X + cpi.startX, position.X + cpi.endX, position.Z, startY, endY, cpi.owner);
                        if(ceiling < endY) // Hit ceiling
                        {
                            position.Y = ceiling - cpi.height;
                            velocity.Y.Velocity256 = -((velocity.Y.Velocity256 * coefficientOfRestitution256) >> 8); // <- Bounce (fixed-point multiply)
                            return MotionResult.HitCeiling;
                        }
                    }
                }
                else // Moving downwards
                {
                    int groundHeight = CharacterPhysics.GroundHeight(ref cpi, physics, position);

                    if(position.Y > groundHeight)
                        onGround = false; // we came off the ground
                    else if(position.Y < groundHeight) // TODO: This needs some rethinking (added to let separation do its thing)
                        onGround = true; // we are embedded in the ground (we may start physics in this state, if we are put here by teleport/animation)
                    else if(position.Y == groundHeight && velocity.Y.Velocity256 == 0)
                        onGround = true; // we were gently placed on the ground (don't trigger OnHitGround)

                    if(!onGround)
                    {
                        position.Y = velocity.Y.Update(position.Y, gravityDown256);

                        if(position.Y <= groundHeight) // Hit the ground
                        {
                            position.Y = groundHeight;

                            if(groundHeight == 0 && groundIsAPit)
                            {
                                velocity = new ThreeDVelocity();
                                return MotionResult.HitBottomOfPit;
                            }
                            else if(velocity.Y.Velocity256 > -128) // <- Kill velocity of we're moving too slowly downwards (start rolling)
                            {
                                onGround = true;
                                velocity.Y.Reset();
                            }
                            else
                            {
                                onGround = false;
                                velocity.Y.Scale256(-coefficientOfRestitution256);
                                velocity.X.Scale256(coefficientOfRestitution256);
                                velocity.Z.Scale256(coefficientOfRestitution256);
                            }

                            return MotionResult.HitGround;
                        }
                    }
                }
            }

            return MotionResult.None;
        }


        public static MotionResult PhysicsStepHorizontal(ref CharacterPhysicsInfo cpi,
                ref Position position, ref ThreeDVelocity velocity,
                bool onGround,
                int coefficientOfRestitution256,
                int groundFriction256,
                WorldPhysics physics)
        {
            MotionResult result = MotionResult.None;

            int newPositionX = velocity.X.Update(position.X);
            int newPositionZ = velocity.Z.Update(position.Z);
            int deltaX = newPositionX - position.X;
            int deltaZ = newPositionZ - position.Z;

            if(deltaX != 0)
            {
                if(CharacterPhysics.TryMoveHorizontalDidHitWall(ref cpi, physics, deltaX, 0, onGround, ref position))
                {
                    // Hit wall, bounce:
                    velocity.X.Scale256(-coefficientOfRestitution256);
                    result = MotionResult.HitWall;
                }
            }

            // Z-motion just stops if it hits anything
            if(deltaZ != 0 && CharacterPhysics.TryMoveHorizontalDidHitWall(ref cpi, physics, 0, deltaZ, onGround, ref position))
            {
                velocity.Z.Reset();
                // NOTE: Currently not even bothering to give a hit result, just carry on...
            }

            // TODO: Slope handling for rolling objects

            if(onGround)
            {
                // Friction:
                velocity.X.Scale256(groundFriction256);
                velocity.Z.Scale256(groundFriction256);
            }

            return result;
        }



    }


}
