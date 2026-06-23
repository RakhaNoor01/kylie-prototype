using System.Collections.Generic;
using UnityEngine;

public static class TempData
{
    private static Dictionary<string, object> yummers = new Dictionary<string, object>();

    public static bool HasKey(string key)
    {
        return yummers.ContainsKey(key);
    }

    public static object GetValue(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (yummers.ContainsKey(key))
        {
            return yummers[key];
        }
        else
        {
            return null;
        }
    }

    public static void SetValue(string key, object value)
    {
        yummers[key] = value;
    }

    public static void GetTempData()
    {
        foreach (var pair in yummers)
        {
            Debug.Log($"{pair.Key}: {pair.Value}");
        }
    }
}
