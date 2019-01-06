// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Pixel3D.Extensions;

namespace Pixel3D
{
    public struct HeightmapView
    {
        public HeightmapView(Heightmap heightmap, Position position, bool flipX)
        {
            this.heightmap = heightmap;
            this.position = position;
            this.flipX = flipX;
        }

        public Heightmap heightmap;
        public Position position;
        public bool flipX; // PERF: Can we change this to a multiplier?


        /// <summary>Bounds of the transformed heightmap in the XZ plane</summary>
        public Rectangle Bounds
        {
            get
            {
                var b = flipX ? heightmap.Bounds.FlipXIndexable() : heightmap.Bounds;
                b.X += position.X;
                b.Y += position.Z; // <- rectangle is XZ
                return b;
            }
        }

        public Range BoundsX
        {
            get
            {
                return new Range(heightmap.StartX, heightmap.EndX).MaybeFlip(flipX) + position.X;
            }
        }

        public Range BoundsZ
        {
            get
            {
                return new Range(heightmap.StartZ + position.Z, heightmap.EndZ + position.Z);
            }
        }



        /// <summary>Get the height at a given position in the heightmap (does not treat default height as special)</summary>
        public int GetUnelevatedHeightAt(int x, int z)
        {
            x -= position.X;
            if(flipX)
                x = -x;
            z -= position.Z;

            return heightmap[x, z];
        }


        /// <summary>Get the height at a given position in the heightmap, with special shadow indexing (does not treat default height as special)</summary>
        public int GetHeightForShadow(int startX, int endX, int z, Oblique extendDirection)
        {
            Debug.Assert(heightmap.HasData); // <- NOTE: Expect our caller to skip calculation if there is no data!!

            // Transform into heightmap space:
            int transformedStartX;
            int transformedEndX;
            if(!flipX)
            {
                transformedStartX = startX - position.X;
                transformedEndX = endX - position.X;
            }
            else
            {
                transformedStartX = -(endX - position.X - 1);
                transformedEndX = -(startX - position.X - 1);
                extendDirection = (Oblique)(-(int)extendDirection);
            }
            Debug.Assert(transformedStartX < transformedEndX);

            z -= position.Z;


            // Move into heightmap Z range in oblique direction
            if(z < heightmap.StartZ)
            {
                int distance = heightmap.StartZ - z; // (positive)
                distance *= (int)extendDirection;
                transformedStartX += distance;
                transformedEndX += distance;
                z = heightmap.StartZ;
            }
            else if(z >= heightmap.EndZ)
            {
                int distance = (heightmap.EndZ-1) - z; // (negative)
                distance *= (int)extendDirection;
                transformedStartX += distance;
                transformedEndX += distance;
                z = heightmap.EndZ - 1;
            }


            // If we are outside the range, early-out:
            if(transformedEndX <= heightmap.StartX)
                return position.Y + heightmap.heightmapData[heightmap.StartX, z]; // Skips bounds check
            if(transformedStartX >= (heightmap.EndX - 1))
                return position.Y + heightmap.heightmapData[heightmap.EndX-1, z]; // Skips bounds check

            // Ensure we are clipped to the range:
            if(transformedStartX < heightmap.StartX)
                transformedStartX = heightmap.StartX;
            if(transformedEndX > heightmap.EndX)
                transformedEndX = heightmap.EndX;

            int maxHeight = 0;
            for(int x = transformedStartX; x < transformedEndX; x++)
            {
                int h = heightmap.heightmapData[x, z]; // Skips bounds check
                if(h > maxHeight)
                    maxHeight = h;
            }

            return position.Y + maxHeight;
        }

    }
}
