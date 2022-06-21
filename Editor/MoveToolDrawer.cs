//#define MOVETOOLDRAWER_DEPRECATED
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using DevSlem.Extensions;

namespace DevSlem.UnityEditor
{
#if UNITY_EDITOR
    /// <summary>
    /// Display the position mode of MoveToolAttribute on editor inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(MoveToolAttribute), true)]
    public sealed class MoveToolDrawer : PropertyDrawer
    {
        // For setting MoveToolEditors
        private static readonly MoveToolEditorManager manager = new MoveToolEditorManager();
        private bool isArray;

#if MOVETOOLDRAWER_DEPRECATED
        private static GameObject targetGameObject;
        private static List<MoveToolEditor> moveToolEditors = new List<MoveToolEditor>();
        private static bool isAdded;

        public MoveToolDrawer()
        {
            if (!isAdded)
            {
                SceneView.duringSceneGui += SetMoveTool;
                isAdded = true;
            }
        }
#endif

        private class MoveToolEditorManager
        {
            private Dictionary<MonoBehaviour, (MoveToolEditor editor, bool isNeeded)> container;
            private GameObject targetGameObject;

            public MoveToolEditorManager()
            {
                container = new Dictionary<MonoBehaviour, (MoveToolEditor, bool)>();
                SceneView.duringSceneGui += SetMoveTool;
            }

            private void SetMoveTool(SceneView obj)
            {
                // If there's no target game-object, destory every old editors and terminate.
                if (Selection.activeGameObject == null)
                {
                    foreach (var moveTool in container.Values)
                        Object.DestroyImmediate(moveTool.editor);

                    container.Clear();
                    targetGameObject = null;
                    return;
                }

                // for prefab game-object
                if (Selection.activeGameObject.scene.name == null)
                {
                    foreach (var moveTool in container.Values)
                        Object.DestroyImmediate(moveTool.editor);

                    container.Clear();
                    return;
                }

                var monoBehaviours = Selection.activeGameObject.GetComponents<MonoBehaviour>();
                // If active target game-object is changed.
                if (Selection.activeGameObject != targetGameObject)
                {
                    // Destory every old editors.
                    foreach (var moveTool in container.Values)
                        Object.DestroyImmediate(moveTool.editor);
                    container.Clear();

                    // Create editors of new object.
                    targetGameObject = Selection.activeGameObject;
                    for (int i = 0; i < monoBehaviours.Length; i++)
                        container[monoBehaviours[i]] = (Editor.CreateEditor(monoBehaviours[i], typeof(MoveToolEditor)) as MoveToolEditor, true);
                }             

                // If a new component is added to the target game-object.
                if (monoBehaviours.Length > container.Count)
                {
                    var targets = container.Values.Select(m => m.editor.target);
                    for (int i = 0; i < monoBehaviours.Length; i++)
                    {
                        if (!targets.Contains(monoBehaviours[i]))
                        {
                            container[monoBehaviours[i]] = (Editor.CreateEditor(monoBehaviours[i], typeof(MoveToolEditor)) as MoveToolEditor, true);
                        }
                    }
                }
                // If a component is removed or switched from the target game-object.
                else
                {
                    var keys = container.Keys.ToArray();
                    foreach (var key in keys)
                    {
                        var editor = container[key].editor;
                        if (editor.target == null)
                        {
                            Object.DestroyImmediate(editor);
                            container.Remove(key);
                        }
                    }
                }

                // Set move-tool.
                try
                {
                    foreach (var pair in container)
                    {
                        if (pair.Value.isNeeded && !pair.Value.editor.SetMoveTool())
                        {
                            container[pair.Key] = (pair.Value.editor, false);
                            Object.DestroyImmediate(pair.Value.editor);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Check to change move-tool editor
            //CheckToChangeMoveToolEditor(property);

            var attr = attribute as MoveToolAttribute;

            // if LabelMode doesn't contain inspector-view, just display default property field in the inpsector.
            if (!attr.LabelMode.HasFlag(MoveToolLabel.InspectorView))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }


            string[] splitPath = property.propertyPath.Split('.');

            // If property is just single field.
            if (splitPath.LastOrDefault(p => p == "Array") == null)
            {
                label.text = string.IsNullOrEmpty(attr.Label) ? property.name.InspectorLabel() : attr.Label;
                EditorGUI.PropertyField(position, property, label, true); // Default form

                var modePos = position;
                modePos.y += EditorGUI.GetPropertyHeight(property, true);
                modePos.height = EditorGUIUtility.singleLineHeight;
                DisplayModeInfo(modePos);
            }
            // If property is the element of a collection.
            else
            {
                this.isArray = true;
                ICollection field = fieldInfo.GetValue(property.serializedObject.targetObject) as ICollection;
                int idx = GetCollectionPropertyIndex(splitPath[splitPath.Length - 1]);
                bool isLast = idx + 1 == (field?.Count ?? -1);

                label.text = (string.IsNullOrEmpty(attr.Label) ? fieldInfo.Name.InspectorLabel() : attr.Label) + $" [{idx}]";

                EditorGUI.PropertyField(position, property, label, true); // Default form

                // If this property is the last of the collection
                if (isLast)
                {
                    var modePos = position;
                    modePos.height = EditorGUIUtility.singleLineHeight;
                    modePos.y += modePos.height + EditorGUI.GetPropertyHeight(property) - 2f * EditorGUIUtility.standardVerticalSpacing;
                    DisplayModeInfo(modePos);
                }
            }
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
            return EditorGUI.GetPropertyHeight(property, true) + (this.isArray ? 0f : EditorGUIUtility.singleLineHeight);
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

            //Debug.Log(EditorGUIUtility.singleLineHeight);
            //EditorGUI.Space(this.modeInfoHeight);
        }

#if MOVETOOLDRAWER_DEPRECATED
        [System.Obsolete]
        private void CheckToChangeMoveToolEditor(SerializedProperty property)
        {
            if (moveToolEditors.Count > 0)
                return;

            var target = property.serializedObject.targetObject;
            var targetGameObject = fieldInfo.ReflectedType.GetProperty("gameObject").GetValue(target) as GameObject;
            var monoBehaviours = targetGameObject.GetComponents<MonoBehaviour>();
            for (int i = 0; i < monoBehaviours.Length; i++)
            {
                moveToolEditors.Add(Editor.CreateEditor(monoBehaviours[i], typeof(MoveToolEditor)) as MoveToolEditor);
                MoveToolDrawer.targetGameObject = targetGameObject;
            }
        }

        [System.Obsolete]
        private static void SetMoveTool(SceneView obj)
        {
            // If there's no target game-object, destory every old editors and terminate.
            if (Selection.activeGameObject == null)
            {
                for (int i = 0; i < moveToolEditors.Count; i++)
                    Object.DestroyImmediate(moveToolEditors[i]);

                moveToolEditors.Clear();
                targetGameObject = null;
                return;
            }

            var monoBehaviours = Selection.activeGameObject.GetComponents<MonoBehaviour>();
            // If active target game-object is changed.
            if (Selection.activeGameObject != targetGameObject)
            {
                // Destory every old editors.
                for (int i = 0; i < moveToolEditors.Count; i++)
                    Object.DestroyImmediate(moveToolEditors[i]);
                moveToolEditors.Clear();

                // Create editors of new object.
                targetGameObject = targetGameObject = Selection.activeGameObject;
                for (int i = 0; i < monoBehaviours.Length; i++)
                {
                    //Debug.Log($"{monoBehaviours[i].GetType().Name} : {monoBehaviours[i].GetInstanceID()}");
                    moveToolEditors.Add(Editor.CreateEditor(monoBehaviours[i], typeof(MoveToolEditor)) as MoveToolEditor);

                }
            }

            // If a new component is added to the target game-object.
            if (monoBehaviours.Length > moveToolEditors.Count)
            {
                var targets = moveToolEditors.Select(e => e.target);
                for (int i = 0; i < monoBehaviours.Length; i++)
                {
                    if (!targets.Contains(monoBehaviours[i]))
                    {
                        moveToolEditors.Add(Editor.CreateEditor(monoBehaviours[i], typeof(MoveToolEditor)) as MoveToolEditor);
                        //Debug.Log($"{monoBehaviours[i].GetType().Name} : {monoBehaviours[i].GetInstanceID()}");
                    }
                }
            }
            // If a component is removed from the target game-object.
            else
            {
                for (int i = 0; i < moveToolEditors.Count; i++)
                {
                    if (moveToolEditors[i].target == null)
                    {
                        Object.DestroyImmediate(moveToolEditors[i]);
                        moveToolEditors.RemoveAt(i--);
                    }
                }
            }

            // Set move-tool.
            try
            {
                for (int i = 0; i < moveToolEditors.Count; i++)
                    moveToolEditors[i].SetMoveTool();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

        }
#endif

    }
#endif
}
