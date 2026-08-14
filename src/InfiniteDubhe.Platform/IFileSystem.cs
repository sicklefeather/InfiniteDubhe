namespace InfiniteDubhe.Platform;

/// <summary>文件系统抽象。路径相对资源根目录。</summary>
public interface IFileSystem
{
    Stream OpenRead(string path);

    bool Exists(string path);

    string GetFullPath(string relative);
}
