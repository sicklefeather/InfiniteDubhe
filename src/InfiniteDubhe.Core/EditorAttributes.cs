namespace InfiniteDubhe.Core;

/// <summary>
/// 编辑器特性标注（G-03）。供编辑器 Inspector 决定属性的展示与编辑方式，
/// 对游戏运行时零影响。定义在 Core，使所有组件程序集都能引用。
/// </summary>

/// <summary>标记该属性不显示在 Inspector 中。</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class HideInInspectorAttribute : Attribute
{
}

/// <summary>数值滑杆范围（float/int）。</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class RangeAttribute : Attribute
{
    public float Min { get; }
    public float Max { get; }

    public RangeAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}

/// <summary>在 Inspector 中给下方属性加分组标题。</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class HeaderAttribute : Attribute
{
    public string Label { get; }

    public HeaderAttribute(string label) => Label = label;
}

/// <summary>属性悬停提示。</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class TooltipAttribute : Attribute
{
    public string Text { get; }

    public TooltipAttribute(string text) => Text = text;
}
