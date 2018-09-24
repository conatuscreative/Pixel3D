using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Pixel3D.Sorting
{
    public static class EdgeBits
    {
        /// <summary>Create sufficiently sized array to contain enough bits to store all possible edges in a directed graph of a given vertex count</summary>
        public static uint[] Create(int vertexCount)
        {
            int size = ((vertexCount * vertexCount) + 31) / 32; // round up to the next largest uint-sized block
            return new uint[size];
        }

        public static int Size(int vertexCount)
        {
            return ((vertexCount * vertexCount) + 31) / 32; // round up to the next largest uint-sized block;
        }


        public static bool IsEdge(this uint[] edgeBits, int vertexCount, int fromVertex, int toVertex)
        {
            int bit = fromVertex * vertexCount + toVertex; // <- ordered so that getting all "from" is fast
            return (edgeBits[bit >> 5] & (1u << (bit & 31))) != 0;
        }

        public static void SetEdge(this uint[] edgeBits, int vertexCount, int fromVertex, int toVertex)
        {
            int bit = fromVertex * vertexCount + toVertex; // <- ordered so that getting all "from" is fast
            edgeBits[bit >> 5] |= (1u << (bit & 31));
        }

        public static void ClearEdge(this uint[] edgeBits, int vertexCount, int fromVertex, int toVertex)
        {
            int bit = fromVertex * vertexCount + toVertex; // <- ordered so that getting all "from" is fast
            edgeBits[bit >> 5] &= ~(1u << (bit & 31));
        }


        /// <summary>Returns 0 or 1 depending on whether the bit for a given edge is set</summary>
        public static uint GetEdgeBit(this uint[] edgeBits, int vertexCount, int fromVertex, int toVertex)
        {
            int bit = fromVertex * vertexCount + toVertex; // <- ordered so that getting all "from" is fast
            return (edgeBits[bit >> 5] >> (bit & 31)) & 1u;
        }

        public static void ChangeEdgeBit(this uint[] edgeBits, int vertexCount, int fromVertex, int toVertex, uint value)
        {
            Debug.Assert((value & 1u) == value);
            int bit = fromVertex * vertexCount + toVertex; // <- ordered so that getting all "from" is fast
            edgeBits[bit >> 5] = (edgeBits[bit >> 5] & ~(1u << (bit & 31))) | (value << (bit & 31));
        }
    }


}
