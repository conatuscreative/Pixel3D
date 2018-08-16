using System.Diagnostics;

namespace Pixel3D.Engine.Navigation
{
    public static class WalkmapTools
    {


        //
        //
        // IMPORTANT: ConvertLocalHeightmapToWalkmap was the slowest method in the game.
        //            I've made a hard-coded 17-width version because EVERY character in the game is that wide (including Abobo, for some reason)
        //
        //
        //
        // Using a branch here (including Math.Max) is very slow because the branch is completely unpredictable!
        // Instead: use a branchless max (many more instructions, but it's about twice as fast)
        //
        //

        public static unsafe void ConvertLocalHeightmapToWalkmap17(byte* input, byte* output, int inputWidth, int depth)
        {
            // NOTE: In theory, input and output can point to the same buffer. Hopefully we've got all the maths right to make that work!!

            Debug.Assert(inputWidth > 0);
            Debug.Assert(depth > 0);

            // IMPORTANT: Beware! Pointer arithmatic!

            const int characterWidth = 17;
            const int characterSides = characterWidth - 1; // <- The sides of the character abutt the heightmap, while a single pixel abutts the walkmap
            int outputWidth = inputWidth - characterSides;
            Debug.Assert(outputWidth > 0);

            byte* inputRow = input;
            byte* outputRow = output;

            for(int z = 0; z < depth; z++)
            {
                for(int x = 0; x < outputWidth; x++)
                {
                    // Still using branchless max in this version:
                    /*
                    int max = inputRow[x+ 0];
                    max = max - ((max-inputRow[x+ 1]) & ((max-inputRow[x+ 1]) >> 31));
                    max = max - ((max-inputRow[x+ 2]) & ((max-inputRow[x+ 2]) >> 31));
                    max = max - ((max-inputRow[x+ 3]) & ((max-inputRow[x+ 3]) >> 31));
                    max = max - ((max-inputRow[x+ 4]) & ((max-inputRow[x+ 4]) >> 31));
                    max = max - ((max-inputRow[x+ 5]) & ((max-inputRow[x+ 5]) >> 31));
                    max = max - ((max-inputRow[x+ 6]) & ((max-inputRow[x+ 6]) >> 31));
                    max = max - ((max-inputRow[x+ 7]) & ((max-inputRow[x+ 7]) >> 31));
                    max = max - ((max-inputRow[x+ 8]) & ((max-inputRow[x+ 8]) >> 31));
                    max = max - ((max-inputRow[x+ 9]) & ((max-inputRow[x+ 9]) >> 31));
                    max = max - ((max-inputRow[x+10]) & ((max-inputRow[x+10]) >> 31));
                    max = max - ((max-inputRow[x+11]) & ((max-inputRow[x+11]) >> 31));
                    max = max - ((max-inputRow[x+12]) & ((max-inputRow[x+12]) >> 31));
                    max = max - ((max-inputRow[x+13]) & ((max-inputRow[x+13]) >> 31));
                    max = max - ((max-inputRow[x+14]) & ((max-inputRow[x+14]) >> 31));
                    max = max - ((max-inputRow[x+15]) & ((max-inputRow[x+15]) >> 31));
                    max = max - ((max-inputRow[x+16]) & ((max-inputRow[x+16]) >> 31));*/
                    
                    // THIS IS MUCH FASTER! Reduced dependency chains, good assembly output.
                    int max = inputRow[x+ 0];
                    int a = inputRow[x+ 1] - ((inputRow[x+ 1]-inputRow[x+ 2]) & ((inputRow[x+ 1]-inputRow[x+ 2]) >> 31)); max = max - ((max-a) & ((max-a) >> 31));
                    a     = inputRow[x+ 3] - ((inputRow[x+ 3]-inputRow[x+ 4]) & ((inputRow[x+ 3]-inputRow[x+ 4]) >> 31)); max = max - ((max-a) & ((max-a) >> 31));
                    a     = inputRow[x+ 5] - ((inputRow[x+ 5]-inputRow[x+ 6]) & ((inputRow[x+ 5]-inputRow[x+ 6]) >> 31)); max = max - ((max-a) & ((max-a) >> 31));
                    a     = inputRow[x+ 7] - ((inputRow[x+ 7]-inputRow[x+ 8]) & ((inputRow[x+ 7]-inputRow[x+ 8]) >> 31)); max = max - ((max-a) & ((max-a) >> 31));
                    a     = inputRow[x+ 9] - ((inputRow[x+ 9]-inputRow[x+10]) & ((inputRow[x+ 9]-inputRow[x+10]) >> 31)); max = max - ((max-a) & ((max-a) >> 31));
                    a     = inputRow[x+11] - ((inputRow[x+11]-inputRow[x+12]) & ((inputRow[x+11]-inputRow[x+12]) >> 31)); max = max - ((max-a) & ((max-a) >> 31));
                    a     = inputRow[x+13] - ((inputRow[x+13]-inputRow[x+14]) & ((inputRow[x+13]-inputRow[x+14]) >> 31)); max = max - ((max-a) & ((max-a) >> 31));
                    a     = inputRow[x+15] - ((inputRow[x+15]-inputRow[x+16]) & ((inputRow[x+15]-inputRow[x+16]) >> 31)); max = max - ((max-a) & ((max-a) >> 31));

                    // This one is slightly slower:
                    /*
                    int max = inputRow[x+ 0];
                    int a, b, c;
                    a = inputRow[x+ 1] - ((inputRow[x+ 1]-inputRow[x+ 2]) & ((inputRow[x+ 1]-inputRow[x+ 2]) >> 31));
                    b = inputRow[x+ 3] - ((inputRow[x+ 3]-inputRow[x+ 4]) & ((inputRow[x+ 3]-inputRow[x+ 4]) >> 31)); c = a - ((a-b) & ((a-b) >> 31)); max = max - ((max-c) & ((max-c) >> 31));
                    a = inputRow[x+ 5] - ((inputRow[x+ 5]-inputRow[x+ 6]) & ((inputRow[x+ 5]-inputRow[x+ 6]) >> 31));
                    b = inputRow[x+ 7] - ((inputRow[x+ 7]-inputRow[x+ 8]) & ((inputRow[x+ 7]-inputRow[x+ 8]) >> 31)); c = a - ((a-b) & ((a-b) >> 31)); max = max - ((max-c) & ((max-c) >> 31));
                    a = inputRow[x+ 9] - ((inputRow[x+ 9]-inputRow[x+10]) & ((inputRow[x+ 9]-inputRow[x+10]) >> 31));
                    b = inputRow[x+11] - ((inputRow[x+11]-inputRow[x+12]) & ((inputRow[x+11]-inputRow[x+12]) >> 31)); c = a - ((a-b) & ((a-b) >> 31)); max = max - ((max-c) & ((max-c) >> 31));
                    a = inputRow[x+13] - ((inputRow[x+13]-inputRow[x+14]) & ((inputRow[x+13]-inputRow[x+14]) >> 31));
                    b = inputRow[x+15] - ((inputRow[x+15]-inputRow[x+16]) & ((inputRow[x+15]-inputRow[x+16]) >> 31)); c = a - ((a-b) & ((a-b) >> 31)); max = max - ((max-c) & ((max-c) >> 31));*/

                    outputRow[x] = (byte)max;
                }

                // Next row!
                inputRow += inputWidth;
                outputRow += outputWidth;
            }
        }



        public static unsafe void ConvertLocalHeightmapToWalkmap(byte* input, byte* output, int inputWidth, int depth, int characterWidth)
        {
            if(characterWidth == 17)
            {
                ConvertLocalHeightmapToWalkmap17(input, output, inputWidth, depth);
                return;
            }
            
            // NOTE: In theory, input and output can point to the same buffer. Hopefully we've got all the maths right to make that work!!

            Debug.Assert(characterWidth > 0);
            Debug.Assert(inputWidth > 0);
            Debug.Assert(depth > 0);

            // IMPORTANT: Beware! Pointer arithmatic!

            int characterSides = characterWidth - 1; // <- The sides of the character abutt the heightmap, while a single pixel abutts the walkmap
            int outputWidth = inputWidth - characterSides;
            Debug.Assert(outputWidth > 0);

            byte* inputRow = input;
            byte* outputRow = output;

            for(int z = 0; z < depth; z++)
            {
                for(int x = 0; x < outputWidth; x++)
                {
                    int readTo = x + characterWidth; // <- the first pixel of input and output (effectively) overlap, which is why reading `characterWidth` instead of `characterSides` is safe

                    int max;
                    int r;
                    if((characterWidth & 1) != 0) // Odd (NOTE: Predictable branch)
                    {
                        max = inputRow[x];
                        r = x+1;
                    }
                    else // Even and >= 2 (because we assert on entry)
                    {
                        max = inputRow[x+ 0] - ((inputRow[x+ 0]-inputRow[x+ 1]) & ((inputRow[x+ 0]-inputRow[x+ 1]) >> 31));
                        r = x+2;
                    }

                    // Doing two elements at once, to reduce dependency chain. Very slightly faster.
                    // (This was an idea from the unrolled version, where it works much, much better! -AR)
                    for( ; r < readTo; r += 2)
                    {
                        int a = inputRow[r+ 0] - ((inputRow[r+ 0]-inputRow[r+ 1]) & ((inputRow[r+ 0]-inputRow[r+ 1]) >> 31));
                        max = max - ((max-a) & ((max-a) >> 31));
                    }

                    outputRow[x] = (byte)max;
                }

                // Next row!
                inputRow += inputWidth;
                outputRow += outputWidth;
            }
        }




        public static Bounds CalculateWalkmapBounds(Bounds heightmapBounds, int characterStartX, int characterEndX)
        {
            // A walkmap is the per-pixel walkable height in a given heightmap, which is derived from all pixels in the character width of that position
            // as such, there is an excess border on the heightmap that gets trimmed off when calculating the walkmap

            #region ASCII Art
            //
            // So if we start off with a heightmap with this width:
            //
            //  hhhhhhhhhhhhhhhhhh 
            //
            // A character of the given dimentions can walk to either end, giving a smaller walkmap
            //
            //  hhhhhhhhhhhhhhhhhh 
            //  |..*.|      |..*.| 
            //     wwwwwwwwwwwww 
            //
            #endregion

            heightmapBounds.startX -= characterStartX;
            heightmapBounds.endX -= characterEndX;
            heightmapBounds.endX += 1; // <- because we combined two exclusive boundaries

            return heightmapBounds;
        }


        public static Bounds CalculateLocalHeightmapBoundsForWalkmap(Bounds walkmapBounds, int characterStartX, int characterEndX)
        {
            walkmapBounds.startX += characterStartX;
            walkmapBounds.endX += characterEndX;
            walkmapBounds.endX -= 1;  // <- because we combined two exclusive boundaries

            return walkmapBounds;
        }




        public static Range CalculateWalkmapRange(Range heightmapRange, int characterStartX, int characterEndX)
        {
            heightmapRange.start -= characterStartX;
            heightmapRange.end -= characterEndX;
            heightmapRange.end += 1; // <- because we combined two exclusive boundaries

            return heightmapRange;
        }

        public static Range CalculateLocalHeightmapRangeForWalkmap(Range walkmapRange, int characterStartX, int characterEndX)
        {
            walkmapRange.start += characterStartX;
            walkmapRange.end += characterEndX;
            walkmapRange.end -= 1;  // <- because we combined two exclusive boundaries

            return walkmapRange;
        }


    }
}
