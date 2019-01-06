// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Extensions;
using Pixel3D.FrameworkExtensions;

namespace Pixel3D.Animations.Serialization
{
    public class ImageWriter
    {
        #region Registration and Serialization

        bool imagesLocked = false;

        // NOTE: Origin only stored for images for applying trim offsets at write time (not used by deduplication)
        public readonly List<Data2D<Color>> images = new List<Data2D<Color>>();
        readonly Dictionary<ImageKey, int> imageIndexByKey = new Dictionary<ImageKey, int>();
        readonly Dictionary<ComparableImageData, int> imageIndexByData = new Dictionary<ComparableImageData, int>();

        // Data for packing:
        public readonly List<Point> sizes = new List<Point>();
        long totalPixels;
        int maxWidth;
        int maxHeight;

        public long TotalPixels { get { return totalPixels; } }



        /// <returns>The version of the sprite that was actually stored. Used as a key. May have been modified from the input for de-duplication.</returns>
        internal Sprite RegisterImage(Sprite sprite)
        {
            Debug.Assert(!imagesLocked);

            if(sprite.texture == null || sprite.sourceRectangle.Width == 0 || sprite.sourceRectangle.Height == 0)
                return default(Sprite);

            ImageKey key = new ImageKey(sprite.texture, sprite.sourceRectangle);

            int index;
            if(imageIndexByKey.TryGetValue(key, out index))
                return sprite; // Already registered

            // Didn't find the item, see if we have the data anyway (and also do a trim)
            Data2D<Color> data = new Sprite(sprite.texture, sprite.sourceRectangle, Point.Zero).GetData();
            Rectangle trimBounds = data.FindTrimBounds(Color.Transparent, true);
            
            // At this point we've done a trim, and we want to apply that modification to the given sprite, as well as the key
            // (So that the sprite and the key still match)
            sprite.origin.X -= trimBounds.X;
            sprite.origin.Y -= trimBounds.Y;
            sprite.sourceRectangle.X += trimBounds.X;
            sprite.sourceRectangle.Y += trimBounds.Y;
            sprite.sourceRectangle.Width = trimBounds.Width;
            sprite.sourceRectangle.Height = trimBounds.Height;
            key = new ImageKey(sprite.texture, sprite.sourceRectangle);

            if (imageIndexByKey.TryGetValue(key, out index))
                return sprite; // Already registered

            if (trimBounds != data.Bounds)
                data = data.CopyWithNewBounds(trimBounds);

            // De-duplication check:
            ComparableImageData cid = new ComparableImageData(data);
            if (imageIndexByData.TryGetValue(cid, out index))
            {
                if (imageIndexByKey.ContainsKey(key))
                    Debug.Assert(imageIndexByKey[key] == index);
                else
                    imageIndexByKey.Add(key, index);
                return sprite; // Have matching data
            }

            // Never seen before, add it:
            if(trimBounds.Width == 0 || trimBounds.Height == 0) // Need to handle the case where the image is trimmed down to nothing!
                index = -1;
            else
            {
                index = images.Count;
                images.Add(data);

                sizes.Add(new Point(data.Width, data.Height));
                totalPixels += data.Width * data.Height;
                if(data.Width > maxWidth)
                    maxWidth = data.Width;
                if(data.Height > maxHeight)
                    maxHeight = data.Height;
            }
            imageIndexByKey.Add(key, index);
            imageIndexByData.Add(cid, index);

            return sprite;
        }

        public int GetImageIndex(Texture2D texture, Rectangle sourceRectangle)
        {
            Debug.Assert(imagesLocked);

            if(texture == null || sourceRectangle.Width == 0 || sourceRectangle.Height == 0)
                return -1;

            int index;
            if(!imageIndexByKey.TryGetValue(new ImageKey(texture, sourceRectangle), out index))
            {
                Debug.Assert(false); // Must register all images before serialization!
                throw new InvalidOperationException();
            }
            return index;
        }

        #endregion

        #region Packing

        int remaining;

        int[] wideOrder;
        int[] tallOrder;

        void CalculateWideAndTallOrders()
        {
            // Get pre-sorted ordering (separate method because LINQ)
            // Neat trick from http://stackoverflow.com/questions/1760185/c-sharp-sort-list-while-also-returning-the-original-index-positions
            wideOrder = sizes.Select((d, i) => new KeyValuePair<int, Point>(i, d)).OrderByDescending(kvp => kvp.Value.X).ThenByDescending(kvp => kvp.Value.Y).Select(kvp => kvp.Key).ToArray();
            tallOrder = sizes.Select((d, i) => new KeyValuePair<int, Point>(i, d)).OrderByDescending(kvp => kvp.Value.Y).ThenByDescending(kvp => kvp.Value.X).Select(kvp => kvp.Key).ToArray();
        }

        int wideIndex = 0;
        int tallIndex = 0;

        void UpdateWide()
        {
            while(wideIndex < wideOrder.Length && sheetPlacement[wideOrder[wideIndex]] != 0)
                wideIndex++;
        }

        void UpdateTall()
        {
            while(tallIndex < tallOrder.Length && sheetPlacement[tallOrder[tallIndex]] != 0)
                tallIndex++;
        }




        int[] sheetPlacement; // <- (sheetIndex+1), or 0 = not placed
        Point[] placements; // <- location in a sheet
        readonly List<Point> sheetSizes = new List<Point>();
        public int SheetCount { get { return sheetSizes.Count; } }

        // NOTE: Skylines are pointing "right" (in client space) and are represented by (X, Y) pairs as:
        //       X = exclusive distance to the right from the left edge of the sheet
        //       Y = exclusive distance of the end of the skyline segment, from the top of the sheet
        //       (Going sideways for better packing of levels, which are large and wide)
        readonly List<List<Point>> skylines = new List<List<Point>>(); // <- skylines by sheet
        List<Point> oldSkyline = new List<Point>(); // <- temporary storage for bouncing
        

        void MakeSheet()
        {
            sheetSizes.Add(new Point());
            skylines.Add(new List<Point>());
        }


        void Place(int i, int sheetIndex, Point place)
        {
            Debug.Assert(sheetPlacement[i] == 0); // not yet placed
            Debug.Assert(remaining > 0);

            Debug.Assert(sheetIndex >= 0);
            Debug.Assert(sheetIndex <= sheetSizes.Count);
            if(sheetIndex == sheetSizes.Count)
            {
                MakeSheet();
                Debug.Assert(sheetSizes.Count-1 == sheetIndex); // <- we appended a sheet
            }

            // Set the mundane stuff:
            sheetPlacement[i] = sheetIndex+1;
            placements[i] = place;
            remaining--;

            int placementEndX = place.X + sizes[i].X;
            int placementEndY = place.Y + sizes[i].Y;
            Debug.Assert(placementEndX <= 2048);
            Debug.Assert(placementEndY <= 2048);

            sheetSizes[sheetIndex] = new Point(Math.Max(sheetSizes[sheetIndex].X, placementEndX), Math.Max(sheetSizes[sheetIndex].Y, placementEndY));
            

            // Swap around the skylines to bounce from...
            var newSkyline = oldSkyline;
            newSkyline.Clear();
            oldSkyline = skylines[sheetIndex];
            skylines[sheetIndex] = newSkyline;

            // Recalculate skyline:
            int placementStartY = place.Y;
            int segmentStartY = 0;
            for(int s = 0; s < oldSkyline.Count; s++)
            {
                int segmentEndY = oldSkyline[s].Y;

                if(segmentEndY <= placementStartY || placementEndY <= segmentStartY) // Segment is entirely before or after the placed rect
                {
                    newSkyline.Add(oldSkyline[s]);
                }
                else // Segment has some overlap with the placed rect
                {
                    Debug.Assert(oldSkyline[s].X <= place.X); // <- Check we're not placing inside the existing skyline

                    if(placementStartY > segmentStartY) // Keep some old range at front of segment
                        newSkyline.Add(new Point(oldSkyline[s].X, placementStartY));
                    if(placementStartY >= segmentStartY) // Insert the new segment
                        newSkyline.Add(new Point(placementEndX, placementEndY));
                    if(placementEndY < segmentEndY) // Keep some old range at end of segment
                        newSkyline.Add(new Point(oldSkyline[s].X, segmentEndY));
                }

                segmentStartY = segmentEndY;
            }

            // Can also place past the end of the existing skyline:
            if(placementStartY > segmentStartY) // Gap from existing skyline to us (shouldn't normally happen)
                newSkyline.Add(new Point(0, placementStartY));
            if(placementStartY >= segmentStartY)
                newSkyline.Add(new Point(place.X + sizes[i].X, placementEndY));
        }


        Point GetPlacementAbuttingSkyline(int sheetIndex, int skylineIndex, int height)
        {
            List<Point> skyline = skylines[sheetIndex];
            Debug.Assert(skylineIndex <= skyline.Count); // <- insert anywhere on skyline, or immediately after

            int x = 0;

            int y; // <- start Y
            if(skylineIndex == 0)
                y = 0;
            else
                y = skyline[skylineIndex-1].Y; // <- end of previous segment is the start of this one

            int segmentStartY = y;
            int endY = y + height;
            while(skylineIndex < skyline.Count && segmentStartY <= endY)
            {
                x = Math.Max(x, skyline[skylineIndex].X);

                segmentStartY = skyline[skylineIndex].Y; // <- next segment
                skylineIndex++;
            }

            return new Point(x, y);
        }


        int GetWastage(int sheetIndex, int skylineIndex, int x, int height)
        {
            int wastage = 0;

            List<Point> skyline = skylines[sheetIndex];
            Debug.Assert(skylineIndex <= skyline.Count); // <- insert anywhere on skyline, or immediately after

            // NOTE: Upcoming loop only handles starting at segment starts, so we recalculate Y
            int placementStartY;
            if(skylineIndex == 0)
                placementStartY = 0;
            else
                placementStartY = skyline[skylineIndex-1].Y; // <- end of previous segment is the start of this one

            // Add up any area floating over the skyline segments:
            int placementEndY = placementStartY + height;
            int segmentStartY = 0;
            while(skylineIndex < skyline.Count && segmentStartY < placementEndY)
            {
                int xDistancePastSegment = x - skyline[skylineIndex].X;
                int yRegionCoveringSegment = Math.Min(placementEndY, skyline[skylineIndex].Y) - segmentStartY;

                Debug.Assert(xDistancePastSegment >= 0);
                Debug.Assert(yRegionCoveringSegment >= 0);
                wastage += xDistancePastSegment * yRegionCoveringSegment;

                segmentStartY = skyline[skylineIndex].Y;
                skylineIndex++;
            }

            // Also add any area hanging over the end of the skyline:
            if(placementEndY > segmentStartY)
            {
                wastage += (placementEndY - segmentStartY) * x;
            }

            return wastage;
        }



        bool packed;

        public void Pack()
        {
            if(packed)
                return;
            packed = true;
            imagesLocked = true;

            int count = sizes.Count;
            remaining = images.Count;
            placements = new Point[images.Count];
            sheetPlacement = new int[images.Count];

            if(count == 0)
                return;

            CalculateWideAndTallOrders();
            MakeSheet();

            // Estimate a good bucket width:
            int guessEdgeSize = (int)Math.Sqrt((double)totalPixels * 1.1);
            

            // First pass: Fill downwards (designed to behave well with levels, ie: large, wide images)
            int firstWidth = sizes[wideOrder[0]].X;
            do
            {
                int i = wideOrder[wideIndex];
                int width = sizes[i].X;
                int height = sizes[i].Y;
                
                if(sheetSizes[0].Y + height > 2048)
                    break; // Would be too tall to insert
                if(firstWidth > 200 && width <= firstWidth / 2)
                    break; // Could start inserting a second column below the first insertion
                if(firstWidth <= 200 && sheetSizes[0].Y + height >= guessEdgeSize)
                    break; // Reached approximate size for a square texture

                Place(i, 0, new Point(0, sheetSizes[0].Y));
                wideIndex++;
            } while(remaining > 1);


            // Second Pass: Skyline fill
            while(remaining > 0)
            {
                UpdateWide();
                UpdateTall();
                int w = wideOrder[wideIndex];
                int t = tallOrder[tallIndex];

                // Greedy Search:
                int bestIndex = 0;
                int bestSheetIndex = 0;
                Point bestPlacement = Point.Zero;
                bool foundBest = false;

                int bestWastage = int.MaxValue;


                for(int q = 0; q < 2; q++) // <- test next widest and next tallest image simulutaneously
                {
                    int i = (q == 0 ? w : t);

                    for(int sheetIndex = 0; sheetIndex < sheetSizes.Count; sheetIndex++)
                    {
                        for(int skylineIndex = 0; skylineIndex <= skylines[sheetIndex].Count; skylineIndex++) // <- NOTE: loop includes testing past the end of the skyline
                        {
                            Point placement = GetPlacementAbuttingSkyline(sheetIndex, skylineIndex, sizes[i].Y);
                            int endX = placement.X + sizes[i].X;
                            int endY = placement.Y + sizes[i].Y;

                            if(endX > 2048 || endY > 2048)
                                continue; // Sheet ends up too large

                            int newSheetEndX = Math.Max(endX, sheetSizes[sheetIndex].X);
                            int newSheetEndY = Math.Max(endY, sheetSizes[sheetIndex].Y);

                            // Optimisation: Tuned on 12/7/15 by -AR, with ~8.9% excess
                            int wastage = GetWastage(sheetIndex, skylineIndex, placement.X, sizes[i].Y);
                            if(wastage <= 81) // <- TUNE: don't care about tiny holes - prefer to avoid creating skyline with "pits"
                                wastage = 1; // <- still allow for a zero-waste solution to be found
                            int expansion = (newSheetEndX * newSheetEndY) - (sheetSizes[sheetIndex].X * sheetSizes[sheetIndex].Y);
                            const int expansionFactor = 1; // TUNE: (seems to work best when there is no extra penalty for expansion -AR)
                            wastage += expansion * expansionFactor;
                            // TODO: Could maybe save some pixels if we detect and remove narrow, unusable pits in the skyline

                            Debug.Assert(wastage >= 0);

                            if(wastage < bestWastage)
                            {
                                bestIndex = i;
                                bestSheetIndex = sheetIndex;
                                bestPlacement = placement;

                                bestWastage = wastage;
                                foundBest = true;
                            }
                        }
                    }

                    if(w == t)
                        break; // No point in checking the same index twice
                }


                // Insert the best:
                if(foundBest)
                {
                    Place(bestIndex, bestSheetIndex, bestPlacement);
                }
                else
                {
                    MakeSheet();
                    Place(w, sheetSizes.Count-1, Point.Zero);
                }
            }
        }



        // Public for debugging:
        public Data2D<Color> CreateFinalSheet(int sheetIndex)
        {
            Debug.Assert(sheetIndex >= 0 && sheetIndex < SheetCount);

            Data2D<Color> sheetData = new Data2D<Color>(0, 0, sheetSizes[sheetIndex].X, sheetSizes[sheetIndex].Y);

            for(int i = 0; i < placements.Length; i++)
            {
                if(sheetPlacement[i] != sheetIndex+1)
                    continue; // <- goes on a different sheet

                Data2D<Color> image = images[i]; // <- copy by value!
                for(int y = 0; y < image.Height; y++)
                {
                    int sourceIndex = y * image.Width;
                    int destinationIndex = (placements[i].Y + y) * sheetData.Width + placements[i].X;
                    Array.Copy(image.Data, sourceIndex, sheetData.Data, destinationIndex, image.Width);
                }
            }

            return sheetData;
        }

        #endregion

        #region Write to file

        public const int formatVersion = 0;

        public void WriteOutAllImagesOLD(BinaryWriter bw)
        {
            imagesLocked = true;
            Pack();

            bw.Write(formatVersion);

            // Write out textures (sheets):
            bw.Write(sheetSizes.Count);
            for(int sheetIndex = 0; sheetIndex < sheetSizes.Count; sheetIndex++)
            {
                Data2D<Color> sheetData = CreateFinalSheet(sheetIndex);
                Debug.Assert(sheetData.Width <= 2048);
                Debug.Assert(sheetData.Height <= 2048);
                bw.Write(sheetData.Width);
                bw.Write(sheetData.Height);

                Debug.Assert(sheetData.Data.Length == sheetData.Width * sheetData.Height);
                for(int i = 0; i < sheetData.Data.Length; i++)
                    bw.Write(sheetData.Data[i].PackedValue);
            }

            // Write out image locations in sheets:
            bw.Write(placements.Length);
            for(int i = 0; i < placements.Length; i++)
            {
                bw.Write(sheetPlacement[i]-1); // Sheet Index
                bw.Write(new Rectangle(placements[i].X, placements[i].Y, images[i].Width, images[i].Height)); // Source rectangle
            }
        }


        public unsafe void WriteOutAllImages(Stream output)
        {
            imagesLocked = true;
            Pack();

            // Write out textures (sheets):
            if(sheetSizes.Count > byte.MaxValue)
                throw new InvalidOperationException("Too many textures!");
            output.WriteByte((byte)sheetSizes.Count);

            // NOTE: Writing counts first, so we can allocate outside of fixed() in the decoder
            int imageCount = placements.Length;
            WriteShort(output, imageCount);

            for(int sheetIndex = 0; sheetIndex < sheetSizes.Count; sheetIndex++)
            {
                Data2D<Color> sheetData = CreateFinalSheet(sheetIndex);
                Debug.Assert(sheetData.Width <= 2048);
                Debug.Assert(sheetData.Height <= 2048);

                WriteShort(output, sheetData.Width);
                WriteShort(output, sheetData.Height);

                fixed(Color* data = sheetData.Data)
                {
                    RCRURLEWriter.Encode((byte*)data, sheetData.Width, sheetData.Height, output);
                }
            }


            // Write out image locations in sheets:
            if(sheetSizes.Count > 1) // <- only bother writing texture indicies if there is more than one texture
            {
                for(int i = 0; i < placements.Length; i++)
                {
                    int textureIndex = sheetPlacement[i]-1;
                    Debug.Assert(textureIndex >= byte.MinValue && textureIndex <= byte.MaxValue);
                    output.WriteByte((byte)textureIndex);
                }
            }

            for(int i = 0; i < placements.Length; i++)
            {
                WriteShort(output, placements[i].X);
                WriteShort(output, placements[i].Y);
                WriteShort(output, images[i].Width);
                WriteShort(output, images[i].Height);
            }
        }

        private static void WriteShort(Stream output, int value)
        {
            if(value < short.MinValue || value > short.MaxValue)
                throw new ArgumentOutOfRangeException();

            // little-endian:
            output.WriteByte((byte)value);
            output.WriteByte((byte)(value >> 8));
        }

        #endregion
    }
}
