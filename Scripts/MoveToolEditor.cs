#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using KgmSlem.Extensions;

namespace KgmSlem.UnityEditor
{
    /// <summary>
    /// It sets position handles for the fields that define MoveToolAttribute. Note that it's an editor for MonoBehaviour. 
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), false)]
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
            SetOnlyHasMoveToolAttribute();
        }

        /// <summary>
        /// Run MoveToolEditor. You can use move-tools of fields which define MoveToolAttribute in your monobehavior class.
        /// </summary>
        public bool SetMoveTool()
        {
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;

            return SetOnlyHasMoveToolAttribute();
        }

        private bool SetOnlyHasMoveToolAttribute()
        {
            var targetType = target.GetType();
            var fields = GetSerializedFields(targetType);
            bool isExisting = false;
            foreach (var field in fields)
            {
                // Check if MoveToolAttribute is defined.
                var attr = field.GetCustomAttribute<MoveToolAttribute>(false);
                if (attr == null)
                    continue;
                isExisting = true;
                SetMoveToolAvailableField((field, -1), (this.target, field, -1), attr);
            }

            return isExisting;
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
            object fieldValue = current.field.GetValue(current.obj);

            if (IsVector(fieldValue))
            {
                string label = string.Empty;
                if (attr.LabelMode.HasFlag(MoveToolLabel.SceneView))
                {
                    label = string.IsNullOrEmpty(attr.Label) ? AddIndexLabel(top.field.Name.InspectorLabel(), top.index) : AddIndexLabel(attr.Label, top.index);
                    if (top.field != current.field)
                        label += $" - {(n > 1 ? AddIndexLabel(current.field.Name.InspectorLabel(), current.index, true) : current.field.Name.InspectorLabel())}";
                }

                SetVectorField(current.obj, current.field, label, attr.PositionMode);
                return;
            }

            var fieldType = current.field.FieldType; //current field type

            // Array field, List field, Custom type field which inherit from IList<T>
            if (fieldType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)) is var listType && listType != null)
            {
                var elementType = listType.GetGenericArguments()[0];
                if (!HasAvailableAttribute(elementType))
                    return;

                var serializedFields = GetSerializedFields(elementType);

                var collectionType = listType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
                var accessor = listType.GetProperty("Item");
                int count = (int)collectionType.GetProperty("Count").GetValue(fieldValue, null);
                object[] index = { 0 };

                for (int i = 0; i < count; i++)
                {
                    if (top.field == current.field)
                        top.index = i;

                    index[0] = i;
                    object element = accessor.GetValue(fieldValue, index);

                    // Recursive call for each field declared in the current field type
                    foreach (var nextField in serializedFields)
                        SetMoveToolAvailableField(top, (element, nextField, i), attr, n + 1);

                    // If the current element is a value type, you must paste boxed element to this element.
                    if (elementType.IsValueType)
                        accessor.SetValue(fieldValue, element, index);
                }
            }
            // Single custom type field which isn't collection
            else
            {
                if (!HasAvailableAttribute(fieldType))
                    return;

                var serializedFields = GetSerializedFields(fieldType);
                // Recursive call for each field declared in the current field type
                object obj = current.field.GetValue(current.obj);
                foreach (var nextField in serializedFields)
                    SetMoveToolAvailableField(top, (obj, nextField, -1), attr, n + 1);

                // If the current field is a value type, you must paste boxed obj to this field. It's because obj isn't the field instance itself, but new boxed instance.
                if (fieldType.IsValueType)
                    current.field.SetValue(current.obj, obj);
            }

            #region === Deprecated ===
            //// Array
            //if (fieldType.IsArray)
            //{
            //    fieldType = fieldType.GetElementType();
            //    if (!HasAvailableAttribute(fieldType))
            //        return;

            //    var serializedFields = GetSerializedFields(fieldType);
            //    var array = current.field.GetValue(current.obj) as Array;
            //    for (int i = 0; i < array.Length; i++)
            //    {
            //        if (top.field == current.field)
            //            top.index = i;

            //        // Recursive call for each field declared in the element type of current array
            //        object obj = array.GetValue(i);
            //        foreach (var nextField in serializedFields)
            //            SetMoveToolAvailableField(top, (obj, nextField, i), attr, n + 1);
            //        if (fieldType.IsValueType)
            //            array.SetValue(obj, i);
            //    }
            //}
            //// List
            //else if (fieldType.IsGenericType && typeof(IList).IsAssignableFrom(fieldType))
            //{
            //    fieldType = fieldType.GetGenericArguments()[0];
            //    if (!HasAvailableAttribute(fieldType))
            //        return;

            //    var serializedFields = GetSerializedFields(fieldType);
            //    var list = current.field.GetValue(current.obj) as IList;
            //    for (int i = 0; i < list.Count; i++)
            //    {
            //        if (top.field == current.field)
            //            top.index = i;

            //        // Recursive call for each field declared in the element type of current list
            //        object obj = list[i];
            //        foreach (var nextField in serializedFields)
            //            SetMoveToolAvailableField(top, (obj, nextField, i), attr, n + 1);
            //        if (fieldType.IsValueType)
            //            list[i] = obj;
            //    }
            //}
            //// Just single field
            //else
            //{
            //    if (!HasAvailableAttribute(fieldType))
            //        return;

            //    var serializedFields = GetSerializedFields(fieldType);

            //    // Recursive call for each field declared in the current field type
            //    object obj = current.field.GetValue(current.obj);
            //    foreach (var nextField in serializedFields)
            //        SetMoveToolAvailableField(top, (obj, nextField, -1), attr, n + 1);

            //    // If current field is a value type, you must copy boxed obj to this field. It's because obj isn't the field instance itself, but new boxed instance.
            //    if (fieldType.IsValueType)
            //        current.field.SetValue(current.obj, obj);
            //}
            #endregion
        }    

        /// <summary>
        /// Classify wheter the field is a vector field or a vector collection field and then place position handles of it in unity-editor scene view.
        /// </summary>
        private void SetVectorField(object obj, FieldInfo field, string label, MoveToolPosition mode)
        {
            // the position origin.
            Vector3 origin = Vector3.zero;

            switch (mode)
            {
                case MoveToolPosition.Local:
                    // If it's local mode, the origin point is set to target(MonoBehaviour) position.
                    origin = (this.target as MonoBehaviour).transform.position;
                    break;
            }

            //var fieldType = field.FieldType;
            bool shiftClicked = Event.current.shift; // check if you've clicked a shift button.
            object fieldValue = field.GetValue(obj);

            // Pattern matching to test the fieldValue to see if it matches a vector type.
            switch (fieldValue)
            {
                case Vector3 oldVector3:
                    field.SetValue(obj, SetPositionHandle(label, origin, oldVector3, obj, field));
                    break;
                case Vector2 oldVector2:
                    field.SetValue(obj, SetPositionHandle(label, (Vector2)origin, oldVector2, obj, field));
                    break;
                case IList<Vector3> vector3List:
                    for (int i = 0; i < vector3List.Count; i++)
                    {
                        string temp = label;
                        if (!string.IsNullOrEmpty(label))
                            temp += $" [{i}]";

                        Vector3 oldValue = vector3List[i];
                        Vector3 newValue = SetPositionHandle(temp, origin, oldValue, obj, field);
                        //SetHandleVector3(temp, origin, oldValue, obj, field, v => list[i] = v);
                        if (shiftClicked && newValue != oldValue)
                        {
                            Vector3 delta = newValue - oldValue;
                            for (int j = 0; j < vector3List.Count; j++)
                                vector3List[j] += delta;
                            break;
                        }
                        vector3List[i] = newValue;
                    }
                    break;
                case IList<Vector2> vector2List:
                    for (int i = 0; i < vector2List.Count; i++)
                    {
                        string temp = label;
                        if (!string.IsNullOrEmpty(label))
                            temp += $" [{i}]";

                        Vector2 oldValue = vector2List[i];
                        Vector2 newValue = SetPositionHandle(temp, (Vector2)origin, oldValue, obj, field);
                        //SetHandleVector2(temp, origin, oldValue, obj, field, v => list[i] = v);
                        if (shiftClicked && newValue != oldValue)
                        {
                            Vector2 delta = newValue - oldValue;
                            for (int j = 0; j < vector2List.Count; j++)
                                vector2List[j] += delta;
                            break;
                        }
                        vector2List[i] = newValue;
                    }
                    break;
                // If you want to use position handles of other serializable collection, then add here.
                default:
                    break;
            }

            #region === Deprecated ===
            //if (fieldValue is Vector3 oldVector3)
            //{
            //    //Vector3 oldValue = (Vector3)field.GetValue(obj);
            //    field.SetValue(obj, SetPositionHandle(label, origin, oldVector3, obj, field));
            //    //SetHandleVector3(label, origin, oldValue, obj, field, v => field.SetValue(obj, v));
            //}
            //else if (fieldValue is Vector2 oldVector2)
            //{
            //    //Vector2 oldValue = (Vector2)field.GetValue(obj);
            //    field.SetValue(obj, SetPositionHandle(label, (Vector2)origin, oldVector2, obj, field));
            //    //SetHandleVector2(label, origin, oldValue, obj, field, v => field.SetValue(obj, v));
            //}
            //// Collection which interits from IList<T> interface. Both Array and List<T> belong to it.
            //else if (fieldValue is IList<Vector3> vector3List)
            //{
            //    for (int i = 0; i < vector3List.Count; i++)
            //    {
            //        string temp = label;
            //        if (!string.IsNullOrEmpty(label))
            //            temp += $" [{i}]";

            //        Vector3 oldValue = vector3List[i];
            //        Vector3 newValue = SetPositionHandle(temp, origin, oldValue, obj, field);
            //        //SetHandleVector3(temp, origin, oldValue, obj, field, v => list[i] = v);
            //        if (shiftClicked && newValue != oldValue)
            //        {
            //            Vector3 delta = newValue - oldValue;
            //            for (int j = 0; j < vector3List.Count; j++)
            //                vector3List[j] += delta;
            //            break;
            //        }
            //        vector3List[i] = newValue;
            //    }
            //}
            //else if (fieldValue is IList<Vector2> vector2List)
            //{
            //    for (int i = 0; i < vector2List.Count; i++)
            //    {
            //        string temp = label;
            //        if (!string.IsNullOrEmpty(label))
            //            temp += $" [{i}]";

            //        Vector2 oldValue = vector2List[i];
            //        Vector2 newValue = SetPositionHandle(temp, (Vector2)origin, oldValue, obj, field);
            //        //SetHandleVector2(temp, origin, oldValue, obj, field, v => list[i] = v);
            //        if (shiftClicked && newValue != oldValue)
            //        {
            //            Vector2 delta = newValue - oldValue;
            //            for (int j = 0; j < vector2List.Count; j++)
            //                vector2List[j] += delta;
            //            break;
            //        }
            //        vector2List[i] = newValue;
            //    }
            //}
            //// Array
            //else if (fieldType.GetElementType() == typeof(Vector3))
            //{
            //    var array = field.GetValue(obj) as Array;
            //    for (int i = 0; i < array.Length; i++)
            //    {
            //        string temp = label;
            //        if (!string.IsNullOrEmpty(label))
            //            temp += $" [{i}]";

            //        Vector3 oldValue = (Vector3)array.GetValue(i);
            //        Vector3 newValue = SetPositionHandle(temp, origin, oldValue, obj, field);
            //        //SetHandleVector3(temp, origin, oldValue, obj, field, v => newValue = v);
            //        if (shiftClicked && newValue != oldValue)
            //        {
            //            Vector3 delta = newValue - oldValue;
            //            for (int j = 0; j < array.Length; j++)
            //                array.SetValue((Vector3)array.GetValue(j) + delta, j);
            //            break;
            //        }
            //        array.SetValue(newValue, i);
            //    }
            //}
            //else if (fieldType.GetElementType() == typeof(Vector2))
            //{
            //    var array = field.GetValue(obj) as Array;
            //    for (int i = 0; i < array.Length; i++)
            //    {
            //        string temp = label;
            //        if (!string.IsNullOrEmpty(label))
            //            temp += $" [{i}]";

            //        Vector2 oldValue = (Vector2)array.GetValue(i);
            //        Vector2 newValue = SetPositionHandle(temp, (Vector2)origin, oldValue, obj, field);
            //        //SetHandleVector2(temp, origin, oldValue, obj, field, v => array.SetValue(v, i));
            //        if (shiftClicked && newValue != oldValue)
            //        {
            //            Vector2 delta = newValue - oldValue;
            //            for (int j = 0; j < array.Length; j++)
            //                array.SetValue((Vector2)array.GetValue(j) + delta, j);
            //            break;
            //        }
            //        array.SetValue(newValue, i);
            //    }
            //}
            // List
            //else if (fieldType == typeof(List<Vector3>))
            //{
            //    var list = field.GetValue(obj) as List<Vector3>;
            //    for (int i = 0; i < list.Count; i++)
            //    {
            //        string temp = label;
            //        if (!string.IsNullOrEmpty(label))
            //            temp += $" [{i}]";

            //        Vector3 oldValue = list[i];
            //        Vector3 newValue = SetPositionHandle(temp, origin, oldValue, obj, field);
            //        //SetHandleVector3(temp, origin, oldValue, obj, field, v => list[i] = v);
            //        if (shiftClicked && newValue != oldValue)
            //        {
            //            Vector3 delta = newValue - oldValue;
            //            for (int j = 0; j < list.Count; j++)
            //                list[j] += delta;
            //            break;
            //        }
            //        list[i] = newValue;
            //    }
            //}
            //else if (fieldType == typeof(List<Vector2>))
            //{
            //    var list = field.GetValue(obj) as List<Vector2>;
            //    for (int i = 0; i < list.Count; i++)
            //    {
            //        string temp = label;
            //        if (!string.IsNullOrEmpty(label))
            //            temp += $" [{i}]";

            //        Vector2 oldValue = list[i];
            //        Vector2 newValue = SetPositionHandle(temp, (Vector2)origin, oldValue, obj, field);
            //        //SetHandleVector2(temp, origin, oldValue, obj, field, v => list[i] = v);
            //        if (shiftClicked && newValue != oldValue)
            //        {
            //            Vector2 delta = newValue - oldValue;
            //            for (int j = 0; j < list.Count; j++)
            //                list[j] += delta;
            //            break;
            //        }
            //        list[i] = newValue;
            //    }
            //}
            #endregion
        }


        /// <summary>
        /// Create a position handle for the vector3 oldValue. If it's changed, record the taget.
        /// </summary>
        /// <returns>changed vector3</returns>
        private Vector3 SetPositionHandle(string label, Vector3 origin, Vector3 oldValue, object obj, FieldInfo field)
        {
            Handles.Label(origin + oldValue, label, style);
            EditorGUI.BeginChangeCheck();
            Vector3 newValue = Handles.PositionHandle(origin + oldValue, Quaternion.identity) - origin;
            if (EditorGUI.EndChangeCheck())
            {
                // enable ctrl + z & set dirty
                Undo.RecordObject(target, $"{target.name}_{target.GetInstanceID()}_{obj.GetHashCode()}_{field.Name}");

                // In the unity document, if the object may be part of a Prefab instance, we have to call this method.
                // But, even if i don't call this method, it works well. I don't know the reason.
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
            return newValue;
        }

        /// <summary>
        /// Create Position Handle for Vector2. If it's changed, record the target.
        /// </summary>
        /// <returns>changed vector2</returns>
        private Vector2 SetPositionHandle(string label, Vector2 origin, Vector2 oldValue, object obj, FieldInfo field)
        {
            Handles.Label(origin + oldValue, label, style);
            EditorGUI.BeginChangeCheck();
            Vector2 newValue = (Vector2)Handles.PositionHandle(origin + oldValue, Quaternion.identity) - origin;
            if (EditorGUI.EndChangeCheck())
            {
                // enable ctrl + z & set dirty
                Undo.RecordObject(target, $"{target.name}_{target.GetInstanceID()}_{obj.GetHashCode()}_{field.Name}");

                // In the unity document, if the object may be part of a Prefab instance, we have to call this method.
                // But, even if i don't call this method, it works well. I don't know the reason.
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
            return newValue;
        }

        /// <summary>
        /// Check the type for which both MoveToolAvailableAttribute and SerializableAttribute are defined.
        /// </summary>
        private bool HasAvailableAttribute(Type type)
        {
            var available = type.GetCustomAttribute<MoveToolAvailableAttribute>(false);
            var seralizable = type.GetCustomAttribute<SerializableAttribute>(false);
            return available != null && seralizable != null;
        }

        /// <summary>
        /// Return the serialized fields for the type.
        /// </summary>
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

        /// <summary>
        /// Check wheter obj is vector type instance or vector type collection.
        /// </summary>
        private bool IsVector(object obj) => obj is Vector3 || obj is Vector2 || obj is IList<Vector3> || obj is IList<Vector2>;


        /// <summary>
        /// Add index label to this label parameter. <br/>
        /// e.g. Label [index]
        /// </summary>
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

        #region === Deprecated ===
        // Create Position Handle for Vector3. If it's changed, set and record new value.
        // You need to implement a mechanism to set the new Vector3 value in setValue delegate.
        [Obsolete]
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
        [Obsolete]
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
        [Obsolete]
        private bool IsVector(Type type) => type == typeof(Vector2) || type == typeof(Vector3) ||
            typeof(IEnumerable<Vector3>).IsAssignableFrom(type) || typeof(IEnumerable<Vector2>).IsAssignableFrom(type);
        #endregion
    }
}
#endif