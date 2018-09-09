using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;
using System.Diagnostics;
using Pixel3D.FrameworkExtensions;

namespace Pixel3D
{
    public static class Data2DExtensions
    {
        public static Rectangle FindTrimBounds(this Data2D<byte> data, byte defaultValue)
        {
            if(data.Data == null)
                return Rectangle.Empty;

            // NOTE: Bounds are found on non-offset data, and offset is applied at the end

            int top = 0;

            // Search downwards to find top row with a matching pixel...
            for(; top < data.Height; top++)
                for(int x = 0; x < data.Width; x++)
                    if(data.Data[x + top*data.Width] != defaultValue)
                        goto foundTop;
            return Rectangle.Empty; // No matching data found

        foundTop:

            int bottom = data.Height - 1, left = 0, right = data.Width - 1;

            // Search upwards to find bottom row with a matching pixel...
            for(; bottom > top; bottom--) // <- (exit if we reach the 'top' row: 1px tall)
                for(int x = 0; x < data.Width; x++)
                    if(data.Data[x + bottom*data.Width] != defaultValue)
                        goto foundBottom;

        foundBottom:

            // Search left to find left column with a matching pixel...
            for(; left < data.Width; left++)
                for(int y = top; y <= bottom; y++)
                    if(data.Data[left + y*data.Width] != defaultValue)
                        goto foundLeft;

        foundLeft:

            // Search right to find right column with a matching pixel...
            for(; right > left; right--) // <- (exit if we reach the 'left' column: 1px wide)
                for(int y = top; y <= bottom; y++)
                    if(data.Data[right + y*data.Width] != defaultValue)
                        goto foundRight;

        foundRight:

            return new Rectangle(left + data.OffsetX, top + data.OffsetY, right - left + 1, bottom - top + 1);
        }




        // TODO: If we can pass in a query direction, we can super-optimise at the top and bottom sides
        public static Rectangle FindTrimBoundsForShadowReceiver(this Data2D<byte> data)
        {
            if(data.Data == null)
                return Rectangle.Empty;

            // NOTE: Bounds are found on non-offset data, and offset is applied at the end

            // Top must all be the same value (due to angular queries)
            byte topValue = data.Data[0];
            int top = 0;
            for(; top < data.Height; top++)
                for(int x = 0; x < data.Width; x++)
                    if(data.Data[x + top*data.Width] != topValue)
                        goto foundTop;
        foundTop:

            int bottom = data.Height - 1, left = 0, right = data.Width - 1;

            // Bottom must all be the same value  (due to angular queries)
            byte bottomValue = data.Data[bottom * data.Width];
            for(; bottom > top; bottom--) // <- (exit if we reach the 'top' row: 1px tall)
                for(int x = 0; x < data.Width; x++)
                    if(data.Data[x + bottom*data.Width] != bottomValue)
                        goto foundBottom;
        foundBottom:

            // Left queries inwards, so columns must match
            for(; left+1 < data.Width; left++)
                for(int y = top; y <= bottom; y++)
                    if(data.Data[left+1 + y*data.Width] != data.Data[left + y*data.Width])
                        goto foundLeft;
        foundLeft:

            // Right queries inwards, so columns must match
            for(; right-1 > left; right--) // <- (exit if we reach the 'left' column: 1px wide)
                for(int y = top; y <= bottom; y++)
                    if(data.Data[right-1 + y*data.Width] != data.Data[right + y*data.Width])
                        goto foundRight;
        foundRight:

            return new Rectangle(left + data.OffsetX, top + data.OffsetY, right - left + 1, bottom - top + 1);
        }



        #region Binary Read/Write

        public static Data2D<byte> ReadData2DBytes(this BinaryReader br)
        {
            Rectangle bounds = br.ReadRectangle();
            int area = bounds.Width * bounds.Height;

            Data2D<byte> result = new Data2D<byte>(area != 0 ? br.ReadBytes(area) : null, bounds);
            return result;
        }

        public static void WriteData2DBytes(this BinaryWriter bw, Data2D<byte> data)
        {
            bw.Write(data.Bounds);
            int area = data.Bounds.Width * data.Bounds.Height;
            if(area != 0)
            {
                Debug.Assert(data.Data.Length == area);
                bw.Write(data.Data);
            }
        }

        #endregion
    }
}
