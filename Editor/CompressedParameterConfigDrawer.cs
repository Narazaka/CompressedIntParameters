using UnityEditor;
using UnityEngine;

namespace Narazaka.VRChat.CompressedIntParameters.Editor
{
    [CustomPropertyDrawer(typeof(CompressedParameterConfig))]
    public class CompressedParameterConfigDrawer : PropertyDrawer
    {
        static readonly string[] FloatPrecisionLabels = new[]
        {
            "2段階 (1bit)",
            "4段階 (2bit)",
            "8段階 (3bit)",
            "16段階 (4bit)",
            "32段階 (5bit)",
            "64段階 (6bit)",
            "128段階 (7bit)",
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            var typeProp = property.FindPropertyRelative(nameof(CompressedParameterConfig.type));
            var isFloat = typeProp.enumValueIndex == (int)CompressedParameterType.Float;

            // Row 1: name | type | (Int: maxValue + N段階(Mbit)) or (Float: N段階(Mbit) popup + min + max)
            var line = position;
            line.width = position.width - 60 - (isFloat ? 100 + 60 + 60 + Spacing * 3 : 110 + 60 + Spacing * 2) - Spacing;
            EditorGUI.PropertyField(line, property.FindPropertyRelative(nameof(CompressedParameterConfig.name)), GUIContent.none);

            line.x += line.width + Spacing;
            line.width = 60;
            EditorGUI.PropertyField(line, typeProp, GUIContent.none);

            line.x += line.width + Spacing;
            if (isFloat)
            {
                line.width = 100;
                var bitsProp = property.FindPropertyRelative(nameof(CompressedParameterConfig.bits));
                if (bitsProp.intValue < 1) bitsProp.intValue = 1;
                if (bitsProp.intValue > 7) bitsProp.intValue = 7;
                EditorGUI.BeginChangeCheck();
                var newIndex = EditorGUI.Popup(line, bitsProp.intValue - 1, FloatPrecisionLabels);
                if (EditorGUI.EndChangeCheck()) bitsProp.intValue = newIndex + 1;

                line.x += line.width + Spacing;
                line.width = 60;
                var minProp = property.FindPropertyRelative(nameof(CompressedParameterConfig.floatMinValue));
                EditorGUIUtility.labelWidth = 25;
                EditorGUI.PropertyField(line, minProp, T.Min.GUIContent);
                minProp.floatValue = Mathf.Clamp(minProp.floatValue, -1f, 1f);

                line.x += line.width + Spacing;
                line.width = 60;
                var maxProp = property.FindPropertyRelative(nameof(CompressedParameterConfig.floatMaxValue));
                EditorGUIUtility.labelWidth = 25;
                EditorGUI.PropertyField(line, maxProp, T.Max.GUIContent);
                maxProp.floatValue = Mathf.Clamp(maxProp.floatValue, -1f, 1f);
            }
            else
            {
                line.width = 110;
                EditorGUIUtility.labelWidth = 60;
                var maxValue = property.FindPropertyRelative(nameof(CompressedParameterConfig.maxValue));
                EditorGUI.PropertyField(line, maxValue, T.MaxValue.GUIContent);
                if (maxValue.intValue < 1) maxValue.intValue = 1;
                if (maxValue.intValue > 127) maxValue.intValue = 127;

                line.x += line.width + Spacing;
                line.width = 60;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(line, 0, new[] { $"{CompressedParameterConfig.Bits(maxValue.intValue)}bitInt" });
                EditorGUI.EndDisabledGroup();
            }

            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Row 2: remapTo | internalParameter
            line = position;
            line.width -= 95 + Spacing;
            EditorGUIUtility.labelWidth = 95;
            EditorGUI.PropertyField(line, property.FindPropertyRelative(nameof(CompressedParameterConfig.remapTo)), T.ChangeNameTo.GUIContent);
            line.x += line.width + Spacing;
            line.width = 95;
            EditorGUIUtility.labelWidth = 0;
            ToggleLeft(line, property.FindPropertyRelative(nameof(CompressedParameterConfig.internalParameter)), T.AutoRename.GUIContent);

            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Row 3: defaultValue | saved | synced
            line = position;
            line.width = 100;
            EditorGUIUtility.labelWidth = 45;
            var hasExplicitDefaultValue = property.FindPropertyRelative(nameof(CompressedParameterConfig.hasExplicitDefaultValue));
            var defaultValue = property.FindPropertyRelative(nameof(CompressedParameterConfig.defaultValue));
            EditorGUI.BeginProperty(line, T.Default.GUIContent, defaultValue);
            EditorGUI.BeginChangeCheck();
            var newDefaultValue = EditorGUI.TextField(line, T.Default.GUIContent,
                hasExplicitDefaultValue.boolValue || Mathf.Abs(defaultValue.floatValue) > CompressedParameterConfig.VALUE_EPSILON
                    ? defaultValue.floatValue.ToString() : "");
            if (EditorGUI.EndChangeCheck())
            {
                if (string.IsNullOrWhiteSpace(newDefaultValue))
                {
                    defaultValue.floatValue = 0;
                    hasExplicitDefaultValue.boolValue = false;
                }
                else if (float.TryParse(newDefaultValue, out var value))
                {
                    defaultValue.floatValue = value;
                    hasExplicitDefaultValue.boolValue = true;
                }
            }
            EditorGUI.EndProperty();

            line.x += line.width + Spacing;
            line.width = 60;
            ToggleLeft(line, property.FindPropertyRelative(nameof(CompressedParameterConfig.saved)), T.Saved.GUIContent);
            line.x += line.width + Spacing;
            line.width = 60;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.ToggleLeft(line, T.Synced.GUIContent, true);
            EditorGUI.EndDisabledGroup();
            EditorGUIUtility.labelWidth = 0;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
        }

        void ToggleLeft(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var toggle = property.boolValue;
            EditorGUI.BeginChangeCheck();
            toggle = EditorGUI.ToggleLeft(position, label, toggle);
            if (EditorGUI.EndChangeCheck())
            {
                property.boolValue = toggle;
            }
            EditorGUI.EndProperty();
        }

        const int Spacing = 3;

        static class T
        {
            public static istring MaxValue = new istring("Max Value", "最大値");
            public static istring ChangeNameTo = new istring("Change name to", "名前を変更");
            public static istring AutoRename = new istring("Auto Rename", "自動リネーム");
            public static istring Default = new istring("Default", "初期値");
            public static istring Saved = new istring("Saved", "保存する");
            public static istring Synced = new istring("Synced", "同期する");
            public static istring OverrideAnimatorDefaults = new istring("Override Animator Defaults", "アニメーターでの初期値を設定");
            public static istring Bits = new istring("Bits", "ビット");
            public static istring Min = new istring("Min", "最小");
            public static istring Max = new istring("Max", "最大");
        }
    }
}
