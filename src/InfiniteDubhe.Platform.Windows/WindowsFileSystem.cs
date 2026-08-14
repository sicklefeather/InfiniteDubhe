using InfiniteDubhe.Platform;

namespace InfiniteDubhe.Platform.Windows;

/// <summary>基于磁盘的文件系统实现。</summary>
public sealed class WindowsFileSystem : IFileSystem
{
    private readonly string _root;

    public WindowsFileSystem(string root) => _root = root;

    public Stream OpenRead(string path) => File.OpenRead(GetFullPath(path));

    public bool Exists(string path) => File.Exists(GetFullPath(path));

    public string GetFullPath(string relative) => Path.Combine(_root, relative);
}
