using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EditorTools
{
    static List<string> layers;
    static string[] layerNames;

    public static LayerMask LayerMaskField(string label, LayerMask selected)
    {
        if (layers == null)
        {
            layers = new List<string>();
            layerNames = new string[4];
        }
        else
        {
            layers.Clear();
        }

        var emptyLayers = 0;
        for (var i = 0; i < 32; i++)
        {
            var layerName = LayerMask.LayerToName(i);

            if (layerName != "")
            {
                for (; emptyLayers > 0; emptyLayers--) layers.Add("Layer " + (i - emptyLayers));
                layers.Add(layerName);
            }
            else
            {
                emptyLayers++;
            }
        }

        if (layerNames.Length != layers.Count) layerNames = new string[layers.Count];
        for (var i = 0; i < layerNames.Length; i++) layerNames[i] = layers[i];

        selected.value = EditorGUILayout.MaskField(label, selected.value, layerNames);

        return selected;
    }
}
