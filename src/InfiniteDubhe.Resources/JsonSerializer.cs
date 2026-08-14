using System.Text.Json;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.Resources;

/// <summary>基于 System.Text.Json 的 <see cref="ISerializer"/> 实现（引擎序列化门面）。</summary>
public sealed class JsonSerializer : ISerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonSerializer(JsonSerializerOptions? options = null)
        => _options = options ?? new JsonSerializerOptions { WriteIndented = true };

    public string Serialize<T>(T value) => System.Text.Json.JsonSerializer.Serialize(value, _options);

    public T? Deserialize<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json, _options);

    public object? Deserialize(string json, Type type) => System.Text.Json.JsonSerializer.Deserialize(json, type, _options);
}
