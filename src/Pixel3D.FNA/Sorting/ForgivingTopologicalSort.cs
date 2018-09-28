// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;

namespace Pixel3D.Sorting
{
    /// <summary>
    /// Uses Tarjan's strongly connected components algorithm (from http://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm)
    /// to do a topological sort, then again to recursivly break down any strongly-connected-components (SCCs) by attempting to remove single backwards edges
    /// to minimise the number of input ordering constraints (edges) that are violated when sorting each SCC.
    /// </summary>
    /// <remarks>
    /// This is a class so that it can allocate storage.
    /// </remarks>
    public class ForgivingTopologicalSort
    {
        TarjansAlgorithm ta = new TarjansAlgorithm();


        // MEMORY:

        // Output of Tarjan's on full input, which we then massage
        int[] primaryVertices;
        SCC[] primarySCCStack; // top is a local

        // Used for iterating Tarjan's on SCCs within primary output
        uint[] secondaryEdgeBits;
        int[] secondaryVertices;
        SCC[] secondarySCCs; // count is a local
        int[] tempOutput;

        // Keep the "best" result from iterations
        int[] bestVertices;
        SCC[] bestSCCs; // count is a local


        /// <summary>NOTE: Returns a reference to an internal array which is cleared on subsequent sorts. May be longer than the passed in vertexCount - excess data is garbage.</summary>
        public int[] Sort(int vertexCount, uint[] edgeBits, Action<int, int> removedEdge)
        {
            //
            // INITIALIZE:
            //
            {
                if(primaryVertices == null || primaryVertices.Length < vertexCount)
                {
                    // NOTE: Lazy over-allocate here, to avoid reallocation
                    primaryVertices = new int[vertexCount * 2];
                    primarySCCStack = new SCC[(vertexCount * 2) / 3]; // worst-case capacity required is that every vertex triplet is an SCC

                    // Secondary stage (NOTE: worst-case is that the whole of primary is a SCC, so this must be the same size)
                    secondaryEdgeBits = EdgeBits.Create(vertexCount * 2); // NOTE: This allocates 4x as much memory as was passed in for edges
                    secondaryVertices = new int[vertexCount * 2];
                    secondarySCCs = new SCC[(vertexCount * 2) / 3];
                    bestVertices = new int[vertexCount * 2];
                    bestSCCs = new SCC[(vertexCount * 2) / 3];
                    tempOutput = new int[vertexCount * 2];
                }
                else
                {
                    // NOTE: Nothing to clear (all items have counts and get re-initialized up to their count upon usage)
                }
            }


            //
            // RUN:
            //
            {
                // Primary:
                // --------
                TarjanOutput primaryOutput = new TarjanOutput();
                primaryOutput.sccList = primarySCCStack;
                primaryOutput.vertexList = primaryVertices;

                ta.FindStronglyConnectedComponents(vertexCount, edgeBits, ref primaryOutput);


                // Secondary: (this is a greedy search)
                // ----------
                // Try to resolve any SCCs remaining in the output
                // Note that changes to a SCC do not affect the ordering of the surrounding DAG
                while(primaryOutput.sccCount > 0)
                {
                    SCC workingSCC = primaryOutput.sccList[--primaryOutput.sccCount]; // Pop

                    // The secondary stage uses indirect indices (ie: vertex number in primary = scc[vertex number in secondary])
                    // This allows the secondary stage to use implicit vertex numbers (0 to N-1, rather than having to access workingSCC)
                    // This requires rebuilding edges to match this indexing scheme (secondaryEdge[v, w] = primaryEdge[scc[v], scc[w]])
                    // But this is desireable, because the access pattern into the primary edges would be cache-unfriendly
                    // Also secondary is generally much smaller than primary (because it works on individual SCCs, which are usually relatively small)

                    // Rebuild edge bits:
                    int v, w;  // v is "from", w is "to"
                    for(v = 0; v < workingSCC.count; v++) for(w = 0; w < workingSCC.count; w++)
                    {
                        secondaryEdgeBits.ChangeEdgeBit(workingSCC.count, v, w,
                                edgeBits.GetEdgeBit(vertexCount, primaryVertices[workingSCC.index + v], primaryVertices[workingSCC.index + w]));
                    }


                    int bestSCCCount = 0;
                    int bestSCCMaximumSize = workingSCC.count;      // The size of the largest SCCs     <- used to select "best"  (reduces size of O(N^2) problem -- unverified -AR)
                    int bestSCCCumulativeSize = workingSCC.count;   // Number of vertices in all SCCs   <- used as a tie-break    (reduces size of O(N) problem   -- unverified -AR)

                    // These are for logging
                    int bestRemovedV = 0;
                    int bestRemovedW = 0;

                    // Try to find the best constraint (ie: edge) to remove:
                    for(v = 0; v < workingSCC.count; v++) for(w = 0; w < workingSCC.count; w++)
                    {
                        if(secondaryEdgeBits.IsEdge(workingSCC.count, v, w)) // PERF: Consider only looking at "backwards" edges (requires primary input is spatially sorted, then compare primary indices)
                        {
                            TarjanOutput secondaryOutput = new TarjanOutput();
                            secondaryOutput.sccList = secondarySCCs;
                            secondaryOutput.vertexList = secondaryVertices;

                            secondaryEdgeBits.ClearEdge(workingSCC.count, v, w); // Temporarly remove an edge (see if it improves the result)

                            ta.FindStronglyConnectedComponents(workingSCC.count, secondaryEdgeBits, ref secondaryOutput);

                            int secondarySCCMaximumSize = 0;
                            int secondarySCCCumulativeSize = 0;
                            for(int i = 0; i < secondaryOutput.sccCount; i++)
			                {
                                int count = secondaryOutput.sccList[i].count;
                                if(count > secondarySCCMaximumSize)
                                    secondarySCCMaximumSize = count;
                                secondarySCCCumulativeSize += count;
			                }

                            // NOTE: If we change to only looking at "backwards" edges, should probably add tie-break to select rearmost edge to remove
                            //       (May require pre-sorting the SCC, to handle the early-out case properly)
                            if(secondaryOutput.sccCount == 0
                                    || secondarySCCMaximumSize < bestSCCMaximumSize
                                    || (secondarySCCMaximumSize == bestSCCMaximumSize && secondarySCCCumulativeSize < bestSCCCumulativeSize))
                            {
                                bestRemovedV = v;
                                bestRemovedW = w;

                                bestSCCCount = secondaryOutput.sccCount;
                                bestSCCMaximumSize = secondarySCCMaximumSize;
                                bestSCCCumulativeSize = secondarySCCCumulativeSize;
                                Swap(ref bestSCCs, ref secondarySCCs);
                                Swap(ref bestVertices, ref secondaryVertices);
                            }

                            if(secondaryOutput.sccCount == 0)
                                goto earlyOut; // Found optimal solution

                            secondaryEdgeBits.SetEdge(workingSCC.count, v, w); // Restore Edge
                        }
                    }

                    if(bestSCCMaximumSize == workingSCC.count) // Failed to find any improvement for this SCC - give up!
                    {
                        // Just leave the primary output in its original order
                        // NOTE: If we change to sorted input (for "backwards" edges), consider re-sorting this SCC in output
                        continue;
                    }
                earlyOut: // skips the above

                    // Notify any debug output that an edge was removed. NOTE: Before the SCC in primary gets re-ordered!
                    if(removedEdge != null)
                        removedEdge(primaryVertices[workingSCC.index + bestRemovedV], primaryVertices[workingSCC.index + bestRemovedW]);

                    // Apply the new ordering
                    for(int i = 0; i < workingSCC.count; i++)
                        tempOutput[i] = primaryVertices[workingSCC.index + bestVertices[i]];
                    Array.Copy(tempOutput, 0, primaryVertices, workingSCC.index, workingSCC.count);

                    // Transfer new SCCs to primary:
                    for(int i = 0; i < bestSCCCount; i++)
                        primaryOutput.sccList[primaryOutput.sccCount++] = new SCC(workingSCC.index + bestSCCs[i].index, bestSCCs[i].count);
                }


                // At this point, primarySCC stack is empty -- we're done
                return primaryVertices;
            }
        }


        private static void Swap<T>(ref T a, ref T b)
        {
            T temp = b;
            b = a;
            a = temp;
        }


    }

}
