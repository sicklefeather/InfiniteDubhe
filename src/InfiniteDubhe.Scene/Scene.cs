using InfiniteDubhe.Core;

namespace InfiniteDubhe.Scene;

/// <summary>场景：运行时对象的容器。由 <see cref="SceneManager"/> 驱动生命周期。</summary>
public sealed class Scene
{
    private readonly List<GameObject> _all = new();
    private readonly List<GameObject> _pendingDestroy = new();

    public string Name { get; }

    /// <summary>根级对象（父为 null）。</summary>
    public IReadOnlyList<GameObject> RootObjects => _all.Where(o => o.Transform.Parent is null).ToArray();

    public Scene(string name) => Name = name;

    public GameObject CreateObject(string name) => new(this, name);

    internal void RegisterInternal(GameObject go) => _all.Add(go);

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
