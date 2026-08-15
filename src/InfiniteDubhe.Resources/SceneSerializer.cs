using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using InfiniteDubhe.Core;
using InfiniteDubhe.Scene;
using SceneType = InfiniteDubhe.Scene.Scene;

namespace InfiniteDubhe.Resources;

/// <summary>
/// 场景序列化器：把活 <see cref="Scene"/> 映射为 <see cref="SceneFile"/> DTO（反之亦然）。
/// 组件用反射读写其公开可写属性，因此无需引用 Physics/UI 等程序集即可覆盖所有组件类型；
/// 纹理属性存资源路径，加载时经 <see cref="ResourceManager"/> 重连为句柄。
/// </summary>
public sealed class SceneSerializer
{
    private readonly ResourceManager _resources;
    private readonly IAssetGuidResolver? _guidResolver;
    private readonly JsonSerializerOptions _options;

    public SceneSerializer(ResourceManager resources, IAssetGuidResolver? guidResolver = null)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _guidResolver = guidResolver;
        _options = new JsonSerializerOptions { WriteIndented = true };
        _options.Converters.Add(new Vector2JsonConverter());
        _options.Converters.Add(new ColorJsonConverter());
        _options.Converters.Add(new RectangleJsonConverter());
        _options.Converters.Add(new JsonStringEnumConverter());
    }

    public string Serialize(SceneType scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        var file = new SceneFile { Name = scene.Name };

        var queue = new Queue<(GameObject go, Guid? parentId)>();
        foreach (var root in scene.RootObjects) queue.Enqueue((root, null));
        while (queue.Count > 0)
        {
            var (go, parentId) = queue.Dequeue();
            file.Objects.Add(ToData(go, parentId));
            foreach (var child in go.Transform.Children) queue.Enqueue((child.Owner, go.Id));
        }

        return System.Text.Json.JsonSerializer.Serialize(file, _options);
    }

    public SceneType Deserialize(string json)
    {
        var file = System.Text.Json.JsonSerializer.Deserialize<SceneFile>(json, _options)
            ?? throw new InvalidOperationException("场景反序列化结果为空。");
        var scene = new SceneType(file.Name);

        // 1) 按列表顺序重建所有对象（含组件与变换），记录 GUID→对象 映射。
        var byId = new Dictionary<Guid, GameObject>();
        foreach (var data in file.Objects)
        {
            var go = scene.CreateObject(data.Name);
            go.Id = data.Id;
            go.Active = data.Active;
            go.Transform.Position = data.Transform.Position;
            go.Transform.RotationDeg = data.Transform.RotationDeg;
            go.Transform.Scale = data.Transform.Scale;

            foreach (var cd in data.Components)
                RestoreComponent(go, cd);

            byId[data.Id] = go;
        }

        // 2) 重连父子层级。
        foreach (var data in file.Objects)
        {
            if (data.ParentId is Guid pid && byId.TryGetValue(pid, out var parent))
                byId[data.Id].Transform.SetParent(parent.Transform);
        }

        return scene;
    }

    /// <summary>序列化单个对象及其子孙为 JSON（供编辑器删除对象的撤销快照等用）。</summary>
    public string SerializeSubtree(GameObject root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var file = new SceneFile { Name = root.Name };

        var queue = new Queue<(GameObject go, Guid? parentId)>();
        queue.Enqueue((root, null));
        while (queue.Count > 0)
        {
            var (go, parentId) = queue.Dequeue();
            file.Objects.Add(ToData(go, parentId));
            foreach (var child in go.Transform.Children) queue.Enqueue((child.Owner, go.Id));
        }

        return System.Text.Json.JsonSerializer.Serialize(file, _options);
    }

    /// <summary>把子树 JSON 还原到指定场景（挂到 <paramref name="parent"/> 下），返回子树根对象。</summary>
    public GameObject DeserializeSubtree(string json, SceneType scene, GameObject? parent)
    {
        var file = System.Text.Json.JsonSerializer.Deserialize<SceneFile>(json, _options)
            ?? throw new InvalidOperationException("子树反序列化结果为空。");

        var byId = new Dictionary<Guid, GameObject>();
        foreach (var data in file.Objects)
        {
            var go = scene.CreateObject(data.Name);
            go.Id = data.Id;
            go.Active = data.Active;
            go.Transform.Position = data.Transform.Position;
            go.Transform.RotationDeg = data.Transform.RotationDeg;
            go.Transform.Scale = data.Transform.Scale;

            foreach (var cd in data.Components)
                RestoreComponent(go, cd);

            byId[data.Id] = go;
        }

        foreach (var data in file.Objects)
        {
            if (data.ParentId is Guid pid && byId.TryGetValue(pid, out var p))
                byId[data.Id].Transform.SetParent(p.Transform);
        }

        var root = byId.Values.First(o => o.Transform.Parent is null);
        if (parent is not null) root.Transform.SetParent(parent.Transform);
        return root;
    }

    private GameObjectData ToData(GameObject go, Guid? parentId)
    {
        var data = new GameObjectData
        {
            Id = go.Id,
            Name = go.Name,
            Active = go.Active,
            ParentId = parentId,
            Transform = new TransformData
            {
                Position = go.Transform.Position,
                RotationDeg = go.Transform.RotationDeg,
                Scale = go.Transform.Scale,
            },
        };

        foreach (var component in go.GetComponents())
        {
            data.Components.Add(new ComponentData
            {
                TypeName = component.GetType().AssemblyQualifiedName!,
                Properties = ToProperties(component),
                UiElements = ToUiElements(component),
            });
        }
        return data;
    }

    private List<PropertyValue> ToProperties(object target)
    {
        var result = new List<PropertyValue>();
        foreach (var prop in EnumerateProperties(target.GetType()))
        {
            var value = prop.GetValue(target);
            if (value is ITexture texture)
            {
                // 纹理存 GUID + 路径（GUID 主引用，改名/移动后靠 GUID 重连；无解析器时退化为路径）。
                var path = _resources.GetPath(texture);
                if (_guidResolver is not null && path is not null)
                {
                    var reference = new AssetReference { Guid = _guidResolver.GetGuid(path), Path = path };
                    result.Add(new PropertyValue { Name = prop.Name, Value = System.Text.Json.JsonSerializer.SerializeToElement(reference, typeof(AssetReference), _options) });
                }
                else
                {
                    result.Add(new PropertyValue { Name = prop.Name, Value = System.Text.Json.JsonSerializer.SerializeToElement(path, _options) });
                }
            }
            else
            {
                result.Add(new PropertyValue { Name = prop.Name, Value = System.Text.Json.JsonSerializer.SerializeToElement(value, prop.PropertyType, _options) });
            }
        }
        return result;
    }

    private void RestoreComponent(GameObject go, ComponentData data)
    {
        var type = Type.GetType(data.TypeName)
            ?? throw new InvalidOperationException($"无法解析组件类型 \"{data.TypeName}\"。");
        var component = go.AddComponent(type);

        foreach (var pv in data.Properties)
            SetPropertyValue(component, type, pv);

        if (data.UiElements.Count > 0)
            RestoreUiElements(component, data.UiElements);
    }

    /// <summary>把单个属性值写回目标对象（含 ITexture→路径/GUID 重连）。</summary>
    private void SetPropertyValue(object target, Type type, PropertyValue pv)
    {
        var prop = type.GetProperty(pv.Name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"类型 {type} 上不存在属性 \"{pv.Name}\"。");

        if (typeof(ITexture).IsAssignableFrom(prop.PropertyType))
        {
            string? path = null;
            if (pv.Value.ValueKind == JsonValueKind.Object)
            {
                // 新格式：{ guid, path }，优先按 GUID 解析（改名/移动后仍能定位）。
                var reference = pv.Value.Deserialize<AssetReference>(_options);
                if (reference is not null)
                    path = _guidResolver?.GetPath(reference.Guid) ?? reference.Path;
            }
            else if (pv.Value.ValueKind == JsonValueKind.String)
            {
                // 旧格式：纯路径字符串。
                path = pv.Value.GetString();
            }
            prop.SetValue(target, string.IsNullOrEmpty(path) ? null : _resources.Load<ITexture>(path));
        }
        else
        {
            prop.SetValue(target, pv.Value.Deserialize(prop.PropertyType, _options));
        }
    }

    // ---- UI 元素树（反射式，避免 Resources 引用 UI 程序集） ----

    /// <summary>检测组件是否为 UI 容器（拥有可读的 Roots 属性），并序列化其根元素列表。</summary>
    private List<UiElementData> ToUiElements(Component component)
    {
        var result = new List<UiElementData>();
        var roots = component.GetType().GetProperty("Roots", BindingFlags.Public | BindingFlags.Instance);
        if (roots is null || !roots.CanRead) return result;
        if (roots.GetValue(component) is not IEnumerable list) return result;

        foreach (var root in list)
            result.Add(ToUiElementData(root));
        return result;
    }

    private UiElementData ToUiElementData(object element)
    {
        var type = element.GetType();
        var data = new UiElementData
        {
            TypeName = type.AssemblyQualifiedName!,
            Properties = ToProperties(element),
        };

        var children = type.GetProperty("Children", BindingFlags.Public | BindingFlags.Instance);
        if (children?.GetValue(element) is IEnumerable list)
        {
            foreach (var child in list)
            {
                if (IsAutoGenerated(child)) continue;
                data.Children.Add(ToUiElementData(child));
            }
        }
        return data;
    }

    private static bool IsAutoGenerated(object element)
    {
        var prop = element.GetType().GetProperty("IsAutoGenerated", BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(element) is true;
    }

    private void RestoreUiElements(object canvas, List<UiElementData> elements)
    {
        var addRoot = canvas.GetType().GetMethod("AddRoot", BindingFlags.Public | BindingFlags.Instance);
        if (addRoot is null) return;

        foreach (var data in elements)
            addRoot.Invoke(canvas, new[] { RestoreUiElement(data) });
    }

    private object RestoreUiElement(UiElementData data)
    {
        var type = Type.GetType(data.TypeName)
            ?? throw new InvalidOperationException($"无法解析 UI 元素类型 \"{data.TypeName}\"。");
        var element = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"无法创建 UI 元素 \"{data.TypeName}\"。");

        foreach (var pv in data.Properties)
            SetPropertyValue(element, type, pv);

        var addChild = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "AddChild" && !m.IsGenericMethod);
        if (addChild is not null)
        {
            foreach (var child in data.Children)
                addChild.Invoke(element, new[] { RestoreUiElement(child) });
        }
        return element;
    }

    private static IEnumerable<PropertyInfo> EnumerateProperties(Type type)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length != 0) continue;
            if (!prop.CanRead) continue;

            // 只序列化「公开可写」的数据属性；internal setter（如 Component.GameObject）与只读属性排除。
            if (prop.SetMethod is null || !prop.SetMethod.IsPublic) continue;

            // 防御：跳过指向活对象（组件/对象/变换）的属性，避免序列化出对象图环。
            var pt = prop.PropertyType;
            if (typeof(Component).IsAssignableFrom(pt) || pt == typeof(GameObject) || pt == typeof(Transform)) continue;

            yield return prop;
        }
    }
}
