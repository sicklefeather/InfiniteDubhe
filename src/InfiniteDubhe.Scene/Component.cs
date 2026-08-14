namespace InfiniteDubhe.Scene;

/// <summary>
/// 组件基类：挂在 <see cref="GameObject"/> 上的数据 + 行为。
/// 生命周期回调由场景框架（同程序集）经 internal 包装调用；用户子类用 <c>protected override</c> 重写。
/// </summary>
public abstract class Component
{
    public GameObject GameObject { get; internal set; } = null!;

    /// <summary>快捷访问所属对象变换。</summary>
    public Transform Transform => GameObject.Transform;

    /// <summary>组件开关（控制 Update/FixedUpdate 是否调用）。</summary>
    public bool Enabled { get; set; } = true;

    internal bool Started;
    internal bool WasActive;

    internal bool IsActive => Enabled && GameObject.ActiveInHierarchy;

    protected virtual void Awake() { }
    protected virtual void OnEnable() { }
    protected virtual void Start() { }
    protected virtual void Update() { }
    protected virtual void FixedUpdate() { }
    protected virtual void OnDisable() { }
    protected virtual void OnDestroy() { }

    // 以下 internal 包装供场景框架（同程序集）调用，用户业务代码勿用。
    internal void DoAwake() => Awake();
    internal void DoOnEnable() => OnEnable();
    internal void DoStart() => Start();
    internal void DoUpdate() => Update();
    internal void DoFixedUpdate() => FixedUpdate();
    internal void DoOnDisable() => OnDisable();
    internal void DoOnDestroy() => OnDestroy();
}
