using System;
using System.Diagnostics;

namespace Pixel3D.Engine.Collections
{
    // NOTE: This does not have a custom network serializer (should not serialize the internal array, just valid data)
    //       BUT: It probably shouldn't appear in the serialization anyway...


    /// <summary>Minimum-first priority queue, implemented with a heap</summary>
    public class PriorityQueue<T>
    {
        public PriorityQueue() : this(16) { }

        public PriorityQueue(int capacity)
        {
            nodes = new Node[capacity];
        }



        #region Node Storage

        // TODO: Seperate arrays for priorities and values?
        struct Node
        {
            public int priority;
            public T value;
        }

        Node[] nodes;
        int count;

        public int Count { get { return count; } }
        public int Capacity { get { return nodes.Length; } }


        // This is a (min-first) priority queue.
        // Represented as a heap.
        // Represented as a binary-tree, where each child has higher priority number than its parent, filled from the left.
        // Stored in an array with the following indexing:
        //
        // childIndex[0] = 2*parentIndex + 1;
        // childIndex[1] = 2*parentIndex + 2;
        // parentIndex = (childIndex-1)/2; // <- relies on integer division rounding down

        #endregion


        public void Enqueue(T value, int priority)
        {
            if(count == nodes.Length)
                Array.Resize(ref nodes, nodes.Length * 2);

            // Insert
            int childIndex = count++;
            nodes[childIndex] = new Node { priority = priority, value = value };

            while(childIndex > 0) // While we are not the root node
            {
                int parentIndex = (childIndex - 1) / 2;
                
                // Keep swapping upwards until we reach the top
                if(nodes[childIndex].priority >= nodes[parentIndex].priority)
                    break;

                Node temp = nodes[childIndex];
                nodes[childIndex] = nodes[parentIndex];
                nodes[parentIndex] = temp;

                childIndex = parentIndex;
            }
        }


        public T Dequeue()
        {
            Debug.Assert(count > 0);

            // Take out the first value, and replace its node with the last
            T output = nodes[0].value;
            nodes[0] = nodes[--count];

            // Sort the value downwards to restore the heap property:
            int parentIndex = 0;
            while(true)
            {
                int childIndex = parentIndex*2 + 1;
                if(childIndex >= count)
                    break; // No children

                // Selct the smaller of the left/right children:
                if(childIndex+1 < count && nodes[childIndex+1].priority < nodes[childIndex].priority)
                    childIndex++;

                // Keep swapping downwards until parent is smaller than both children
                if(nodes[parentIndex].priority <= nodes[childIndex].priority)
                    break;

                Node temp = nodes[childIndex];
                nodes[childIndex] = nodes[parentIndex];
                nodes[parentIndex] = temp;

                parentIndex = childIndex;
            }

            return output;
        }

    }
}
