using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Pixel3D.Animations;

namespace Pixel3D
{
    public class Heightmap
    {
        public Heightmap(byte defaultHeight = 0)
        {
            this.DefaultHeight = defaultHeight;
            this.heightmapData = default(Data2D<byte>); // Does not include buffer (but that's ok because zero-sized bounds ensure it won't be accessed)
        }

        public Data2D<byte> heightmapData;
        public byte DefaultHeight;

        public bool OneWay;
        public byte OneWayThickness; // 0 for standard one-way platform. Small value for some thickness to the one-way-ness of the platform



        public List<HeightmapInstruction> instructions;


        public bool HasData { get { return heightmapData.HasData; } }


        public Heightmap Clone()
        {
            return new Heightmap() { DefaultHeight = this.DefaultHeight, heightmapData = this.heightmapData.Clone() };
        }

        public Heightmap CloneAndFlipX()
        {
            Heightmap heightmap = Clone();
            heightmap.heightmapData.FlipXInPlace();
            return heightmap;
        }


        public int OffsetX { get { return heightmapData.OffsetX; } set { heightmapData.OffsetX = value; } }
        public int OffsetZ { get { return heightmapData.OffsetY; } set { heightmapData.OffsetY = value; } }
        public int Width { get { return heightmapData.Width; } }
        public int Depth { get { return heightmapData.Height; } }

        /// <summary>Bounds of the heightmap in the XZ plane</summary>
        public Rectangle Bounds { get { return new Rectangle(OffsetX, OffsetZ, Width, Depth); } }

        /// <summary>Inclusive X-axis minimum bound</summary>
        public int StartX { get { return OffsetX; } }
        /// <summary>Inclusive Z-axis minimum bound</summary>
        public int StartZ { get { return OffsetZ; } }
        /// <summary>Exclusive X-axis maximum bound</summary>
        public int EndX { get { return OffsetX + Width; } }
        /// <summary>Exclusive Z-axis maximum bound</summary>
        public int EndZ { get { return OffsetZ + Depth; } }



        /// <summary>Special case sentinal value that represents an unpassable wall</summary>
        public const byte Infinity = byte.MaxValue;


        /// <summary>Get a value from the heightmap. Or the default height if outside of the available data.</summary>
        public byte this[int x, int z]
        {
            get
            {
                return heightmapData.GetOrDefault(x, z, DefaultHeight);
            }
            set
            {
                heightmapData[x, z] = value;
            }
        }



        public void Optimise()
        {
            Rectangle extents = heightmapData.FindTrimBounds(DefaultHeight);
            if(heightmapData.Bounds != extents)
                heightmapData = heightmapData.CopyWithNewBounds(extents, DefaultHeight);
        }

        // TODO: This is disabled at the moment, because it will probably make authoring difficult...
        //       (Also it's less of a big deal than physics heightmaps)
        public void OptimiseForShadowReceiver()
        {
            if(heightmapData.Data == null)
                return; // <- nothing to optimise

            Debug.Assert(heightmapData.Data.Length > 0); // <- Data2D should be enforcing this condition

            // See if we have a heightmap filled with just a single value:
            byte startValue = heightmapData.Data[0];
            for(int i = 0; i < heightmapData.Data.Length; i++)
                if(heightmapData.Data[i] != startValue)
                    goto hasMoreThanOneValue;
            // Convert to fixed-height:
            DefaultHeight = startValue;
            heightmapData = new Data2D<byte>();
            return;
        hasMoreThanOneValue:

            Rectangle extents = heightmapData.FindTrimBoundsForShadowReceiver();

            // Assert that we're shrinking (because if we expand, then we're adding in wrong data, due to DefaultHeight)
            Debug.Assert(extents.Left >= heightmapData.Bounds.Left);
            Debug.Assert(extents.Right <= heightmapData.Bounds.Right);
            Debug.Assert(extents.Top >= heightmapData.Bounds.Top);
            Debug.Assert(extents.Bottom <= heightmapData.Bounds.Bottom);

            if(heightmapData.Bounds != extents)
                heightmapData = heightmapData.CopyWithNewBounds(extents, DefaultHeight);
        }



        /// <param name="baseHeightmap">NOTE: This gets used as a proxy for "is this a shadow receiver"</param>
        public void RefreshFromInstructions(Heightmap baseHeightmap)
        {
            if(instructions != null) // No instructions means we are raw data (do not touch!)
            {
                ClearToHeight(0); // <- Start blank.

                foreach(var instruction in instructions)
                {
                    instruction.Process(this, baseHeightmap);
                }

                if(baseHeightmap == null)
                    Optimise();
            }
        }


        #region Serialization

        public void Serialize(AnimationSerializeContext context)
        {
            context.bw.Write(DefaultHeight);

            context.bw.Write(OneWay);
            context.bw.Write(OneWayThickness);

            if(context.bw.WriteBoolean(HasData))
            {
                context.bw.Write(heightmapData.Bounds);
                context.bw.Write(heightmapData.Data, 0, heightmapData.Width * heightmapData.Height);
            }

            HeightmapInstructionExtensions.Serialize(instructions, context);
        }

        public Heightmap(AnimationDeserializeContext context)
        {
            DefaultHeight = context.br.ReadByte();

            OneWay = context.br.ReadBoolean();
            OneWayThickness = context.br.ReadByte();

            if(context.br.ReadBoolean())
            {
                Rectangle bounds = context.br.ReadRectangle();
                byte[] data = context.br.ReadBytes(bounds.Width * bounds.Height);
                heightmapData = new Data2D<byte>(data, bounds);
            }
            else
            {
                heightmapData = default(Data2D<byte>);
            }

            instructions = HeightmapInstructionExtensions.Deserialize(context);
        }

        #endregion



        #region Write to Heightmap

        public void ClearToHeight(byte height)
        {
            DefaultHeight = height;
            heightmapData = default(Data2D<byte>);
        }


        /// <summary>Set an area to a flat height given a mask of the base of the object</summary>
        public void SetFromFlatBaseMask(MaskData maskData, byte height)
        {
            // Ensure that there's enough room in the heightmap to contain mask...
            heightmapData = heightmapData.LazyCopyExpandToContain(maskData.Bounds, DefaultHeight);

            for(int y = maskData.StartY; y < maskData.EndY; y++) for(int x = maskData.StartX; x < maskData.EndX; x++)
            {
                if(maskData[x, y])
                    heightmapData[x, y] = height;
            }
        }

        /// <summary>Set an area to a flat height given a mask of the top of the object.</summary>
        public void SetFromFlatTopMask(MaskData maskData, byte height)
        {
            // Just convert it to a "base" mask and use that...
            SetFromFlatBaseMask(maskData.Translated(0, -(int)height), height);
        }

        /// <summary>Set an area to a flat height, relative to the given height of the input mask</summary>
        public void SetFlatRelative(MaskData maskData, byte height, int offset)
        {
            var baseMask = maskData.Translated(0, -(int)height); // Convert a "top" mask
            int absoluteHeight = Math.Max(byte.MinValue, Math.Min(byte.MaxValue, (int)height + offset));
            SetFromFlatBaseMask(baseMask, (byte)absoluteHeight);
        }

        
        /// <summary>Set heights from a top-surface mask where the front edge is at a particular depth</summary>
        /// <param name="maskData">The 1-bit mask representing the top surface</param>
        /// <param name="frontEdgeDepth">The depth of the front edge of pixels in the mask</param>
        /// <param name="perspective">Oblique direction that the mask projects backwards towards</param>
        public void SetFromObliqueTopMask(MaskData maskData, int frontEdgeDepth, Oblique oblique)
        {
            // Ensure that there's enough room in the heightmap to contain the maximum extents of the mask
            Rectangle outputPotentialBounds = maskData.Bounds;
            outputPotentialBounds.Y = frontEdgeDepth; // The usable Z range = [frontEdgeDepth, frontEdgeDepth + Height)
            heightmapData = heightmapData.LazyCopyExpandToContain(outputPotentialBounds, DefaultHeight);

            // Note: Y axis seeks from the top downwards (from back to front)
            for(int y = maskData.EndY - 1; y >= maskData.StartY; y--) for(int x = maskData.StartX; x < maskData.EndX; x++)
            {
                if(maskData[x, y])
                {
                    int height = GetHeightByWalkingObliqueForward(maskData, frontEdgeDepth, oblique, x, y);
                    int z = y - height;

                    heightmapData[x, z] = (byte)height; // (If height overflows... at least it will be obvious)
                }
            }
        }

        public int GetHeightByWalkingObliqueForward(MaskData maskData, int frontEdgeDepth, Oblique oblique, int x, int y)
        {
            // Try to walk forward in the mask to find the front edge, from which we have a specified depth, and can calculate the height
            while(true)
            {
                // NOTE: Don't need to do special handling of "upright" sections, because our caller works top-down through the image
                //       (So these sections will be overwritten as appropriate)

                int nextX = x - (int)oblique; // Walk forward (down the mask) in the oblique direction
                int nextY = y - 1;

                if(!maskData.GetOrDefault(nextX, nextY)) // Reached the front of the mask data
                    return y - frontEdgeDepth;

                x = nextX;
                y = nextY;
            }
        }


        /// <summary>Set from a 1px deep alpha mask (such as for a railing)</summary>
        public void SetFromRailingMask(MaskData maskData)
        {
            heightmapData = heightmapData.LazyCopyExpandToContain(new Rectangle(maskData.OffsetX, 0, maskData.Width, 1), DefaultHeight);

            for(int x = maskData.StartX; x < maskData.EndX; x++) // For each column in the image
            {
                for(int y = maskData.EndY - 1; y >= maskData.StartY; y--) // Search top-to-bottom
                {
                    if(maskData[x, y])
                    {
                        heightmapData[x, 0] = (byte)y;
                        goto nextColumn;
                    }
                }
            nextColumn:
                ;
            }
        }



        /// <param name="slope">Number of pixels to travel backwards before traveling in the oblique direction</param>
        public void SetFromFrontEdge(MaskData maskData, int frontEdgeDepth, int depth, Oblique obliqueDirection, int slope, int offset)
        {
            Debug.Assert(depth > 0);
            Debug.Assert(slope > 0);

            // How far do we travel on the X axis as we go backwards?
            int pixelsTraveledSideways = ((depth + slope - 1) / slope - 1) * (int)obliqueDirection;
            int outputStartX = Math.Min(maskData.StartX, maskData.StartX + pixelsTraveledSideways);
            int outputEndX = Math.Max(maskData.EndX, maskData.EndX + pixelsTraveledSideways);

            // Ensure that there's enough room in the heightmap to contain the maximum extents of the processed mask...
            Rectangle outputPotentialBounds = new Rectangle(outputStartX, frontEdgeDepth, outputEndX - outputStartX, depth);
            heightmapData = heightmapData.LazyCopyExpandToContain(outputPotentialBounds, DefaultHeight);


            // Read the mask upwards to find the "lip" of the mask surface
            for(int x = maskData.StartX; x < maskData.EndX; x++) // For each column in the image
            {
                for(int y = maskData.StartY; y < maskData.EndY; y++) // Search from bottom-to-top
                {
                    if(maskData[x, y])
                    {
                        // Found the lip at a given Y height, copy it backwards at the given pitch
                        for(int d = 0; d < depth; d++)
                        {
                            int zz = frontEdgeDepth + d;
                            int xx = x + (d / slope) * (int)obliqueDirection;

                            heightmapData[xx, zz] = (byte)(y - frontEdgeDepth + offset);
                        }

                        goto nextColumn;
                    }
                }

            nextColumn:
                ;
            }
        }


        public void SetFromObliqueSide(MaskData maskData, Oblique obliqueDirection, int offset)
        {
            // Straight and Right use the same input direction (because Straight input does not make sense, but Straight output is ok)
            int inputReadDirection = 1;
            int x = maskData.StartX;
            if(obliqueDirection == Oblique.Left)
            {
                inputReadDirection = -1;
                x = maskData.EndX - 1;
            }

            int y;
            while(x >= maskData.StartX && x < maskData.EndX)
            {
                for(y = maskData.StartY; y < maskData.EndY; y++) // read bottom-to-top
                {
                    if(maskData[x, y])
                        goto foundStartPosition;
                }
                x += inputReadDirection;
            }

            // No data found!
            return;

        foundStartPosition:

            // Ensure that there's enough room in the heightmap to contain the maximum extents of the processed mask...
            {
                int left, right;
                if(inputReadDirection == 1)
                {
                    left = x;
                    right = maskData.EndX - 1;
                }
                else // reading right-to-left
                {
                    left = maskData.StartX;
                    right = x;
                }
                // Account for offset:
                left += Math.Min(offset, 0);
                right += Math.Max(offset, 0);
                int front = y;
                int back = front + (right - left); // can move back one pixel for each column of input

                Rectangle outputPotentialBounds = new Rectangle(left, front, (right-left)+1, (back-front)+1);
                heightmapData = heightmapData.LazyCopyExpandToContain(outputPotentialBounds, DefaultHeight);
            }


            // Convert mask to heightmap:
            int writeX = x;
            int writeZ = y;
            int baseY = y;

            while(x >= maskData.StartX && x < maskData.EndX) // For each column to end of image
            {
                y = baseY; // Count pixels from base upwards
                while(y < maskData.EndY && maskData[x, y])
                    y++;

                int height = y - baseY;
                if(height > 0)
                {
                    int i = 0;
                    do
                    {
                        heightmapData[writeX + i * Math.Sign(offset), writeZ] = (byte)Math.Min(byte.MaxValue, height);
                        i++;
                    } while(i < Math.Abs(offset));
                }

                // Move input:
                x += inputReadDirection;
                baseY++;

                // Move output:
                writeX += (int)obliqueDirection;
                writeZ++;
            }
        }


        #endregion



        #region Methods for creating Shadow Heightmaps

        /// <summary>Create a new heightmap that extends its content (non-default heights) out in oblique directions (sutiable for creating shadow heightmaps)</summary>
        public Heightmap CreateExtendedOblique(Oblique oblique)
        {
            // Expand the region containing content in the specified oblique direction(s)
            #region ASCII Art...
            //
            // Given a heightmap that looks like this:
            //      ________
            //     | /\  /\ |
            //     |/  \/  \|
            //     |\      /|
            //     |/      \|
            //     |\  /\  /|
            //     |_\/__\/_|
            //
            // Expand it by projecting its content in the oblique direction:
            //  ________________
            // |.  | /\  /\ |   |
            // | . |/  \/  \|   |
            // |  .|\      /.   |
            // |   ./      \|.  |
            // |   |\  /\  /| . |
            // |___|_\/__\/_|__.|
            //
            #endregion

            int extendedLeft = StartX;
            int extendedRight = EndX - 1; // <- inclusive
            for(int z = StartZ; z < EndZ; z++) for(int x = StartX; x < EndX; x++)
            {
                if(this[x, z] != DefaultHeight) // Has content to extend
                {
                    int deltaFromFront = z - StartZ; // (positive)
                    int deltaFromBack = z - (EndZ-1); // (negative)

                    int frontX = x - deltaFromFront * (int)oblique;
                    int backX = x - deltaFromBack * (int)oblique;

                    // Expand:
                    if(frontX < extendedLeft)
                        extendedLeft = frontX;
                    if(frontX > extendedRight)
                        extendedRight = frontX;
                    if(backX < extendedLeft)
                        extendedLeft = backX;
                    if(backX > extendedRight)
                        extendedRight = backX;
                }
            }

            // HACK: To allow edges to have zero values when filled, without adding expansion to the Fill methods,
            //       simply expand by one pixel here:
            // TODO: Allow the FillLeft/FillRight methods to automatically expand the size of the heightmap sideways if necessary
            //       (So that zero values on the very edge aren't lost)
            extendedLeft -= 1;
            extendedRight += 1;


            Rectangle destinationBounds = new Rectangle(extendedLeft, StartZ, extendedRight - extendedLeft + 1, EndZ - StartZ);

            Heightmap destination = new Heightmap(DefaultHeight);
            destination.heightmapData = new Data2D<byte>(destinationBounds);

            // Copy while extending content
            for(int destinationZ = destination.StartZ; destinationZ < destination.EndZ; destinationZ++)
            {
                for(int destinationX = destination.StartX; destinationX < destination.EndX; destinationX++)
                {
                    // We could add a bunch of early-outs here (but we don't care if this is a tad slow)
                    int sourceX = destinationX, sourceZ = destinationZ;

                    // Try and extend to find content:
                    if(this[sourceX, sourceZ] == DefaultHeight)
                    {
                        // Search backwards to back of heightmap
                        while(true)
                        {
                            sourceZ += 1;
                            sourceX += (int)oblique;
                            if(sourceZ >= this.EndZ)
                                goto searchForward;
                            if(this[sourceX, sourceZ] != DefaultHeight)
                                goto done;
                        }

                        // Search forward to front of heightmap
                    searchForward:
                        sourceX = destinationX; sourceZ = destinationZ;
                        while(true)
                        {
                            sourceZ -= 1;
                            sourceX -= (int)oblique;
                            if(sourceZ < this.StartZ)
                                goto fail;
                            if(this[sourceX, sourceZ] != DefaultHeight)
                                goto done;
                        }

                    fail:
                        sourceX = destinationX; sourceZ = destinationZ;
                    done:
                        ;
                    }
                    destination[destinationX, destinationZ] = this[sourceX, sourceZ];
                }
            }

            return destination;
        }


        public void FillLeft(byte? fillWith = null)
        {
            for(int z = StartZ; z < EndZ; z++)
            {
                // Search right for first pixel with content
                int x = StartX;
                while(x < EndX && this[x, z] == DefaultHeight)
                {
                    ++x;
                }

                // Take that pixel's content and fill leftwards from there
                byte content = fillWith.HasValue ? fillWith.Value : this[x, z];
                while(x > StartX)
                {
                    --x;
                    this[x, z] = content;
                }
            }
        }

        public void FillRight(byte? fillWith = null)
        {
            for(int z = StartZ; z < EndZ; z++)
            {
                // Search left for first pixel with content
                int x = EndX - 1;
                while(x >= StartX && this[x, z] == DefaultHeight)
                {
                    --x;
                }

                // Take that pixel's content and fill rightwards from there
                byte content = fillWith.HasValue ? fillWith.Value : this[x, z];
                while(x < EndX - 1)
                {
                    ++x;
                    this[x, z] = content;
                }
            }
        }

        #endregion



        #region Queries

        /// <summary>
        /// For a given position at Z=0 (in world coordinates), unproject that point into the heightmap to find either
        /// a point on the surface of the heightmap, or a point above the surface of the hightmap where
        /// unprojecting further would intersect the heightmap.
        /// </summary>
        public Position GetPointAboveSurface(int x, int y)
        {
            // TODO: BUG: This treats one-way platforms as simple colliders (should probably ignore front-faces of one-way platforms)

            // Translate our starting point so that we start at StartZ:
            int z = StartZ;
            y -= z;

            // If we are a level heightmap, skip any initial areas of infinite height at the front of the heightmap
            // (specifically useful for clicking levels with hidden unwalkable areas in front)
            if(DefaultHeight == Infinity) // <- reasonable proxy for "are we a level heightmap?"
            {
                while(z < EndZ && this[x, z] == Infinity)
                {
                    z++;
                    y--;
                }
            }

            // Unproject pixel by pixel:
            while(y > 0) // <- never go under
            {
                // Fast-forward if we go past the back of the heightmap into open space (we can calculate directly instead of using a loop)
                if(z == EndZ && y >= DefaultHeight) // ('y' check is to ensure the back of the heightmap is open space)
                {
                    z += (y - DefaultHeight);
                    y = DefaultHeight;
                }

                // See if we hit the heightmap
                byte height = this[x, z];

                if(height > y || (height == Infinity && DefaultHeight == Infinity)) // (always blocked by infinitely high objects)
                {
                    // If we can back up a bit in the regular unprojection, without intersecting something, do that
                    if(this[x, z-1] <= y+1)
                        return new Position(x, y+1, z-1);
                    else // Otherwise go directly upwards
                        return new Position(x, height, z);
                }
                else if(height == y)
                {
                    return new Position(x, y, z); // exact position
                }

                // Due to the way projection works, we drop 1 pixel in height for each 1 pixel in depth we move inwards during unprojection
                y--;
                z++;
            }

            return new Position(x, 0, z); // <- blast upwards and select a point (not accurate, but Y=0 result gets ignored for objects anyway)
        }


        /// <summary>Get the distance to the first zero entry in the heightmap, to the left of some position, up until the given search distance</summary>
        public int DistanceToFirstZeroLeft(int x, int z, int searchDistance)
        {
            x -= heightmapData.OffsetX;
            z -= heightmapData.OffsetY;
            int rowStart = heightmapData.Width * z;

            int start = Math.Max(0, x - searchDistance);
            int distance = 0;
            while(x >= start)
            {
                if(heightmapData.Data[rowStart + x] == 0)
                    return distance;
                distance++;
                x--;
            }

            return searchDistance;
        }

        /// <summary>Get the distance to the first zero entry in the heightmap, to the right of some position, up until the given search distance</summary>
        public int DistanceToFirstZeroRight(int x, int z, int searchDistance)
        {
            x -= heightmapData.OffsetX;
            z -= heightmapData.OffsetY;
            int rowStart = heightmapData.Width * z;

            int end = Math.Min(heightmapData.Width, x + searchDistance + 1);
            int distance = 0;
            while(x < end)
            {
                if(heightmapData.Data[rowStart + x] == 0)
                    return distance;
                distance++;
                x++;
            }

            return searchDistance;
        }

        #endregion



        #region Derived Data

        public bool IsObjectHeightmap { get { return DefaultHeight == 0; } }

        /// <summary>The maximum value stored in the heightmap</summary>
        public int GetMaxHeight()
        {
            int maxHeight = DefaultHeight;
            var data = heightmapData.Data;
            if (data == null)
                return maxHeight;
            for(int i = 0; i < data.Length; i++)
            {
                if(data[i] > maxHeight)
                    maxHeight = data[i];
            }
            return maxHeight;
        }

        #endregion




    }
}
