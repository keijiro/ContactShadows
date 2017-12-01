// Experimental implementation of contact shadows for Unity
// https://github.com/keijiro/ContactShadows

using UnityEngine;
using UnityEditor;

namespace PostEffects
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ContactShadows))]
    public class ContactShadowsEditor : Editor
    {
        SerializedProperty _light;
        SerializedProperty _rejectionDepth;
        SerializedProperty _sampleCount;
        SerializedProperty _temporalFilter;
        SerializedProperty _downsample;

        void OnEnable()
        {
            _light = serializedObject.FindProperty("_light");
            _rejectionDepth = serializedObject.FindProperty("_rejectionDepth");
            _sampleCount = serializedObject.FindProperty("_sampleCount");
            _temporalFilter = serializedObject.FindProperty("_temporalFilter");
            _downsample = serializedObject.FindProperty("_downsample");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_light);
            EditorGUILayout.PropertyField(_rejectionDepth);
            EditorGUILayout.PropertyField(_sampleCount);
            EditorGUILayout.PropertyField(_temporalFilter);
            EditorGUILayout.PropertyField(_downsample);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
