using KgmSlem;
using KgmSlem.UnityEditor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

public class MoveToolTest : MonoBehaviour
{
    [MoveTool] public CustomClass customClass = new CustomClass();
}

[Serializable, MoveToolAvailable]
public class CustomClass
{
    public Vector3 publicVector; // Serialized public field
    [SerializeField] private Vector3 serializedPrivateVector; // Serialized private field
    [NonSerialized] public Vector3 nonSerializePublicdVector; // Non-serialized public field
    private Vector3 privateVector; // Non-serialized private field
}