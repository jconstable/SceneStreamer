using System;
using UnityEditor;

[CustomEditor(typeof(CollisionEvent))]
public class CollisionEventEditor : Editor
{
    SerializedProperty LayerProperty;

    void OnEnable()
    {
        LayerProperty = serializedObject.FindProperty("Layer");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        base.OnInspectorGUI();
        var originalLayer = LayerProperty.intValue;
        LayerProperty.intValue = EditorTools.LayerMaskField("Layers", LayerProperty.intValue);
        if (originalLayer != LayerProperty.intValue) serializedObject.ApplyModifiedProperties();
    }
}
