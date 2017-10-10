using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Pixel3D.Animations.Serialization
{
    static class RCRURLEReader
    {
        // NOTE: Sync with RCRURLEWriter!
        const int maxPalletSize = 126; // enough space for codes:
        // A bunch of other constants are hard-coded and shared (keep in sync, duh)


        /// <param name="input">Pointer to input data</param>
        /// <param name="output">Pointer to a buffer with enough space to store the decoded image</param>
        /// <param name="count">Number of pixels in the output</param>
        public static unsafe byte* Decode(byte* input, uint* output, int count)
        {
            // Read pallete:
            int palleteCount = *(input++); // <- NOTE: does not include transparency
            uint* pallete = stackalloc uint[palleteCount+1]; // <- I'd allocate maxPalletSize, but .NET is probably going to do a memory clear on me...

            pallete[0] = 0; // <- set transparency
            uint* palleteWrite = pallete + 1; // <- IMPORTANT: skip transparency
            for(int i = 0; i < palleteCount; i++)
            {
                uint palleteEntry = 0xFF000000;
                // 24-bit little-endian:
                palleteEntry |= ((uint)*(input++));
                palleteEntry |= ((uint)*(input++) << 8);
                palleteEntry |= ((uint)*(input++) << 16);
                palleteWrite[i] = palleteEntry;
            }

            // Decode RLE:
            {
                uint lastColor = 0; // <- Dear JIT, please make this a register, Love Andrew

                uint* outputEnd = output + count;
                while(output < outputEnd)
                {
                    byte code = *(input++);
                    
                    if((code & 0x80) == 0) // Pallete Entry
                    {
                        if((code & 0x7E) != 0x7E) // Non-special
                        {
                            *(output++) = lastColor = pallete[code];
                        }
                        else // Special
                        {
                            lastColor = ((uint)*(input++));
                            lastColor |= ((uint)*(input++) << 8);
                            lastColor |= ((uint)*(input++) << 16);

                            if((code & 1) == 0) // Solid literal
                                lastColor |= 0xFF000000u;
                            else
                                lastColor |= ((uint)*(input++) << 24);

                            *(output++) = lastColor;
                        }
                    }
                    else // RLE:
                    {
                        int runLength = code & ((1 << 6) - 1); // NOTE: This is "encodeSmallBits" in the writer
                        if((code & 0x40) != 0)
                        {
                            runLength <<= 8;
                            runLength |= *(input++);
                        }
                        runLength += 2;

                        while(runLength != 0)
                        {
                            --runLength;
                            *(output++) = lastColor;
                        }
                    }

                    Debug.Assert(output <= outputEnd); // <- please don't overflow.
                }

                Debug.Assert(output == outputEnd);
            }

            return input;
        }



    }
}
