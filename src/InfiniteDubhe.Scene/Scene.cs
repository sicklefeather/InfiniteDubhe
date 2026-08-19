using InfiniteDubhe.Core;

namespace InfiniteDubhe.Scene;

/// <summary>场景：运行时对象的容器。由 <see cref="SceneManager"/> 驱动生命周期。</summary>
public sealed class Scene
{
    private readonly List<GameObject> _all = new();
    private readonly List<GameObject> _pendingDestroy = new();

    /// <summary>场景名。不入 JSON：加载时以文件名命名，保存后同步为文件名（重命名场景 = 重命名文件）。</summary>
    public string Name { get; set; }

    /// <summary>根级对象（父为 null）。</summary>
    public IReadOnlyList<GameObject> RootObjects => _all.Where(o => o.Transform.Parent is null).ToArray();

    public Scene(string name) => Name = name;

    public GameObject CreateObject(string name) => new(this, name);

    internal void RegisterInternal(GameObject go) => _all.Add(go);

    /// <summary>获取对象在同级中的索引（根级按根列表，子级按父级子列表）。</summary>
    internal int GetSiblingIndex(GameObject go)
    {
        if (go.Transform.Parent is not null)
        {
            var children = go.Transform.Parent.Children;
            for (int i = 0; i < children.Count; i++)
                if (ReferenceEquals(children[i], go.Transform)) return i;
            return 0;
        }
        var roots = _all.Where(o => o.Transform.Parent is null).ToList();
        for (int i = 0; i < roots.Count; i++)
            if (ReferenceEquals(roots[i], go)) return i;
        return 0;
    }

    /// <summary>把对象移动到同级中的指定索引（根级重排 _all，子级重排父级子列表）。</summary>
    internal void SetSiblingIndex(GameObject go, int index)
    {
        if (go.Transform.Parent is not null)
        {
            go.Transform.SetSiblingIndex(index);
            return;
        }

        var roots = _all.Where(o => o.Transform.Parent is null).ToList();
        int from = roots.IndexOf(go);
        if (from < 0) return;
        int to = Math.Clamp(index, 0, roots.Count - 1);
        if (from == to) return;

        _all.Remove(go);
        int anchor = _all.IndexOf(roots[to]);
        if (to > from) anchor++;
        _all.Insert(Math.Clamp(anchor, 0, _all.Count), go);
    }

    internal void DestroyDeferred(GameObject go)
    {
        if (!_pendingDestroy.Contains(go)) _pendingDestroy.Add(go);
    }

    internal void Update()
    {
        foreach (var go in _all)
        {
            var goActive = go.ActiveInHierarchy;
            foreach (var c in go.GetComponents())
            {
                var activeNow = c.Enabled && goActive;
                if (activeNow != c.WasActive)
                {
                    if (activeNow) c.DoOnEnable();
                    else c.DoOnDisable();
                    c.WasActive = activeNow;
                }
                if (!activeNow) continue;

                if (!c.Started)
                {
                    c.Started = true;
                    c.DoStart();
                }
                c.DoUpdate();
            }
        }
    }

    internal void FixedUpdate()
    {
        foreach (var go in _all)
        {
            if (!go.ActiveInHierarchy) continue;
            foreach (var c in go.GetComponents())
                if (c.Enabled) c.DoFixedUpdate();
        }
    }

    internal void EndOfFrame()
    {
        if (_pendingDestroy.Count == 0) return;
        var toDestroy = _pendingDestroy.ToArray();
        _pendingDestroy.Clear();
        foreach (var go in toDestroy) DestroyImmediate(go);
    }

    internal void CollectRenderables(ICollection<IRenderable> renderables)
    {
        foreach (var go in _all)
        {
            if (!go.ActiveInHierarchy) continue;
            foreach (var c in go.GetComponents())
                if (c.Enabled && c is IRenderable r) renderables.Add(r);
        }
    }

    private void DestroyImmediate(GameObject go)
    {
        foreach (var child in go.Transform.Children.Select(t => t.Owner).ToArray())
            DestroyImmediate(child);

        foreach (var c in go.GetComponents())
            c.DoOnDestroy();

        go.Transform.SetParent(null);
        _all.Remove(go);
    }
}
