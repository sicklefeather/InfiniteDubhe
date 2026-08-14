namespace InfiniteDubhe.Core;

/// <summary>
/// 序列化门面。引擎内部一律经此接口访问序列化能力，不直接引用具体 JSON 库。
/// 具体实现（如 System.Text.Json）位于 <c>Resources</c>。
/// </summary>
public interface ISerializer
{
    string Serialize<T>(T value);

    T? Deserialize<T>(string json);

    /// <summary>多态场景用（组件子类等）。</summary>
    object? Deserialize(string json, Type type);
}
