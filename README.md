# Move-Tool for Unity

## Introduction

Move-Tool is a tool that makes you use position handles for vector in unity editor scene view.  
If you want to use position handles, then define `MoveToolAttribute` for the field belonging to `MonoBehaviour`.  

> Please note that the any type that you want to use Move-Tool, it must be serializable.


## Preview

![preview1](/images/vector-movetool-control.webp)


## MoveToolAttribute

You just define it for the field for which you want to use position handle.  
The field is okay whether it's vector or vector collection.


### Example Code

```c#
public class MoveToolTest : MonoBehaviour
{
    [MoveTool, SerializeField] private Vector3 vector;
    [MoveTool] public List<Vector3> list = new List<Vector3>();
}
```

## MoveToolAvailableAttribute

If you want to use position handles for a custom type field that declare vector or other custom type field declaring vector, then you need to define `MoveToolAvailableAttribute` for the custom type.  
It's okay whether it's class or struct.  

### Example Code

```c#
public class MoveToolTest : MonoBehaviour
{
    [MoveTool] public MyCustomType my;
}

[MoveToolAvailable, Serializable]
public class MyCustomType
{
    public Vector3 vector;
    public List<Vector3> list = new List<Vector3>();
}
```

## MoveToolEditor

It's an editor for `MonoBehaviour`. It sets position handles for the fields that define `MoveToolAttribute`.  

> Please note that if you want to use another editor at the same time, you must create a `MoveToolEditor` instance, then call both `MoveToolEditor.OnEnalbe()` and `MoveToolEditor.OnSceneGUI()`.  
> It's also okay to call `MoveToolEditor.SetMoveTool()` instead of `MoveToolEditor.OnSceneGUI()`.


### Example Code

```cs
[CustomEditor(typeof(Another)), CanEditMultipleObjects]
public class AnotherEditor : Editor
{
    private MoveToolEditor moveToolEditor;

    private void OnSceneGUI()
    {
        if (moveToolEditor == null)
            moveToolEditor = Editor.CreateEditor(target, typeof(MoveToolEditor)) as MoveToolEditor;

        moveToolEditor.OnEnable();
        moveToolEditor.OnSceneGUI(); // It's also okay to call moveToolEditor.SetMoveTool() instead of it.
    }

    private void OnDisable()
    {
        DestroyImmediate(moveToolEditor);
    }
}
```