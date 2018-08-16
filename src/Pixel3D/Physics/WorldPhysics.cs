using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Common;
using Pixel3D.Animations;
using System.Runtime.InteropServices;
using System.Security;

namespace Pixel3D
{
    //
    // IMPORTANT: Lots of the methods that query the physics state are optimised and copy-pasted between each other.
    //            Care must be taken to keep them in-sync. Any attempt to merge common code must be done carefully.
    //

    public class WorldPhysics
    {
        /// <summary>Infinite height, for practical purposes</summary>
        public const int MaximumHeight = 10000;

        protected const int defaultCapacity = 64;


        // IMPORTANT: This type is a transient UpdateContext object, so must be resettable by that method.
        public void Reset()
        {
            levelHeightmap = null;
            levelCeiling = null;

            ResetCollisionObjects();
            ResetIgnored();
        }



        #region Level

        public int StartX { get { return levelHeightmap.StartX; } }
        public int EndX { get { return levelHeightmap.EndX; } }
        public int StartZ { get { return levelHeightmap.StartZ; } }
        public int EndZ { get { return levelHeightmap.EndZ; } }


        public void SetLevel(AnimationSet animationSet)
        {
            this.levelHeightmap = animationSet.Heightmap;
            this.levelCeiling = animationSet.Ceiling;
        }

        public Heightmap levelHeightmap, levelCeiling;


        #endregion



        #region Collision Objects

        // NOTE: Performance here is designed around spinning through the X bounds quickly (minimal), and then when that hits,
        //       bringing in all of the data required for remaining checks (so we don't wait on cache for later checks).

        protected int colliderCount;

        protected struct ColliderEntry // NOTE: 32 bytes (so we can be cache line aligned, and maybe get two at once)
        {
            public Range boundsZ;
            public object owner;
            public HeightmapView heightmapView;

            // TODO: If we can somehow get a height in here, we can bounds check it in all the queries (probably requires heightmaps to know their max height)
        }

        protected bool[] colliderIsStatic = new bool[defaultCapacity]; // <- split out, because it's only required in some cases
        protected Range[] colliderBoundsX = new Range[defaultCapacity];
        protected ColliderEntry[] colliderEntries = new ColliderEntry[defaultCapacity];


        void ResetCollisionObjects()
        {
            Array.Clear(colliderEntries, 0, colliderCount); // <- clear GC-able references
            colliderCount = 0;
        }

        private void IncreaseColliderCapacity()
        {
            int newCapacity = colliderBoundsX.Length * 2;

            Array.Resize(ref colliderIsStatic, newCapacity);
            Array.Resize(ref colliderBoundsX, newCapacity);
            Array.Resize(ref colliderEntries, newCapacity);
        }

        private void EnsureColliderCapacity()
        {
            if(colliderCount == colliderBoundsX.Length)
                IncreaseColliderCapacity();
        }


        protected void AddCollider(object owner, HeightmapView heightmapView, bool isStatic)
        {
            Debug.Assert(heightmapView.heightmap != null);

            if(heightmapView.heightmap.DefaultHeight != 0)
                return; // Refuse to add objects with non-zero default heights (only valid for levels and shadow-receivers)


            EnsureColliderCapacity();

            // Doing this allows us to later on check for skipping with a single comparison (don't have to null-check as well)
            if(owner == null)
                owner = this;

            colliderIsStatic[colliderCount] = isStatic;
            colliderBoundsX[colliderCount] = heightmapView.BoundsX;
            colliderEntries[colliderCount] = new ColliderEntry { boundsZ = heightmapView.BoundsZ, owner = owner, heightmapView = heightmapView };

            colliderCount++;
        }

        protected void ChangeCollider(int i, HeightmapView heightmapView)
        {
            colliderBoundsX[i] = heightmapView.BoundsX;
            colliderEntries[i].boundsZ = heightmapView.BoundsZ;
            colliderEntries[i].heightmapView = heightmapView;
        }


        public void AddStatic(object owner, HeightmapView heightmapView)
        {
            AddCollider(owner, heightmapView, true);
        }

        #endregion



        #region Ignores

        struct IgnoreNode
        {
            public object ignored;
            public int next;
        }

        readonly Dictionary<object, IgnoreNode> ignore = new Dictionary<object, IgnoreNode>(ReferenceEqualityComparer<object>.Instance);
        readonly List<IgnoreNode> moreIgnored = new List<IgnoreNode>();

        private void ResetIgnored()
        {
            ignore.Clear();
            moreIgnored.Clear();
        }


        public void AddIgnoredPair(object a, object b)
        {
            AddIgnoredOneWay(a, b);
            AddIgnoredOneWay(b, a);
        }

        private void AddIgnoredOneWay(object from, object to)
        {
            Debug.Assert(!ReferenceEquals(from, to));

            int next = -1;

            IgnoreNode existing;
            if(ignore.TryGetValue(from, out existing))
            {
                next = moreIgnored.Count;
                moreIgnored.Add(existing);
            }

            ignore[from] = new IgnoreNode { next = next, ignored = to };
        }



        private IgnoreNode GetFirstIgnored(object from)
        {
            IgnoreNode firstNode;
            if(from != null && ignore.TryGetValue(from, out firstNode))
                return firstNode;
            else
                return new IgnoreNode { ignored = this, next = -1 }; // <- NOTE: Setting `ignored = this` so that we can skip null checks
        }

        private bool IsIgnored(IgnoreNode firstIgnored, object to)
        {
            if(ReferenceEquals(firstIgnored.ignored, to))
                return true;

            int index = firstIgnored.next;
            while(index != -1)
            {
                if(ReferenceEquals(moreIgnored[index].ignored, to))
                    return true;
                index = moreIgnored[index].next;
            }

            return false;
        }

        #endregion



        #region Collision Queries

        /// <param name="referenceY">Starting Y position for one-way platforms</param>
        /// <param name="endY">Exclusive boundary where we stop caring about objects (because we are under them)</param>
        public int GetGroundHeightAt(int x, int z, int referenceY, int endY, object owner, bool staticOnly = false)
        {
            int maxHeight = levelHeightmap[x, z];
            if(maxHeight == Heightmap.Infinity)
                return WorldPhysics.MaximumHeight;

            if(levelCeiling != null)
            {
                int c = levelCeiling[x, z];
                if(c != 0 && c < endY)
                    return WorldPhysics.MaximumHeight; // not really the ground, but we can't move here
            }


            IgnoreNode firstIgnored = GetFirstIgnored(owner);

            for(int i = 0; i < colliderCount; i++)
            {
                // NOTE: Conditions are checked in this order to improve cache behaviour!

                if(staticOnly)
                    if(!colliderIsStatic[i])
                        continue;

                if(!colliderBoundsX[i].Contains(x))
                    continue;

                if(!colliderEntries[i].boundsZ.Contains(z))
                    continue;

                if(ReferenceEquals(owner, colliderEntries[i].owner)) // Don't collide with ourselves
                    continue;

                var heightmapView = colliderEntries[i].heightmapView;
                if(heightmapView.position.Y >= endY) // Object is above us
                    continue;

                if(IsIgnored(firstIgnored, colliderEntries[i].owner)) // Don't collide with things we're holding, etc
                    continue;


                int xx = x - heightmapView.position.X;
                if(heightmapView.flipX)
                    xx = -xx;
                int zz = z - heightmapView.position.Z;

                int transformedReferenceY = referenceY - heightmapView.position.Y;

                int h = heightmapView.heightmap[xx, zz];
                if(h != 0)
                {
                    if(h == Heightmap.Infinity)
                        return WorldPhysics.MaximumHeight;

                    if(heightmapView.heightmap.OneWay && h - heightmapView.heightmap.OneWayThickness > transformedReferenceY)
                        continue; // <- one-way platform is above us

                    h += heightmapView.position.Y;
                    if(h > maxHeight)
                        maxHeight = h;
                }
            }

            return maxHeight;
        }



        /// <param name="referenceY">Starting Y position for one-way platforms</param>
        /// <param name="endY">Exclusive boundary where we stop caring about objects (because we are under them)</param>
        public int GetGroundHeightInXRange(int startX, int endX, int z, int referenceY, int endY, object owner, bool staticOnly = false)
        {
            Debug.Assert(endX > startX);

            // Handle level heightmap
            int maxHeight = 0;
            for(int x = startX; x < endX; x++)
            {
                int h = levelHeightmap[x, z];
                if(h > maxHeight)
                    maxHeight = h;
            }

            if(maxHeight == Heightmap.Infinity)
                return WorldPhysics.MaximumHeight;

            // Handle level ceiling
            if(levelCeiling != null)
            {
                for(int x = startX; x < endX; x++)
                {
                    int c = levelCeiling[x, z];
                    if(c != 0 && c < endY)
                        return WorldPhysics.MaximumHeight; // not really the ground, but we can't move here
                }
            }


            // Handle objects
            IgnoreNode firstIgnored = GetFirstIgnored(owner);
            for(int i = 0; i < colliderCount; i++)
            {
                // NOTE: Conditions are checked in this order to improve cache behaviour!

                if(staticOnly)
                    if(!colliderIsStatic[i])
                        continue;

                if(!colliderBoundsX[i].Contains(startX, endX))
                    continue;

                if(!colliderEntries[i].boundsZ.Contains(z))
                    continue;

                if(ReferenceEquals(owner, colliderEntries[i].owner)) // Don't collide with ourselves
                    continue;

                var heightmapView = colliderEntries[i].heightmapView;
                if(heightmapView.position.Y >= endY) // Object is above us
                    continue;

                if(IsIgnored(firstIgnored, colliderEntries[i].owner)) // Don't collide with things we're holding, etc
                    continue;


                // PERF: We don't need to look outside the heightmap range (which just gives us default values)
                int transformedStartX;
                int transformedEndX;
                if(!heightmapView.flipX)
                {
                    transformedStartX = startX - heightmapView.position.X;
                    transformedEndX = endX - heightmapView.position.X;
                }
                else
                {
                    transformedStartX = -(endX - heightmapView.position.X - 1);
                    transformedEndX = -(startX - heightmapView.position.X - 1);
                }

                int transformedZ = z - heightmapView.position.Z;
                int transformedReferenceY = referenceY - heightmapView.position.Y;
                int transformedEndY = endY - heightmapView.position.Y;

                for(int transformedX = transformedStartX; transformedX < transformedEndX; transformedX++)
                {
                    int h = heightmapView.heightmap[transformedX, transformedZ];
                    if(h != 0)
                    {
                        if(h == Heightmap.Infinity)
                            return WorldPhysics.MaximumHeight;

                        if(heightmapView.heightmap.OneWay && h - heightmapView.heightmap.OneWayThickness > transformedReferenceY)
                            continue; // <- one-way platform is above us

                        h += heightmapView.position.Y;
                        if(h > maxHeight)
                            maxHeight = h;
                    }
                }
            }

            return maxHeight;
        }



        /// <summary>Find the lowest ceiling in some search range</summary>
        /// <param name="startY">Inclusive start height to start searching for ceilings</param>
        /// <param name="endY">Exclusive end height for the search</param>
        /// <returns>The height of the lowest found ceiling in range, or the endY height if none were found</returns>
        public int GetCeilingHeightInXRange(int startX, int endX, int z, int startY, int endY, object owner, bool staticOnly = false)
        {
            // NOTE: endY becomes our return value

            Debug.Assert(endX > startX);
            Debug.Assert(endY > startY);

            if(startY <= 0) // There will never be a ceiling this low...
                startY = 1;

            // Handle level ceilings:
            if(levelCeiling != null)
            {
                for(int x = startX; x < endX; x++)
                {
                    int c = levelCeiling[x, z];
                    if(c >= startY && c < endY)
                        endY = c;
                }
            }


            // Handle objects
            IgnoreNode firstIgnored = GetFirstIgnored(owner);
            for(int i = 0; i < colliderCount; i++)
            {
                // NOTE: Conditions are checked in this order to improve cache behaviour!

                if(staticOnly)
                    if(!colliderIsStatic[i])
                        continue;

                if(!colliderBoundsX[i].Contains(startX, endX))
                    continue;

                if(!colliderEntries[i].boundsZ.Contains(z))
                    continue;

                if(ReferenceEquals(owner, colliderEntries[i].owner)) // Don't collide with ourselves
                    continue;

                var heightmapView = colliderEntries[i].heightmapView;
                if(heightmapView.position.Y < startY || heightmapView.position.Y >= endY)
                    continue;

                if(IsIgnored(firstIgnored, colliderEntries[i].owner)) // Don't collide with things we're holding, etc
                    continue;

                if(heightmapView.heightmap.OneWay)
                    continue; // one-way platforms do not generate ceilings


                int transformedStartX;
                int transformedEndX;
                if(!heightmapView.flipX)
                {
                    transformedStartX = startX - heightmapView.position.X;
                    transformedEndX = endX - heightmapView.position.X;
                }
                else
                {
                    transformedStartX = -(endX - heightmapView.position.X - 1);
                    transformedEndX = -(startX - heightmapView.position.X - 1);
                }

                int transformedZ = z - heightmapView.position.Z;

                for(int transformedX = transformedStartX; transformedX < transformedEndX; transformedX++)
                {
                    int h = heightmapView.heightmap[transformedX, transformedZ];
                    if(h != 0) // If there is data in the heightmap, there is a ceiling here (objects always have flat bottoms)
                    {
                        endY = heightmapView.position.Y;
                        goto nextObject;
                    }
                }

            nextObject:
                ;
            }

            return endY;
        }



        /// <summary>
        /// For a given position at Z=0 (in world coordinates), unproject that point into the heightmap to find either
        /// a point on the surface of the heightmap, or a point above the surface of the hightmap where
        /// unprojecting further would intersect the heightmap.
        /// </summary>
        public Position GetPointAboveSurface(int x, int y)
        {
            Position bestPosition = levelHeightmap.GetPointAboveSurface(x, y);

            for(int i = 0; i < colliderCount; i++)
            {
                if(!colliderBoundsX[i].Contains(x))
                    continue;


                var heightmapView = colliderEntries[i].heightmapView;

                // Transform X to be in front of the unflipped heightmap:
                int xx = x - heightmapView.position.X;
                if(heightmapView.flipX)
                    xx = -xx;

                Debug.Assert(!(xx < heightmapView.heightmap.StartX || xx >= heightmapView.heightmap.EndX)); // <- earlier range check was correct

                if(xx < heightmapView.heightmap.StartX || xx >= heightmapView.heightmap.EndX)
                {
                    Debug.Assert(false); // <- should have hit the earlier range check...
                    continue; // out of range
                }

                // Transform Y to be at the "Z=0" position of the heightmap on the ground:
                int yy = y - heightmapView.position.Y - heightmapView.position.Z;

                Position position = heightmapView.heightmap.GetPointAboveSurface(xx, yy);

                Debug.Assert(heightmapView.heightmap.DefaultHeight == 0);
                if(position.Y == 0)
                    continue; // Hit blank space

                // Transform position back to world space:
                position.X = x; // <- too lazy to revert the flip and translate...
                position.Y += heightmapView.position.Y;
                position.Z += heightmapView.position.Z;

                // The best position is the one closest to the camera
                if(position.Z < bestPosition.Z || (position.Z == bestPosition.Z && position.Y > bestPosition.Y))
                    bestPosition = position;
            }

            return bestPosition;
        }



        public static readonly Point NoFitFound = new Point(int.MinValue, int.MinValue);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false), SuppressUnmanagedCodeSecurity]
        private static unsafe extern void* memset(void* dest, int c, UIntPtr count);

        public unsafe Point TryToFitNearestInZSlice(int objectStartX, int objectEndX, int objectHeight,
                    int desiredX, int desiredY, int referenceY,
                    int worldStartX, int worldEndX, int worldZ, int worldStartY, int worldEndY,
                    object owner, bool staticOnly)
        {
            const int align32 = 5;

            if(objectEndX - objectStartX <= 0)
                throw new ArgumentOutOfRangeException("objectEndX", "Object width must be positive and non-zero.");
            if(worldEndX - worldStartX > 256 || worldEndY - worldStartY > 256) // (test before clipping, for more predictable error)
                throw new ArgumentException("World test region too large"); // protect from huge stackalloc


            // Clip the test region to viable positions inside the level:
            worldStartY = Math.Max(0, worldStartY);
            worldStartX = Math.Max(StartX - objectStartX, worldStartX);
            worldEndX = Math.Min(EndX - objectEndX + 1, worldEndX); // +1 because both are exclusive

            // Check our test region actually exists
            if(worldZ < StartZ || worldZ >= EndZ) // (and is inside the level)
                return NoFitFound;
            if(worldStartY >= worldEndY)
                return NoFitFound;
            if(worldStartX >= worldEndX)
                return NoFitFound;


            // Allocate memory for storing test results:
            int testWidth = worldEndX - worldStartX; // bits
            int testHeight = worldEndY - worldStartY;
            int testStrideDWords = (testWidth + 31) >> align32;
            int testSize = testStrideDWords * testHeight;
            Debug.Assert(testWidth > 0 && testHeight > 0 && testWidth <= 256 && testHeight <= 256); // safety for stackalloc (should have exited earlier)

            uint* test = stackalloc uint[testSize];
            memset(test, 0, (UIntPtr)testSize); // The C# spec on stack allocation: "The content of the newly allocated memory is undefined."


            // Minkowski

            // How large is the area we look at in the world?
            int searchStartX = worldStartX + objectStartX;
            int searchEndX = worldEndX + objectEndX - 1; // -1 because both are exclusive
            int searchStartY = worldStartY;
            int searchEndY = worldEndY + objectHeight - 1; // -1 because both are exclusive
            int searchZ = worldZ;


            // Handle the level + ceiling
            MinkowskiSliceFillHelper(objectStartX, objectEndX, objectHeight, referenceY,
                        worldStartX, worldEndX, worldZ, worldStartY, worldEndY,
                        testWidth, testHeight, testStrideDWords, testSize, test,
                        new HeightmapView(levelHeightmap, Position.Zero, false), isCeiling: false);

            if(levelCeiling != null)
            {
                MinkowskiSliceFillHelper(objectStartX, objectEndX, objectHeight, referenceY,
                        worldStartX, worldEndX, worldZ, worldStartY, worldEndY,
                        testWidth, testHeight, testStrideDWords, testSize, test,
                        new HeightmapView(levelCeiling, Position.Zero, false), isCeiling: true);
            }


            // Handle objects
            IgnoreNode firstIgnored = GetFirstIgnored(owner);
            for(int i = 0; i < colliderCount; i++)
            {
                // NOTE: Conditions are checked in this order to improve cache behaviour!

                if(staticOnly)
                    if(!colliderIsStatic[i])
                        continue;

                if(!colliderBoundsX[i].Contains(searchStartX, searchEndX))
                    continue;

                if(!colliderEntries[i].boundsZ.Contains(searchZ))
                    continue;

                if(ReferenceEquals(owner, colliderEntries[i].owner)) // Don't collide with ourselves
                    continue;

                var heightmapView = colliderEntries[i].heightmapView;
                if(heightmapView.position.Y >= searchEndY) // Object is above the search area
                    continue;
                if(heightmapView.position.Y + Heightmap.Infinity < searchStartY) // Object is below the search area
                    continue;

                if(IsIgnored(firstIgnored, colliderEntries[i].owner)) // Don't collide with things we're holding, etc
                    continue;

                // One-way platforms with zero thickness have no contribution to separation:
                if(heightmapView.heightmap.OneWay && heightmapView.heightmap.OneWayThickness == 0)
                    continue;


                MinkowskiSliceFillHelper(objectStartX, objectEndX, objectHeight, referenceY,
                        worldStartX, worldEndX, worldZ, worldStartY, worldEndY,
                        testWidth, testHeight, testStrideDWords, testSize, test,
                        heightmapView, isCeiling: false);
            }



            //
            // At this point, the test buffer is filled with bits indiciating where we cannot fit.
            // Now it's a simple matter of searching for the closest bit where we *can* fit.
            //

            Point bestLocalPosition = new Point(-1, -1);
            int bestDistance = int.MaxValue;

            int localDesiredX = desiredX - worldStartX;
            int localDesiredY = desiredY - worldStartY;

            // PERF: Could potentially speed this up by searching from localDesiredY upwards,
            //       possibly doing an early-out once we have a good result, and then only searching
            //       in the downwards area in the range where we could get a better result.
            // PERF: Could also potentially improve speed by checking entire 32-bit blocks at once
            //       and shortcutting 0 and 0xFFFFFFFF. (Requires filling unused space.)
            //
            // NOTE: This loop is probably cycle-bound, as the buffer we are using is very likely already in cache
            for(int y = 0; y < testHeight; y++) for(int x = 0; x < testWidth; x++)
            {
                // NOTE: Using manhatten distance - worse result, but allows for a bunch of optimisations to happen
                //       (range reduction for other slices in DoSeparation, might be handy for testing 32-bit blocks)
                int distance = Math.Abs(x - localDesiredX) + Math.Abs(y - localDesiredY);

                // NOTE: Order of conditionals selected for best branch-predictor behaviour (hopefully; not tested -AR)
                if(distance < bestDistance)
                {
                    int bitPosition = x & 31;
                    uint bitMask = (1u << bitPosition);
                    int bufferPosition = (x >> align32) + y * testStrideDWords;

                    if((test[bufferPosition] & bitMask) == 0u)
                    {
                        bestDistance = distance;
                        bestLocalPosition.X = x;
                        bestLocalPosition.Y = y;
                    }
                }
            }

            if(bestLocalPosition.X == -1)
                return NoFitFound;

            return new Point(bestLocalPosition.X + worldStartX, bestLocalPosition.Y + worldStartY);
        }


        // PERF: Passing in some debug-only arguments here - maybe remove?
        private static unsafe void MinkowskiSliceFillHelper(int objectStartX, int objectEndX, int objectHeight, int referenceY,
                int worldStartX, int worldEndX, int worldZ, int worldStartY, int worldEndY,
                int testWidth, int testHeight, int testStrideDWords, int testSize, uint* test,
                HeightmapView heightmapView, bool isCeiling)
        {
            const int align32 = 5;


            int maskWidth = objectEndX - objectStartX;
            int maskStrideDWords = (maskWidth + 31) >> align32;
            int maskSize = maskStrideDWords + 1; // +1 allows shifting the mask into place
            if(maskWidth > 256)
                throw new InvalidOperationException("Object too wide"); // protect from huge stackalloc

            uint* mask = stackalloc uint[maskSize]; // <- NOTE: We overwrite the content, so we don't need to worry about clearing this


            int searchStartX = worldStartX + objectStartX;
            int searchEndX = worldEndX + objectEndX - 1; // -1 because both are exclusive
            var boundsX = heightmapView.BoundsX.Clip(searchStartX, searchEndX);

            int transformedReferenceY = referenceY - heightmapView.position.Y;

            for(int x = boundsX.start; x < boundsX.end; x++)
            {
                int height = heightmapView.GetUnelevatedHeightAt(x, worldZ);
                if(height == 0)
                    continue; // No solidity at this position
                if(!isCeiling && height == Heightmap.Infinity)
                    height = WorldPhysics.MaximumHeight;

                // Handle one-way-ness, by ignoring if we are too far below
                if(heightmapView.heightmap.OneWay && height - heightmapView.heightmap.OneWayThickness > transformedReferenceY)
                    continue;

                // Figure out the area blocked by this column:
                int minkowskiStartY, minkowskiEndY;
                if(isCeiling)
                {
                    minkowskiStartY = heightmapView.position.Y + height + 1 - objectHeight;
                    minkowskiEndY = worldEndY;
                }
                else
                {
                    minkowskiStartY = heightmapView.position.Y + 1 - objectHeight;
                    minkowskiEndY = heightmapView.position.Y + height;
                }
                if(minkowskiEndY <= worldStartY || minkowskiStartY >= worldEndY)
                    continue; // does not reach into the test area
                int minkowskiStartX = x + 1 - objectEndX;
                int minkowskiEndX = x + 1 - objectStartX;

                // Clip to the test area:
                minkowskiStartY = Math.Max(worldStartY, minkowskiStartY);
                minkowskiEndY = Math.Min(worldEndY, minkowskiEndY);
                minkowskiStartX = Math.Max(worldStartX, minkowskiStartX);
                minkowskiEndX = Math.Min(worldEndX, minkowskiEndX);

                // Figure out what to write:
                int writeStartXBits = minkowskiStartX - worldStartX;
                int writeEndXBits = minkowskiEndX - worldStartX;
                Debug.Assert(writeStartXBits >= 0 && writeStartXBits <= testWidth);
                Debug.Assert(writeEndXBits >= 0 && writeEndXBits <= testWidth);
                Debug.Assert(writeStartXBits <= writeEndXBits);

                int writeStartXDWord = writeStartXBits >> align32; // round down
                int writeEndXDWord = (writeEndXBits + 31) >> align32; // round up

                int writeWidthDWords = writeEndXDWord - writeStartXDWord;
                Debug.Assert(writeWidthDWords > 0);
                Debug.Assert(writeWidthDWords <= maskSize);

                int writeStartY = minkowskiStartY - worldStartY;
                int writeEndY = minkowskiEndY - worldStartY;
                Debug.Assert(writeStartY >= 0 && writeStartY <= testHeight);
                Debug.Assert(writeEndY >= 0 && writeEndY <= testHeight);
                Debug.Assert(writeStartY <= writeEndY);

                // Create mask:
                for(int w = 1; w < writeWidthDWords; w++) // skip [0], it is written directly
                    mask[w] = uint.MaxValue;
                int startBitsToClear = writeStartXBits & 31;
                mask[0] = ~((1u << startBitsToClear) - 1);
                int endBitsToKeep = writeEndXBits & 31;
                if(endBitsToKeep != 0) // <- end is on boundary (keep all bits)
                    mask[writeWidthDWords - 1] &= ((1u << endBitsToKeep) - 1);

                
#if DEBUG // Are we writing enough bits?
                {
                    int maskBitsExpected = writeEndXBits - writeStartXBits;
                    int count = 0;
                    for(int w = 0; w < writeWidthDWords; w++)
                    {
                        Debug.Assert(mask[w] != 0);

                        // https://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel
                        uint v = mask[w] - ((mask[w] >> 1) & 0x55555555);
                        v = (v & 0x33333333) + ((v >> 2) & 0x33333333);
                        uint c = ((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24; // count
                        count += (int)c;
                    }
                    Debug.Assert(count == maskBitsExpected);
                }
#endif


                // Write the mask:
                for(int y = writeStartY; y < writeEndY; y++)
                {
                    for(int w = 0; w < writeWidthDWords; w++)
                    {
                        int testWriteDWord = w + writeStartXDWord + (y * testStrideDWords);
                        Debug.Assert((uint)testWriteDWord < (uint)testSize); // bounds check
                        Debug.Assert((uint)w < (uint)maskSize);
                        test[testWriteDWord] |= mask[w];
                    }
                }
            }
        }

        #endregion



        #region Lookups

        /// <summary>
        /// NOTE: Does not handle one-way-ness (assumes everything is solid)
        /// </summary>
        public T FindTypedColliderInColumn<T>(int x, int z, int startY, int endY, object owner) where T : class
        {
            IgnoreNode firstIgnored = GetFirstIgnored(owner);

            for(int i = 0; i < colliderCount; i++)
            {
                // NOTE: Conditions are checked in this order to improve cache behaviour!

                if(!colliderBoundsX[i].Contains(x))
                    continue;

                if(ReferenceEquals(owner, colliderEntries[i].owner)) // Don't collide with ourselves
                    continue;

                T result = colliderEntries[i].owner as T;
                if(result == null)
                    continue;

                if(!colliderEntries[i].boundsZ.Contains(z))
                    continue;

                var heightmapView = colliderEntries[i].heightmapView;
                if(heightmapView.position.Y >= endY) // Object is above us
                    continue;

                if(IsIgnored(firstIgnored, colliderEntries[i].owner)) // Don't collide with things we're holding, etc
                    continue;


                int xx = x - heightmapView.position.X;
                if(heightmapView.flipX)
                    xx = -xx;
                int zz = z - heightmapView.position.Z;

                int h = heightmapView.heightmap[xx, zz];
                if(h != 0)
                {
                    h += heightmapView.position.Y;
                    if(startY < h)
                        return result;
                }
            }

            return null;
        }

        #endregion



        #region Local Heightmap Generation (for AI)

        // This is faster than memcpy for our use case
        // http://code4k.blogspot.com.au/2010/10/high-performance-memcpy-gotchas-in-c.html
        static unsafe void SimpleMemcpy(void* dest, void* src, int count)
        {
            int block = count >> 3;

            long* pDest = (long*)dest;
            long* pSrc = (long*)src;
            for(int i = 0; i < block; i++)
            {
                *pDest = *pSrc; pDest++; pSrc++;
            }
            dest = pDest;
            src = pSrc;
            count = count - (block << 3);

            if(count > 0)
            {
                byte* pDestB = (byte*)dest;
                byte* pSrcB = (byte*)src;
                for(int i = 0; i < count; i++)
                {
                    *pDestB = *pSrcB; pDestB++; pSrcB++;
                }
            }
        }


        public unsafe void FillLocalHeightmap(byte* data, int startX, int endX, int startZ, int endZ, int startY, int referenceY, int endY, object owner, bool staticOnly = false)
        {
            Debug.Assert(startX < endX);
            Debug.Assert(startZ < endZ);
            int width = endX - startX;
            int depth = endZ - startZ;


            // Handle level heightmap:
            {
                int relativeHeightmapStartX = levelHeightmap.heightmapData.OffsetX - startX;
                int relativeHeightmapStartZ = levelHeightmap.heightmapData.OffsetY - startZ;
                int relativeHeightmapEndX = relativeHeightmapStartX + levelHeightmap.heightmapData.Width;
                int relativeHeightmapEndZ = relativeHeightmapStartZ + levelHeightmap.heightmapData.Height;

                // Check we're not entirely outside the level:
                if(relativeHeightmapStartX >= width || relativeHeightmapEndX <= 0 || relativeHeightmapStartZ >= depth || relativeHeightmapEndZ <= 0)
                {
                    memset(data, byte.MaxValue, (UIntPtr)(width*depth));
                    return;
                }

                // Check what area of the output we're overlapping
                bool needToClearOutOfBounds = false;
                int clampedStartX, clampedStartZ, clampedEndX, clampedEndZ;

                if(relativeHeightmapStartX <= 0)
                    clampedStartX = 0;
                else
                {
                    clampedStartX = relativeHeightmapStartX;
                    needToClearOutOfBounds = true;
                }

                if(relativeHeightmapStartZ <= 0)
                    clampedStartZ = 0;
                else
                {
                    clampedStartZ = relativeHeightmapStartZ;
                    needToClearOutOfBounds = true;
                }

                if(relativeHeightmapEndX >= width)
                    clampedEndX = width;
                else
                {
                    clampedEndX = relativeHeightmapEndX;
                    needToClearOutOfBounds = true;
                }

                if(relativeHeightmapEndZ >= depth)
                    clampedEndZ = depth;
                else
                {
                    clampedEndZ = relativeHeightmapEndZ;
                    needToClearOutOfBounds = true;
                }

                // TODO: PERF: Strictly speaking, we don't have to clear all of the memory (just the regions that are out-of-bounds)
                if(needToClearOutOfBounds)
                    memset(data, byte.MaxValue, (UIntPtr)(width*depth));


                if(startY == 0) // NOTE: Fast path at ground level:
                {
                    fixed(byte* levelHeightmapData = levelHeightmap.heightmapData.Data)
                    {
                        for(int z = clampedStartZ; z < clampedEndZ; z++)
                        {
                            byte* outputRow = data + z*width;
                            int inputRow = (z-relativeHeightmapStartZ) * levelHeightmap.heightmapData.Width;

                            SimpleMemcpy(outputRow + clampedStartX,
                                    levelHeightmapData + inputRow + (clampedStartX - relativeHeightmapStartX),
                                    (clampedEndX - clampedStartX));
                        }
                    }
                }
                else // Slow path needs to apply an offset:
                {
                    for(int z = clampedStartZ; z < clampedEndZ; z++)
                    {
                        byte* outputRow = data + z*width;
                        int inputRow = (z-relativeHeightmapStartZ) * levelHeightmap.heightmapData.Width;

                        for(int x = clampedStartX; x < clampedEndX; x++)
                        {
                            int h = levelHeightmap.heightmapData.Data[inputRow + (x - relativeHeightmapStartX)];
                            if(h != Heightmap.Infinity)
                                h = Math.Max(0, Math.Min(255, (h - startY)));

                            outputRow[x] = (byte)h;
                        }
                    }
                }
            }

            // Handle level ceiling
            if(levelCeiling != null)
            {
                int relativeHeightmapStartX = levelCeiling.heightmapData.OffsetX - startX;
                int relativeHeightmapStartZ = levelCeiling.heightmapData.OffsetY - startZ;
                int relativeHeightmapEndX = relativeHeightmapStartX + levelCeiling.heightmapData.Width;
                int relativeHeightmapEndZ = relativeHeightmapStartZ + levelCeiling.heightmapData.Height;

                if(!(relativeHeightmapStartX >= width || relativeHeightmapEndX <= 0 || relativeHeightmapStartZ >= depth || relativeHeightmapEndZ <= 0))
                {
                    int clampedStartX = Math.Max(0, relativeHeightmapStartX);
                    int clampedStartZ = Math.Max(0, relativeHeightmapStartZ);
                    int clampedEndX = Math.Min(width, relativeHeightmapEndX);
                    int clampedEndZ = Math.Min(depth, relativeHeightmapEndZ);

                    for(int z = clampedStartZ; z < clampedEndZ; z++)
                    {
                        byte* outputRow = data + z*width;
                        int inputRow = (z-relativeHeightmapStartZ) * levelCeiling.heightmapData.Width;

                        for(int x = clampedStartX; x < clampedEndX; x++)
                        {
                            int c = levelCeiling.heightmapData.Data[inputRow + (x - relativeHeightmapStartX)];
                            if(c != 0 && c < endY)
                                outputRow[x] = byte.MaxValue; // not really the ground, but we can't move here
                        }
                    }
                }
            }


            // Handle objects (NOTE: Copy-pasted query to HasLocalColliders)
            IgnoreNode firstIgnored = GetFirstIgnored(owner);
            for(int i = 0; i < colliderCount; i++)
            {
                // NOTE: Conditions are checked in this order to improve cache behaviour!

                if(staticOnly)
                    if(!colliderIsStatic[i])
                        continue;

                int clippedStartX = Math.Max(startX, colliderBoundsX[i].start);
                int clippedEndX = Math.Min(endX, colliderBoundsX[i].end);
                Debug.Assert((clippedEndX <= clippedStartX) == !colliderBoundsX[i].Contains(startX, endX) || colliderBoundsX[i].Size == 0); // <- matches the regular test (except when size = 0 and we don't care)
                if(clippedEndX <= clippedStartX)
                    continue;

                int clippedStartZ = Math.Max(startZ, colliderEntries[i].boundsZ.start);
                int clippedEndZ = Math.Min(endZ, colliderEntries[i].boundsZ.end);
                Debug.Assert((clippedEndZ <= clippedStartZ) == !colliderEntries[i].boundsZ.Contains(startZ, endZ) || colliderEntries[i].boundsZ.Size == 0); // <- matches the regular test (except when size = 0 and we don't care)
                if(clippedEndZ <= clippedStartZ)
                    continue;

                if(ReferenceEquals(owner, colliderEntries[i].owner)) // Don't collide with ourselves
                    continue;

                var heightmapView = colliderEntries[i].heightmapView;
                if(heightmapView.position.Y >= endY) // Object is above us
                    continue;

                if(IsIgnored(firstIgnored, colliderEntries[i].owner)) // Don't collide with things we're holding, etc
                    continue;


                int transformedReferenceY = referenceY - heightmapView.position.Y;
                int heightmapViewDirectionX = heightmapView.flipX ? -1 : 1;
                var heightmapData = heightmapView.heightmap.heightmapData;
                
                for(int z = clippedStartZ; z < clippedEndZ; z++)
                {
                    byte* p = data + (z - startZ) * width + (clippedStartX - startX);

                    // INLINED: heightmapView.GetUnelevatedHeightAt(x, z);
                    int zz = z - heightmapView.position.Z;
                    int rowStart = (zz - heightmapData.OffsetY) * heightmapData.Width;

                    for(int x = clippedStartX; x < clippedEndX; x++)
                    {
                        // INLINED: heightmapView.GetUnelevatedHeightAt(x, z);
                        // NOTE: Assuming that the cached bounds are sane, so we never read an out-of-bounds value!
                        int xx = (x - heightmapView.position.X) * heightmapViewDirectionX; // <- TODO: Can probably remove a sub here
                        int h = heightmapData.Data[(xx - heightmapData.OffsetX) + rowStart];

                        if(h != 0)
                        {
                            if(h == Heightmap.Infinity)
                            {
                                *p = byte.MaxValue;
                            }
                            else if(heightmapView.heightmap.OneWay && h - heightmapView.heightmap.OneWayThickness > transformedReferenceY)
                            {
                                // Do nothing
                            }
                            else
                            {
                                int relativeHeight = (h + heightmapView.position.Y) - startY;
                                *p = (byte)Math.Max(relativeHeight.Clamp(byte.MinValue, byte.MaxValue), *p);
                            }
                        }

                        p++;
                    }
                }
            }

        }

        /// <summary>NOTE: Matches the collider query for FillLocalHeightmap</summary>
        public unsafe bool HasLocalColliders(int startX, int endX, int startZ, int endZ, int referenceY, int endY, object owner, bool staticOnly = false)
        {
            IgnoreNode firstIgnored = GetFirstIgnored(owner);
            for(int i = 0; i < colliderCount; i++)
            {
                // NOTE: Conditions are checked in this order to improve cache behaviour!

                if(staticOnly)
                    if(!colliderIsStatic[i])
                        continue;

                int clippedStartX = Math.Max(startX, colliderBoundsX[i].start);
                int clippedEndX = Math.Min(endX, colliderBoundsX[i].end);
                Debug.Assert((clippedEndX <= clippedStartX) == !colliderBoundsX[i].Contains(startX, endX) || colliderBoundsX[i].Size == 0); // <- matches the regular test (except when size = 0 and we don't care)
                if(clippedEndX <= clippedStartX)
                    continue;

                int clippedStartZ = Math.Max(startZ, colliderEntries[i].boundsZ.start);
                int clippedEndZ = Math.Min(endZ, colliderEntries[i].boundsZ.end);
                Debug.Assert((clippedEndZ <= clippedStartZ) == !colliderEntries[i].boundsZ.Contains(startZ, endZ) || colliderEntries[i].boundsZ.Size == 0); // <- matches the regular test (except when size = 0 and we don't care)
                if(clippedEndZ <= clippedStartZ)
                    continue;

                if(ReferenceEquals(owner, colliderEntries[i].owner)) // Don't collide with ourselves
                    continue;

                var heightmapView = colliderEntries[i].heightmapView;
                if(heightmapView.position.Y >= endY) // Object is above us
                    continue;

                if(IsIgnored(firstIgnored, colliderEntries[i].owner)) // Don't collide with things we're holding, etc
                    continue;

                // Found one!
                return true;
            }

            return false;
        }

        #endregion


    }
}
