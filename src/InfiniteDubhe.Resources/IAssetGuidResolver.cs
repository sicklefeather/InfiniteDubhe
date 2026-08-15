namespace InfiniteDubhe.Resources;

/// <summary>
/// 资源 GUID 解析器（由编辑器导入管线实现），供场景序列化用 GUID 稳定引用资源：
/// 改名/移动资源后，场景里的 GUID 仍能解析到新路径。
/// </summary>
public interface IAssetGuidResolver
{
    /// <summary>取路径对应的 GUID（无则创建 .meta 并返回新 GUID）。</summary>
    Guid GetGuid(string path);

    /// <summary>按 GUID 解析当前路径（找不到返回 null）。</summary>
    string? GetPath(Guid guid);
}
