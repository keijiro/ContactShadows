using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(CustomShadowTest))]
public class CustomShadowTestEditor : Editor
{
    SerializedProperty _light;
    SerializedProperty _rejectionDepth;
    SerializedProperty _sampleCount;
    SerializedProperty _sampleWeight;

    void OnEnable()
    {
        _light = serializedObject.FindProperty("_light");
        _rejectionDepth = serializedObject.FindProperty("_rejectionDepth");
        _sampleCount = serializedObject.FindProperty("_sampleCount");
        _sampleWeight = serializedObject.FindProperty("_sampleWeight");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_light);
        EditorGUILayout.PropertyField(_rejectionDepth);
        EditorGUILayout.PropertyField(_sampleCount);
        EditorGUILayout.PropertyField(_sampleWeight);

        serializedObject.ApplyModifiedProperties();
    }
}
