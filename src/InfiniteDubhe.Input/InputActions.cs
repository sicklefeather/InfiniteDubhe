using InfiniteDubhe.Core;

namespace InfiniteDubhe.Input;

/// <summary>
/// 动作映射：把命名动作（"Jump"/"MoveX" 等）绑定到键/鼠标键，便于改键与逻辑解耦。
/// 查询经 <see cref="Input"/> 门面聚合其所有绑定（任一绑定触发即触发）。
/// </summary>
public static class InputActions
{
    private sealed class Binding
    {
        public readonly List<Key> Keys = new();
        public readonly List<MouseButton> Buttons = new();
    }

    private static readonly Dictionary<string, Binding> Bindings = new(StringComparer.Ordinal);

    /// <summary>把动作绑定到一组键（重复绑定同名动作会追加键位）。</summary>
    public static void Bind(string name, params Key[] keys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var binding = GetOrAdd(name);
        foreach (var key in keys)
            if (!binding.Keys.Contains(key)) binding.Keys.Add(key);
    }

    /// <summary>把动作绑定到一组鼠标键。</summary>
    public static void Bind(string name, params MouseButton[] buttons)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var binding = GetOrAdd(name);
        foreach (var button in buttons)
            if (!binding.Buttons.Contains(button)) binding.Buttons.Add(button);
    }

    /// <summary>解除动作的全部绑定。</summary>
    public static void Unbind(string name) => Bindings.Remove(name);

    /// <summary>动作是否处于按下状态（持续）。</summary>
    public static bool IsDown(string name)
    {
        if (!Bindings.TryGetValue(name, out var binding)) return false;
        foreach (var key in binding.Keys)
            if (Input.IsKeyDown(key)) return true;
        foreach (var button in binding.Buttons)
            if (Input.IsMouseButtonDown(button)) return true;
        return false;
    }

    /// <summary>动作是否在本帧刚按下（边沿触发）。</summary>
    public static bool WasPressed(string name)
    {
        if (!Bindings.TryGetValue(name, out var binding)) return false;
        foreach (var key in binding.Keys)
            if (Input.IsKeyPressed(key)) return true;
        foreach (var button in binding.Buttons)
            if (Input.IsMouseButtonPressed(button)) return true;
        return false;
    }

    private static Binding GetOrAdd(string name)
    {
        if (!Bindings.TryGetValue(name, out var binding))
        {
            binding = new Binding();
            Bindings[name] = binding;
        }
        return binding;
    }
}
