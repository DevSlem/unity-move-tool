using System;
using UnityEngine;

namespace KgmSlem.UnityEditor
{
    /// <summary>
    /// If you want to use position handles in unity editor scene view, then declare it for the field belonging to MonoBehaviour.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class MoveToolAttribute : PropertyAttribute
    {
        /// <summary>
        /// If it's true, your vector is local position mode. Default is world position mode.
        /// </summary>
        public bool LocalMode { get; set; }

        /// <summary>
        /// If it's false, don't display label in the unity editor secne view. Default is true.
        /// </summary>
        public bool LabelOn { get; set; } = true;

        /// <summary>
        /// Custom Label. Default is field label for display.
        /// </summary>
        public string Label { get; set; } = string.Empty;

    }
}