using InfiniteDubhe.Core;

namespace InfiniteDubhe.Resources;

/// <summary>
/// 纹理工厂：由宿主把解码后的 RGBA 像素上传为 GPU 纹理（实现 <see cref="ITexture"/>）。
/// 该回调由引擎运行时注入（绑定到渲染器），使 Resources 无需直接依赖具体 GPU 后端。
/// </summary>
public delegate ITexture TextureFactory(int width, int height, byte[] rgba);
