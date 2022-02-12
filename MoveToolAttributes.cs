using System;
using UnityEngine;

namespace KgmSlem
{
    /// <summary>
    /// If you want to use position handles in unity editor scene view, then declare it for the field belonging to MonoBehaviour.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class MoveToolAttribute : PropertyAttribute
    {
        /// <summary>
        /// You can control the position mode of move-tool. Default is world position mode.
        /// </summary>
        public MoveToolMode PositionMode { get; set; } = MoveToolMode.World;

        /// <summary>
        /// If it's false, don't display label in the unity editor secne view. Default is true.
        /// </summary>
        public bool LabelOn { get; set; } = true;

        /// <summary>
        /// Custom Label. Default is field label for display.
        /// </summary>
        public string Label { get; set; } = string.Empty;

    }

    /// <summary>
    /// If it's defined for a custom type, you can use MoveToolAttribute to the type field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class MoveToolAvailableAttribute : PropertyAttribute { }

    public enum MoveToolMode
    {
        World,
        Local
    }
}
