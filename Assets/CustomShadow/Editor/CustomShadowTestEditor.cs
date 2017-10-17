using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(CustomShadowTest))]
public class CustomShadowTestEditor : Editor
{
    SerializedProperty _light;
    SerializedProperty _rejectionDepth;
    SerializedProperty _sampleCount;

    void OnEnable()
    {
        _light = serializedObject.FindProperty("_light");
        _rejectionDepth = serializedObject.FindProperty("_rejectionDepth");
        _sampleCount = serializedObject.FindProperty("_sampleCount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_light);
        EditorGUILayout.PropertyField(_rejectionDepth);
        EditorGUILayout.PropertyField(_sampleCount);

        serializedObject.ApplyModifiedProperties();
    }
}
