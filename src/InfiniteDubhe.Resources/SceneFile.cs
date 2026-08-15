using System.Numerics;
using System.Text.Json;

namespace InfiniteDubhe.Resources;

/// <summary>
/// 场景文件的顶层 DTO。采用「扁平对象列表 + GUID 父子引用」结构，
/// 避免递归 JSON 嵌套，也便于加载时先建对象、后重连层级。
/// </summary>
public sealed class SceneFile
{
    /// <summary>格式版本，供未来迁移。</summary>
    public int Version { get; set; } = 1;

    public string Name { get; set; } = "";

    /// <summary>扁平对象列表（父子关系靠 <see cref="GameObjectData.ParentId"/>）。</summary>
    public List<GameObjectData> Objects { get; set; } = new();
}

/// <summary>单个游戏对象的序列化数据。</summary>
public sealed class GameObjectData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool Active { get; set; } = true;

    /// <summary>父对象 GUID；null 表示根对象。</summary>
    public Guid? ParentId { get; set; }

    public TransformData Transform { get; set; } = new();
    public List<ComponentData> Components { get; set; } = new();
}

/// <summary>变换数据（位置/旋转/缩放）。</summary>
public sealed class TransformData
{
    public Vector2 Position { get; set; }
    public float RotationDeg { get; set; }
    public Vector2 Scale { get; set; } = Vector2.One;
}

/// <summary>
/// 组件数据：类型全名 + 可写属性集合。经反射重建，不依赖具体组件程序集（Physics/UI 等）。
/// </summary>
public sealed class ComponentData
{
    /// <summary>组件类型 AssemblyQualifiedName，用于反序列化时按类型重建。</summary>
    public string TypeName { get; set; } = "";

    public List<PropertyValue> Properties { get; set; } = new();

    /// <summary>UI 元素树（仅 <c>Canvas</c> 组件使用）：根元素列表，父组件按类型反射重建。</summary>
    public List<UiElementData> UiElements { get; set; } = new();
}

/// <summary>
/// UI 元素树节点（Canvas 的 Roots 树）。反射式：类型全名 + 可写属性 + 子元素，
/// 不依赖 UI 程序集即可覆盖 Panel/Image/Text/Button 等元素类型。
/// </summary>
public sealed class UiElementData
{
    /// <summary>元素类型 AssemblyQualifiedName，用于反序列化时按类型重建。</summary>
    public string TypeName { get; set; } = "";

    public List<PropertyValue> Properties { get; set; } = new();

    public List<UiElementData> Children { get; set; } = new();
}

/// <summary>单个属性值。值为 <see cref="JsonElement"/>，反序列化时由反射按目标类型转换。</summary>
public sealed class PropertyValue
{
    public string Name { get; set; } = "";
    public JsonElement Value { get; set; }
}

/// <summary>资源引用（GUID + 路径）：场景/预制体里引用纹理等资源时，以 GUID 为主、路径为回退。</summary>
public sealed class AssetReference
{
    public Guid Guid { get; set; }
    public string Path { get; set; } = "";
}
