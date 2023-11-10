using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        if (JsonUtility.FromJson<Wrapper<T>>(json) == null) return null;
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        Debug.Log("JsonHelper, FromJson : json = " + json);
        Debug.Log("JsonHelper, FromJson : wrapper.Length = " + wrapper.Items.Length);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}