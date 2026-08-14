namespace InfiniteDubhe.Scene;

/// <summary>游戏对象：标识 + 组件列表 + 父子层级。对象本身无逻辑。</summary>
public sealed class GameObject
{
    private readonly List<Component> _components = new();

    public string Name { get; set; }
    public Transform Transform { get; }
    public Scene Scene { get; }
    public Guid Id { get; }

    private bool _active = true;
    /// <summary>激活开关（影响子节点）。</summary>
    public bool Active
    {
        get => _active;
        set => _active = value;
    }

    internal bool ActiveInHierarchy => _active && (Transform.Parent is null || Transform.Parent.Owner.ActiveInHierarchy);

    internal GameObject(Scene scene, string name)
    {
        Scene = scene;
        Name = name;
        Id = Guid.NewGuid();
        Transform = new Transform { Owner = this };
        scene.RegisterInternal(this);
    }

    public T AddComponent<T>() where T : Component, new()
    {
        var component = new T { GameObject = this };
        _components.Add(component);
        component.DoAwake();
        return component;
    }

    public T? GetComponent<T>() where T : Component
        => _components.OfType<T>().FirstOrDefault();

    public bool TryGetComponent<T>(out T? component) where T : Component
    {
        component = _components.OfType<T>().FirstOrDefault();
        return component is not null;
    }

    public IEnumerable<Component> GetComponents() => _components;

    /// <summary>建立父子层级。</summary>
    public GameObject CreateChild(string name)
    {
        var child = new GameObject(Scene, name);
        child.Transform.SetParent(Transform);
        return child;
    }

    /// <summary>延迟到本帧末销毁（含子对象与组件）。</summary>
    public void Destroy() => Scene.DestroyDeferred(this);
}
