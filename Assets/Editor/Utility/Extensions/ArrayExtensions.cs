using System.Collections.Generic;
using UnityEngine;

namespace ModelColourEditor
{
    public static class ArrayExtensions
    {
        public static T PickRandom<T>(this IList<T> array)
        {
            if (array == null || array.Count == 0) { return default(T); }

            return array[UnityEngine.Random.Range(0, array.Count)];
        }
    }
}