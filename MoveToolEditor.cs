#define LEGACY1
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

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

#if LEGACY
    [Obsolete("Don't use this method. It has a possibility to occur error about serialziedObject and OnSceneGUI.", true)]
    public void SetMoveToolLegacy()
    {
        var property = serializedObject.GetIterator();
        while (property.Next(true))
        {
            // 현재 직렬화된 프로퍼티가 Vector가 아닌 경우 스킵
            if (!IsVector(property.propertyType))
                continue;

            // 현재 프로퍼티의 최상위 필드(컴포넌트에 선언한 필드)에 MoveTool 특성이 없는 경우 Skip
            string[] pathSplit = property.propertyPath.Split('.');
            var mt = HasMoveToolAttribute(pathSplit[0]);
            if (!mt.isChecked)
                continue;

            // 프로퍼티의 최상위 필드가 벡터인 경우
            if (IsVector(mt.topLevelField.FieldType))
            {
                if (mt.attr.LabelOn)
                {

                    //string firstIndexLabel = GetCollectionIndexLabel(pathSplit)
                    string indexLabel = GetCollectionIndexLabel(pathSplit.LastOrDefault());
                    string label = (string.IsNullOrEmpty(mt.attr.Label) ? mt.topLevelField.Name.InspectorLabel() : mt.attr.Label) +
                        (string.IsNullOrEmpty(indexLabel) ? string.Empty : $" {indexLabel}");
                    SetPositionHandle(property, label, mt.attr.LocalMode);
                }
                else
                {
                    SetPositionHandle(property, string.Empty, mt.attr.LocalMode);
                }
            }
            // 최상위 필드가 벡터는 아니지만, 프로퍼티 멤버를 포함하는 커스텀 타입이 MoveToolAvailable 특성을 포함하는 경우
            else if (HasMoveToolAvailableAttribute(property))
            {

                //string fieldName = isArray ? pathSplit[pathSplit.ToList().LastIndexOf("Array") - 1] : property.name;
                if (mt.attr.LabelOn)
                {
                    string subFieldName = property.name.InspectorLabel();
                    string topFieldName = string.IsNullOrEmpty(mt.attr.Label) ? mt.topLevelField.Name.InspectorLabel() : mt.attr.Label;
                    try
                    {
                        if (pathSplit[pathSplit.Length - 2] == "Array")
                            subFieldName = pathSplit[pathSplit.Length - 3].InspectorLabel() + $" {GetCollectionIndexLabel(pathSplit[pathSplit.Length - 1])}";

                        if (pathSplit[1] == "Array")
                            topFieldName += $" {GetCollectionIndexLabel(pathSplit[2])}";
                    }
                    catch (IndexOutOfRangeException) { }

                    string label = $"{topFieldName} - {subFieldName}";
                    SetPositionHandle(property, label, mt.attr.LocalMode);
                }
                else
                {
                    SetPositionHandle(property, string.Empty, mt.attr.LocalMode);
                }
            }
        }
    }

    // 이 프로퍼티를 포함하는 모든 상위 목록에 있는 프로퍼티의 타입이 MoveToolAvailable 특성을 가지고 있는지 체크
    // 직렬화된 프로퍼티가 Vector이고, 최상위 필드가 Vector가 아닌 경우에만 실행하는 것을 권장함.
    private bool HasMoveToolAvailableAttribute(SerializedProperty property)
    {
        var splitPath = property.propertyPath.Split('.');
        StringBuilder buffer = new StringBuilder();
        buffer.Append(splitPath[0]);
        int idx = 0;

        while (idx + 1 < splitPath.Length)
        {
            if (splitPath[idx + 1] == "Array")
            {
                int next = idx + 3;
                if (next >= splitPath.Length)
                    return true;

                var attr = FindAttribute<MoveToolAvailableAttribute>(property, buffer.ToString());
                if (attr == null)
                    return false;

                // 경로 추가
                for (int i = idx + 1; i <= next; i++)
                    buffer.Append('.').Append(splitPath[i]);

                idx = next;
            }
            else
            {
                int next = idx + 1;
                if (next >= splitPath.Length)
                    return true;

                var attr = FindAttribute<MoveToolAvailableAttribute>(property, buffer.ToString());
                if (attr == null)
                    return false;

                // 경로 추가
                for (int i = idx + 1; i <= next; i++)
                    buffer.Append('.').Append(splitPath[i]);

                idx = next;
            }
        }

        // 위 while문을 실행하지 않은 경우
        if (idx == 0)
        {
            var attr = FindAttribute<MoveToolAvailableAttribute>(property, buffer.ToString());
            if (attr == null)
                return false;
        }

        return true;
    }

    // path에 해당하는 프로퍼티를 찾은 후 특성 존재 여부 체크
    private T FindAttribute<T>(SerializedProperty property, string path) where T : Attribute
    {

        var sub = property.serializedObject.FindProperty(path);
        var subType = sub.GetPropertyType();
        return subType.IsGenericType ? subType.GetGenericArguments()[0].GetCustomAttribute<T>(false) : subType.GetCustomAttribute<T>(false);
    }

    // 문자열에서 인덱스 기호 추출
    private string GetCollectionIndexLabel(string str)
    {
        if (str != null)
        {
            (int start, int end) = (str.IndexOf('['), str.IndexOf(']'));
            if (start >= 0 && end > start)
                return str.Substring(start, end - start + 1);
        }
        return string.Empty;
    }

    // Vector 타입 여부를 SerializedPropertyType enum 타입으로 체크
    private bool IsVector(SerializedPropertyType typeInfo) => typeInfo == SerializedPropertyType.Vector2 || typeInfo == SerializedPropertyType.Vector3;

    // Vector 타입이거나 Vector 타입을 포함하는 컬렉션 타입인 경우

    // property에 MoveTool Attribute가 할당되어있는지 체크
    private (bool isChecked, FieldInfo topLevelField, MoveToolAttribute attr) HasMoveToolAttribute(string fieldName)
    {
        // SerializedProperty의 이름에 해당하는 필드 정보를 가져오는데 실패 시 false 반환
        FieldInfo field = serializedObject.targetObject.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        // 가져온 필드에 MoveTool Attribute가 할당되어있는지 체크
        MoveToolAttribute attr = field?.GetCustomAttribute<MoveToolAttribute>(false);
        return (attr != null, field, attr);
    }

    // Scene View에 Position Handle을 생성 및 배치함
    private void SetPositionHandle(SerializedProperty property, string labelText, bool localMode)
    {
        // 로컬 모드일 경우 기준 좌표를 원점이 아닌 현재 target 오브젝트의 위치로 설정
        Vector3 center = localMode ? (target as MonoBehaviour).transform.position : Vector3.zero;

        switch (property.propertyType)
        {
            case SerializedPropertyType.Vector3:
                Handles.Label(center + property.vector3Value, labelText, style); // 레이블 할당
                property.vector3Value = Handles.PositionHandle(center + property.vector3Value, Quaternion.identity) - center; // 핸들 배치
                serializedObject.ApplyModifiedProperties();
                break;
            case SerializedPropertyType.Vector2:
                Handles.Label((Vector2)center + property.vector2Value, labelText, style); // 레이블 할당
                property.vector2Value = Handles.PositionHandle((Vector2)center + property.vector2Value, Quaternion.identity) - center; // 핸들 배치
                serializedObject.ApplyModifiedProperties();
                break;
        }
    }
#endif
}
