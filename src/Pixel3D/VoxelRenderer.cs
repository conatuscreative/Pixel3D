// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Animations;
using Pixel3D.FrameworkExtensions;
using Pixel3D.Physics;

namespace Pixel3D
{
    /// <summary>Renderer for debug visualisation (allows software rendering with a Z-buffer)</summary>
    public class VoxelRenderer : IDisposable
    {
        public VoxelRenderer(GraphicsDevice device, Rectangle initialDisplayBounds)
        {
            CreateBuffers(device, ConstrainBounds(initialDisplayBounds));
        }

	    public void Clear()
	    {
		    Array.Clear(zBuffer, 0, zBuffer.Length);
		    Array.Clear(colorBuffer, 0, colorBuffer.Length);
		    dirty = true;
	    }

	    public int DepthBias { get { return depthBias; } set { depthBias = value; } }
		
		#region Buffer Management

		static Rectangle ConstrainBounds(Rectangle bounds)
        {
            // Enforce minimum size
            if(bounds.Width < 1)
                bounds.Width = 1;
            if(bounds.Height < 1)
                bounds.Height = 1;

            // Enforce maximum size (XNA HiDef)
            if(bounds.Width > 4096)
                bounds.Width = 4096;
            if(bounds.Height > 4096)
                bounds.Height = 4096;

            return bounds;
        }

        public void SetRenderExtentsAndClear(Rectangle requestedDisplayBounds, bool forceResize)
        {
            // Enforce minimum size
            requestedDisplayBounds = ConstrainBounds(requestedDisplayBounds);

            // If the buffer is the wrong size, or we could shrink the buffer significantly...
            if(forceResize
                    || requestedDisplayBounds.Width > displayBounds.Width || requestedDisplayBounds.Height > displayBounds.Height
                    || requestedDisplayBounds.Width < displayBounds.Width - 20 || requestedDisplayBounds.Height < displayBounds.Height - 20)
            {
                CreateBuffers(texture.GraphicsDevice, requestedDisplayBounds);
            }
            else // Reuse the existing buffer, and just move it...
            {
                displayBounds.X = requestedDisplayBounds.X;
                displayBounds.Y = requestedDisplayBounds.Y;
                Clear();
            }

            depthBias = 0; // <- reset every frame
        }
		
        /// <summary>Bounds of the rendered area in Display space</summary>
        Rectangle displayBounds;

        /// <summary>Bounds of the rendered area in Display space</summary>
        public Rectangle DisplayBounds { get { return displayBounds; } }

		int depthBias = 0;
        byte[] zBuffer;
        Color[] colorBuffer;
        Texture2D texture;

		void CreateBuffers(GraphicsDevice device, Rectangle newBounds)
        {
            if(texture != null)
                texture.Dispose();

            displayBounds = newBounds;
            texture = new Texture2D(device, displayBounds.Width, displayBounds.Height);

            zBuffer = new byte[displayBounds.Width * displayBounds.Height];
            colorBuffer = new Color[displayBounds.Width * displayBounds.Height];

            dirty = true;
        }
        public void Dispose()
        {
            if(texture != null)
                texture.Dispose();
        }

        #endregion

		#region Write Texture

        bool dirty;

        public Vector2 TextureOrigin
        {
            get { return new Vector2(-displayBounds.X, -displayBounds.Y); }
        }

        public Texture2D ResolveTexture()
        {
            if(dirty)
            {
                texture.SetData(colorBuffer);
                dirty = false;
            }

            return texture;
        }

        #endregion
		
        #region Shameful reuse of voxel renderer for 2D rendering...
		public void DrawWorldZeroPixelBlend(Point position, Color color)
        {
            int x = position.X;
            int y = -position.Y - 1; // Convert World to Display coordinates

            // Translate into buffer region
            x -= displayBounds.X;
            y -= displayBounds.Y;

            // Bounds check:
            if((uint)x < (uint)displayBounds.Width && (uint)y < (uint)displayBounds.Height)
            {
                int i = x + y * displayBounds.Width;

                // Blend (would rather avoid floating point here, but oh well)
                Vector3 source = color.ToVector3();
                Vector3 destination = colorBuffer[i].ToVector3();
                float alpha = (float)color.A / 255f;
                colorBuffer[i] = new Color(source + destination * (1f - alpha));

                dirty = true;
            }
        }

        public void DrawMaskBlend(TransformedMaskData tmd, Color foreground, Color background)
        {
            for(int maskY = tmd.maskData.StartY; maskY < tmd.maskData.EndY; maskY++) for(int maskX = tmd.maskData.StartX; maskX < tmd.maskData.EndX; maskX++)
            {
                int x = tmd.flipX ? -maskX : maskX;
                int y = -maskY - 1; // Convert World to Display coordinates

                // Translate into buffer region
                x -= displayBounds.X;
                y -= displayBounds.Y;

                Color color = tmd.maskData[maskX, maskY] ? foreground : background;

                // Bounds check:
                if((uint)x < (uint)displayBounds.Width && (uint)y < (uint)displayBounds.Height)
                {
                    int i = x + y * displayBounds.Width;

                    // Blend (would rather avoid floating point here, but oh well)
                    Vector4 source = color.ToVector4();
                    Vector4 destination = colorBuffer[i].ToVector4();
                    float alpha = (float)color.A / 255f;
                    colorBuffer[i] = new Color(source + destination * (1f - alpha));

                    dirty = true;
                }
            }
        }

        #endregion

		#region Render Lines

        public void DrawPixel(Position p, Color color, int zTestOffset = 0)
        {
            // Convert World position to Display position with a Z buffer
            int x = p.X;
            int y = -(p.Y + p.Z);
            int z = (int)byte.MaxValue - (p.Z + depthBias); // <- depth (inverse so zero is the back of the depth buffer)

            // Translate into buffer region
            x -= displayBounds.X;
            y -= displayBounds.Y;
            // Account for inversion of coordinate system (ie: 0 -> height-1)
            y -= 1;

            // Bounds check:
            if((uint)x < (uint)displayBounds.Width && (uint)y < (uint)displayBounds.Height && (uint)z <= (uint)byte.MaxValue)
            {
                int i = x + y * displayBounds.Width;
                if(z >= zBuffer[i] + zTestOffset)
                {
                    colorBuffer[i] = color;
                    zBuffer[i] = (byte)z;
                    dirty = true;
                }
            }
        }


        static int IntegerLerp(int from, int to, int numerator, int denominator)
        {
            return from + ((to-from) * numerator) / denominator;
        }

        static Color ColorIntegerLerp(Color from, Color to, int numerator, int denominator)
        {
            return new Color(IntegerLerp(from.R, to.R, numerator, denominator),
                    IntegerLerp(from.G, to.G, numerator, denominator),
                    IntegerLerp(from.B, to.B, numerator, denominator),
                    IntegerLerp(from.A, to.A, numerator, denominator));
        }


        public void DrawLine(Position start, Position end, Color color)
        {
            DrawLine(start, end, color, color);
        }

        public void DrawLine(Position start, Position end, Color startColor, Color endColor)
        {
            // Based on code from https://gist.github.com/yamamushi/5823518
            // (Was in public domain)

            int x1 = start.X, y1 = start.Y, z1 = start.Z;
            int x2 = end.X, y2 = end.Y, z2 = end.Z;

            //int i, dx, dy, dz, l, m, n, x_inc, y_inc, z_inc, err_1, err_2, dx2, dy2, dz2;
            //int point0, point1, point2;

            Position currentPosition = start;
            int dx = x2 - x1;
            int dy = y2 - y1;
            int dz = z2 - z1;
            int x_inc = (dx < 0) ? -1 : 1;
            int l = Math.Abs(dx);
            int y_inc = (dy < 0) ? -1 : 1;
            int m = Math.Abs(dy);
            int z_inc = (dz < 0) ? -1 : 1;
            int n = Math.Abs(dz);
            int dx2 = l << 1;
            int dy2 = m << 1;
            int dz2 = n << 1;

            if((l >= m) && (l >= n))
            {
                int err_1 = dy2 - l;
                int err_2 = dz2 - l;
                for(int i = 0; i < l; i++)
                {
                    DrawPixel(currentPosition, ColorIntegerLerp(startColor, endColor, i, l));
                    if(err_1 > 0)
                    {
                        currentPosition.Y += y_inc;
                        err_1 -= dx2;
                    }
                    if(err_2 > 0)
                    {
                        currentPosition.Z += z_inc;
                        err_2 -= dx2;
                    }
                    err_1 += dy2;
                    err_2 += dz2;
                    currentPosition.X += x_inc;
                }
            }
            else if((m >= l) && (m >= n))
            {
                int err_1 = dx2 - m;
                int err_2 = dz2 - m;
                for(int i = 0; i < m; i++)
                {
                    DrawPixel(currentPosition, ColorIntegerLerp(startColor, endColor, i, m));
                    if(err_1 > 0)
                    {
                        currentPosition.X += x_inc;
                        err_1 -= dy2;
                    }
                    if(err_2 > 0)
                    {
                        currentPosition.Z += z_inc;
                        err_2 -= dy2;
                    }
                    err_1 += dx2;
                    err_2 += dz2;
                    currentPosition.Y += y_inc;
                }
            }
            else
            {
                int err_1 = dy2 - n;
                int err_2 = dx2 - n;
                for(int i = 0; i < n; i++)
                {
                    DrawPixel(currentPosition, ColorIntegerLerp(startColor, endColor, i, n));
                    if(err_1 > 0)
                    {
                        currentPosition.Y += y_inc;
                        err_1 -= dz2;
                    }
                    if(err_2 > 0)
                    {
                        currentPosition.X += x_inc;
                        err_2 -= dz2;
                    }
                    err_1 += dy2;
                    err_2 += dx2;
                    currentPosition.Z += z_inc;
                }
            }
            DrawPixel(currentPosition, endColor);
        }


        /// <summary>Draw an exclusive (outer) boundary around a rectangle</summary>
        public void DrawRectOutside(Rectangle bounds, int depth, CubeCornerColors colors)
        {
            Position bottomLeft = new Position(bounds.X - 1, bounds.Y - 1, depth);
            Position bottomRight = new Position(bounds.X + bounds.Width, bounds.Y - 1, depth);
            Position topLeft = new Position(bounds.X - 1, bounds.Y + bounds.Height, depth);
            Position topRight = new Position(bounds.X + bounds.Width, bounds.Y + bounds.Height, depth);

            DrawLine(bottomLeft, topLeft, colors.BottomFrontLeft, colors.TopFrontLeft);
            DrawLine(topLeft, topRight, colors.TopFrontLeft, colors.TopFrontRight);
            DrawLine(topRight, bottomRight, colors.TopFrontRight, colors.BottomFrontRight);
            DrawLine(bottomRight, bottomLeft, colors.BottomFrontRight, colors.BottomFrontLeft);
        }

        /// <summary>Draw an inclusive (inner) boundary around a rectangle</summary>
        public void DrawRectInside(Rectangle bounds, int depth, CubeCornerColors colors)
        {
            Position bottomLeft = new Position(bounds.X, bounds.Y, depth);
            Position bottomRight = new Position(bounds.X + bounds.Width - 1, bounds.Y, depth);
            Position topLeft = new Position(bounds.X, bounds.Y + bounds.Height - 1, depth);
            Position topRight = new Position(bounds.X + bounds.Width - 1, bounds.Y + bounds.Height - 1, depth);

            DrawLine(bottomLeft, topLeft, colors.BottomFrontLeft, colors.TopFrontLeft);
            DrawLine(topLeft, topRight, colors.TopFrontLeft, colors.TopFrontRight);
            DrawLine(topRight, bottomRight, colors.TopFrontRight, colors.BottomFrontRight);
            DrawLine(bottomRight, bottomLeft, colors.BottomFrontRight, colors.BottomFrontLeft);
        }

        /// <summary>Draw an inclusive (inner) boundary around a rectangle</summary>
        public void DrawRectInside(Bounds bounds, int depth, CubeCornerColors colors)
        {
            Position bottomLeft = new Position(bounds.startX, bounds.startY, depth);
            Position bottomRight = new Position(bounds.endX - 1, bounds.startY, depth);
            Position topLeft = new Position(bounds.startX, bounds.endY - 1, depth);
            Position topRight = new Position(bounds.endX - 1, bounds.endY - 1, depth);

            DrawLine(bottomLeft, topLeft, colors.BottomFrontLeft, colors.TopFrontLeft);
            DrawLine(topLeft, topRight, colors.TopFrontLeft, colors.TopFrontRight);
            DrawLine(topRight, bottomRight, colors.TopFrontRight, colors.BottomFrontRight);
            DrawLine(bottomRight, bottomLeft, colors.BottomFrontRight, colors.BottomFrontLeft);
        }

        /// <summary>Draw an inclusive (inner) boundary around a rectangle</summary>
        public void DrawRectInside(Bounds bounds, int depth, Color color)
        {
            Position bottomLeft = new Position(bounds.startX, bounds.startY, depth);
            Position bottomRight = new Position(bounds.endX - 1, bounds.startY, depth);
            Position topLeft = new Position(bounds.startX, bounds.endY - 1, depth);
            Position topRight = new Position(bounds.endX - 1, bounds.endY - 1, depth);

            DrawLine(bottomLeft, topLeft, color, color);
            DrawLine(topLeft, topRight, color, color);
            DrawLine(topRight, bottomRight, color, color);
            DrawLine(bottomRight, bottomLeft, color, color);
        }


        public void DrawAABB(AABB aabb, Color color)
        {
            DrawLine(aabb.BottomBackLeft, aabb.BottomBackRight, color);
            DrawLine(aabb.BottomBackLeft, aabb.BottomFrontLeft, color);
            DrawLine(aabb.BottomBackRight, aabb.BottomFrontRight, color);
            DrawLine(aabb.BottomFrontLeft, aabb.BottomFrontRight, color);

            DrawLine(aabb.BottomBackLeft, aabb.TopBackLeft, color);
            DrawLine(aabb.BottomBackRight, aabb.TopBackRight, color);
            DrawLine(aabb.BottomFrontLeft, aabb.TopFrontLeft, color);
            DrawLine(aabb.BottomFrontRight, aabb.TopFrontRight, color);

            DrawLine(aabb.TopBackLeft, aabb.TopBackRight, color);
            DrawLine(aabb.TopBackLeft, aabb.TopFrontLeft, color);
            DrawLine(aabb.TopBackRight, aabb.TopFrontRight, color);
            DrawLine(aabb.TopFrontLeft, aabb.TopFrontRight, color);
        }

        public void DrawAABB(AABB aabb, CubeCornerColors colors)
        {
            DrawLine(aabb.BottomBackLeft,  aabb.BottomBackRight,  colors.BottomBackLeft,  colors.BottomBackRight);
            DrawLine(aabb.BottomBackLeft,  aabb.BottomFrontLeft,  colors.BottomBackLeft,  colors.BottomFrontLeft);
            DrawLine(aabb.BottomBackRight, aabb.BottomFrontRight, colors.BottomBackRight, colors.BottomFrontRight);
            DrawLine(aabb.BottomFrontLeft, aabb.BottomFrontRight, colors.BottomFrontLeft, colors.BottomFrontRight);

            DrawLine(aabb.BottomBackLeft,   aabb.TopBackLeft,   colors.BottomBackLeft,   colors.TopBackLeft);
            DrawLine(aabb.BottomBackRight,  aabb.TopBackRight,  colors.BottomBackRight,  colors.TopBackRight);
            DrawLine(aabb.BottomFrontLeft,  aabb.TopFrontLeft,  colors.BottomFrontLeft,  colors.TopFrontLeft);
            DrawLine(aabb.BottomFrontRight, aabb.TopFrontRight, colors.BottomFrontRight, colors.TopFrontRight);

            DrawLine(aabb.TopBackLeft,  aabb.TopBackRight,  colors.TopBackLeft,  colors.TopBackRight);
            DrawLine(aabb.TopBackLeft,  aabb.TopFrontLeft,  colors.TopBackLeft,  colors.TopFrontLeft);
            DrawLine(aabb.TopBackRight, aabb.TopFrontRight, colors.TopBackRight, colors.TopFrontRight);
            DrawLine(aabb.TopFrontLeft, aabb.TopFrontRight, colors.TopFrontLeft, colors.TopFrontRight);
        }

        #endregion
		
        #region Render Heightmap

        /// <summary>Constrain rendering to the display bounds</summary>
        void HeightmapDrawRangeHelper(HeightmapView heightmapView, out int startX, out int endX)
        {
            if(heightmapView.flipX)
            {
                startX = Math.Max(1 - heightmapView.heightmap.EndX, displayBounds.Left - heightmapView.position.X);
                endX = Math.Min(1 - heightmapView.heightmap.StartX, displayBounds.Right - heightmapView.position.X);
            }
            else
            {
                startX = Math.Max(heightmapView.heightmap.StartX, displayBounds.Left - heightmapView.position.X);
                endX = Math.Min(heightmapView.heightmap.EndX, displayBounds.Right - heightmapView.position.X);
            }
        }

        public void DrawHeightmapSolid(HeightmapView heightmapView, SortedList<int, Color> heightColorGradient)
        {
            int startX, endX;
            HeightmapDrawRangeHelper(heightmapView, out startX, out endX);
            if(endX <= startX)
                return; // Off screen
             
            int xFlipFactor = heightmapView.flipX ? -1 : 1;

            for(int z = heightmapView.heightmap.EndZ - 1; z >= heightmapView.heightmap.StartZ; z--) // From back to front of heightmap
            {
                for(int x = startX; x < endX; x++)
                {
                    byte height = heightmapView.heightmap[x * xFlipFactor, z];
                    if(height == heightmapView.heightmap.DefaultHeight)
                        continue;

                    byte nextHeight = heightmapView.heightmap[x * xFlipFactor, z-1];
                    if(nextHeight != heightmapView.heightmap.DefaultHeight && nextHeight > height)
                        continue; // Next row will cover this one entirely

                    // Draw top surface
                    const int zTestOffset = 1; // <- The top surface should be "under" any other pixels 
                    DrawPixel(heightmapView.position + new Position(x, height, z),
                            heightColorGradient.GetColorFromGradient(height + heightmapView.position.Y), zTestOffset);

                    if(nextHeight != heightmapView.heightmap.DefaultHeight && nextHeight == height)
                        continue; // Next row covers this one's "solid" section

                    // Draw solidness
                    for(int h = height + heightmapView.position.Y - 1; h >= heightmapView.position.Y; h--)
                    {
                        Color c = Color.Lerp(heightColorGradient.GetColorFromGradient(h), Color.Black, 0.6f);
                        DrawPixel(new Position(heightmapView.position.X + x, h, heightmapView.position.Z + z), c, 0);
                    }
                }
            }
        }


        public void DrawHeightmapFlat(HeightmapView heightmapView, SortedList<int, Color> heightColorGradient)
        {
            int startX, endX;
            HeightmapDrawRangeHelper(heightmapView, out startX, out endX);
            if(endX <= startX)
                return; // Off screen

            int xFlipFactor = heightmapView.flipX ? -1 : 1;

            for(int z = heightmapView.heightmap.EndZ - 1; z >= heightmapView.heightmap.StartZ; z--) // From back to front of heightmap
            {
                for(int x = startX; x < endX; x++)
                {
                    byte height = heightmapView.heightmap[x * xFlipFactor, z];
                    if(height == heightmapView.heightmap.DefaultHeight)
                        continue;

                    // Draw top surface
                    DrawPixel(new Position(heightmapView.position.X + x, 0, heightmapView.position.Z + z),
                            heightColorGradient.GetColorFromGradient(height + heightmapView.position.Y), 0);
                }
            }
        }

        #endregion

		#region Render Combined Heightmap (Copy-pasted code! Also very slow!)

        public void DrawHeightmapSolid(WorldPhysics worldPhysics, Position offset, SortedList<int, Color> heightColorGradient)
        {
            // Constrain rendering to the display bounds
            int startX = Math.Max(worldPhysics.StartX, displayBounds.Left - offset.X);
            int endX = Math.Min(worldPhysics.EndX, displayBounds.Right - offset.X);

            if(endX <= startX)
                return; // Off screen

            for(int z = worldPhysics.EndZ - 1; z >= worldPhysics.StartZ; z--) // From back to front of heightmap
            {
                for(int x = startX; x < endX; x++)
                {
                    int height = worldPhysics.GetGroundHeightAt(x, z, WorldPhysics.MaximumHeight, WorldPhysics.MaximumHeight, null);
                    if(height == WorldPhysics.MaximumHeight)
                        continue;

                    int nextHeight = worldPhysics.GetGroundHeightAt(x, z-1, WorldPhysics.MaximumHeight, WorldPhysics.MaximumHeight, null);
                    if(nextHeight != WorldPhysics.MaximumHeight && nextHeight > height)
                        continue; // Next row will cover this one entirely

                    // Draw top surface
                    const int zTestOffset = 1; // <- The top surface should be "under" any other pixels 
                    DrawPixel(offset + new Position(x, height, z), heightColorGradient.GetColorFromGradient(height + offset.Y), zTestOffset);

                    if(nextHeight != WorldPhysics.MaximumHeight && nextHeight == height)
                        continue; // Next row covers this one's "solid" section

                    // Draw solidness
                    for(int h = height + offset.Y - 1; h >= offset.Y; h--)
                    {
                        Color c = Color.Lerp(heightColorGradient.GetColorFromGradient(h), Color.Black, 0.6f);
                        DrawPixel(new Position(offset.X + x, h, offset.Z + z), c);
                    }
                }
            }
        }


        public void DrawHeightmapFlat(WorldPhysics worldPhysics, Position offset, SortedList<int, Color> heightColorGradient)
        {
            // Constrain rendering to the display bounds
            int startX = Math.Max(worldPhysics.StartX, displayBounds.Left - offset.X);
            int endX = Math.Min(worldPhysics.EndX, displayBounds.Right - offset.X);

            if(endX <= startX)
                return; // Off screen

            for(int z = worldPhysics.EndZ - 1; z >= worldPhysics.StartZ; z--) // From back to front of heightmap
            {
                for(int x = startX; x < endX; x++)
                {
                    int height = worldPhysics.GetGroundHeightAt(x, z, WorldPhysics.MaximumHeight, WorldPhysics.MaximumHeight, null);
                    if(height == WorldPhysics.MaximumHeight)
                        continue;

                    // Draw top surface
                    DrawPixel(new Position(offset.X + x, 0, offset.Z + z), heightColorGradient.GetColorFromGradient(height + offset.Y));
                }
            }
        }

        #endregion
		
        #region Render Depth Bounds

        public void DrawDepthSlice(AnimationSet animationSet, Position position, bool flipX, int sliceY, Color frontColor, Color backColor, Color overColor)
        {
            if(animationSet.physicsHeight == 0)
                return;

            sliceY = sliceY.Clamp(0, animationSet.physicsHeight-1);

            DepthSlice slice = animationSet.depthBounds.GetSlice(sliceY);

            int direction = flipX ? -1 : 1;
            int startX = direction * slice.xOffset;

            if(backColor != Color.Transparent)
            {
                for(int i = 0; i < slice.depths.Length; i++)
                {
                    if(slice.depths[i].back + 1 != slice.depths[i].front)
                        DrawPixel(position + new Position(startX + i * direction, sliceY, slice.zOffset + slice.depths[i].back), backColor);
                    else // "over"
                        DrawPixel(position + new Position(startX + i * direction, sliceY, slice.zOffset + slice.depths[i].back), overColor);
                }
            }

            if(frontColor != Color.Transparent)
            {
                for(int i = 0; i < slice.depths.Length; i++)
                {
                    if(slice.depths[i].back + 1 != slice.depths[i].front)
                        DrawPixel(position + new Position(startX + i * direction, sliceY, slice.zOffset + slice.depths[i].front), frontColor);
                }
            }
        }

        #endregion

		#region Ground mask rendering

        // NOTE: Rendering from world heightmap is slow!!
        // NOTE: Much copy-pasted code from normal heightmap rendering
        public void DrawWorldPhysicsXZRegion(WorldPhysics worldPhysics, MaskData xzMask, SortedList<int, Color> heightColorGradient)
        {
            // Constrain rendering to the display bounds
            int startX = Math.Max(worldPhysics.StartX, displayBounds.Left);
            int endX = Math.Min(worldPhysics.EndX, displayBounds.Right);

            if(endX <= startX)
                return; // Off screen

            for(int z = worldPhysics.EndZ - 1; z >= worldPhysics.StartZ; z--) // From back to front of heightmap
            {
                for(int x = startX; x < endX; x++)
                {
                    if(!xzMask.GetOrDefault(x, z)) // TODO: Fix up bounds so we never leave this area (and use regular [,] access) - PERF
                        continue;

                    int height = worldPhysics.GetGroundHeightAt(x, z, WorldPhysics.MaximumHeight, WorldPhysics.MaximumHeight, null);
                    if(height == WorldPhysics.MaximumHeight)
                        continue;

                    int nextHeight = 0; // Height of the next Z-value (row)
                    if(xzMask.GetOrDefault(x, z-1))
                        nextHeight = worldPhysics.GetGroundHeightAt(x, z-1, WorldPhysics.MaximumHeight, WorldPhysics.MaximumHeight, null);

                    if(nextHeight != WorldPhysics.MaximumHeight && nextHeight > height)
                        continue; // Next row will cover this one entirely

                    // Draw top surface
                    const int zTestOffset = 1; // <- The top surface should be "under" any other pixels 
                    DrawPixel(new Position(x, height, z), heightColorGradient.GetColorFromGradient(height), zTestOffset);

                    if(nextHeight != WorldPhysics.MaximumHeight && nextHeight == height)
                        continue; // Next row covers this one's "solid" section

                    // Draw solidness
                    for(int h = height - 1; h >= 0; h--)
                    {
                        Color c = Color.Lerp(heightColorGradient.GetColorFromGradient(h), Color.Black, 0.6f);
                        DrawPixel(new Position(x, h, z), c);
                    }
                }
            }
        }


        public void DrawMaskXZ(TransformedMaskData tmd, int y, Color color)
        {
            for(int maskZ = tmd.maskData.StartY; maskZ < tmd.maskData.EndY; maskZ++) for(int maskX = tmd.maskData.StartX; maskX < tmd.maskData.EndX; maskX++)
            {
                const int zTestOffset = 1; // <- The top surface should be "under" any other pixels 
                if(tmd[maskX, maskZ])
                    DrawPixel(new Position(maskX, y, maskZ), color, zTestOffset); // <- NOTE: Use of Y value as Z value (and 
            }
        }


        public void DrawMaskXZSolidRange(TransformedMaskData tmd, int startY, int endY, SortedList<int, Color> heightColorGradient)
        {
            // NOTE: Draw back-to-front
            for(int maskZ = tmd.maskData.StartY; maskZ < tmd.maskData.EndY; maskZ++) for(int maskX = tmd.maskData.StartX; maskX < tmd.maskData.EndX; maskX++)
            {
                const int zTestOffset = 1; // <- The top surface should be "under" any other pixels 
                if(tmd[maskX, maskZ])
                {
                    DrawPixel(new Position(maskX, endY, maskZ),
                            heightColorGradient.GetColorFromGradient(endY), zTestOffset);

                    if(maskZ > tmd.maskData.StartY && tmd[maskX, maskZ-1])
                        continue; // Previous row covers this one's "solid" section

                    for(int h = startY; h < endY; h++)
                    {
                        Color c = Color.Lerp(heightColorGradient.GetColorFromGradient(h), Color.Black, 0.6f);
                        DrawPixel(new Position(maskX, h, maskZ), c, 0);
                    }
                }
            }
        }

        #endregion
    }
}
