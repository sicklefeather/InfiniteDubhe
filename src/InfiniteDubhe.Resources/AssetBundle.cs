using System.Text;
using InfiniteDubhe.Platform;

namespace InfiniteDubhe.Resources;

/// <summary>
/// 资源包（M3）：把一个资源目录打包成单个 `.dubhe` 文件，运行时从包内加载。
/// 通过 <see cref="CreateFileSystem"/> 提供 <see cref="IFileSystem"/>，可无缝替换磁盘文件系统
/// （<see cref="ResourceManager"/>、<see cref="SceneLoader"/> 等直接复用）。
/// </summary>
public sealed class AssetBundle
{
    private const uint Magic = 0x48425544; // "DUBH"（小端）
    private const int Version = 1;

    private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

    /// <summary>包内资源路径（相对资源根的 '/' 分隔路径）。</summary>
    public IReadOnlyCollection<string> Paths => _entries.Keys;

    /// <summary>把一个目录下所有文件打包到 <paramref name="outputPath"/>。</summary>
    public static void Pack(string rootDir, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        PackInMemory(rootDir).WriteToFile(outputPath);
    }

    /// <summary>打包目录到内存（构建工具/测试用）。</summary>
    public static AssetBundle PackInMemory(string rootDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDir);
        var bundle = new AssetBundle();
        foreach (var file in Directory.EnumerateFiles(rootDir, "*.*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(rootDir, file).Replace('\\', '/');
            bundle._entries[relative] = File.ReadAllBytes(file);
        }
        return bundle;
    }

    /// <summary>从包文件加载到内存。</summary>
    public static AssetBundle Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    /// <summary>把包内容作为 <see cref="IFileSystem"/> 提供给引擎子系统。</summary>
    public IFileSystem CreateFileSystem() => new BundleFileSystem(this);

    public bool Contains(string path) => _entries.ContainsKey(path);

    internal byte[] GetBytes(string path) => _entries[path];

    private void WriteToFile(string path)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(_entries.Count);
        foreach (var (name, data) in _entries)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            writer.Write(nameBytes.Length);
            writer.Write(nameBytes);
            writer.Write(data.Length);
            writer.Write(data);
        }
    }

    private static AssetBundle Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        if (reader.ReadUInt32() != Magic)
            throw new InvalidDataException("不是有效的资源包文件。");
        if (reader.ReadInt32() != Version)
            throw new InvalidDataException($"资源包版本不受支持（当前 {Version}）。");

        var bundle = new AssetBundle();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            int nameLen = reader.ReadInt32();
            var name = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
            int dataLen = reader.ReadInt32();
            bundle._entries[name] = reader.ReadBytes(dataLen);
        }
        return bundle;
    }

    private sealed class BundleFileSystem : IFileSystem
    {
        private readonly AssetBundle _bundle;

        public BundleFileSystem(AssetBundle bundle) => _bundle = bundle;

        public Stream OpenRead(string path) => new MemoryStream(_bundle.GetBytes(path), writable: false);

        public bool Exists(string path) => _bundle.Contains(path);

        public string GetFullPath(string relative) => relative; // 包内路径即相对路径
    }
}
