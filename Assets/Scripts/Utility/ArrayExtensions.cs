using System.Collections.Generic;
using UnityEngine;

public static class ArrayExtensions
{
    public static T PickRandom<T>(this IList<T> array)
    {
        if (array == null || array.Count == 0) { return default(T); }

        return array[UnityEngine.Random.Range(0, array.Count)];
    }

    public static void SortRandom<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static Vector3 GetAveragePosition(this IEnumerable<Vector3> list)
    {
        Vector3 average = Vector3.zero;
        int count = 0;
        foreach(var position in list)
        {
            count++;
            average += position;
        }
        if (count == 0) { return Vector3.zero; }
        return average / count;
    }
}