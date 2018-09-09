using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Pixel3D.Animations;
using Pixel3D.Physics;

namespace Pixel3D.Engine
{
    public class GamePhysics : WorldPhysics
    {
        // IMPORTANT: This type is a transient UpdateContext object, so must be resettable by that method.
        public new void Reset()
        {
            base.Reset();

            ResetMovers();
            stuckObjects.Clear();
        }

        
        #region Movers
        
        public int moverCount;

        // PERF: This is over-SOA'd
        // TODO: PERF: Convert bools to single set of flags
        bool[] moverWeDoNotMove = new bool[defaultCapacity]; // <- we do not move it -- there has to be a better way of doing this...
        bool[] moverNoAutoIgnore = new bool[defaultCapacity];
        Range[] moverBoundsX = new Range[defaultCapacity];
        Range[] moverBoundsZ = new Range[defaultCapacity];
        Range[] moverBoundsY = new Range[defaultCapacity];
        public Actor[] moverActors = new Actor[defaultCapacity];
        Heightmap[] moverHeightmaps = new Heightmap[defaultCapacity];
        Position[] moverInitialPositions = new Position[defaultCapacity];
        bool[] moverInitialFacingLeft = new bool[defaultCapacity];

        public int[] moverParentIndex = new int[defaultCapacity];
        int[] moverFirstChildIndex = new int[defaultCapacity];
        int[] moverNextSiblingIndex = new int[defaultCapacity];

        CharacterPhysicsInfo[] moverCharacterPhysics = new CharacterPhysicsInfo[defaultCapacity];

        readonly Dictionary<Actor, int> actorToMoverIndex = new Dictionary<Actor, int>(ReferenceEqualityComparer<Actor>.Instance);

        

        private void ResetMovers()
        {
            actorToMoverIndex.Clear();
            collidersToUpdateBeforeSeparation.Clear();

            // clear references (for GC)
            Array.Clear(moverActors, 0, moverCount);
            Array.Clear(moverHeightmaps, 0, moverCount);
            Array.Clear(moverCharacterPhysics, 0, moverCount);

            moverCount = 0;
        }


        private void IncreaseMoverCapacity()
        {
            int newCapacity = moverActors.Length * 2;

            Array.Resize(ref moverWeDoNotMove, newCapacity);
            Array.Resize(ref moverNoAutoIgnore, newCapacity);
            Array.Resize(ref moverBoundsX, newCapacity);
            Array.Resize(ref moverBoundsZ, newCapacity);
            Array.Resize(ref moverBoundsY, newCapacity);
            Array.Resize(ref moverActors, newCapacity);
            Array.Resize(ref moverHeightmaps, newCapacity);
            Array.Resize(ref moverInitialPositions, newCapacity);
            Array.Resize(ref moverInitialFacingLeft, newCapacity);

            Array.Resize(ref moverParentIndex, newCapacity);
            Array.Resize(ref moverFirstChildIndex, newCapacity);
            Array.Resize(ref moverNextSiblingIndex, newCapacity);

            Array.Resize(ref moverCharacterPhysics, newCapacity);
        }

        private void EnsureMoverCapacity()
        {
            if(moverCount == moverActors.Length)
                IncreaseMoverCapacity();
        }


        // Movers (typically with noAutoIgnore set) may want to update their collision status (NOTE: collider indexing)
        readonly List<int> collidersToUpdateBeforeSeparation = new List<int>();


        /// <summary>
        /// Add a moving object to the physics system. A moving object may have a heightmap that will be considered for collision (like passing to <see cref="AddCollider"/>),
        /// but the moving object itself is a flat plane at the object's origin with the object's physics width and height (depth of one pixel).
        /// The moving object will be moved in response to various physics conditions (separation out of walls, movement when stacked, movement on conveyers, etc).
        /// </summary>
        /// <param name="owner">The object that the mover instance is associated with. Only one mover per actor.</param>
        /// <param name="physicsSource">The animation set that provides physics data for the mover.</param>
        /// <param name="doNotMove">
        /// The mover will not be moved by the physics engine for any reason (eg: separation, stacking, conveyers).
        /// Typically used for objects that are moved by the engine on a fixed path, but we still want them to interact with other game objects (eg: things stacked on them will move).
        /// </param>
        /// <param name="noAutoIgnore">
        /// If two movers interpenetrate, then they will automatically ignore each other for collisions, allowing them to move through each other.
        /// This disables that behaviour, which will cause interpenetrating objects to get stuck, because every possible movement results in a collision.
        /// Typically combined with <see cref="pretendToBeStatic"/>, which causes separation to resolve the interpenetration.
        /// </param>
        /// <param name="pretendToBeStatic">
        /// Makes the mover act like a part of the static scenery. Affects query results that specifically ask for static collisions,
        /// but is mostly used to make the mover object considered for separation. Should typically only be set on objects that have
        /// <see cref="doNotMove"/> set, otherwise the separation system can get into messy looping situations.
        /// </param>
        /// <param name="updateBeforeSeparation">
        /// The state of the world for collisions is based on what it was at the beginning of the frame. This causes the mover's entry to be
        /// updated immediately before separation is processed, so that separation from moving objects happens based on their position at the
        /// end of the frame, rather than the beginning. Combine with <see cref="pretendToBeStatic"/> for meaningful results.
        /// </param>
        public void AddMover(Actor owner, AnimationSet physicsSource, bool doNotMove = false, bool noAutoIgnore = false, bool pretendToBeStatic = false, bool updateBeforeSeparation = false)
        {
            //
            // Add collider:

            if(physicsSource.Heightmap != null)
            {
                if(physicsSource.Heightmap.DefaultHeight != 0)
                    return; // Refuse to add objects with non-zero default heights (only valid for levels and shadow-receivers)

                AddCollider(owner, new HeightmapView(physicsSource.Heightmap, owner.position, owner.facingLeft), pretendToBeStatic);

                if(updateBeforeSeparation)
                    collidersToUpdateBeforeSeparation.Add(colliderCount-1);
            }
                

            //
            // Add mover:

            EnsureMoverCapacity();

            actorToMoverIndex.Add(owner, moverCount); // <- Ensure movers are unique 

            moverWeDoNotMove[moverCount] = doNotMove;
            moverNoAutoIgnore[moverCount] = noAutoIgnore;
            moverActors[moverCount] = owner;

            Position position = moverInitialPositions[moverCount] = owner.position;
            bool facingLeft = moverInitialFacingLeft[moverCount] = owner.facingLeft;

            moverBoundsX[moverCount] = new Range(physicsSource.physicsStartX, physicsSource.physicsEndX).MaybeFlip(facingLeft) + position.X;
            moverBoundsZ[moverCount] = ((physicsSource.Heightmap == null) ?  new Range(0, 1) : new Range(physicsSource.physicsStartZ, physicsSource.physicsEndZ)) + position.Z; // Flat for characters

            // NOTE: We assume that heightmap'd objects have their auto-generated physics sizes here:
            if(physicsSource.Heightmap != null && physicsSource.physicsHeight >= Heightmap.Infinity)
                moverBoundsY[moverCount] = new Range(0, MaximumHeight);
            else
                moverBoundsY[moverCount] = new Range(position.Y, position.Y + physicsSource.physicsHeight);

            moverHeightmaps[moverCount] = physicsSource.Heightmap; // May be null

            // TODO: Make this a parameter of the animation set
            moverCharacterPhysics[moverCount] = new CharacterPhysicsInfo(physicsSource, owner);

            Debug.Assert(physicsSource.Heightmap == null || physicsSource.Heightmap.DefaultHeight == 0); // If we have a heightmap, it must be a standard object heightmap

            //
            // Stacking and intersection-ignores:

            moverParentIndex[moverCount] = -1;
            moverFirstChildIndex[moverCount] = -1;
            moverNextSiblingIndex[moverCount] = -1;

            for(int i = 0; i < moverCount; i++)
            {
                if(!Range.Overlaps(moverBoundsX[moverCount], moverBoundsX[i]) || !Range.Overlaps(moverBoundsZ[moverCount], moverBoundsZ[i]))
                    continue;

                // PERF: We're touching each heightmap twice for stacking and intersection. Can probably reduce down to one check.
                //       (Then again, once we cache it, is spinning on it longer such a big deal?)

                // Handle stacking:
                if(moverBoundsY[moverCount].end < MaximumHeight && moverBoundsY[i].end < MaximumHeight) // <- infinite height things, or things above the world, do not stack
                {
                    if(moverHeightmaps[i] != null && moverParentIndex[moverCount] == -1 && moverBoundsY[moverCount].start > moverBoundsY[i].start && moverBoundsY[moverCount].start <= moverBoundsY[i].end) // added can rest on existing
                    {
                        TryAddRestingOn(moverCount, i);
                    }
                    else if(moverHeightmaps[moverCount] != null && moverParentIndex[i] == -1 && moverBoundsY[i].start > moverBoundsY[moverCount].start && moverBoundsY[i].start <= moverBoundsY[moverCount].end) // existing can rest on added
                    {
                        TryAddRestingOn(i, moverCount);
                    }
                }

                // Handle intersection:
                if(!noAutoIgnore && !moverNoAutoIgnore[i] && Range.Overlaps(moverBoundsY[moverCount], moverBoundsY[i]))
                {
                    bool hasIntersection = false;
                    if(moverHeightmaps[i] != null)
                    {
                        if(moverHeightmaps[moverCount] != null)
                            hasIntersection = SolidVsSolidIntersection(i, moverCount);
                        else
                            hasIntersection = SolidVsCharacterIntersection(i, moverCount);
                    }
                    else if(moverHeightmaps[moverCount] != null)
                    {
                        hasIntersection = SolidVsCharacterIntersection(moverCount, i);
                    }

                    if(hasIntersection)
                    {
                        AddIgnoredPair(moverActors[i], moverActors[moverCount]);
                    }
                }
            }

            moverCount++;
        }


        /// <summary>Remove an actor from separation and stacking (for when you want to externally control its position). Requires that physics registration has already run.</summary>
        public void SetAsDoNotMove(Actor actor)
        {
            int index;
            if(actorToMoverIndex.TryGetValue(actor, out index))
            {
                moverWeDoNotMove[index] = true;
            }
        }


        #region Stack Registration

        private bool IsRestingOn(int top, int bottom)
        {
            HeightmapView bottomHeightmapView = new HeightmapView(moverHeightmaps[bottom], moverInitialPositions[bottom], moverInitialFacingLeft[bottom]);
            int targetY = moverInitialPositions[top].Y - bottomHeightmapView.position.Y;

            if(targetY <= 0) // <- This is just safety if external range checks didn't exclude something correctly (this was a bug with infinite-height objects, before I made them unstackable -AR)
                return false;

            if(moverHeightmaps[top] != null)
            {
                //
                // Heightmap vs Heightmap

                HeightmapView topHeightmapView = new HeightmapView(moverHeightmaps[top], new Position(moverInitialPositions[top].X, 0, moverInitialPositions[top].Z), moverInitialFacingLeft[top]);
                Rectangle xzIntersection = Rectangle.Intersect(bottomHeightmapView.Bounds, topHeightmapView.Bounds);

                for(int z = 0; z < xzIntersection.Height; z++) for(int x = 0; x < xzIntersection.Width; x++)
                {
                    int xx = x + xzIntersection.X;
                    int zz = z + xzIntersection.Y;

                    if(topHeightmapView.GetUnelevatedHeightAt(xx, zz) != 0)
                    {
                        if(bottomHeightmapView.GetUnelevatedHeightAt(xx, zz) == targetY)
                            return true;
                    }
                }
            }
            else
            {
                //
                // Heightmap vs Character

                for(int x = moverBoundsX[top].start; x < moverBoundsX[top].end; x++)
                {
                    if(bottomHeightmapView.GetUnelevatedHeightAt(x, moverInitialPositions[top].Z) == targetY)
                        return true;
                }
            }

            return false;
        }


        // TODO: PERF: Unnecessary return?
        private bool TryAddRestingOn(int top, int bottom)
        {
            if(IsRestingOn(top, bottom))
            {
                Debug.Assert(moverParentIndex[top] == -1); // <- not already inserted
                moverParentIndex[top] = bottom;

                if(moverFirstChildIndex[bottom] == -1)
                {
                    moverFirstChildIndex[bottom] = top;
                }
                else
                {
                    int lastChild = moverFirstChildIndex[bottom];

                    while(moverNextSiblingIndex[lastChild] != -1)
                    {
                        Debug.Assert(moverParentIndex[lastChild] == bottom);
                        lastChild = moverNextSiblingIndex[lastChild];
                    }

                    Debug.Assert(moverParentIndex[lastChild] == bottom);
                    moverNextSiblingIndex[lastChild] = top;
                }

                return true;
            }

            return false;
        }

        #endregion


        #region Intersection Registration

        private bool SolidVsSolidIntersection(int first, int second)
        {
            // NOTE: Calling code has done a bounds check on all three axes

            HeightmapView firstHeightmapView = new HeightmapView(moverHeightmaps[first], moverInitialPositions[first], moverInitialFacingLeft[first]);
            HeightmapView secondHeightmapView = new HeightmapView(moverHeightmaps[second], moverInitialPositions[second], moverInitialFacingLeft[second]);

            Rectangle xzIntersection = Rectangle.Intersect(firstHeightmapView.Bounds, secondHeightmapView.Bounds);
            for(int z = 0; z < xzIntersection.Height; z++) for(int x = 0; x < xzIntersection.Width; x++)
            {
                int xx = x + xzIntersection.X;
                int zz = z + xzIntersection.Y;

                int firstHeight = firstHeightmapView.GetUnelevatedHeightAt(xx, zz);
                int secondHeight = secondHeightmapView.GetUnelevatedHeightAt(xx, zz);

                if(firstHeight == 0 || secondHeight == 0)
                    continue;

                if(firstHeight == Heightmap.Infinity)
                    firstHeight = WorldPhysics.MaximumHeight;
                if(secondHeight == Heightmap.Infinity)
                    secondHeight = WorldPhysics.MaximumHeight;         

                Range firstRangeY = new Range(firstHeightmapView.position.Y, firstHeightmapView.position.Y + firstHeight);
                Range secondRangeY = new Range(secondHeightmapView.position.Y, secondHeightmapView.position.Y + secondHeight);
                if(Range.Overlaps(firstRangeY, secondRangeY))
                    return true;
            }

            return false;
        }

        private bool SolidVsCharacterIntersection(int solid, int character)
        {
            // NOTE: Calling code has done a bounds check on all three axes

            HeightmapView heightmapView = new HeightmapView(moverHeightmaps[solid], moverInitialPositions[solid], moverInitialFacingLeft[solid]);
            Range heightmapRangeX = heightmapView.BoundsX;

            int characterX = moverInitialPositions[character].X;
            int characterY = moverInitialPositions[character].Y;
            int characterZ = moverInitialPositions[character].Z;

            Range characterRangeX = new Range(characterX + moverCharacterPhysics[character].startX, characterX + moverCharacterPhysics[character].endX);
            Range characterRangeY = new Range(characterY, characterY + moverCharacterPhysics[character].height);

            Debug.Assert(heightmapView.BoundsZ.Contains(characterZ));

            Range xIntersection = heightmapRangeX.Clip(characterRangeX.start, characterRangeX.end);
            for(int x = xIntersection.start; x < xIntersection.end; x++)
            {
                int solidHeight = heightmapView.GetUnelevatedHeightAt(x, characterZ);

                if(solidHeight == 0)
                    continue;

                if(solidHeight == Heightmap.Infinity)
                    solidHeight = WorldPhysics.MaximumHeight;

                Range solidRangeY = new Range(heightmapView.position.Y, heightmapView.position.Y + solidHeight);
                if(Range.Overlaps(solidRangeY, characterRangeY))
                    return true;
            }

            return false;
        }

        #endregion


        #endregion



        #region Stacking

        public struct ChildListEnumerable : IEnumerable<Actor>
        {
            public static ChildListEnumerable Empty { get { return new ChildListEnumerable(null, -1); } }

            GamePhysics owner;
            int firstChildIndex;

            public ChildListEnumerable(GamePhysics owner, int firstChildIndex)
            {
                this.owner = owner;
                this.firstChildIndex = firstChildIndex;
            }

            public struct Enumerator : IEnumerator<Actor>
            {
                public Enumerator(GamePhysics owner, int nextIndex)
                {
                    this.owner = owner;
                    this.current = null;
                    this.nextIndex = nextIndex;
                }

                GamePhysics owner;
                Actor current;
                int nextIndex;

                public Actor Current { get { return current; } }

                public bool MoveNext()
                {
                    if(nextIndex != -1)
                    {
                        current = owner.moverActors[nextIndex];
                        nextIndex = owner.moverNextSiblingIndex[nextIndex];
                        return true;
                    }
                    return false;
                }

                public void Dispose() { }
                object System.Collections.IEnumerator.Current { get { return Current; } }
                public void Reset() { throw new InvalidOperationException(); }
            }

            Enumerator GetEnumerator() { return new Enumerator(owner, firstChildIndex); }
            IEnumerator<Actor> IEnumerable<Actor>.GetEnumerator() { return GetEnumerator(); }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }



        /// <summary>NOTE: Only considers whether an object is on a surface (caller must inspect IncomingConnection)</summary>
        public ChildListEnumerable GetStackChildren(Actor actor)
        {
            int index;
            if(actorToMoverIndex.TryGetValue(actor, out index))
            {
                return new ChildListEnumerable(this, moverFirstChildIndex[index]);
            }

            return ChildListEnumerable.Empty;
        }

        /// <summary>NOTE: Only considers whether an object is on a surface (caller must inspect IncomingConnection of the given child)</summary>
        public Actor GetStackParent(Actor actor)
        {
            int index;
            if(actorToMoverIndex.TryGetValue(actor, out index))
            {
                int pi = moverParentIndex[index];
                if(pi != -1)
                    return moverActors[pi];
            }

            return null;
        }



        public int GetGlobalMaximumStackDepth()
        {
            int maxDepth = 0;
            for(int i = 0; i < moverCount; i++)
            {
                if(moverParentIndex[i] == -1)
                {
                    int depth = GetStackDepthRecursive(i);
                    if(depth > maxDepth)
                        maxDepth = depth;
                }
            }
            return maxDepth;
        }

        private int GetStackDepthRecursive(int index)
        {
            int maxDepth = 0;
            int child = moverFirstChildIndex[index];
            while(child != -1)
            {
                int depth = GetStackDepthRecursive(child) + 1;
                if(depth > maxDepth)
                    maxDepth = depth;

                child = moverNextSiblingIndex[child];
            }
            return maxDepth;
        }



        /// <summary>If an object has moved, apply the same motion to any objects that were resting on it at the start of the frame.</summary>
        public void ProcessStackedMotion(UpdateContext updateContext)
        {
            // NOTE: If two objects happened to be stacked, and they experience the same force (eg: an explosion pushes them)
            //       the stacked object will get doubled motion (then tripled, and so on, for addditional layers). Not sure we even have this in-game.
            //       In these cases, it may be worthwhile to allow objects to be registered as "not stackable" while they are
            //       experiencing such a force.

            for(int r = 0; r < moverCount; r++)
            {
                if(moverParentIndex[r] == -1) // All root objects (not resting on anything else)
                {
                    if(moverActors[r].IncomingConnection == null) // Not held by anyone else either
                    {
                        DoRecursiveMotion(r, Position.Zero);
                    }
                }
            }
        }

        private void DoRecursiveMotion(int i, Position motionFromParent)
        {
            if(moverWeDoNotMove[i])
                motionFromParent = Position.Zero;

            moverActors[i].position += motionFromParent; // Add this first so it is considered for...
            Position motionToChildren = moverActors[i].position - moverInitialPositions[i];

            // Pass on the *same* motion to our held objects (ie: they stay lined up with us)
            var heldObject = moverActors[i].OutgoingConnection;
            if(heldObject != null)
            {
                int heldIndex;
                if(actorToMoverIndex.TryGetValue(heldObject, out heldIndex))
                {
                    DoRecursiveMotion(heldIndex, motionFromParent);
                }
            }

            // Pass on our motion to any children resting on us
            int currentChild = moverFirstChildIndex[i];
            while(currentChild != -1)
            {
                // NOTE: Objects being held are locked to their holder (and not the surface under them), so we skip them
                if(moverActors[currentChild].IncomingConnection == null)
                {
                    DoRecursiveMotion(currentChild, motionToChildren);
                }

                currentChild = moverNextSiblingIndex[currentChild]; // Walk child list
            }
        }

        #endregion



        #region Separation

        public readonly List<Actor> stuckObjects = new List<Actor>();


        public void DoSeparation(UpdateContext updateContext)
        {
            //
            // Process anyone requesting to be updated before separation
            //

            foreach(var i in collidersToUpdateBeforeSeparation)
            {
                Actor actor = (Actor)colliderEntries[i].owner; // <- will always succeed, because we registered it that way
                HeightmapView heightmapView = new HeightmapView(actor.animationSet.Heightmap, actor.position, actor.facingLeft);
                ChangeCollider(i, heightmapView);
            }



            //
            // Remove everyone from static geometry
            //

            for(int i = 0; i < moverCount; i++)
            {
                if(moverWeDoNotMove[i])
                    continue;

                if(moverActors[i].IncomingConnection != null)
                    continue; // Don't separate if we're attached to someone

                var cpi = moverCharacterPhysics[i];
                int simpleGroundHeight = CharacterPhysics.GroundHeight(ref cpi, this, moverActors[i].position, true);
                if(simpleGroundHeight <= moverActors[i].position.Y)
                    continue; // We are in open space

                //
                // Oh no! We are stuck in the level!
                //


                // Attempt 1: Move back to start point, and then try moving to new position:
                //            (Doing this first, to try to prevent tunneling behaviour - also probably faster than TryToFitNearestInZSlice)
                Position moveDelta = moverActors[i].position - moverInitialPositions[i];
                if(moveDelta != Position.Zero // <- Can't undo non-moves.
                        && moveDelta.ManhattenLength <= 8) // <- Don't try to undo huge moves, as they may be teleports/animations, and we could get snagged on the way there.
                {
                    var groundHeightAtInitial = CharacterPhysics.GroundHeight(ref cpi, this, moverInitialPositions[i], true);
                    if(groundHeightAtInitial <= moverInitialPositions[i].Y) // <- OK at initial position
                    {
                        // Attempt to move towards target:
                        Position target = moverActors[i].position;
                        moverActors[i].position = moverInitialPositions[i];

                        CharacterPhysics.TryMove(ref moverCharacterPhysics[i], this, moveDelta, false, ref moverActors[i].position, true);

                        continue; // Success!
                    }
                }

                // Attempt 2: Attempt to extricate ourselves from the situation directly
                #region // a whole bunch of code
                {
                    int statsZSliceCount = 0;
                    long statsVoxelCount = 0;

                    Position desiredPosition = moverActors[i].position;

                    // Ensure we didn't come out of the level entirely
                    if(desiredPosition.X < StartX)
                        desiredPosition.X = StartX;
                    if(desiredPosition.X >= EndX)
                        desiredPosition.X = EndX - 1;
                    if(desiredPosition.Y < 0)
                        desiredPosition.Y = 0;
                    if(desiredPosition.Z < StartZ)
                        desiredPosition.Z = StartZ;
                    if(desiredPosition.Z >= EndZ)
                        desiredPosition.Z = EndZ - 1;

                    // Grab the nearest point on our current plane:
                    const int maxRadiusXY = 128;
                    Point nearest = TryToFitNearestInZSlice(moverCharacterPhysics[i].startX, moverCharacterPhysics[i].endX, moverCharacterPhysics[i].height,
                            desiredPosition.X, desiredPosition.Y, moverActors[i].position.Y,
                            desiredPosition.X - maxRadiusXY, desiredPosition.X + maxRadiusXY, desiredPosition.Z, desiredPosition.Y - maxRadiusXY, desiredPosition.Y + maxRadiusXY,
                            moverActors[i], true);
                    statsZSliceCount++;
                    statsVoxelCount += (maxRadiusXY * maxRadiusXY * 4);

                    Position bestPosition;
                    int bestDistance;
                    if(nearest != NoFitFound)
                    {
                        bestDistance = Math.Abs(desiredPosition.X - nearest.X) + Math.Abs(desiredPosition.Y - nearest.Y);
                        bestPosition = new Position(nearest.X, nearest.Y, desiredPosition.Z);

                        // Debugging: If check that we're not ending up back in the same place (why did we try to separate, if we fit?)
                        //            If this assert fires, there is a bug in the separation algorithm.
                        // NOTE: checks vs non-clipped position!
                        Debug.Assert(Position.ManhattenDistance(moverActors[i].position, bestPosition) > 0,
                                "SEPARATION DID NOTHING. Actor is: " + moverActors[i].ToString()
                                + "\n\nIf you can reproduce this, please report to Andrew. Otherwise hit \"Ignore\" and carry on.");
                    }
                    else
                    {
                        bestPosition = default(Position);
                        bestDistance = int.MaxValue;
                    }


                    // Search forwards and backwards for better positions on nearby planes:
                    const int maxRadiusZ = 10;
                    int searchBoundStartZ = Math.Max(StartZ, desiredPosition.Z - maxRadiusZ);
                    int searchBoundEndZ = Math.Min(EndZ, desiredPosition.Z + maxRadiusZ);

                    // Searching forwards:  (NOTE: Copy-paste with searching backwards)
                    int searchZ = desiredPosition.Z - 1;
                    // NOTE: Checking vs bestDistance each time, so we might be able to early-out
                    while(searchZ >= searchBoundStartZ && (desiredPosition.Z - searchZ) < bestDistance) // <- IMPORTANT: not doing maths on bestDistance, as it can be int.MaxValue!
                    {
                        Debug.Assert(bestDistance > 0);
                        int radiusXY = Math.Min(bestDistance, maxRadiusXY); // <- hopefully reduces search range

                        nearest = TryToFitNearestInZSlice(moverCharacterPhysics[i].startX, moverCharacterPhysics[i].endX, moverCharacterPhysics[i].height,
                                desiredPosition.X, desiredPosition.Y, moverActors[i].position.Y,
                                desiredPosition.X - radiusXY, desiredPosition.X + radiusXY, searchZ, desiredPosition.Y - radiusXY, desiredPosition.Y + radiusXY,
                                moverActors[i], true);
                        statsZSliceCount++;
                        statsVoxelCount += (radiusXY * radiusXY * 4);

                        if(nearest != NoFitFound)
                        {
                            int distance = Math.Abs(desiredPosition.X - nearest.X) + Math.Abs(desiredPosition.Y - nearest.Y) + Math.Abs(desiredPosition.Z - searchZ);
                            if(distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestPosition = new Position(nearest.X, nearest.Y, searchZ);
                            }
                        }

                        searchZ--;
                    }

                    // Searching backwards:  (NOTE: Copy-paste with searching forwards)
                    searchZ = desiredPosition.Z + 1;
                    // NOTE: Checking vs bestDistance each time, so we might be able to early-out
                    while(searchZ < searchBoundEndZ && (searchZ - desiredPosition.Z) < bestDistance) // <- IMPORTANT: not doing maths on bestDistance, as it can be int.MaxValue!
                    {
                        Debug.Assert(bestDistance > 0);
                        int radiusXY = Math.Min(bestDistance, maxRadiusXY); // <- hopefully reduces search range

                        nearest = TryToFitNearestInZSlice(moverCharacterPhysics[i].startX, moverCharacterPhysics[i].endX, moverCharacterPhysics[i].height,
                                desiredPosition.X, desiredPosition.Y, moverActors[i].position.Y,
                                desiredPosition.X - radiusXY, desiredPosition.X + radiusXY, searchZ, desiredPosition.Y - radiusXY, desiredPosition.Y + radiusXY,
                                moverActors[i], true);
                        statsZSliceCount++;
                        statsVoxelCount += (radiusXY * radiusXY * 4);

                        if(nearest != NoFitFound)
                        {
                            int distance = Math.Abs(desiredPosition.X - nearest.X) + Math.Abs(desiredPosition.Y - nearest.Y) + Math.Abs(desiredPosition.Z - searchZ);
                            if(distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestPosition = new Position(nearest.X, nearest.Y, searchZ);
                            }
                        }

                        searchZ++;
                    }

#if DEBUG // Separation stats
                    {
                        bool isActuallyTheMainSimulation = (updateContext.DebugNetworkUnsafeHasLocalSettings); // <- Somewhat hacky mechanisim so we don't get debug writes for "alternate universes" when debugging
                        if(isActuallyTheMainSimulation)
                        {
                            Debug.WriteLine("Separation of \"" + moverActors[i] + "\" searched " + statsZSliceCount + " Z slices, checking "
                                    + statsVoxelCount + " voxels; moved from " + moverActors[i].position + " to " + bestPosition + ".");
                        }
                    }
#endif

                    if(bestDistance != int.MaxValue) // <- Did we find a suitable position?
                    {
                        moverActors[i].position = bestPosition;
                        continue;
                    }
                }
                #endregion


                //
                // At this point... holy crap... how did you manage to get an object here??
                //
                
                stuckObjects.Add(moverActors[i]); // <- game state can deal with it.

            }

        }



        #endregion



        #region Region Checks

        public struct MoverInMaskEnumerable : IEnumerable<Actor>
        {
            GamePhysics owner;
            MaskData maskData;

            public MoverInMaskEnumerable(GamePhysics owner, MaskData maskData)
            {
                this.owner = owner;
                this.maskData = maskData;
            }

            public struct Enumerator : IEnumerator<Actor>
            {
                public Enumerator(GamePhysics owner, MaskData maskData)
                {
                    this.owner = owner;
                    this.maskData = maskData;
                    this.current = null;
                    this.nextIndex = 0;
                }

                GamePhysics owner;
                MaskData maskData;
                Actor current;
                int nextIndex;

                public Actor Current { get { return current; } }

                public bool MoveNext()
                {
                    while(nextIndex < owner.moverCount)
                    {
                        int i = nextIndex;
                        nextIndex++;

                        if(maskData.IsSetInXRange(owner.moverInitialPositions[i].X + owner.moverCharacterPhysics[i].startX,
                                owner.moverInitialPositions[i].X + owner.moverCharacterPhysics[i].endX,
                                owner.moverInitialPositions[i].Z))
                        {
                            current = owner.moverActors[i];
                            return true;
                        }
                    }

                    return false;
                }

                public void Dispose() { }
                object System.Collections.IEnumerator.Current { get { return Current; } }
                public void Reset() { throw new InvalidOperationException(); }
            }

            Enumerator GetEnumerator() { return new Enumerator(owner, maskData); }
            IEnumerator<Actor> IEnumerable<Actor>.GetEnumerator() { return GetEnumerator(); }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }


        public MoverInMaskEnumerable GetAllMoversInMask(MaskData maskData)
        {
            return new MoverInMaskEnumerable(this, maskData);
        }

        #endregion



        #region Conveyors

        public void DoStaticGroundConveyorRegion(MaskData conveyorRegion, int deltaX, int deltaZ)
        {
            for(int i = 0; i < moverCount; i++)
            {
                if(moverWeDoNotMove[i])
                    continue;

                int moverStartX = moverInitialPositions[i].X + moverCharacterPhysics[i].startX;
                int moverEndX = moverInitialPositions[i].X + moverCharacterPhysics[i].endX;
                
                int moverY = moverInitialPositions[i].Y;
                int moverZ = moverInitialPositions[i].Z;

                if(conveyorRegion.IsSetInXRange(moverStartX, moverEndX, moverZ))
                {
                    int groundHeight = GetGroundHeightInXRange(moverStartX, moverEndX, moverZ, moverY, moverY + moverCharacterPhysics[i].height, moverActors[i], staticOnly: true);

                    if(moverY == groundHeight) // <- on the ground
                        CharacterPhysics.TryMoveHorizontalDidHitWall(ref moverCharacterPhysics[i], this, deltaX, deltaZ, true, ref moverActors[i].position, staticOnly: true);
                }
            }
        }

        public void DoStaticBlowerRegion(MaskData blowerRegion, int deltaX, int deltaZ, int startY, int endY)
        {
            for (int i = 0; i < moverCount; i++)
            {
                if (moverWeDoNotMove[i])
                    continue;

                int moverStartX = moverInitialPositions[i].X + moverCharacterPhysics[i].startX;
                int moverEndX = moverInitialPositions[i].X + moverCharacterPhysics[i].endX;

                int moverStartY = moverInitialPositions[i].Y;
                int moverEndY = moverStartY + moverCharacterPhysics[i].height;
                int moverZ = moverInitialPositions[i].Z;

                if (blowerRegion.IsSetInXRange(moverStartX, moverEndX, moverZ))
                {
                    int groundHeight = GetGroundHeightInXRange(moverStartX, moverEndX, moverZ, moverStartY, moverEndY, moverActors[i], staticOnly: true);
                    bool onGround = (groundHeight == moverStartY);

                    if (!(moverEndY < startY || moverStartY >= endY)) // <- in the blower's Y range
                        CharacterPhysics.TryMoveHorizontalDidHitWall(ref moverCharacterPhysics[i], this, deltaX, deltaZ, onGround, ref moverActors[i].position, staticOnly: true);
                }
            }
        }

        #endregion

    }
}


