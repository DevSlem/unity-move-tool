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

            SetMoveToolAvailableField((field, -1), (field, -1), attr, this.target);
        }
    }

    /// <summary>
    /// Set Position Handles in the unity editor scene view.
    /// </summary>
    /// <param name="top">top level field declared in the MonoBehaviour script</param>
    /// <param name="current">current field checked now</param>
    /// <param name="attr">applied to top level field</param>
    /// <param name="obj">instance that declare the current field</param>
    private void SetMoveToolAvailableField((FieldInfo info, int index) top, (FieldInfo info, int index) current, MoveToolAttribute attr, object obj)
    {
        // If it's vector, call immediately SetPositionHandle() method and then terminate.
        if (IsVector(current.info.FieldType))
        {
            string label = string.Empty;
            if (attr.LabelOn)
            {
                label = string.IsNullOrEmpty(attr.Label) ? AddIndexLabel(top.info.Name.InspectorLabel(), top.index) : AddIndexLabel(attr.Label, top.index);
                if (top.info != current.info)
                    label += $" - {AddIndexLabel(current.info.Name.InspectorLabel(), current.index)}";
            }

            SetPositionHandle(current.info, label, attr.LocalMode, obj);
            return;
        }

        var type = current.info.FieldType; //current field type

        // Array
        if (type.IsArray)
        {
            type = type.GetElementType();
            if (!HasAvailableAttribute(type))
                return;

            var serializedFields = GetSerializedFields(type);
            var array = current.info.GetValue(obj) as Array;
            for (int i = 0; i < array.Length; i++)
            {
                if (top.info == current.info)
                    top.index = i;

                // Recursive call for each field declared in the element type of current array
                foreach (var nextField in serializedFields)
                {
                    SetMoveToolAvailableField(top, (nextField, i), attr, array.GetValue(i));
                }
            }
        }
        // List(It's okay to check IEnumerable or ICollection type if you want to use other collection.)
        else if (type.IsGenericType && typeof(IList).IsAssignableFrom(type))
        {
            type = type.GetGenericArguments()[0];
            if (!HasAvailableAttribute(type))
                return;

            var serializedFields = GetSerializedFields(type);
            var collection = current.info.GetValue(obj) as IEnumerable;
            int i = 0;
            foreach (var element in collection)
            {
                if (top.info == current.info)
                    top.index = i;

                // Recursive call for each field declared in the element type of current collection
                foreach (var nextField in serializedFields)
                {
                    SetMoveToolAvailableField(top, (nextField, i), attr, element);
                }
                i++;
            }
        }
        // Just single field
        else
        {
            if (!HasAvailableAttribute(type))
                return;

            var serializedFields = GetSerializedFields(type);

            // Recursive call for each field declared in the current field type
            foreach (var nextField in serializedFields)
            {
                SetMoveToolAvailableField(top, (nextField, -1), attr, current.info.GetValue(obj));
            }
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
    private void SetPositionHandle(FieldInfo field, string labelText, bool localMode, object obj)
    {
        // If it's local mode, then origin point is set to target(MonoBehaviour) position.
        Vector3 origin = localMode ? (this.target as MonoBehaviour).transform.position : Vector3.zero;

        var fieldType = field.FieldType;

        // Field
        if (fieldType == typeof(Vector3))
        {
            Vector3 oldValue = (Vector3)field.GetValue(obj);
            Handles.Label(origin + oldValue, labelText, style);
            Vector3 newValue = Handles.PositionHandle(origin + oldValue, Quaternion.identity) - origin;
            field.SetValue(obj, newValue);
        }
        else if (fieldType == typeof(Vector2))
        {
            Vector2 oldValue = (Vector2)field.GetValue(obj);
            Handles.Label((Vector2)origin + oldValue, labelText, style);
            Vector2 newValue = Handles.PositionHandle((Vector2)origin + oldValue, Quaternion.identity) - origin;
            field.SetValue(obj, newValue);
        }
        // Array
        else if (fieldType.GetElementType() == typeof(Vector3))
        {
            var array = field.GetValue(obj) as Array;
            for (int i = 0; i < array.Length; i++)
            {
                string temp = labelText;
                if (!string.IsNullOrEmpty(labelText))
                    temp += $" [{i}]";

                Vector3 oldValue = (Vector3)array.GetValue(i);
                Handles.Label(origin + oldValue, temp, style);
                Vector3 newValue = Handles.PositionHandle(origin + oldValue, Quaternion.identity) - origin;
                array.SetValue(newValue, i);
            }
        }
        else if (fieldType.GetElementType() == typeof(Vector2))
        {
            var array = field.GetValue(obj) as Array;
            for (int i = 0; i < array.Length; i++)
            {
                string temp = labelText;
                if (!string.IsNullOrEmpty(labelText))
                    temp += $" [{i}]";

                Vector2 oldValue = (Vector2)array.GetValue(i);
                Handles.Label((Vector2)origin + oldValue, temp, style);
                Vector2 newValue = Handles.PositionHandle((Vector2)origin + oldValue, Quaternion.identity) - origin;
                array.SetValue(newValue, i);
            }
        }
        // List
        else if (fieldType == typeof(List<Vector3>))
        {
            var list = field.GetValue(obj) as List<Vector3>;
            for (int i = 0; i < list.Count; i++)
            {
                string temp = labelText;
                if (!string.IsNullOrEmpty(labelText))
                    temp += $" [{i}]";

                Vector3 oldValue = list[i];
                Handles.Label(origin + oldValue, temp, style);
                list[i] = Handles.PositionHandle(origin + oldValue, Quaternion.identity) - origin;
            }
        }
        else if (fieldType == typeof(List<Vector2>))
        {
            var list = field.GetValue(obj) as List<Vector2>;
            for (int i = 0; i < list.Count; i++)
            {
                string temp = labelText;
                if (!string.IsNullOrEmpty(labelText))
                    temp += $" [{i}]";

                Vector2 oldValue = list[i];
                Handles.Label((Vector2)origin + oldValue, temp, style);
                list[i] = Handles.PositionHandle((Vector2)origin + oldValue, Quaternion.identity) - origin;
            }
        }
        // if you want to use position handles of other serializable collection, then add here or modify list part.
    }

    // Check if it's vector type or vector collection type.
    private bool IsVector(Type type) => type == typeof(Vector2) || type == typeof(Vector3) ||
        typeof(IEnumerable<Vector3>).IsAssignableFrom(type) || typeof(IEnumerable<Vector2>).IsAssignableFrom(type);

    private string AddIndexLabel(string label, int index)
    {
        if (index >= 0)
            label += $" [{index}]";

        return label;
    }
}
