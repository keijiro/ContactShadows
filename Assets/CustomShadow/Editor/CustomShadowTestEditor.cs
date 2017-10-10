using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(CustomShadowTest))]
public class CustomShadowTestEditor : Editor
{
    SerializedProperty _light;
    SerializedProperty _rejectionDepth;

    void OnEnable()
    {
        _light = serializedObject.FindProperty("_light");
        _rejectionDepth = serializedObject.FindProperty("_rejectionDepth");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_light);
        EditorGUILayout.PropertyField(_rejectionDepth);

        serializedObject.ApplyModifiedProperties();
    }
}
