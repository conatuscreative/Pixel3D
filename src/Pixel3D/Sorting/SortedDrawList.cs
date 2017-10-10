using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Pixel3D.Animations;

namespace Pixel3D.Sorting
{
    public class SortedDrawList
    {
        // NOTE: Lots of members made public for debugging viewing only


        #region Register items for drawing

        public struct SortingInfo
        {
            public AnimationSet animationSet;
            public Position position;
            public bool facingLeft;
        }

        public struct DrawingInfo : IEquatable<DrawingInfo>
        {
            public IDrawObject drawObject;
            public int tag;

            public bool Equals(DrawingInfo other)
            {
                return ReferenceEquals(this.drawObject, other.drawObject) && (this.tag == other.tag);
            }
        }


        // Registered items for drawing (stored SOA)
        public readonly List<Bounds> bounds = new List<Bounds>();
        public readonly List<SortingInfo> sortingInfo = new List<SortingInfo>();
        public readonly List<DrawingInfo> drawingInfo = new List<DrawingInfo>();

        public int Count { get { return bounds.Count; } }


        public void Clear()
        {
            bounds.Clear();
            sortingInfo.Clear();
            drawingInfo.Clear();
            forceDrawOrder.Clear();
            forceDrawOrderDeferred.Clear();
            inheritDrawOrders.Clear();

#if DEBUG
            expectNotToDraw.Clear();
#endif

            IsSorted = false;
        }

        public void Add(IDrawObject drawObject, int tag, Position position, bool facingLeft, AnimationSet animationSet, Animation currentAnimation)
        {
            Add(drawObject, tag, position, facingLeft, animationSet, currentAnimation.GetBoundsInWorld(position, facingLeft));
        }

        public void Add(IDrawObject drawObject, int tag, Position position, bool facingLeft, AnimationSet animationSet, Bounds worldZeroBounds)
        {
            Debug.Assert(!IsSorted); // <- We don't handle adding to the sorted list

            bounds.Add(worldZeroBounds);
            sortingInfo.Add(new SortingInfo { animationSet = animationSet, position = position, facingLeft = facingLeft });
            drawingInfo.Add(new DrawingInfo { drawObject = drawObject, tag = tag });
        }





        List<FromTo> inheritDrawOrders = new List<FromTo>();
        /// <summary>
        /// Parameters are relative to the insertion point.
        /// Copies ordering constraints from the source to the subject, for constraint pairs where the other member is shared between the source and the subject.
        /// IMPORTANT: A draw item cannot simultaneously be a subject and a source.
        /// </summary>
        public void ForceInheritDrawOrders(int sourceInheritFrom, int subjectCopyTo)
        {
            Debug.Assert(!IsSorted);
            Debug.Assert(sourceInheritFrom != subjectCopyTo);

            int f = bounds.Count + sourceInheritFrom;
            int t = bounds.Count + subjectCopyTo;

#if DEBUG
            foreach(var inheritance in inheritDrawOrders)
            {
                Debug.Assert(inheritance.from != t);
                Debug.Assert(inheritance.to != f);
            }
#endif

            inheritDrawOrders.Add(new FromTo { from = f, to = t });
        }


        struct FromTo { public int from, to; }
        List<FromTo> forceDrawOrder = new List<FromTo>();

        /// <summary>Parameters are relative to the insertion point. Does NOT check if the objects overlap.</summary>
        public void ForceDrawOrder(int drawFirst, int drawSecond)
        {
            Debug.Assert(!IsSorted);
            Debug.Assert(drawFirst != drawSecond);
            forceDrawOrder.Add(new FromTo { from = bounds.Count + drawFirst, to = bounds.Count + drawSecond });
        }


        struct DeferredFromTo { public DrawingInfo from, to; }
        readonly List<DeferredFromTo> forceDrawOrderDeferred = new List<DeferredFromTo>();

        /// <summary>Force two objects to be drawn in a given order. The objects do not have to be registered yet. DOES check if the objects overlap.</summary>
        public void ForceDrawOrderDeferred(IDrawObject fromDrawObject, int fromTag, IDrawObject toDrawObject, int toTag)
        {
            Debug.Assert(!IsSorted);
            forceDrawOrderDeferred.Add(new DeferredFromTo
            {
                from = new DrawingInfo { drawObject = fromDrawObject, tag = fromTag },
                to = new DrawingInfo { drawObject = toDrawObject, tag = toTag }
            });
        }

        private void ProcesseDeferredForcedDrawOrders()
        {
            foreach(var item in forceDrawOrderDeferred)
            {
                // PERF: Avoid this linear lookup
                int fromIndex = drawingInfo.IndexOf(item.from);
                int toIndex = drawingInfo.IndexOf(item.to);

                if(fromIndex != -1 && toIndex != -1)
                    if(bounds[fromIndex].Intersects(bounds[toIndex]))
                        forceDrawOrder.Add(new FromTo { from = fromIndex, to = toIndex });
            }
        }

        #endregion
        

        #region Debug Stuff

#if DEBUG
        private List<DrawingInfo> expectNotToDraw = new List<DrawingInfo>();

        private void CheckAllowedToDraw(IDrawObject drawObject, int tag)
        {
            for(int i = 0; i < expectNotToDraw.Count; i++)
            {
                // Whoops, did some attached actor forget to suppress its drawing registration?
                Debug.Assert(!(ReferenceEquals(expectNotToDraw[i].drawObject, drawObject) && expectNotToDraw[i].tag == tag));
            }
        }
#endif

        [Conditional("DEBUG")]
        public void DebugExpectNotToDraw(IDrawObject drawObject, int tag)
        {
            Debug.Assert(!IsSorted);
#if DEBUG
            expectNotToDraw.Add(new DrawingInfo { drawObject = drawObject, tag = tag });
#endif
        }

        #endregion



        #region Sorting

        // NOTE: Edge bit storage is reused in the sort algorithm
        public uint[] edgeBits = EdgeBits.Create(128);

        public bool IsSorted { get; private set; }


        ForgivingTopologicalSort forgivingTopologicalSort = new ForgivingTopologicalSort();

        // NOTE: This is storage that is internal to ForgivingTopologicalSort
        public int[] sortedOrder;


        public void Sort(Action<int, int> removedEdge)
        {
            if(IsSorted)
                return;
            IsSorted = true;

            ProcesseDeferredForcedDrawOrders();


            int vertexCount = this.Count;

            // INITIALISE
            {
                if(edgeBits.Length < EdgeBits.Size(vertexCount))
                    edgeBits = EdgeBits.Create(vertexCount * 2);
                else
                    Array.Clear(edgeBits, 0, EdgeBits.Size(vertexCount));
            }


            // Step 1: Determine what items overlap and store in the edge bits:
            {
                // This is O(n^2) on drawn items, so we want it to be fast (later we could consider bucketing)
                for(int f = 0; f < vertexCount; f++) for(int t = f+1; t < vertexCount; t++)
                {
                    if(bounds[f].Intersects(bounds[t]))
                        edgeBits.SetEdge(vertexCount, f, t); // Abuse "edgeBits" for temporary storage
                }

                // Don't do ordering comparisons if we're just going to force the order:
                foreach(var order in forceDrawOrder)
                {
                    // Rather than picking the correct bit, just clear both:
                    edgeBits.ClearEdge(vertexCount, order.from, order.to);
                    edgeBits.ClearEdge(vertexCount, order.to, order.from);
                }

                // Don't do ordering comparisons in cases that can be handled by inheritance:
                foreach(var inheritance in inheritDrawOrders)
                {
                    for(int f = 0; f < inheritance.from; f++) // <- split loops due to ordering in edgeBits
                    {
                        if(f != inheritance.to && edgeBits.IsEdge(vertexCount, f, inheritance.from))
                        {
                            // Rather than picking the correct bit, just clear both:
                            edgeBits.ClearEdge(vertexCount, f, inheritance.to);
                            edgeBits.ClearEdge(vertexCount, inheritance.to, f);
                        }
                    }
                    for(int t = inheritance.from + 1; t < vertexCount; t++) // <- split loops due to ordering in edgeBits
                    {
                        if(t != inheritance.to && edgeBits.IsEdge(vertexCount, inheritance.from, t))
                        {
                            // Rather than picking the correct bit, just clear both:
                            edgeBits.ClearEdge(vertexCount, t, inheritance.to);
                            edgeBits.ClearEdge(vertexCount, inheritance.to, t);
                        }
                    }
                }
            }



            // Step 2: Compare all overlapping items to geneate the directed graph:
            {
                for(int f = 0; f < vertexCount; f++) for(int t = f+1; t < vertexCount; t++)
                {
                    if(edgeBits.IsEdge(vertexCount, f, t))
                    {
                        int compare = DrawOrdering.Compare(sortingInfo[f].animationSet, sortingInfo[t].animationSet,
                                sortingInfo[f].position, sortingInfo[t].position, sortingInfo[f].facingLeft, sortingInfo[t].facingLeft, null);

                        // If compare is negative, we basically want to leave the existing bit in place, otherwise clear it and maybe set its reverse
                        if(compare >= 0)
                        {
                            edgeBits.ClearEdge(vertexCount, f, t);
                            if(compare > 0)
                                edgeBits.SetEdge(vertexCount, t, f);
                        }
                    }
                }

                // Apply forced ordering:
                foreach(var order in forceDrawOrder)
                    edgeBits.SetEdge(vertexCount, order.from, order.to);

                // Apply inherited ordering:
                foreach(var inheritance in inheritDrawOrders)
                {
                    for(int i = 0; i < vertexCount; i++)
                    {
                        if(i == inheritance.from || i == inheritance.to)
                            continue;
                        
                        uint edgeF = edgeBits.GetEdgeBit(vertexCount, inheritance.from, i);
                        uint edgeT = edgeBits.GetEdgeBit(vertexCount, i, inheritance.from);
                        if((edgeF | edgeT) != 0) // <- if the source has an opinion, copy it
                        {
                            edgeBits.ChangeEdgeBit(vertexCount, inheritance.to, i, edgeF);
                            edgeBits.ChangeEdgeBit(vertexCount, i, inheritance.to, edgeT);
                        }
                    }
                }              
            }


            // Step 3: Topological Sort!
            {
                sortedOrder = forgivingTopologicalSort.Sort(vertexCount, edgeBits, removedEdge);
            }
        }


        #endregion



        #region Rendering

        public void DrawAll(DrawContext context, IDrawSmoothProvider sp)
        {
            Debug.Assert(IsSorted); // <- Expect our caller to have sorted!

            int vertexCount = this.Count;
            
            for(int i = vertexCount - 1; i >= 0; i--) // The topological sort returns in front-to-back order
            {
                DrawingInfo di = drawingInfo[sortedOrder[i]];

#if DEBUG
                CheckAllowedToDraw(di.drawObject, di.tag);
#endif

                di.drawObject.Draw(context, di.tag, sp);
            }
        }

        #endregion



        #region Enumeration (for editor)

        #region Front to Back (for hit-testing)

        public FrontToBackEnumerable FrontToBack { get { Debug.Assert(IsSorted); return new FrontToBackEnumerable(this); } }

        public struct FrontToBackEnumerable : IEnumerable<DrawingInfo>
        {
            public FrontToBackEnumerable(SortedDrawList sortedDrawList)
            {
                this.sortedDrawList = sortedDrawList;
            }

            public SortedDrawList sortedDrawList;

            public FrontToBackEnumerator GetEnumerator()
            {
                return new FrontToBackEnumerator(sortedDrawList);
            }

            IEnumerator<DrawingInfo> IEnumerable<DrawingInfo>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct FrontToBackEnumerator : IEnumerator<DrawingInfo>
        {
            private SortedDrawList sortedDrawList;
            private int i;
            private int current; // <- index into DrawingInfo table

            public FrontToBackEnumerator(SortedDrawList sortedDrawList)
            {
                i = 0;
                current = 0;
                this.sortedDrawList = sortedDrawList;
            }

            public void Reset()
            {
                i = 0;
                current = 0;
            }

            public void Dispose() { }
            public DrawingInfo Current { get { return sortedDrawList.drawingInfo[current]; } }
            object IEnumerator.Current { get { return this.Current; } }

            public bool MoveNext()
            {
                if(i < sortedDrawList.Count)
                {
                    current = sortedDrawList.sortedOrder[i];
                    i++;
                    return true;
                }
                return false;
            }
        }

        #endregion


        #region Back to Front (for rendering)

        public BackToFrontEnumerable BackToFront { get { Debug.Assert(IsSorted); return new BackToFrontEnumerable(this); } }

        public struct BackToFrontEnumerable : IEnumerable<DrawingInfo>
        {
            public BackToFrontEnumerable(SortedDrawList sortedDrawList)
            {
                this.sortedDrawList = sortedDrawList;
            }

            public SortedDrawList sortedDrawList;

            public BackToFrontEnumerator GetEnumerator()
            {
                return new BackToFrontEnumerator(sortedDrawList);
            }

            IEnumerator<DrawingInfo> IEnumerable<DrawingInfo>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct BackToFrontEnumerator : IEnumerator<DrawingInfo>
        {
            private SortedDrawList sortedDrawList;
            private int i;
            private int current; // <- index into DrawingInfo table

            public BackToFrontEnumerator(SortedDrawList sortedDrawList)
            {
                i = sortedDrawList.Count - 1;
                current = 0;
                this.sortedDrawList = sortedDrawList;
            }

            public void Reset()
            {
                i = sortedDrawList.Count - 1;
                current = 0;
            }

            public void Dispose() { }
            public DrawingInfo Current { get { return sortedDrawList.drawingInfo[current]; } }
            object IEnumerator.Current { get { return this.Current; } }

            public bool MoveNext()
            {
                if(i >= 0)
                {
                    current = sortedDrawList.sortedOrder[i];
                    i--;
                    return true;
                }
                return false;
            }
        }

        #endregion

        #endregion


    }
}
