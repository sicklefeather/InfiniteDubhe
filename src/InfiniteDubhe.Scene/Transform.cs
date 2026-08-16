using System.Numerics;

namespace InfiniteDubhe.Scene;

/// <summary>
/// 2D 变换（位置/旋转/缩放 + 父子层级）。旋转单位：度；矩阵用 <see cref="Matrix3x2"/>。
/// MVP 仅组合父级平移到世界坐标；父级旋转/缩放对子级的影响留待后续（当前层级用于组织与销毁）。
/// </summary>
public sealed class Transform
{
    private readonly List<Transform> _children = new();

    internal GameObject Owner { get; set; } = null!;

    public Vector2 Position { get; set; }
    public float RotationDeg { get; set; }
    public Vector2 Scale { get; set; } = Vector2.One;

    public Transform? Parent { get; private set; }
    public IReadOnlyList<Transform> Children => _children;

    /// <summary>世界坐标（累加父级平移）。</summary>
    public Vector2 WorldPosition => Parent is null ? Position : Vector2.Transform(Position, Parent.LocalToWorld);

    /// <summary>本地到世界矩阵（TRS 自底向上组合）。</summary>
    public Matrix3x2 LocalToWorld => Parent is null ? LocalMatrix : LocalMatrix * Parent.LocalToWorld;

    private Matrix3x2 LocalMatrix =>
        Matrix3x2.CreateScale(Scale) *
        Matrix3x2.CreateRotation(RotationDeg * MathF.PI / 180f) *
        Matrix3x2.CreateTranslation(Position);

    /// <summary>重新挂载；传 null 移到根级。</summary>
    public void SetParent(Transform? parent)
    {
        if (parent == this) throw new InvalidOperationException("不能把 Transform 挂到自己身上。");
        if (parent is not null && IsAncestorOf(parent))
            throw new InvalidOperationException("不能建立循环父子关系。");

        Parent?._children.Remove(this);
        Parent = parent;
        Parent?._children.Add(this);
    }

    /// <summary>在同级中移动到指定索引（供编辑器拖拽排序 / 撤销恢复位置）。越界则夹到末尾。</summary>
    public void SetSiblingIndex(int index)
    {
        if (Parent is null || !Parent._children.Remove(this)) return;
        Parent._children.Insert(Math.Clamp(index, 0, Parent._children.Count), this);
    }

    private bool IsAncestorOf(Transform t)
    {
        for (var p = t.Parent; p is not null; p = p.Parent)
            if (p == this) return true;
        return false;
    }
}
