using StbImageSharp;
using InfiniteDubhe.Core;
using InfiniteDubhe.Platform;

namespace InfiniteDubhe.Resources;

/// <summary>
/// 纹理加载器：读文件 → StbImageSharp 解码为 RGBA → 经 <see cref="TextureFactory"/> 上传 GPU。
/// 支持 PNG/JPG/BMP/TGA/GIF 等常见格式。
/// </summary>
public sealed class TextureLoader : IResourceLoader<ITexture>
{
    private readonly IFileSystem _fileSystem;
    private readonly TextureFactory _factory;

    public TextureLoader(IFileSystem fileSystem, TextureFactory factory)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public ITexture Load(string path)
    {
        var bytes = ReadAllBytes(path);
        var image = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
        // _factory 同步上传到 GPU（复制像素），返回后 image 即可被 GC 回收。
        return _factory(image.Width, image.Height, image.Data);
    }

    private byte[] ReadAllBytes(string path)
    {
        using var stream = _fileSystem.OpenRead(path);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
