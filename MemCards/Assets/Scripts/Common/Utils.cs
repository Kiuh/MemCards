using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Utils
{
    public static T GetRandom<T>(this List<T> list)
    {
        return list[Random.Range(0, list.Count())];
    }

    public static T TakeRandom<T>(this List<T> list)
    {
        T item = list.GetRandom();
        _ = list.Remove(item);
        return item;
    }
}
