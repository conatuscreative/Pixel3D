// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;

namespace Pixel3D.FrameworkExtensions
{
    public static class ListExtensions
    {
        public static void Shuffle<T>(this List<T> list, Random random)
        {
            for(int i = 0; i < list.Count; i++)
            {
                int j = random.Next(i, list.Count);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }


        public static void RemoveAtUnordered<T>(this List<T> list, int index)
        {
            int last = list.Count-1;
            list[index] = list[last];
            list.RemoveAt(last);
        }

        public static void RemoveUnorderedWithReferenceEquality<T>(this List<T> list, T item) where T : class
        {
            int count = list.Count;
            for(int i = 0; i < count; i++)
            {
                if(ReferenceEquals(list[i], item))
                {
                    RemoveAtUnordered(list, i);
                    return;
                }
            }
        }

    }
}
