using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using nadena.dev.ndmf.ui;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Narazaka.VRChat.CompressedIntParameters.Editor
{
    [CustomEditor(typeof(CompressedIntParameters))]
    public class CompressedIntParametersEditor : UnityEditor.Editor
    {
        SerializedProperty parameters;

        void OnEnable()
        {
            parameters = serializedObject.FindProperty(nameof(CompressedIntParameters.parameters));
            parameters.isExpanded = true;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            var prevSize = parameters.arraySize;
            EditorGUILayout.PropertyField(parameters, true);
            if (parameters.arraySize > prevSize)
            {
                for (int i = prevSize; i < parameters.arraySize; i++)
                {
                    var max = parameters.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(CompressedParameterConfig.floatMaxValue));
                    if (max.floatValue == 0f) max.floatValue = 1f;
                }
            }
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Separator();
            LanguageSwitcher.DrawImmediate();
        }
    }
}
