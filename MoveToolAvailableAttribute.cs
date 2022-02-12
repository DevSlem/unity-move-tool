using System;
using UnityEngine;

namespace KgmSlem.UnityEditor
{
    /// <summary>
    /// If it's defined for a custom type, you can use MoveToolAttribute to the type field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class MoveToolAvailableAttribute : PropertyAttribute { }
}