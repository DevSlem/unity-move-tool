#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// It's an editor for MonoBehaviour. It sets position handles for the fields that define MoveToolAttribute.
/// </summary>
[CustomEditor(typeof(MonoBehaviour), true)]
public class MoveToolEditor : Editor
{
    private readonly GUIStyle style = new GUIStyle();

    public void OnEnable()
    {
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
    }

    public void OnSceneGUI()
    {
        SetMoveTool();
    }

    /// <summary>
    /// Run MoveToolEditor. You can use position handles of fields that have MoveToolAttribute in the unity editor scene view. 
    /// </summary>
    public void SetMoveTool()
    {
        var targetType = target.GetType();
        var fields = GetSerializedFields(targetType);
        foreach (var field in fields)
        {
            // Check if MoveToolAttribute is defined.
            var attr = field.GetCustomAttribute<MoveToolAttribute>(false);
            if (attr == null)
                continue;

            SetMoveToolAvailableField((field, -1), (this.target, field, -1), attr);
        }
    }

    /// <summary>
    /// Set Position Handles in the unity editor scene view.
    /// </summary>
    /// <param name="top">top level field declared in the MonoBehaviour component</param>
    /// <param name="current">current field checked now, current.obj is the instance where current.field is declared</param>
    /// <param name="attr">defined for the top level field</param>
    /// <param name="n">Don't set any value. It's the count of recursive calls.</param>
    private void SetMoveToolAvailableField((FieldInfo field, int index) top, (object obj, FieldInfo field, int index) current, MoveToolAttribute attr, int n = 0)
    {
        // If it's vector, call immediately SetPositionHandle() method and then terminate.
        if (IsVector(current.field.FieldType))
        {
            string label = string.Empty;
            if (attr.LabelOn)
            {
                label = string.IsNullOrEmpty(attr.Label) ? AddIndexLabel(top.field.Name.InspectorLabel(), top.index) : AddIndexLabel(attr.Label, top.index);
                if (top.field != current.field)
                    label += $" - {(n > 1 ? AddIndexLabel(current.field.Name.InspectorLabel(), current.index, true) : current.field.Name.InspectorLabel())}";
            }

            SetVectorField(current.obj, current.field, label, attr.LocalMode);
            return;
        }

        var type = current.field.FieldType; //current field type

        // Array
        if (type.IsArray)
        {
            type = type.GetElementType();
            if (!HasAvailableAttribute(type))
                return;

            var serializedFields = GetSerializedFields(type);
            var array = current.field.GetValue(current.obj) as Array;
            for (int i = 0; i < array.Length; i++)
            {
                if (top.field == current.field)
                    top.index = i;

                // Recursive call for each field declared in the element type of current array
                object obj = array.GetValue(i);
                foreach (var nextField in serializedFields)
                    SetMoveToolAvailableField(top, (obj, nextField, i), attr, n + 1);
                if (type.IsValueType)
                    array.SetValue(obj, i);
            }
        }
        // List
        else if (type.IsGenericType && typeof(IList).IsAssignableFrom(type))
        {
            type = type.GetGenericArguments()[0];
            if (!HasAvailableAttribute(type))
                return;

            var serializedFields = GetSerializedFields(type);
            var list = current.field.GetValue(current.obj) as IList;
            for (int i = 0; i < list.Count; i++)
            {
                if (top.field == current.field)
                    top.index = i;

                // Recursive call for each field declared in the element type of current list
                object obj = list[i];
                foreach (var nextField in serializedFields)
                    SetMoveToolAvailableField(top, (obj, nextField, i), attr, n + 1);
                if (type.IsValueType)
                    list[i] = obj;
            }
        }
        // Just single field
        else
        {
            if (!HasAvailableAttribute(type))
                return;       

            var serializedFields = GetSerializedFields(type);

            // Recursive call for each field declared in the current field type
            object obj = current.field.GetValue(current.obj);
            foreach (var nextField in serializedFields)
                SetMoveToolAvailableField(top, (obj, nextField, -1), attr, n + 1);

            // If current field is a value type, you must copy boxed obj to this field. It's because obj isn't the field instance itself, but new boxed instance.
            if (type.IsValueType)
                current.field.SetValue(current.obj, obj);
        }
    }

    // Check if both MoveToolAvailableAttribute and SerializableAttribute are defined.
    private bool HasAvailableAttribute(Type type)
    {
        var available = type.GetCustomAttribute<MoveToolAvailableAttribute>(false);
        var seralizable = type.GetCustomAttribute<SerializableAttribute>(false);
        return available != null && seralizable != null;
    }

    // Return SerializedFields.
    private IEnumerable<FieldInfo> GetSerializedFields(Type type)
    {
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // If it's the public field and doesn't have NonSerializedAttribute, then add it.
        // If it's the non-public field and has UnityEngine.SerializeField, then add it.
        var serializedFields =
            from f in fields
            where (f.IsPublic && f.GetCustomAttribute<NonSerializedAttribute>(false) == null) || (!f.IsPublic && f.GetCustomAttribute<SerializeField>(false) != null)
            select f;

        return serializedFields;
    }

    // Add position handles of this field to unity editor scene view. This field is okay whether vector field or vector collection field.
    private void SetVectorField(object obj, FieldInfo field, string label, bool localMode)
    {
        // If it's local mode, then origin point is set to target(MonoBehaviour) position.
        Vector3 origin = localMode ? (this.target as MonoBehaviour).transform.position : Vector3.zero;

        var fieldType = field.FieldType;

        // Field
        if (fieldType == typeof(Vector3))
        {
            Vector3 oldValue = (Vector3)field.GetValue(obj);      
            SetHandleVector3(label, origin, oldValue, obj, field, v => field.SetValue(obj, v));
        }
        else if (fieldType == typeof(Vector2))
        {
            Vector2 oldValue = (Vector2)field.GetValue(obj);
            SetHandleVector2(label, origin, oldValue, obj, field, v => field.SetValue(obj, v));
        }
        // Array
        else if (fieldType.GetElementType() == typeof(Vector3))
        {
            var array = field.GetValue(obj) as Array;
            for (int i = 0; i < array.Length; i++)
            {
                string temp = label;
                if (!string.IsNullOrEmpty(label))
                    temp += $" [{i}]";

                Vector3 oldValue = (Vector3)array.GetValue(i);
                SetHandleVector3(temp, origin, oldValue, obj, field, v => array.SetValue(v, i));
            }
        }
        else if (fieldType.GetElementType() == typeof(Vector2))
        {
            var array = field.GetValue(obj) as Array;
            for (int i = 0; i < array.Length; i++)
            {
                string temp = label;
                if (!string.IsNullOrEmpty(label))
                    temp += $" [{i}]";

                Vector2 oldValue = (Vector2)array.GetValue(i);
                SetHandleVector2(temp, origin, oldValue, obj, field, v => array.SetValue(v, i));
            }
        }
        // List
        else if (fieldType == typeof(List<Vector3>))
        {
            var list = field.GetValue(obj) as List<Vector3>;
            for (int i = 0; i < list.Count; i++)
            {
                string temp = label;
                if (!string.IsNullOrEmpty(label))
                    temp += $" [{i}]";

                Vector3 oldValue = list[i];
                SetHandleVector3(temp, origin, oldValue, obj, field, v => list[i] = v);
            }
        }
        else if (fieldType == typeof(List<Vector2>))
        {
            var list = field.GetValue(obj) as List<Vector2>;
            for (int i = 0; i < list.Count; i++)
            {
                string temp = label;
                if (!string.IsNullOrEmpty(label))
                    temp += $" [{i}]";

                Vector2 oldValue = list[i];
                SetHandleVector2(temp, origin, oldValue, obj, field, v => list[i] = v);
            }
        }
        // If you want to use position handles of other serializable collection, then add here or modify list part.
    }

    // Create Position Handle for Vector3. If it's changed, set and record new value.
    // You need to implement a mechanism to set the new Vector3 value in setValue delegate.
    private void SetHandleVector3(string label, Vector3 origin, Vector3 oldValue, object obj, FieldInfo field, Action<Vector3> setValue)
    {
        Handles.Label(origin + oldValue, label, style);
        EditorGUI.BeginChangeCheck();
        Vector3 newValue = Handles.PositionHandle(origin + oldValue, Quaternion.identity) - origin;
        if (EditorGUI.EndChangeCheck())
        {
            // enable ctrl + z & set dirty
            Undo.RecordObject(target, $"{target.name}_{target.GetInstanceID()}_{obj.GetHashCode()}_{field.Name}");

            setValue(newValue);

            // In the unity document, if the object may be part of a Prefab instance, we have to call this method.
            // But, even if i don't call this method, it works well. I don't know the reason.
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }

    // Create Position Handle for Vector2. If it's changed, set and record new value.
    // You need to implement a mechanism to set the new Vector2 value in setValue delegate.
    private void SetHandleVector2(string label, Vector2 origin, Vector2 oldValue, object obj, FieldInfo field, Action<Vector2> setValue)
    {
        Handles.Label(origin + oldValue, label, style);
        EditorGUI.BeginChangeCheck();
        Vector2 newValue = (Vector2)Handles.PositionHandle(origin + oldValue, Quaternion.identity) - origin;
        if (EditorGUI.EndChangeCheck())
        {
            // enable ctrl + z & set dirty
            Undo.RecordObject(target, $"{target.name}_{target.GetInstanceID()}_{obj.GetHashCode()}_{field.Name}");

            setValue(newValue);

            // In the unity document, if the object may be part of a Prefab instance, we have to call this method.
            // But, even if i don't call this method, it works well. I don't know the reason.
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }

    // Check if it's vector type or vector collection type.
    private bool IsVector(Type type) => type == typeof(Vector2) || type == typeof(Vector3) ||
        typeof(IEnumerable<Vector3>).IsAssignableFrom(type) || typeof(IEnumerable<Vector2>).IsAssignableFrom(type);

    // Add index label to this label parameter.
    // e.g. Label [index]
    private string AddIndexLabel(string label, int index, bool isFront = false)
    {
        if (index >= 0)
        {
            if (isFront)
            {
                label = $"[{index}] {label}";
            }
            else
            {
                label += $" [{index}]";
            }
        }

        return label;
    }
}

#endif