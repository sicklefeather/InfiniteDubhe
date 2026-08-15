using Xunit;

namespace InfiniteDubhe.Resources.Tests;

public sealed class AssetBundleTests
{
    [Fact]
    public void Pack_Load_RoundTripsFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "id_bundle_" + Guid.NewGuid().ToString("N"));
        var contentDir = Path.Combine(tempDir, "Content");
        Directory.CreateDirectory(Path.Combine(contentDir, "textures"));
        try
        {
            File.WriteAllText(Path.Combine(contentDir, "textures", "a.txt"), "hello");
            File.WriteAllText(Path.Combine(contentDir, "b.txt"), "world");

            var packagePath = Path.Combine(tempDir, "content.dubhe");
            AssetBundle.Pack(contentDir, packagePath);
            Assert.True(File.Exists(packagePath));

            var bundle = AssetBundle.Load(packagePath);
            Assert.Contains("textures/a.txt", bundle.Paths);
            Assert.Contains("b.txt", bundle.Paths);

            var fs = bundle.CreateFileSystem();
            using var reader = new StreamReader(fs.OpenRead("textures/a.txt"));
            Assert.Equal("hello", reader.ReadToEnd());
            Assert.True(fs.Exists("b.txt"));
            Assert.False(fs.Exists("missing.txt"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_InvalidFile_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "id_bundle_bad_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var path = Path.Combine(tempDir, "bad.dubhe");
            File.WriteAllText(path, "not a bundle");
            Assert.ThrowsAny<Exception>(() => AssetBundle.Load(path));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Pack_WithFilter_ExcludesFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "id_bundle_filter_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "a.txt"), "hello");
            File.WriteAllText(Path.Combine(tempDir, "a.txt.meta"), "meta");

            var bundle = AssetBundle.PackInMemory(tempDir, p => !p.EndsWith(".meta"));
            Assert.Contains("a.txt", bundle.Paths);
            Assert.DoesNotContain("a.txt.meta", bundle.Paths);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
