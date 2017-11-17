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
        SerializedProperty _convergenceSpeed;

        void OnEnable()
        {
            _light = serializedObject.FindProperty("_light");
            _rejectionDepth = serializedObject.FindProperty("_rejectionDepth");
            _sampleCount = serializedObject.FindProperty("_sampleCount");
            _convergenceSpeed = serializedObject.FindProperty("_convergenceSpeed");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_light);
            EditorGUILayout.PropertyField(_rejectionDepth);
            EditorGUILayout.PropertyField(_sampleCount);
            EditorGUILayout.PropertyField(_convergenceSpeed);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
