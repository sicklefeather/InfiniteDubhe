using InfiniteDubhe.Platform;
using InfiniteDubhe.Scene;
using SceneType = InfiniteDubhe.Scene.Scene;

namespace InfiniteDubhe.Resources;

/// <summary>
/// 场景文件加载/保存：封装「读文件 → 反序列化 → 引用重连」全过程。
/// 场景是资源的一种，故落在 Resources 层（<see cref="SceneManager"/> 只做切换）。
/// </summary>
public sealed class SceneLoader
{
    private readonly IFileSystem _fileSystem;
    private readonly SceneSerializer _serializer;

    public SceneLoader(IFileSystem fileSystem, SceneSerializer serializer)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public SceneType LoadScene(string path)
    {
        using var stream = _fileSystem.OpenRead(path);
        using var reader = new StreamReader(stream);
        return _serializer.Deserialize(reader.ReadToEnd());
    }

    /// <summary>指定路径的场景文件是否存在。</summary>
    public bool Exists(string path) => _fileSystem.Exists(path);

    /// <summary>把场景序列化为 JSON 文本（不落盘），供编辑器做脏状态比对。</summary>
    public string Serialize(SceneType scene) => _serializer.Serialize(scene);

    public void SaveScene(SceneType scene, string path)
    {
        ArgumentNullException.ThrowIfNull(scene);
        var json = _serializer.Serialize(scene);
        var fullPath = _fileSystem.GetFullPath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, json);
    }
}
