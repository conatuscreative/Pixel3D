using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace Pixel3D.Animations.Serialization
{
    public static class RCRURLEWriter // <- RLE specifically for RCRU data
    {
        // We need a compression format for our texture data that decompresses extremely quicky
        // (much faster than GZipStream which was making us drop frames because it was so painfully slow)
        //
        // We have an EXCELLENT idea about what our image data contains:
        // - Extremely low colour counts (less than 64 colours, except in a few rare cases)
        // - 1-bit transparancy (except in a few rare cases)
        // - Very long runs of a few colours (especially transparent, white, black, but sometimes as many as 10 for skys, roads, etc)
        // - Short runs of some other colours
        // - High degree of colour locality (long runs with only a tiny palette)
        //      (I was thinking about handling this with a local-pallet scheme, but that is actually very complicated to encode well (when to change mode?)
        //       so instead we could just slap LZ4 over the top and hope for the best - it should be about as good - possibly better because it can "see" patterns
        //       in a more generic way (we have lots of patterns) rather than just constrained pallets)
        //
        // The decoder needs to be absurdly fast!
        // - It runs at draw time just before we need a texture (can't be parallelised), and we don't have enough memory to keep decompressed textures in RAM
        // - Performance gain from having a tiny input
        // - Strategy: Try to encode long runs that the branch-predictor will accurately handle (~2-3 misses per coding)
        // - Considered format with aligned 32-bit reads (too hard)
        // - LZ4 is apparently wicked-fast (could use it, if RLE is insufficient compression)
        //
        //
        //
        // RLE Scheme:
        //
        // Single colour entry
        //   - 1 bits to select mode
        //   - 7 bits for palette entry with special codes
        //
        //   Special codes:
        //      - 24-bit literal (escape hatch)
        //      - 32-bit literal (escape hatch)
        //
        //   (Maximum palette size = 126, with 0 always = transparent)
        //
        // RLE Solid Color:
        //   - 1 bits to select mode
        //   - 1 bit to decide to read another length byte
        //   - 6 bits of length => [N]
        //   - optional 8 bits of length data => [N] (maximum value of N is 16383)
        //   (Writes [N+k] repeats of the previous colour)
        // 
        //   Minimum size with k=1 will write 1 pixel using up 1 byte (same as flat encoding), so make k=2
        //
        //
        //



        /// <param name="rleCount">Number of pixels run we want to encode</param>
        private static void WriteRLECount(Stream output, uint[] palette, uint color, int rleCount)
        {
            const int encodeSmallBits = 6;
            const int encodeExtraBits = 8;
            const int encodeLargeBits = encodeSmallBits + encodeExtraBits;
            const int encodeMax = (1 << encodeLargeBits) - 1 + 2; // <- Largest value we can encode

            while(rleCount >= encodeMax)
            {
                output.WriteByte((byte)0xFF); // rle on, extra count on, highest value
                output.WriteByte((byte)0xFF);
                rleCount -= encodeMax;
            }

            Debug.Assert(rleCount >= 0);

            if(rleCount == 0)
            {
                return; // nothing to encode
            }

            if(rleCount == 1) // <- single pixel simply encodes as another copy (worst case: two sequential transparent pixels)
            {
                WriteFlatPixel(output, palette, color);
                return;
            }

            int encodeValue = rleCount - 2; // <- decoder does +2

            if(encodeValue < (1 << encodeSmallBits)) // <- Do small encoding:
            {
                output.WriteByte((byte)(0x80 | encodeValue));
            }
            else // <- Do large encoding
            {
                output.WriteByte((byte)((0x80 | 0x40) | (encodeValue >> 8)));
                output.WriteByte((byte)encodeValue);
            }
        }


        // NOTE: Sync with RCRURLEReader!
        const int maxPalletSize = 126; // enough space for codes:
        const byte solidColorLiteralCode = 126;
        const byte semiTransparentLiteralCode = 127;


        private static void WriteFlatPixel(Stream output, uint[] palette, uint color)
        {
            // Because we are sorted by colour frequency, this should exit reasonably quickly:
            for(int i = 0; i < palette.Length; i++)
            {
                if(palette[i] == color)
                {
                    output.WriteByte((byte)i); // <- pallet entry
                    return;
                }
            }

            // Not found in the pallet
            if((color & 0xFF000000u) == 0xFF000000u)
            {
                output.WriteByte(solidColorLiteralCode);
                output.WriteByte((byte)(color >> 0));
                output.WriteByte((byte)(color >> 8));
                output.WriteByte((byte)(color >> 16));
            }
            else
            {
                output.WriteByte(semiTransparentLiteralCode);
                output.WriteByte((byte)(color >> 0));
                output.WriteByte((byte)(color >> 8));
                output.WriteByte((byte)(color >> 16));
                output.WriteByte((byte)(color >> 24));
            }
        }

        
        private static uint[] GetPalette(Dictionary<uint, int> colorCounts)
        {
            // Separate function for LINQ so it doesn't break edit-and-continue
            
            // 7 bits = 128 entries, so the 3 we're not including are:
            // 1) fully transparent (all other palette entries are solid)  @0
            // 2) trigger 24-bit literal  @126
            // 3) trigger 32-bit literal  @127
            var palette = colorCounts.OrderByDescending(kvp => kvp.Value).Select(kvp => kvp.Key).Take(maxPalletSize-1);
            var transparency = Enumerable.Repeat(0u, 1);

            var result = transparency.Concat(palette).ToArray();
            Debug.Assert(result.Length <= maxPalletSize);
            return result;
        }



        public static unsafe void Encode(byte* data, int width, int height, Stream output)
        {
            // Number of pixels:
            int count = width * height;

            if(count == 0) // <- we don't cope with non-existant textures
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            uint* pixels = (uint*)data; // <- pixels

            //
            // Determine the image pallet
            // (NOTE: most frequent colours have smaller indicies)
            //
            uint[] palette;

            // Fast version (no dictionary, limited pallet size)
            // TODO: Could potentially improve performance with swapping scheme to keep {palette, count} sorted
            {
                int lastColorIndex = -1;
                uint lastColor = 0; // <- Guaranteed to not match in the following loop

                int colorCount = 1; // <- Start with transparency in the palette
                palette = new uint[maxPalletSize];
                int[] colorTouchCount = new int[maxPalletSize];

                for(int i = 0; i < count; i++)
                {
                    uint color = pixels[i];
                    if((color & 0xFF000000u) == 0xFF000000u) // <- palette is solid-colour only
                    {
                        if(color == lastColor) // <- Optimisation to skip search (hey, it works for RLE)
                        {
                            colorTouchCount[lastColorIndex]++;
                            continue;
                        }
                        lastColor = color;

                        for(int c = 0; c < colorCount; c++)
                        {
                            if(palette[c] == color)
                            {
                                colorTouchCount[c]++;
                                lastColorIndex = c;
                                goto next;
                            }
                        }

                        // not found:
                        if(colorCount == maxPalletSize)
                            goto safeFindPalette;
                        palette[colorCount] = color;
                        colorTouchCount[colorCount] = 1;
                        lastColorIndex = colorCount;
                        colorCount++;
                    }

                next:
                    ;
                }

                // Success - sort the result:
                Array.Sort(colorTouchCount, palette, 1, colorCount - 1);
                Array.Resize(ref palette, colorCount); // <- Reduce size to actual palette count used
                goto donePalette;
            }


            // Slow version (safe for any pallet size, but slower due to dictionary)
        safeFindPalette:
            {
                Dictionary<uint, int> colorCounts = new Dictionary<uint, int>();

                for(int i = 0; i < count; i++)
                {
                    uint color = pixels[i];
                    if((color & 0xFF000000u) == 0xFF000000u) // <- palette is solid-colour only
                    {
                        int oldValue;
                        colorCounts.TryGetValue(color, out oldValue); // <- sets oldValue to zero if it's not found
                        colorCounts[color] = oldValue + 1;
                    }
                }

                palette = GetPalette(colorCounts);
            }

        donePalette:
            ;


            //
            // Write out the palette:
            //
            {
                Debug.Assert(palette.Length <= 126);
                Debug.Assert(palette.Length >= 1);
                output.WriteByte((byte)(palette.Length - 1));

                for(int i = 1; i < palette.Length; i++) // <- skip first entry (transparency)
                {
                    // Write 24-bit values:
                    Debug.Assert((palette[i] & 0xFF000000u) == 0xFF000000u);
                    output.WriteByte((byte)(palette[i] >> 0));
                    output.WriteByte((byte)(palette[i] >> 8));
                    output.WriteByte((byte)(palette[i] >> 16));
                }
            }



            //
            // Write out image with RLE
            //

            // Handle first pixel directly (no RLE)
            int indexOfLastNewColor = 0;
            uint lastNewColor = pixels[0];
            if((lastNewColor & 0xFF000000u) == 0u) // <- Force transparent to transparent
                lastNewColor = 0;

            WriteFlatPixel(output, palette, lastNewColor);

            for(int i = 1; i < count; i++)
            {
                uint color = pixels[i];
                if((color & 0xFF000000u) == 0u) // <- Force transparent to transparent
                    color = 0;

                if(color != lastNewColor)
                {
                    int rleCount = i - 1 - indexOfLastNewColor;
                    if(rleCount > 0)
                        WriteRLECount(output, palette, lastNewColor, rleCount);

                    WriteFlatPixel(output, palette, color);
                    indexOfLastNewColor = i;
                    lastNewColor = color;
                }
            }

            // Write final count if we were in RLE:
            {
                int rleCount = count - 1 - indexOfLastNewColor;
                if(rleCount > 0)
                    WriteRLECount(output, palette, lastNewColor, rleCount);
            }
        }



    }
}

