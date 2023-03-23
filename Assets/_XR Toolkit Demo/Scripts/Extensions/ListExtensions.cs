using System;
using System.Collections.Generic;


// ================================================================================================
// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
// ================================================================================================
/**
 *  Extension methods for the C# List class (with the Unity Random class);
 */
// ================================================================================================
// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
// ================================================================================================
public static class ListExtenstions
{
    // Remove the given element from the list and return it -----------------------------
    public static T RemoveGrab<T>(this List<T> list, T element)
    {
        if (list.Remove(element))
            return element;

        return default;
    }

    // Remove the element at the given index and return it ------------------------------
    public static T RemoveGrabAt<T>(this List<T> list, int index)
    {
        if (index < 0 || index >= list.Count)
            return default;

        T t = list[index];
        list.RemoveAt(index);
        return t;
    }

    // Return a random element and its index from the list ------------------------------
    public static Tuple<int, T> Random<T>(this List<T> list)
    {
        int i = UnityEngine.Random.Range(0, list.Count);
        return new Tuple<int, T>(i, list[i]);
    }

    // Return a random element from the list --------------------------------------------
    public static T RandomElement<T>(this List<T> list)
    {
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    // Return a random element from the list while removing it from the list ------------
    public static T RandomRemove<T>(this List<T> list)
    {
        return list.RemoveGrabAt(UnityEngine.Random.Range(0, list.Count));
    }
}
// ================================================================================================
// ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
// ================================================================================================
