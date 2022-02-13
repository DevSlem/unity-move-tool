#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using KgmSlem.Extensions;

namespace KgmSlem.UnityEditor
{
    /// <summary>
    /// Display the position mode of MoveToolAttribute on editor inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(MoveToolAttribute), true)]
    public class MoveToolDrawer : PropertyDrawer
    {
        private readonly float modeInfoHeight = 18f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as MoveToolAttribute;

            if (!attr.LabelMode.HasFlag(MoveToolLabel.InspectorView))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            string[] splitPath = property.propertyPath.Split('.');

            //EditorGUI.BeginProperty(position, label, property);

            // If property is just single field.
            if (splitPath.LastOrDefault(p => p == "Array") == null)
            {
                label.text = string.IsNullOrEmpty(attr.Label) ? property.name.InspectorLabel() : attr.Label;
                EditorGUI.PropertyField(position, property, label, true); // Default form

                var modePos = position;
                modePos.y += modePos.height;
                modePos.height = this.modeInfoHeight;
                DisplayModeInfo(modePos);
            }
            // If property is the element of a collection.
            else
            {
                ICollection field = fieldInfo.GetValue(property.serializedObject.targetObject) as ICollection;
                int idx = GetCollectionPropertyIndex(splitPath[splitPath.Length - 1]);
                bool isLast = idx + 1 == (field?.Count ?? -1);

                label.text = (string.IsNullOrEmpty(attr.Label) ? fieldInfo.Name.InspectorLabel() : attr.Label) + $" [{idx}]";

                EditorGUI.PropertyField(position, property, label, true); // Default form

                // If this property is the last of the collection
                if (isLast)
                {
                    var modePos = position;
                    modePos.height = this.modeInfoHeight;
                    modePos.y += modePos.height + EditorGUI.GetPropertyHeight(property);
                    DisplayModeInfo(modePos);
                }
            }

            //EditorGUI.EndProperty();
        }

        private int GetCollectionPropertyIndex(string str)
        {
            if (str != null)
            {
                (int start, int end) = (str.LastIndexOf('['), str.LastIndexOf(']'));
                if (start >= 0 && end > start + 1 && int.TryParse(str.Substring(start + 1, end - start - 1), out int result))
                    return result;
            }
            return -1;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }

        // Display current position mode.
        private void DisplayModeInfo(Rect position)
        {
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            string modeLabel = (attribute as MoveToolAttribute).PositionMode.ToString();
            EditorGUI.Foldout(position, false, $"Position Mode  -  {modeLabel}"); // Display information.

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUILayout.Space(this.modeInfoHeight);
        }
    }
}
#endif