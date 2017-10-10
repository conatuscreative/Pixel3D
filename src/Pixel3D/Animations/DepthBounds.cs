using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Pixel3D.Animations
{
    public struct FrontBack
    {
        public byte front, back;

        // NOTE: Hacky mechanisim for storing "on top" status without requiring more data (we're nicely 16-bits)
        //       If we are "on top", then only the back bounds contains usable data
        bool IsOnTop { get { return front > back; } }
    }

    public class DepthSlice : IEquatable<DepthSlice>
    {
        public int xOffset;
        public int zOffset;
        public FrontBack[] depths;

        public int Width { get { return depths.Length; } }

        public static DepthSlice CreateBlank(int xOffset, int zOffset)
        {
            return new DepthSlice { xOffset = xOffset, zOffset = zOffset, depths = new FrontBack[1] };
        }


        public bool Equals(DepthSlice other)
        {
            if(xOffset != other.xOffset || zOffset != other.zOffset || depths.Length != other.depths.Length)
            {
                return false;
            }

            // Too lazy to import memcmp...
            for(int i = 0; i < depths.Length; i++)
                if(depths[i].front != other.depths[i].front || depths[i].back != other.depths[i].back)
                    return false;

            return true;
        }

    }

    public struct DepthBounds
    {
        // Heights is the split-points (has one less item than slices)
        public byte[] heights;
        public DepthSlice[] slices; // <- may be null if the owner has no physics height

        public DepthSlice GetSlice(int yOffset)
        {
            // Early-out in the obvious cases:
            if(yOffset == 0 || heights == null)
                return slices[0];

            // Dumb linear search:
            int i;
            for(i = 0; i < heights.Length; i++)
            {
                if(heights[i] > yOffset)
                    break;
            }

            return slices[i];
        }




        #region Serialize

        public void Serialize(AnimationSerializeContext context)
        {
            Debug.Assert(slices != null); // <- should never serialize an empty depth bound (check in caller)

            if(heights == null)
            {
                context.bw.Write((int)0);
            }
            else
            {
                context.bw.Write(heights.Length);
                context.bw.Write(heights);
            }

            // NOTE: slices.Length is implicit
            for(int i = 0; i < slices.Length; i++)
            {
                context.bw.Write(slices[i].xOffset);
                context.bw.Write(slices[i].zOffset);

                context.bw.Write(slices[i].depths.Length);
                for(int j = 0; j < slices[i].depths.Length; j++)
                {
                    context.bw.Write(slices[i].depths[j].front);
                    context.bw.Write(slices[i].depths[j].back);
                }
            }
        }

        /// <summary>Deserialize.</summary>
        public DepthBounds(AnimationDeserializeContext context)
        {
            int heightCount = context.br.ReadInt32();
            heights = (heightCount == 0) ? null : context.br.ReadBytes(heightCount);

            slices = new DepthSlice[heightCount + 1];
            for(int i = 0; i < slices.Length; i++)
            {
                slices[i] = new DepthSlice() 
                {
                    xOffset = context.br.ReadInt32(),
                    zOffset = context.br.ReadInt32(),
                    depths = new FrontBack[context.br.ReadInt32()],
                };

                for(int j = 0; j < slices[i].depths.Length; j++)
                {
                    slices[i].depths[j].front = context.br.ReadByte();
                    slices[i].depths[j].back = context.br.ReadByte();
                }
            }
        }

        #endregion




        #region Generate for Flats

        public static int CalculateFlatPhysicsDepth(int physicsWidth, Oblique flatDirection)
        {
            // NOTE: This method should match calculation of flat Z-sort data
            if(flatDirection == Oblique.Straight)
                return 0;

            return physicsWidth = Math.Min(physicsWidth, ((int)byte.MaxValue)+1);
        }

        public static DepthBounds CreateForFlat(AnimationSet animationSet)
        {
            Debug.Assert(animationSet.Heightmap == null); // <- should be going through other path

            DepthSlice ds = new DepthSlice();
            ds.xOffset = animationSet.physicsStartX;
            ds.zOffset = animationSet.physicsStartZ;

            int width = animationSet.physicsEndX - animationSet.physicsStartX;

            ds.depths = new FrontBack[width];
            if(animationSet.flatDirection != 0)
            {
                for(int i = 0; i < width; i++)
                {
                    int depth = i;
                    if(animationSet.flatDirection == Oblique.Left)
                        depth = width - 1 - i;

                    byte d = (byte)Math.Min(byte.MaxValue, depth);
                    ds.depths[i].front = d;
                    ds.depths[i].back = d;
                }
            }

            return new DepthBounds { slices = new[] { ds } }; // <- single slice
        }

        #endregion



        #region Generate for Heightmaps


        public static DepthBounds CreateForHeightmap(Heightmap heightmap)
        {
            if(heightmap.heightmapData.Data == null) // <- Empty heightmap makes me sad
                return new DepthBounds();
            if(!heightmap.IsObjectHeightmap) // <- Don't generate depth bounds for levels
                return new DepthBounds();


            // Determine what heights are used in the heightmap, and only generate depth slices for those heights
            bool[] usedHeights = new bool[256];
            for(int i = 0; i < heightmap.heightmapData.Data.Length; i++)
                usedHeights[heightmap.heightmapData.Data[i]] = true;


            List<byte> heights = new List<byte>();
            List<DepthSlice> slices = new List<DepthSlice>();

            DepthSlice previousSlice = null;
            int previousHeight = 0;

            for(int y = 1; y < usedHeights.Length; y++)
            {
                if(!usedHeights[y])
                    continue;

                Debug.Assert(y-1 <= byte.MaxValue);

                if(previousSlice == null)
                {
                    // First slice always starts at height = 0, and its starting height is implicit (not stored in heights):
                    Debug.Assert(slices.Count == 0);
                    slices.Add(previousSlice = MakeBaseDepthSlice(heightmap));
                }
                else
                {
                    slices.Add(previousSlice = MakeNonBaseDepthSlice(heightmap, y-1, previousSlice)); // <- note that the slice is wrapped around height at y-1
                    heights.Add((byte)previousHeight);
                }

                previousHeight = y;
            }

            if(slices.Count == 0)
            {
                return new DepthBounds(); // <- no physics
            }
            else
            {
                Debug.Assert(heights.Count == slices.Count - 1);
                return new DepthBounds
                {
                    heights = heights.Count == 0 ? null : heights.ToArray(),
                    slices = slices.ToArray(),
                };
            }
        }




        private struct DataRange
        {
            public int index;
            public int count;
        }

        private struct RawDepthData
        {
            public int startX;
            public int[] depthFront;
            public int[] depthBack;
            public List<DataRange> missingDataRanges;

            public int Width { get { return depthFront.Length; } }

            public RawDepthData(int startX, int width)
            {
                this.startX = startX;
                this.depthFront = new int[width];
                this.depthBack = new int[width];
                this.missingDataRanges = new List<DataRange>();
            }

            public bool IsBlank
            {
                get
                {
                    return (missingDataRanges.Count == 1 && missingDataRanges[0].index == 0 && missingDataRanges[0].count == Width);
                }
            }


            public void RepairMissingRanges()
            {
                Debug.Assert(!IsBlank); // Should be checked by caller
                if(IsBlank)
                    return;

                for(int i = 0; i < missingDataRanges.Count; i++)
                {
                    DataRange range = missingDataRanges[i];

                    // Depth at each edge of the missing range
                    int leftFront = int.MinValue;
                    int leftBack = int.MinValue;
                    int rightFront = int.MinValue;
                    int rightBack = int.MinValue;

                    // Can we do a fancy extrapolation? (Because we have lots of data that comes to a point)
                    bool twoDataPointsToLeft = range.index >= 2 && (i == 0 || (missingDataRanges[i-1].index + missingDataRanges[i-1].count <= range.index - 2));
                    bool twoDataPointsToRight = (range.index + range.count <= Width - 2) && (i == missingDataRanges.Count-1 || (missingDataRanges[i+1].index >= range.index + range.count + 2));

                    if(twoDataPointsToLeft)
                    {
                        int front = depthFront[range.index-1];
                        int frontNext = depthFront[range.index-2];
                        int back = depthBack[range.index-1];
                        int backNext = depthBack[range.index-2];

                        leftBack = Math.Max(front, Math.Min(back, (back +  back -  backNext)));
                        leftFront = Math.Min(leftBack, Math.Max(front, Math.Min(back, (front + front - frontNext))));
                    }
                    else if(range.index != 0) // only one data point to the left
                    {
                        leftFront = depthFront[range.index-1];
                        leftBack = depthBack[range.index-1];
                    }
                    // (else no data points to the left, will use right side value)

                    if(twoDataPointsToRight)
                    {
                        int front = depthFront[range.index+range.count];
                        int frontNext = depthFront[range.index+range.count+1];
                        int back = depthBack[range.index+range.count];
                        int backNext = depthBack[range.index+range.count+1];

                        rightBack = Math.Max(front, Math.Min(back, (back +  back -  backNext)));
                        rightFront = Math.Min(rightBack, Math.Max(front, Math.Min(back, (front + front - frontNext))));
                    }
                    else if(range.index + range.count < Width) // only one data point to the right
                    {
                        rightFront = depthFront[range.index+range.count];
                        rightBack = depthBack[range.index+range.count];
                    }
                    // (else no data points to the right, will use left side value)


                    if(leftFront == int.MinValue)
                    {
                        Debug.Assert(leftBack == int.MinValue);
                        leftFront = rightFront;
                        leftBack = rightBack;
                    }
                    else if(rightFront == int.MinValue)
                    {
                        Debug.Assert(rightBack == int.MinValue);
                        rightFront = leftFront;
                        rightBack = leftBack;
                    }

                    Debug.Assert(leftFront != int.MinValue);
                    Debug.Assert(leftBack != int.MinValue);
                    Debug.Assert(rightFront != int.MinValue);
                    Debug.Assert(rightBack != int.MinValue);


                    if(range.count == 1)
                    {
                        if(leftBack > rightBack || (leftBack == rightBack && leftFront > rightFront))
                        {
                            depthFront[range.index] = leftFront;
                            depthBack[range.index] = leftBack;
                        }
                        else
                        {
                            depthFront[range.index] = rightFront;
                            depthBack[range.index] = rightBack;
                        }
                    }
                    else
                    {
                        // We could do something fancy here, but just halving the range and setting the left and right half should work fine
                        int half = range.count / 2;
                        for(int j = 0; j < half; j++)
                        {
                            depthFront[range.index+j] = leftFront;
                            depthBack[range.index+j] = leftBack;
                        }
                        for(int j = half; j < range.count; j++)
                        {
                            depthFront[range.index+j] = rightFront;
                            depthBack[range.index+j] = rightBack;
                        }
                    }
                }
            }
        }



        static RawDepthData GetRawDepthData(Heightmap heightmap, int yOffset, int startX, int width)
        {
            RawDepthData output = new RawDepthData(startX, width);

            // Now walk left-to-right, finding front and back depth bounds,
            // as well as ranges where the heightmap has no data:
            int endOfLastDataIndex = 0;
            for(int i = 0; i < width; i++)
            {
                int x = i + startX;

                if(x >= heightmap.StartX && x < heightmap.EndX)
                {
                    // Walk back to find front of heightmap for this column, at the given height
                    int front = heightmap.StartZ; // <- front is an inclusive bound
                    while(true)
                    {
                        if(front >= heightmap.EndZ)
                            goto skipColumn; // no data
                        if(heightmap[x, front] > yOffset)
                            break;
                        front++;
                    }

                    // Walk forward to find the back of the heightmap
                    int back = heightmap.EndZ; // <- back is an exclusive bound
                    while(true)
                    {
                        Debug.Assert(back > front); // <- if this fires, there's a bug in the algorithm (or heightmap misbehaved)
                        if(heightmap[x, back-1] > yOffset)
                            break;
                        back--;
                    }

                    // Note any missing data up to here:
                    Debug.Assert(endOfLastDataIndex <= i);
                    if(endOfLastDataIndex != i)
                        output.missingDataRanges.Add(new DataRange { index = endOfLastDataIndex, count = (i - endOfLastDataIndex) });
                    endOfLastDataIndex = i+1;

                    // Save the bounds:
                    output.depthFront[i] = front;
                    output.depthBack[i] = back;
                }

            skipColumn:
                ;
            }

            if(endOfLastDataIndex != width)
                output.missingDataRanges.Add(new DataRange { index = endOfLastDataIndex, count = (width - endOfLastDataIndex) });

            return output;
        }



        static private DepthSlice MakeBaseDepthSlice(Heightmap heightmap)
        {
            RawDepthData rawDepthData = GetRawDepthData(heightmap, 0, heightmap.StartX - 1, heightmap.Width + 2); // <- Guarantuee missing ranges on each end (for extrapolation)

            if(rawDepthData.IsBlank)
                return DepthSlice.CreateBlank(heightmap.StartX, heightmap.StartZ);

            // Fill in missing data and do extrapolation:
            rawDepthData.RepairMissingRanges();
            int dataStart = rawDepthData.missingDataRanges[0].index + rawDepthData.missingDataRanges[0].count;
            int dataEnd = rawDepthData.missingDataRanges[rawDepthData.missingDataRanges.Count-1].index;

            // Bring extrapolations into the legitimate data range:
            if(rawDepthData.depthFront[dataStart-1] == rawDepthData.depthBack[dataStart-1])
                dataStart--;
            if(rawDepthData.depthFront[dataEnd] == rawDepthData.depthBack[dataEnd])
                dataEnd++;


            return PackDepthSlice(ref rawDepthData, dataStart, dataEnd);
        }


        private static DepthSlice PackDepthSlice(ref RawDepthData rawDepthData, int dataStart, int dataEnd)
        {
            DepthSlice depthSlice = new DepthSlice();
            depthSlice.xOffset = rawDepthData.startX + dataStart;

            depthSlice.zOffset = int.MaxValue;
            for(int i = dataStart; i < dataEnd; i++)
            {
                Debug.Assert(rawDepthData.depthFront[i] <= rawDepthData.depthBack[i]);
                Debug.Assert(rawDepthData.depthBack[i] != int.MinValue);

                if(rawDepthData.depthFront[i] < depthSlice.zOffset)
                    depthSlice.zOffset = rawDepthData.depthFront[i];
            }

            depthSlice.depths = new FrontBack[dataEnd - dataStart];
            for(int i = 0; i < depthSlice.depths.Length; i++)
            {
                depthSlice.depths[i].front = (byte)Math.Min(byte.MaxValue, rawDepthData.depthFront[i + dataStart] - depthSlice.zOffset);
                depthSlice.depths[i].back  = (byte)Math.Min(byte.MaxValue, rawDepthData.depthBack[i + dataStart]  - depthSlice.zOffset);
            }

            return depthSlice;
        }



        static DepthSlice MakeNonBaseDepthSlice(Heightmap heightmap, int yOffset, DepthSlice previousSlice)
        {
            Debug.Assert(yOffset != 0);
            Debug.Assert(previousSlice != null);

            RawDepthData rawDepthData = GetRawDepthData(heightmap, yOffset, previousSlice.xOffset, previousSlice.Width);

            if(rawDepthData.IsBlank)
                return DepthSlice.CreateBlank(previousSlice.xOffset, previousSlice.zOffset);

            DepthSlice depthSlice = PackDepthSlice(ref rawDepthData, 0, previousSlice.Width);

            // Pull up missing data from previous layers:
            foreach(var missingRange in rawDepthData.missingDataRanges)
            {
                for(int j = 0; j < missingRange.count; j++)
                {
                    int i = j + missingRange.index;
                    var belowBounds = previousSlice.depths[i];

                    if(belowBounds.front == belowBounds.back && missingRange.count == 1) // <- Indicates the lower layer is an extrapolation (keep it that way)
                        depthSlice.depths[i] = belowBounds;
                    else // Lower bounds is a surface, encode an "above" bounds
                        depthSlice.depths[i] = new FrontBack { back = belowBounds.back, front = (byte)Math.Min(byte.MaxValue, (int)belowBounds.back + 1) };
                }
            }

            return depthSlice;
        }

        #endregion


    }

}
