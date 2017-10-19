using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Pixel3D.Sorting
{
    public struct SCC
    {
        public SCC(int index, int count) { this.index = index; this.count = count; }
        public int index;
        public int count;
    }

    /// <summary>Point to storage for the algorithm output</summary>
    public struct TarjanOutput
    {
        public int sccCount;
        public int vertexCount;

        /// <summary>List of strongly connected components of size > 1</summary>
        public SCC[] sccList;
        public int[] vertexList;


        /// <summary>Constructor intended for simple testing (allocates new arrays)</summary>
        public TarjanOutput(int vertexCapacity)
        {
            this.sccCount = 0;
            this.vertexCount = 0;
            this.sccList = new SCC[vertexCapacity/2]; // <- NOTE: worst-case is all vertices are pairs (if we allow for bidirectional graphs in the input)
            this.vertexList = new int[vertexCapacity];
        }
    }
    

    /// <summary>
    /// Tarjan's strongly connected components algorithm (from http://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm)
    /// </summary>
    /// <remarks>
    /// This is a class so that it can allocate storage.
    /// </remarks>
    public class TarjansAlgorithm
    {
        const uint OnStackBit = 1u << 31; // High bit indicates that we are on the stack (for vertexIndex)

        // Working storage:
        uint index;
        int[] stack;
        int stackEnd;
        uint[] vertexIndex;
        uint[] vertexLowLink;


        public void FindStronglyConnectedComponents(int vertexCount, uint[] edgeBits, ref TarjanOutput output)
        {
            Debug.Assert(output.sccList.Length >= vertexCount/3); // <- NOTE: we could check for "vertexCount/2" but having less space is an optimisation for when we don't have a bi-directional graph
            Debug.Assert(output.vertexList.Length >= vertexCount);

            // INITIALIZE:
            {
                index = 1; // <- 0 is "undefined" (so we can fast-clear our indicies)
                stackEnd = 0;

                if(stack == null || stack.Length < vertexCount)
                {
                    // PERF: Convert this to use a single buffer, with sections reserved for each purpose, so there are no gaps
                    //       Also interleave lowlink and index. And could use short instead of int. All probably overkill.

                    // NOTE: Lazy over-allocate here, to avoid reallocation
                    stack = new int[vertexCount * 2]; // <- the maximum size for the stack is vertexCount
                    vertexIndex = new uint[vertexCount * 2];
                    vertexLowLink = new uint[vertexCount * 2];
                }
                else // <- don't have to clear if we just got fresh memory from .NET
                {
                    Array.Clear(vertexIndex, 0, vertexCount); // <- only have to clear as much as we'll use here...
                    // NOTE: don't need to inialize vertexLowLink or stack
                }

                output.sccCount = 0;
                output.vertexCount = 0;
            }

            // RUN:
            {
                for(int v = 0; v < vertexCount; v++)
                {
                    if(vertexIndex[v] == 0) // "not visited"
                        StrongConnect(v, vertexCount, edgeBits, ref output);
                }
            }

            Debug.Assert(output.vertexCount == vertexCount);
        }


        private void StrongConnect(int v, int vertexCount, uint[] edgeBits, ref TarjanOutput output)
        {
            Debug.Assert((vertexIndex[v] & OnStackBit) == 0);

            vertexIndex[v] = index | OnStackBit;
            vertexLowLink[v] = index;
            index++;
            stack[stackEnd++] = v; // Stack Push


            // Consider successors of v
            for(int w = 0; w < vertexCount; w++)
            {
                // NOTE: Considered using FFS-bit operation, but that only moves O(|V|) down to O(|E|) on a per-32-bit-block basis (by block will always be O(|V|)), probably not worth it -AR
                // PERF: Validate that this is inlined by the JIT (I think it should be -AR)
                if(!edgeBits.IsEdge(vertexCount, v, w))
                    continue;

                if(vertexIndex[w] == 0) // "not visited"
                {
                    // Successor w has not yet been visited; recurse on it
                    StrongConnect(w, vertexCount, edgeBits, ref output);
                    vertexLowLink[v] = System.Math.Min(vertexLowLink[v], vertexLowLink[w]);
                }
                else if((vertexIndex[w] & OnStackBit) != 0)
                {
                    // Successor w is in the stack and hence in the current strongly-connected-component
                    vertexLowLink[v] = System.Math.Min(vertexLowLink[v], vertexIndex[w] & ~OnStackBit);
                }
            }


            // If v is a root node, pop the stack and generate a strongly-connected-component
            if((vertexIndex[v] & ~OnStackBit) == vertexLowLink[v])
            {
                int componentEnd = stackEnd; // <- rather than popping into a buffer, keep track of the top of the stack so we can return the range

                // Repeatedly pop until we pop off v:
                do
                {
                    stackEnd--; // Stack Pop (stack[stackEnd] is what was popped)
                    Debug.Assert((vertexIndex[v] & OnStackBit) != 0);
                    vertexIndex[stack[stackEnd]] &= ~OnStackBit; // Clear stack bit
                } while(stack[stackEnd] != v);

                int count = componentEnd-stackEnd;
                Debug.Assert(count >= 1);


                // Write output:
                if(count > 1)
                {
                    output.sccList[output.sccCount++] = new SCC(output.vertexCount, count); // Add
                    Array.Copy(stack, stackEnd, output.vertexList, output.vertexCount, count); // AddRange
                    output.vertexCount += count;
                }
                else // Lone node (don't bother tracking these as SCCs, and don't bother with the bulk copy)
                {
                    output.vertexList[output.vertexCount++] = stack[stackEnd]; // Add
                }
            }

        }
    }
}
