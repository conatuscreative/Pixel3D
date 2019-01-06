// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;
using Pixel3D.Animations;

namespace Pixel3D.Sorting
{

    public static class DrawOrdering
    {

        public delegate void ReportComparisonDelegate(AnimationSet aAnimationSet, AnimationSet bAnimationSet,
                Position aPosition, Position bPosition, bool aFlipX, bool bFlipX,
                int aYSlice, int bYSlice, int zAccumulate, int zTieBreak, int zAbove);


        /// <summary>Check if a mover is above a static object by physics heightmap</summary>
        private static bool DoAboveCheck(AnimationSet staticAnimationSet, AnimationSet moverAnimationSet, Position staticPosition, Position moverPosition, bool staticFlipX, bool moverFlipX)
        {
            HeightmapView heightmapView = new HeightmapView(staticAnimationSet.Heightmap, staticPosition, staticFlipX);

            if(!heightmapView.BoundsZ.Contains(moverPosition.Z))
                return false; // out of range

            var staticRangeX = heightmapView.BoundsX;
            var moverRangeX = new Range(moverAnimationSet.physicsStartX, moverAnimationSet.physicsEndX).MaybeFlip(moverFlipX) + moverPosition.X;
            var rangeX = moverRangeX.Clip(staticRangeX.start, staticRangeX.end);

            for(int x = rangeX.start; x < rangeX.end; x++)
            {
                int height = heightmapView.GetUnelevatedHeightAt(x, moverPosition.Z);
                if(height == 0 || height == Heightmap.Infinity)
                    continue;

                height += heightmapView.position.Y;
                if(moverPosition.Y >= height)
                    return true; // <- above the object at at least one position
            }

            return false;
        }


        /// <summary>Returns a negative number if a is behind b, returns a positive number if b is behind a</summary>
        public static int Compare(AnimationSet aAnimationSet, AnimationSet bAnimationSet, Position aPosition, Position bPosition, bool aFlipX, bool bFlipX, ReportComparisonDelegate report)
        {
            // Try front/back first (because we don't have to look at the heightmap)
            {
                int aFront = aPosition.Z + aAnimationSet.physicsStartZ; // inclusive
                int aBack  = aPosition.Z + aAnimationSet.physicsEndZ;   // exclusive
                int bFront = bPosition.Z + bAnimationSet.physicsStartZ; // inclusive
                int bBack  = bPosition.Z + bAnimationSet.physicsEndZ;   // exclusive
                if(aFront >= bBack)
                    return -1;
                if(bFront >= aBack)
                    return 1;

                // Check co-planar priority:
                if(aFront == bFront)
                {
                    if(aFront == aBack && aAnimationSet.coplanarPriority > bAnimationSet.coplanarPriority)
                        return 1;
                    if(bFront == bBack && bAnimationSet.coplanarPriority > aAnimationSet.coplanarPriority)
                        return -1;
                }
            }


            // Try vertical position
            {
                int aBottom = aPosition.Y; // inclusive
                int aTop    = aPosition.Y + aAnimationSet.physicsHeight; // exclusive
                int bBottom = bPosition.Y; // inclusive
                int bTop    = bPosition.Y + bAnimationSet.physicsHeight; // exclusive
                if(aBottom >= bTop)
                    return 1;
                if(bBottom >= aTop)
                    return -1;
            }

            // Special handling for carpets (a little bit of a hack)
            if(aAnimationSet.physicsHeight == 0 || bAnimationSet.physicsHeight == 0)
            {
                if(aPosition.Y > bPosition.Y)
                    return 1;
                else if(aPosition.Y < bPosition.Y)
                    return -1;
            }


            // Handle the case where an object requests an "above" check:
            bool aDoAboveCheck = aAnimationSet.doAboveCheck & (aAnimationSet.Heightmap != null);
            bool bDoAboveCheck = bAnimationSet.doAboveCheck & (bAnimationSet.Heightmap != null);
            if(aDoAboveCheck | bDoAboveCheck)
            {
                bool bAboveA = false, aAboveB = false;

                if(aDoAboveCheck)
                    bAboveA = DoAboveCheck(aAnimationSet, bAnimationSet, aPosition, bPosition, aFlipX, bFlipX);
                if(bDoAboveCheck)
                    aAboveB = DoAboveCheck(bAnimationSet, aAnimationSet, bPosition, aPosition, bFlipX, aFlipX);

                if(bAboveA | aAboveB)
                {
                    if(aAboveB)
                        return 1;
                    else if(bAboveA)
                        return -1;
                    else
                        return 0; // <- both are above
                }
            }


            // Handle the case where we don't have the data to do a depth-bound check
            if(aAnimationSet.depthBounds.slices == null || bAnimationSet.depthBounds.slices == null)
                return 0;

            // At this point, object bounds are intersecting. Use the tighter wrapped bounds:
            {
                // Find the minimum shared Y-slice on both objects
                int worldHorizontalSlice = Math.Max(aPosition.Y, bPosition.Y);
                int aHorizontalSlice = worldHorizontalSlice - aPosition.Y;
                int bHorizontalSlice = worldHorizontalSlice - bPosition.Y;

                // Get the bounds for that slice
                DepthSlice aDepthBounds = aAnimationSet.depthBounds.GetSlice(aHorizontalSlice);
                DepthSlice bDepthBounds = bAnimationSet.depthBounds.GetSlice(bHorizontalSlice);

                int aWorldStartX;
                if(!aFlipX)
                    aWorldStartX = aPosition.X + aDepthBounds.xOffset;
                else
                    aWorldStartX = aPosition.X + (1 - (aDepthBounds.xOffset + aDepthBounds.Width));
                int aWorldEndX = aWorldStartX + aDepthBounds.Width;

                int bWorldStartX;
                if(!bFlipX)
                    bWorldStartX = bPosition.X + bDepthBounds.xOffset;
                else
                    bWorldStartX = bPosition.X + (1 - (bDepthBounds.xOffset + bDepthBounds.Width));
                int bWorldEndX = bWorldStartX + bDepthBounds.Width;


                // Region of the overlap (if one exists)
                int worldStartX = Math.Max(aWorldStartX, bWorldStartX);
                int worldEndX = Math.Min(aWorldEndX, bWorldEndX);
                int count = worldEndX - worldStartX;

                int aDirection = aFlipX ? -1 : 1;
                int bDirection = bFlipX ? -1 : 1;

                int aStartIndex, bStartIndex;
                if(count > 0) // We have an overlap
                {
                    aStartIndex = worldStartX - aWorldStartX;
                    bStartIndex = worldStartX - bWorldStartX;
                }
                else // Just check one pixel from the edge of each. NOTE: This allows us to disregard bounding boxes when comparing depths.
                {
                    count = 1;

                    if(aWorldEndX <= bWorldStartX)
                    {
                        aStartIndex = aDepthBounds.Width - 1;
                        bStartIndex = 0;
                    }
                    else
                    {
                        Debug.Assert(bWorldEndX <= aWorldStartX);
                        aStartIndex = 0;
                        bStartIndex = bDepthBounds.Width - 1;
                    }
                }

                if(aFlipX)
                    aStartIndex = aDepthBounds.Width - aStartIndex - 1;
                if(bFlipX)
                    bStartIndex = bDepthBounds.Width - bStartIndex - 1;

                int aZStart = aPosition.Z + aDepthBounds.zOffset;
                int bZStart = bPosition.Z + bDepthBounds.zOffset;

                int zAccumulate = 0;
                int frontMostPixel = int.MaxValue;
                int zTieBreak = 0; // <- who has the frontmost pixel?
                int zAbove = 0;
                for(int i = 0; i < count; i++)
                {
                    int aFront = aDepthBounds.depths[i * aDirection + aStartIndex].front;
                    int aBack  = aDepthBounds.depths[i * aDirection + aStartIndex].back - 1; // inclusive
                    int bFront = bDepthBounds.depths[i * bDirection + bStartIndex].front;
                    int bBack  = bDepthBounds.depths[i * bDirection + bStartIndex].back - 1; // inclusive

                    int aWorldFront = aZStart + aFront;
                    int aWorldBack  = aZStart + aBack;
                    int bWorldFront = bZStart + bFront;
                    int bWorldBack  = bZStart + bBack;

                    bool aBelow = (aWorldFront == aWorldBack + 2);
                    bool bBelow = (bWorldFront == bWorldBack + 2);


                    if(aBelow && bBelow)
                    {
                        // No useful data
                    }
                    else if(aBelow) // NOTE: We could do fancier things with the "back" value (accumulate behind if we are behind)
                    {
                        zAbove--;
                    }
                    else if(bBelow)
                    {
                        zAbove++;
                    }
                    else
                    {
                        // In hindsight this is not a great test, because it doesn't preference "who has the frontmost overlapping pixel"
                        // so it doesn't work so well for cover-shims on walls, which we expect to interpenetrate with things like boxes
                        int aBehindBy = Math.Max(0, aWorldFront - bWorldBack);
                        int bBehindBy = Math.Max(0, bWorldFront - aWorldBack);

                        if(aBehindBy != 0 || bBehindBy != 0)
                        {
                            zAccumulate -= aBehindBy;
                            zAccumulate += bBehindBy;
                        }
                        else
                        {
                            // Kind of a hack: if we are interpenetrating, but we have the very frontmost pixel, pay a lot of attention to that!
                            // (This may be unnecessary / unwanted given the code below that does a proper frontmost check)
                            if(aZStart < bZStart && aFront == 0)
                                zAccumulate += 4;
                            if(bZStart < aZStart && bFront == 0)
                                zAccumulate -= 4;
                        }


                        // zTieBreak += bWorldFront - aWorldFront; // <- old version (accumulate points)
                        if(aWorldFront == bWorldFront)
                        {
                            if(aWorldFront < frontMostPixel)
                            {
                                frontMostPixel = aWorldFront;
                                zTieBreak = 0;
                            }
                        }
                        else if(aWorldFront < bWorldFront)
                        {
                            if(aWorldFront < frontMostPixel)
                            {
                                frontMostPixel = aWorldFront;
                                zTieBreak = 1;
                            }
                        }
                        else // bWorldFront < aWorldFront
                        {
                            if(bWorldFront < frontMostPixel)
                            {
                                frontMostPixel = bWorldFront;
                                zTieBreak = -1;
                            }
                        }

                    }
                }


                if(report != null)
                    report(aAnimationSet, bAnimationSet, aPosition, bPosition, aFlipX, bFlipX, aHorizontalSlice, bHorizontalSlice, zAccumulate, zTieBreak, zAbove);

                if(zAccumulate != 0)
                    return zAccumulate;
                else if(zTieBreak != 0)
                    return zTieBreak;
                else
                    return zAbove;
            }
        }

    }

}
