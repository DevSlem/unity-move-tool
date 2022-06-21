using System;
using UnityEngine;

namespace DevSlem
{
    /// <summary>
    /// You can use move-tool for the vector in unity editor scene view.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class MoveToolAttribute : PropertyAttribute
    {
        /// <summary>
        /// You can control the position mode of move-tool. Default is world position mode.
        /// </summary>
        public MoveToolPosition PositionMode { get; set; } = MoveToolPosition.World;

        /// <summary>
        /// You can display the move-tool label on unity editor through this enum flags.
        /// </summary>
        public MoveToolLabel LabelMode { get; set; } = MoveToolLabel.SceneView | MoveToolLabel.InspectorView;

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

    public enum MoveToolPosition
    {
        World,
        Local
    }

    [Flags]
    public enum MoveToolLabel
    {
        None = 0,
        SceneView = 1,
        InspectorView = 2
    }
}
