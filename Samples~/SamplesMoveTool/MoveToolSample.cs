using DevSlem;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveToolSample : MonoBehaviour
{
    [MoveTool] public Vector3 vector;
    [MoveTool] public Vector2 vector2;
    [MoveTool] public List<Vector3> vectorCollection = new List<Vector3>(); // Vector3[] array is also okay.
    [SerializeField, MoveTool] private Vector3 privateVector;
    [SerializeField, MoveTool] private List<Vector3> privateCollection = new List<Vector3>();
    [MoveTool(PositionMode = MoveToolPosition.Local, LabelMode = MoveToolLabel.SceneView, Label = "My Custom Label")]
    public Vector3 customPropertyVector;
    [MoveTool] public List<CustomClass> customClasses = new List<CustomClass>();
}

[Serializable, MoveToolAvailable]
public class CustomClass
{
    public Vector3 publicVector; // Can use move-tool.
    [SerializeField] private Vector3 serializedPrivateVector; // Can use move-tool.
    [NonSerialized] public Vector3 nonSerializedPublicVector; // Can't use move-tool.
    private Vector3 privateVector; // Can't use move-tool.
}