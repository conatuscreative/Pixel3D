using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pixel3D.Animations;
using System.IO;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Pixel3D.Extensions;

#region GIF Format Documentation
// http://www.w3.org/Graphics/GIF/spec-gif89a.txt
// http://www.matthewflickinger.com/lab/whatsinagif/bits_and_bytes.asp
// http://www.matthewflickinger.com/lab/whatsinagif/lzw_image_data.asp
// http://www.matthewflickinger.com/lab/whatsinagif/animation_and_transparency.asp
// http://warp.povusers.org/EfficientLZW/index.html
#endregion


namespace Pixel3D
{
    public class GifWriter
    {
        Stream stream;

        public void SetStream(Stream stream)
        {
            if(stream == null)
                throw new ArgumentNullException("stream");
            if(this.stream != null)
                throw new InvalidOperationException("Stream already set");

            this.stream = stream;
        }

        public void CloseStream()
        {
            if(this.stream == null)
                throw new InvalidOperationException("No stream set");

            this.stream.Close();
            this.stream = null;
        }



        // TODO: Factor this out such that frames can be appended on-the-fly (for capturing "animated screenshots").
        //
        //       May want to do threading  (outside this class). Could thread the following:
        //       - Pulling GPU surfaces (want to return surfaces quikcly)
        //       - GIF encoding (maybe fast enough to combine, rather than take buffer re-cache hit and thread costs)
        //       - File I/O (want to avoid blocking useful work while writing to file, and built-in async allocates, uses a thread pool anyway??)
        //       Debatable about what, if anything, to combine.


        public void WriteAnimation(Animation animation)
        {
            if(animation == null)
                throw new ArgumentNullException("animation");
            if(animation.FrameCount == 0)
                return;

            if(stream == null)
                throw new InvalidOperationException("No stream set");

            Rectangle animationBounds = animation.GetSoftRenderBounds(true);
            Position accumulatedGameplayMotion = Position.Zero;

            bool animated = animation.FrameCount > 1;

            // Preconditions:
            if(animationBounds.Width > ushort.MaxValue || animationBounds.Height > ushort.MaxValue)
                return;

            bool transparency = true;

            // GIF: Header
            {
                WriteBuffer(gifHeader);
            }

            // GIF: Logical Screen Descriptor
            {
                WriteUShort((ushort)animationBounds.Width);
                WriteUShort((ushort)animationBounds.Height);

                byte packed = 0;
                packed |= 1 << 7; // Global colour table present
                packed |= 7 << 4; // Bits-per-channel minus 1, in source data
                packed |= 0 << 3; // Is GCT priority-sorted?
                packed |= 7 << 0; // Global colour table size = 2^(N+1) 
                WriteByte(packed);

                WriteByte(0); // Background colour index into GCT (or 0 if not present)
                WriteByte(0); // Pixel aspect ratio, or 0 for "not-specified"
            }

            // GIF: Global Color Table
            // TODO: Consider using pre-built NES palette? (Especially for game export)
            // TODO: Add support for local color tables
            // Reserve and clear space in output buffer:
            const int colorTableBytes = 256 * 3; // 256 RGB Colors
            int gctStart = bufferPosition;
            EnsureBufferSpace(colorTableBytes);
            bufferPosition += colorTableBytes;
            Array.Clear(buffer, gctStart, colorTableBytes);
            int gctCount = 1; // <- first value is always pure black, and transparent if transparency is enabled

            // GIF: NETSCAPE2.0 Application Extension Block
            if(animated)
            {
                WriteBuffer(netscapeBlock);
            }

            foreach(var frame in animation.Frames)
            {
                var data = frame.SoftRender();

                // HACK: If the image has zero size (blank, cropped to nothing), replace it with 1px of transparency
                //       (Because I'm not fiddling around trying to figure out how to encode a zero-sized frame, or how compatible that is, right now -AR)
                if(data.Width == 0 || data.Height == 0)
                    data = new Data2D<Color>(animationBounds.X, animationBounds.Y, 1, 1);

                accumulatedGameplayMotion += frame.positionDelta;
                Point motion = accumulatedGameplayMotion.ToWorldZero.FlipY(); // Convert to texture space
                data.OffsetX += motion.X;
                data.OffsetY += motion.Y;

                int delayTime = (frame.delay * 100) / 60; // Convert ticks at 60FPS to ticks at 100FPS (the GIF standard)
                // NOTE: Browsers do retarded things with timings of 0 or 1. Some even do stupid things with 2-5.
                //       See http://nullsleep.tumblr.com/post/16524517190/animated-gif-minimum-frame-delay-browser for details
                delayTime = System.Math.Max(2, System.Math.Min(delayTime, ushort.MaxValue)); 

                // GIF: Graphic Control Extension
                {
                    WriteByte(0x21); // extension block
                    WriteByte(0xF9); // graphics control label
                    WriteByte(0x04); // block size in bytes (fixed)

                    byte packed = 0;
                    packed |= (byte)((animated ? 2u : 0u) << 2); // Disposal method (0 = none specified, 1 = do not clear, 2 = clear to BG)
                    packed |= 0 << 1; // User input flag
                    packed |= (byte)((transparency ? 1u : 0) << 0); // Transparent colour flag
                    WriteByte(packed);

                    WriteUShort((ushort)(animated ? delayTime : 0)); // Delay time
                    WriteByte(0); // Transparent colour index // TODO: Put this outside the table, so that non-transparent images don't have fixed black entry in palette.
                    WriteByte(0); // Block terminator
                }

                // GIF: Image descriptor
                {
                    WriteByte(0x2C); // Image separator
                    WriteUShort((ushort)(data.OffsetX - animationBounds.X)); // Image left
                    WriteUShort((ushort)(data.OffsetY - animationBounds.Y)); // Image top
                    WriteUShort((ushort)data.Width); // Image width
                    WriteUShort((ushort)data.Height); // Image height

                    byte packed = 0;
                    packed |= 0 << 7; // Local colour table
                    packed |= 0 << 6; // Interlace
                    packed |= 0 << 5; // Is LCT priority sorted?
                    packed |= 0 << 0; // Local colour table size = 2^(N+1)
                    WriteByte(packed);

                    // NOTE: Local colour table would folow if specified
                }

                // GIF: Image Data (with LZW compression)
                {
                    WriteByte(8); // LZW minimum code size (hard-coding for 256-colour images for now)
                    WriteByte(0); // Placeholder for chunk size
                    PendingBitsAndAndChunks pending = new PendingBitsAndAndChunks();

                    // Code table is a series of unbalanced binary trees of available suffixes, one for each prefix
                    LZWTableEntry[] codeTable;
                    ushort[] treeRoots;
                    if(codeTableStorage == null)
                    {
                        codeTable = codeTableStorage = new LZWTableEntry[4096 - codeStart]; // maximum 12-bit codes
                        treeRoots = treeRootsStorage = new ushort[4096];
                    }
                    else
                    {
                        codeTable = codeTableStorage;
                        treeRoots = treeRootsStorage;
                        Array.Clear(treeRoots, 0, treeRoots.Length);
                    }

                    int codeCount = 0; // <- number of used entries in the (stored) code table
                    int codeBitsUsed = 9;

                    // Always start with clear code:
                    WriteVariableBitsChunked(clearCode, codeBitsUsed, ref pending);

                    // Setup active prefix:
                    uint activePrefix = (uint)GetColorTableIndex(buffer, gctStart, ref gctCount, data.Data[0], transparency);
                    Debug.Assert(activePrefix < 256);

                    // Loop through remaining indicies:
                    int imageSize = data.Width * data.Height;
                    for(int i = 1; i < imageSize; i++)
                    {
                        uint activeSuffix = (uint)GetColorTableIndex(buffer, gctStart, ref gctCount, data.Data[i], transparency);
                        Debug.Assert(activeSuffix < 256);

                        uint lastTreePosition = 0; // <- sentinal value for the root
                        uint foundSuffix = 0;
                        // Search for matching entry:
                        {
                            uint treePosition = treeRoots[activePrefix];
                            while(treePosition != 0)
                            {
                                lastTreePosition = treePosition;
                                foundSuffix = codeTable[treePosition - codeStart].SuffixValue;

                                if(activeSuffix == foundSuffix)
                                {
                                    // FOUND:
                                    activePrefix = treePosition;
                                    goto nextIndex;
                                }
                                else if(activeSuffix < foundSuffix)
                                    treePosition = codeTable[treePosition - codeStart].LowerIndex;
                                else // activeSuffix > foundSuffix
                                    treePosition = codeTable[treePosition - codeStart].HigherIndex;
                            }
                        }

                        // NOT FOUND:

                        // Write the code we do know about
                        WriteVariableBitsChunked(activePrefix, codeBitsUsed, ref pending);

                        // If we fill up the code table, reset it
                        if(codeCount == codeTable.Length)
                        {
                            WriteVariableBitsChunked(clearCode, codeBitsUsed, ref pending);
                            Array.Clear(treeRoots, 0, treeRoots.Length);
                            codeCount = 0;
                            codeBitsUsed = 9;
                        }
                        else // Add to the code table
                        {
                            // Insert into unbalanced binary tree (prefix index is implicit in the tree itself)
                            if(lastTreePosition == 0)
                                treeRoots[activePrefix] = (ushort)(codeStart + codeCount);
                            else if(activeSuffix < foundSuffix)
                                codeTable[lastTreePosition - codeStart].LowerIndex = (uint)(codeStart + codeCount);
                            else // activeSuffix > foundSuffix
                                codeTable[lastTreePosition - codeStart].HigherIndex = (uint)(codeStart + codeCount);

                            // If the new code is past the maximum representable at this code width, increase the code width
                            if((codeStart + codeCount) == (1 << codeBitsUsed))
                                codeBitsUsed++; // NOTE: GIF does *not* do an early change of the code-width

                            // Fill table entry:
                            codeTable[codeCount++] = new LZWTableEntry(activeSuffix);
                        }

                        // Active prefix has been written, start searching from the suffix
                        activePrefix = activeSuffix;

                    nextIndex: ;
                    }

                    WriteVariableBitsChunked(activePrefix, codeBitsUsed, ref pending);
                    WriteVariableBitsChunked(endCode, codeBitsUsed, ref pending);

                    FinishVariableBitsChunked(ref pending);
                    FinishChunked(ref pending);
                }
            }

            // GIF: Trailer
            WriteByte(0x3B);


            // Finished creating gif data at this point, write it out
            stream.Write(buffer, 0, bufferPosition);
            bufferPosition = 0;
        }



        #region GIF Constants

        private static readonly byte[] gifHeader = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; // GIF89a
        // The only configurable bit of data is the loop count, and we always want "forever"
        private static readonly byte[] netscapeBlock = { 0x21, 0xFF, 0x0B, 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50, 0x45, 0x32, 0x2E, 0x30, 0x03, 0x01, 0x00, 0x00, 0x00 };

        #endregion


        #region LZW

        // NOTE: Hard-coding 8-bit palette:
        const uint clearCode = 256;
        const uint endCode = 257;
        const int codeStart = 258; // Stored code table starts at this index in the logical table

        /// <summary>An entry in a 12-bit (maximum) LZW dictionary. Acts as an unbalanced binary tree.</summary>
        struct LZWTableEntry
        {
            // 12-bits of index = 4096 possible values
            // 32-bits table entries = 16kB (fits in L1 cache with room to spare, and is native size)

            // 12 bits for next higher suffix in tree
            // 12 bits for next lower suffix in tree
            // 8 bits suffix value (byte-sized)
            public uint packedData;

            const uint mask12 = (1u<<12)-1u;

            public uint HigherIndex { get { return packedData >> 20; } set { Debug.Assert(value < 4096); packedData = (packedData & ~(mask12<<20)) | value << 20; } }
            public uint LowerIndex { get { return (packedData >> 8) & mask12; } set { Debug.Assert(value < 4096); packedData = (packedData & ~(mask12<<8)) | value << 8; } }
            public uint SuffixValue { get { return packedData & 0xFFu; } set { Debug.Assert(value < 256); packedData = (packedData & ~0xFFu) | (uint)value; } }

            /// <summary>Initialize for a given suffix value</summary>
            public LZWTableEntry(uint suffixValue)
            {
                Debug.Assert(suffixValue < 256u);
                packedData = suffixValue;
            }
        }

        // LZW table/tree structure:
        private LZWTableEntry[] codeTableStorage = new LZWTableEntry[4096 - codeStart];
        private ushort[] treeRootsStorage = new ushort[4096]; // TODO: Wasting 2 whole kb by not packing this...

        #endregion


        #region Buffer writing

        // TODO: Pre-size this buffer before all writes

        byte[] buffer = new byte[16*1024];
        int bufferPosition;

        void WriteByte(byte value)
        {
            if(bufferPosition >= buffer.Length)
                Array.Resize(ref buffer, buffer.Length * 2);
            buffer[bufferPosition++] = value;
        }

        void WriteUShort(ushort value)
        {
            WriteByte((byte)(value));
            WriteByte((byte)(value >> 8));
        }

        void WriteBuffer(byte[] sourceBuffer)
        {
            EnsureBufferSpace(sourceBuffer.Length);
            Array.Copy(sourceBuffer, 0, buffer, bufferPosition, sourceBuffer.Length);
            bufferPosition += sourceBuffer.Length;
        }

        void EnsureBufferSpace(int bytesToWrite)
        {
            if(bufferPosition + bytesToWrite > buffer.Length)
                ExpandBuffer(bytesToWrite);
        }

        void ExpandBuffer(int bytesToWrite) // Uncommon path
        {
            int desiredLength = bufferPosition + bytesToWrite;
            int newLength = buffer.Length;
            do
            {
                newLength *= 2;
            } while(newLength < desiredLength);

            Array.Resize(ref buffer, newLength);
        }

        #endregion


        #region Variable bit writer (with chunking)

        struct PendingBitsAndAndChunks
        {
            // Variable bit width:
            public uint value;
            public int bits;

            // Chunking:
            public int chunkSize;
        }

        private void WriteVariableBitsChunked(uint value, int bits, ref PendingBitsAndAndChunks pending)
        {
            // NOTE: GIF uses LSB-first packing

            Debug.Assert(pending.bits >= 0 && pending.bits < 8);
            Debug.Assert(value < (1u<<bits));

            while(bits > 0)
            {
                int takeBits = System.Math.Min(bits, 8-pending.bits);
                uint takeMask = (1u << takeBits) - 1u;

                pending.value |= ((value & takeMask) << pending.bits);

                pending.bits += takeBits;
                bits -= takeBits;
                value >>= takeBits;

                if(pending.bits == 8)
                {
                    WriteChunked((byte)(pending.value & 0xFF), ref pending);
                    pending.value = 0;
                    pending.bits = 0;
                }
            }
        }

        private void FinishVariableBitsChunked(ref PendingBitsAndAndChunks pending)
        {
            if(pending.bits > 0)
            {
                WriteChunked((byte)(pending.value & 0xFF), ref pending);
                pending.value = 0;
                pending.bits = 0;
            }
        }

        private void WriteChunked(byte byteToWrite, ref PendingBitsAndAndChunks pending)
        {
            WriteByte(byteToWrite);
            pending.chunkSize++;

            if(pending.chunkSize == 255)
            {
                buffer[bufferPosition - 256] = 255;
                pending.chunkSize = 0;
                WriteByte(0); // Placeholder for chunk size
            }
        }

        private void FinishChunked(ref PendingBitsAndAndChunks pending)
        {
            if(pending.chunkSize > 0) // If we're right on the chunk boundary (0 bytes in current chunk), just use the placeholder
            {
                Debug.Assert(pending.chunkSize <= byte.MaxValue);
                buffer[bufferPosition - pending.chunkSize - 1] = (byte)pending.chunkSize;
                pending.chunkSize = 0;
                WriteByte(0); // Terminator
            }
        }

        #endregion


        #region Palette

        private static int GetColorTableIndex(byte[] buffer, int colorTableStart, ref int colorCount, Color target, bool hasTransparency)
        {
            if(target.A != 0xFF) // <- Not even going to bother with semi-transparent values
                return 0; // Always transparent in this encoder (or black)

            // NOTE: Is a linear lookup fast enough?
            int i = hasTransparency ? 1 : 0;
            int colorCountLocal = colorCount; // <- should probably check the x86 output to see if the optimiser would do this for us anyway
            for(; i < colorCountLocal; i++)
            {
                if(buffer[colorTableStart + i * 3 + 0] != target.R)
                    continue;
                if(buffer[colorTableStart + i * 3 + 1] != target.G)
                    continue;
                if(buffer[colorTableStart + i * 3 + 2] != target.B)
                    continue;
                // Found a match
                return i;
            }

            // Failed to find desired colour (break out infrequent operations)
            if(colorCountLocal == 256) // <- colour table is full!
                return GetColorTableBestMatch(buffer, colorTableStart, colorCountLocal, target, hasTransparency);
            else
                return AddColorToTable(buffer, colorTableStart, ref colorCount, target);
        }

        private static int AddColorToTable(byte[] buffer, int colorTableStart, ref int colorCount, Color target)
        {
            buffer[colorTableStart + colorCount * 3 + 0] = target.R;
            buffer[colorTableStart + colorCount * 3 + 1] = target.G;
            buffer[colorTableStart + colorCount * 3 + 2] = target.B;
            return colorCount++;
        }

        private static int GetColorTableBestMatch(byte[] buffer, int colorTableStart, int colorCount, Color target, bool hasTransparency)
        {
            int tR = target.R;
            int tG = target.G;
            int tB = target.B;

            int bestDistance = hasTransparency ? int.MaxValue : (tR * tR + tG * tG + tB * tB); // <- distance to black
            int bestIndex = 0;

            for(int i = 1; i < colorCount; i++)
            {
                int rr = buffer[colorTableStart + i * 3 + 0] - tR;
                int gg = buffer[colorTableStart + i * 3 + 1] - tG;
                int bb = buffer[colorTableStart + i * 3 + 2] - tB;

                int distance = rr*rr + gg*gg + bb*bb;
                if(distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        #endregion

    }
}

