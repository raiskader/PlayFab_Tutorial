using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ColorExtensions
{
    public static Color ParseColor(string aCol)
    {
        var value = aCol.Split('(')[1];
        value = value.Split(')')[0];
        var strings = value.Split(',');
        Color output = Color.black;
        for (var i = 0; i < strings.Length; i++)
        {
            output[i] = float.Parse(strings[i]);
        }
        return output;
    }
}
