using System;
using System.Diagnostics;
using Pixel3D.Animations;

namespace Pixel3D
{
    public struct CharacterPhysicsInfo
    {
        public int startX, endX, height, stairStepMaxHeight;

        /// <summary>Avoid self-collision</summary>
        public object owner;


        public CharacterPhysicsInfo(AnimationSet animationSet, object owner)
        {
            // TODO: BUG: This does not take into account facing direction!!
            // TODO: Consider putting this as configurable data on AnimationSet (eg: crates with different CP vs solid physics)
            this.startX = animationSet.physicsStartX;
            this.endX = animationSet.physicsEndX;
            this.height = animationSet.physicsHeight;
            this.stairStepMaxHeight = CharacterPhysics.globalStairStepMaxHeight;
            this.owner = owner;
        }
    }



    public static class CharacterPhysics
    {
        // TODO: Remove Me?
        /// <summary>The inclusive number of pixels we may move upwards to climb stairs. Or downwards to remain stuck to the floor.</summary>
        public const int globalStairStepMaxHeight = 6;


        #region Ground Info

        public static int GroundHeight(ref CharacterPhysicsInfo info, WorldPhysics world, Position position, bool staticOnly = false)
        {
            int startX = position.X + info.startX;
            int endX = position.X + info.endX;

            return world.GetGroundHeightInXRange(startX, endX, position.Z, position.Y, position.Y + info.height, info.owner, staticOnly);
        }

        /// <summary>Determine whether a position is on (or inside) the ground at a given position.</summary>
        public static bool OnGround(ref CharacterPhysicsInfo info, WorldPhysics world, Position position)
        {
            return position.Y <= GroundHeight(ref info, world, position);
        }


        /// <summary>Get the height of the topmost ground at a given position. Used for generating navigation data. (Eventually replace with a multi-layer heightmap.)</summary>
        public static int TopmostGroundHeight(ref CharacterPhysicsInfo info, WorldPhysics world, int x, int z, bool staticOnly = false)
        {
            int startX = x + info.startX;
            int endX = x + info.endX;

            // Hack: If we slip in just under the ceiling, we can get the result for the next-highest floor
            //       This is a hack because it depends on knowledge of what GetGroundHeightInXRange is doing
            int minCeiling = WorldPhysics.MaximumHeight + info.height;

            if(world.levelCeiling != null)
            {
                for(int xx = startX; xx < endX; xx++)
                {
                    int c = world.levelCeiling[xx, z];
                    if(c == 0)
                        continue;

                    if(c < minCeiling)
                        minCeiling = c;
                }
            }

            return world.GetGroundHeightInXRange(startX, endX, z, minCeiling - info.height, minCeiling, info.owner, staticOnly);
        }

        /// <summary>Get the height of the lowest ground position we can stand at at the given XZ coordinates, in some given Y range.</summary>
        /// <returns>The resulting height, or WorldPhysics.MaximumHeight if it did not fit</returns>
        public static int LowestValidGroundHeight(ref CharacterPhysicsInfo info, WorldPhysics world, int x, int z, int startY, int endY, bool staticOnly = false)
        {
            int startX = x + info.startX;
            int endX   = x + info.endX;

            if(endY > WorldPhysics.MaximumHeight)
                endY = WorldPhysics.MaximumHeight;

            // Repeatedly test the walkable height upwards, until it returns somewhere we are confirmed to fit
            int tryingHeight = startY;
            int resultingHeight;
            do
            {
                resultingHeight = world.GetGroundHeightInXRange(startX, endX, z, tryingHeight, tryingHeight + info.height, info.owner, staticOnly);
                if(resultingHeight <= startY)
                    return startY;
                if(resultingHeight == tryingHeight)
                    break;
                tryingHeight = resultingHeight;
            }
            while(resultingHeight < endY);

            if(resultingHeight == tryingHeight && resultingHeight >= startY && resultingHeight < endY)
                return resultingHeight;

            return WorldPhysics.MaximumHeight; // <- Failed
        }


        /// <summary>Get the height of the level walkable heightmap at this position, if it is flat, otherwise WorldPhysics.MaximumHeight</summary>
        public static int LevelOnlyFlatGround(ref CharacterPhysicsInfo info, WorldPhysics world, int x, int z)
        {
            if(world.levelHeightmap == null)
                return WorldPhysics.MaximumHeight;

            int startX = x + info.startX;
            int endX = x + info.endX;

            int h = world.levelHeightmap[startX, z];
            for (int xx = startX+1; xx < endX; xx++)
			{
                int c = world.levelHeightmap[xx, z];
                if(c != h)
                    return WorldPhysics.MaximumHeight;
			}

            return h;
        }

        #endregion



        #region Movement

        private static bool MoveStep(ref CharacterPhysicsInfo info, WorldPhysics world, int deltaX, int deltaZ, ref bool groundSnap, ref Position position, bool staticOnly = false)
        {
            int groundHeight = GroundHeight(ref info, world, new Position(position.X + deltaX, position.Y, position.Z + deltaZ), staticOnly);

            // NOTE: This ensures that we cannot get kicked up later on in the method, when we set to groundHeight
            if(position.Y + info.stairStepMaxHeight < groundHeight)
                return false; // Cannot move up that high

            position.X += deltaX;
            position.Z += deltaZ;
            
            if(position.Y < groundHeight) 
            {
                // TODO: BUG: We need to know if we're going to hit the ceiling by moving upwards here!
                // Do not go below ground
                position.Y = groundHeight;
            }
            else if(groundSnap)
            {
                if(position.Y < groundHeight + info.stairStepMaxHeight)
                {
                    // Snap to the ground if we are nearby
                    position.Y = groundHeight;
                }
                else
                {
                    // We came "off" the ground, stop trying to snap to it
                    groundSnap = false;
                }
            }

            return true;
        }


        public static void TryMove(ref CharacterPhysicsInfo info, WorldPhysics world, Position delta, bool groundSnap, ref Position position, bool staticOnly = false)
        {
            TryMoveVertical(ref info, world, delta.Y, ref position, staticOnly);
            TryMoveHorizontalDidHitWall(ref info, world, delta.X, delta.Z, groundSnap, ref position, staticOnly);
        }


        public static void TryMoveVertical(ref CharacterPhysicsInfo info, WorldPhysics world, int deltaY, ref Position position, bool staticOnly = false)
        {
            //
            // Move on Y axis first...
            //

            if (deltaY < 0)
            {
                int groundHeight = GroundHeight(ref info, world, position, staticOnly);
                if(position.Y > groundHeight) // Don't move down further if already below ground
                {
                    int newY = position.Y + deltaY;
                    if(newY < groundHeight) // Don't go below ground
                        position.Y = groundHeight;
                    else
                        position.Y = newY;
                }
            }
            else if (deltaY > 0)
            {
                int startY = position.Y + info.height;
                int endY = startY + deltaY;
                endY = world.GetCeilingHeightInXRange(position.X + info.startX, position.X + info.endX, position.Z, startY, endY, info.owner, staticOnly);
                position.Y = endY - info.height;
            }
        }


        public static bool TryMoveHorizontalDidHitWall(ref CharacterPhysicsInfo info, WorldPhysics world, int deltaX, int deltaZ, bool groundSnap, ref Position position, bool staticOnly = false)
        {
            bool hitWall = false;

            //
            // Sweep on XZ plane (slow but safe)...
            //

            while(deltaX > 0)
            {
                deltaX--;
                if(!MoveStep(ref info, world, 1, 0, ref groundSnap, ref position, staticOnly))
                {
                    hitWall = true;
                    goto doneX;
                }
            }
            while(deltaX < 0)
            {
                deltaX++;
                if(!MoveStep(ref info, world, -1, 0, ref groundSnap, ref position, staticOnly))
                {
                    hitWall = true;
                    goto doneX;
                }
            }

        doneX:

            while(deltaZ > 0)
            {
                deltaZ--;
                if(!MoveStep(ref info, world, 0, 1, ref groundSnap, ref position, staticOnly))
                {
                    hitWall = true;
                    goto doneZ;
                }
            }
            while(deltaZ < 0)
            {
                deltaZ++;
                if(!MoveStep(ref info, world, 0, -1, ref groundSnap, ref position, staticOnly))
                {
                    hitWall = true;
                    goto doneZ;
                }
            }

        doneZ:
            return hitWall;
        }

        #endregion

        

        #region Lemmings Motion

        public static bool DoLemmingsMotion(ref CharacterPhysicsInfo info, WorldPhysics world, int inputX, int inputZ, int deltaX, int deltaZ, bool groundSnap, bool wallSlide, ref Position position)
        {
            Debug.Assert(Math.Abs(inputX) <= 1 && Math.Abs(inputZ) <= 1);
            bool resultX = true, resultZ = true;

            // WARNING: Delicate copy-paste code ahead (for each direction: X+, X-, Z+, Z-)

            while(deltaX > 0)
            {
                if(MoveStep(ref info, world, 1, 0, ref groundSnap, ref position))
                {
                    deltaX--;
                }
                else if(wallSlide) // Move failed, try wall-slide
                {
                    if(inputX > 0 && inputZ <= 0 && MoveStep(ref info, world, 1, -1, ref groundSnap, ref position))
                    {
                        deltaX--;
                        if(deltaZ < 0)
                            deltaZ++;
                        else if(deltaX > 0)
                            deltaX--;
                    }
                    else if(inputX > 0 && inputZ >= 0 && MoveStep(ref info, world, 1, 1, ref groundSnap, ref position))
                    {
                        deltaX--;
                        if(deltaZ > 0)
                            deltaZ--;
                        else if(deltaX > 0)
                            deltaX--;
                    }
                    else
                    {
                        resultX = false;
                        goto doneX; // Failed to move on the X axis
                    }
                }
                else
                {
                    resultX = false;
                    goto doneX; // Failed to move on the X axis
                }
            }

            while(deltaX < 0)
            {
                if(MoveStep(ref info, world, -1, 0, ref groundSnap, ref position))
                {
                    deltaX++;
                }
                else if(wallSlide) // Move failed, try wall-slide
                {
                    if(inputX < 0 && inputZ <= 0 && MoveStep(ref info, world, -1, -1, ref groundSnap, ref position))
                    {
                        deltaX++;
                        if(deltaZ < 0)
                            deltaZ++;
                        else if(deltaX < 0)
                            deltaX++;
                    }
                    else if(inputX < 0 && inputZ >= 0 && MoveStep(ref info, world, -1, 1, ref groundSnap, ref position))
                    {
                        deltaX++;
                        if(deltaZ > 0)
                            deltaZ--;
                        else if(deltaX < 0)
                            deltaX++;
                    }
                    else
                    {
                        resultX = false;
                        goto doneX; // Failed to move on the X axis
                    }
                }
                else
                {
                    resultX = false;
                    goto doneX; // Failed to move on the X axis
                }
            }

        doneX:

            while(deltaZ > 0)
            {
                if(MoveStep(ref info, world, 0, 1, ref groundSnap, ref position))
                {
                    deltaZ--;
                }
                else if(wallSlide) // Move failed, try wall-slide
                {
                    if(inputZ > 0 && inputX <= 0 && MoveStep(ref info, world, -1, 1, ref groundSnap, ref position))
                    {
                        deltaZ--;
                        if(deltaX < 0)
                            deltaX++;
                        else if(deltaZ > 0)
                            deltaZ--;
                    }
                    else if(inputZ > 0 && inputX >= 0 && MoveStep(ref info, world, 1, 1, ref groundSnap, ref position))
                    {
                        deltaZ--;
                        if(deltaX > 0)
                            deltaX--;
                        else if(deltaZ > 0)
                            deltaZ--;
                    }
                    else
                    {
                        resultZ = false;
                        goto doneZ; // Failed to move on the Z axis
                    }
                }
                else
                {
                    resultZ = false;
                    goto doneZ; // Failed to move on the Z axis
                }
            }

            while(deltaZ < 0)
            {
                if(MoveStep(ref info, world, 0, -1, ref groundSnap, ref position))
                {
                    deltaZ++;
                }
                else if(wallSlide) // Move failed, try wall-slide
                {
                    if(inputZ < 0 && inputX <= 0 && MoveStep(ref info, world, -1, -1, ref groundSnap, ref position))
                    {
                        deltaZ++;
                        if(deltaX < 0)
                            deltaX++;
                        else if(deltaZ < 0)
                            deltaZ++;
                    }
                    else if(inputZ < 0 && inputX >= 0 && MoveStep(ref info, world, 1, -1, ref groundSnap, ref position))
                    {
                        deltaZ++;
                        if(deltaX > 0)
                            deltaX--;
                        else if(deltaZ < 0)
                            deltaZ++;
                    }
                    else
                    {
                        resultZ = false;
                        goto doneZ; // Failed to move on the Z axis
                    }
                }
                else
                {
                    resultZ = false;
                    goto doneZ; // Failed to move on the Z axis
                }
            }

        doneZ:

            return (resultX && inputX != 0) || (resultZ && inputZ != 0) || (inputX == 0 && inputZ == 0);
        }

        #endregion



    }
}
