# Move-Tool for Unity

**Move-Tool** is a tool that makes you use position handles for vector in unity editor scene view.  
You can use move-tool so easily by just defining ***some attributes***.

> Note that it require **C# 7** or higher.  
> If you want to use it in a lower version, you need to modify **pattern matching**(only can use in C# 7 or higher) part in `MoveToolEditor` class to `if` statement and simple type checking, but it'll make you tired.

* [Basic usage](#basic-usage)
* [MoveToolAttribute Properties](#movetoolattribute-properties)
* [Move-Tool available custom type](#move-tool-available-custom-type)
* [Editor](#editor)

## Releases

It is recommended to use a latest, stable version.
[main](https://github.com/DevSlem/unity-move-tool/tree/main) version is unstable because it's under active development.

|                                        Version                                         | Release Date |                                     Source                                     |      C#       |     .Net Compatibility      |
| :------------------------------------------------------------------------------------: | :----------: | :----------------------------------------------------------------------------: | :-----------: | :-------------------------: |
|                                     main(unstable)                                     |      --      |          [main](https://github.com/DevSlem/unity-move-tool/tree/main)          | 7.0 or higher | .Net Standard 2.0 or higher |
| [Release 3.1.0](https://github.com/DevSlem/unity-move-tool/releases/tag/release-3.1.0) |  2022-09-16  | [release-3.1.0](https://github.com/DevSlem/unity-move-tool/tree/release-3.1.0) | 7.0 or higher | .Net standard 2.0 or higher |

## Latest Update

* You can use **Move-Tool** for the `float` field.
* Arrow of z direction of **Move-Tool** for the `Vector2` field has been removed. To be more intuitive!

## Installation

You can select 2 installation methods.

* Clone the repository and add `package.json` file to your project through Unity Package Manager.
* You can add directly the package from git URL to your project through Unity Package Manager. See the [Installing from a Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html).

## Basic usage

You just define `MoveTool` attribute for a field for which you want to use position handle.  
The field is okay whether it's vector or vector collection.  
It works only if the type of the field is one of the `Vector3`, `Vector2`, `float` type.

If you want to use ***attributes*** about move-tool, you must declare the following `using` directive.

```c#
using DevSlem;
```

> Note that the any type that you want to use Move-Tool, it must be serializable.

### Vector3

```c#
public class MoveToolSample : MonoBehaviour
{
    [MoveTool] public Vector3 vector;
}
```

![](/Images/move-tool-vector3.webp)

### Vector2

```c#
[MoveTool] public Vector2 vector2;
```

> Note that `Vector2` type field only moves along the x and y axes.

![](/Images/move-tool-vector2.webp)

### Float

```c#
[MoveTool] public float floatField;
```

> Note that `float` type field only moves along the x axis.

![](/Images/move-tool-float.webp)

### Collection

You can use move-tool to `Array` or `List<T>` collection where each element is vector value.  
While you click the **shift** key, you can control all elements of the list at once.

```c#
[MoveTool] public List<Vector3> vectorCollection = new List<Vector3>(); // Vector3[] array is also okay.
```

![](/Images/move-tool-collection.webp)

### Non-Public field

You can only use move-tool for a serializable vector.
So, if you want to use move-tool for a ***non-public*** field like `private` or `protected`, you have to define `UnityEngine.SerializeField` attribute for the field.  
See the following code.

```c#
[SerializeField, MoveTool] private Vector3 privateVector;
[SerializeField, MoveTool] private List<Vector3> privateCollection = new List<Vector3>();
```

## MoveToolAttribute Properties

`MoveToolAttribute` has the following properties.

* `PositionMode` sets the coordinate space of the vector. If you set it to `MoveToolPosition.Local` enum value, the vector works in local coordinate. Default is world coordinate.
* `LabelMode` is a enum flag property. You can display the move-tool label on unity editor through it. By default, the label is displayed on both scene and inspector view.
* `Label` is a custom label that you want to display your own label instead of the default label. Default label is the field name for display.

### Sample Code

```c#
[MoveTool(PositionMode = MoveToolPosition.Local, LabelMode = MoveToolLabel.SceneView, Label = "My Custom Label")]
public Vector3 customPropertyVector;
```

## Move-Tool available custom type

If you want to use move-tool for a custom type field which declare vector fields, you must define `System.Serializable` and `MoveToolAvailable` attributes for the type.  
The custom type is okay whether class or struct.

```c#
public class MoveToolSample : MonoBehaviour
{
    [MoveTool] public List<CustomClass> customClasses = new List<CustomClass>();
}

[Serializable, MoveToolAvailable]
public class CustomClass
{
    public List<Vector3> vectors = new List<Vector3>();
}
```

![](/Images/move-tool-custom-type-collection.webp)

### Serialize

A custom type for which you define `MoveToolAvailable` attribute must be serializable. So, if you don't want to use move-tools for the fields which is declared in the custom type, you should set the fields to be non-serializable.  

> Note that you can only use move-tool for a serialized field.

See the following example.

```c#
[Serializable, MoveToolAvailable]
public class CustomClass
{
    public Vector3 publicVector; // Can use move-tool.
    [SerializeField] private Vector3 serializedPrivateVector; // Can use move-tool.
    [NonSerialized] public Vector3 nonSerializedPublicVector; // Can't use move-tool.
    private Vector3 privateVector; // Can't use move-tool.
}
```

Public vector is always serialized. If you don't want to use move-tool for a public field, you need to define `System.NonSerialized` attribute for it.  
Non-public vector isn't always serialized. If you want to use move-tool for a non-public field, you need to define `UnityEngine.SerializeField` attribute for it.  

## Editor

Now, you don't have to worry about editor conflict problem. It is solved.  
You don't need to create `MoveToolEditor` instance for using concurrently `AnotherEditor` with it.  
You just use normally `MoveTool` attribute.
