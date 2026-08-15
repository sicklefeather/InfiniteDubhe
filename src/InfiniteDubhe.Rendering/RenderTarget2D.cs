using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.Rendering;

/// <summary>
/// 离屏渲染目标（D3D11）。同时可作为渲染目标（RTV）与纹理采样（SRV），
/// 供编辑器视口等场景先渲染到纹理、再作为普通纹理呈现。实现 <see cref="ITexture"/>。
/// </summary>
public sealed unsafe class RenderTarget2D : ITexture, IDisposable
{
    private ComPtr<ID3D11Texture2D> _texture;
    private ComPtr<ID3D11RenderTargetView> _rtv;
    private ComPtr<ID3D11ShaderResourceView> _srv;

    public int Width { get; }
    public int Height { get; }

    /// <summary>渲染目标视图（供 <see cref="Renderer"/> 绑定绘制）。</summary>
    internal ComPtr<ID3D11RenderTargetView> Rtv => _rtv;

    /// <summary>着色器资源视图（供 SpriteBatch / 编辑器 UI 采样）。</summary>
    internal ComPtr<ID3D11ShaderResourceView> Srv => _srv;

    public RenderTarget2D(ComPtr<ID3D11Device> device, int width, int height)
    {
        Width = width;
        Height = height;

        var desc = new Texture2DDesc
        {
            Width = (uint)width,
            Height = (uint)height,
            Format = Format.FormatR8G8B8A8Unorm,
            MipLevels = 1,
            ArraySize = 1,
            BindFlags = (uint)(BindFlag.RenderTarget | BindFlag.ShaderResource),
            Usage = Usage.Default,
            CPUAccessFlags = 0,
            MiscFlags = (uint)ResourceMiscFlag.None,
            SampleDesc = new SampleDesc(1, 0),
        };

        SilkMarshal.ThrowHResult(device.CreateTexture2D(in desc, (SubresourceData*)null, ref _texture));
        SilkMarshal.ThrowHResult(device.CreateRenderTargetView(_texture, null, ref _rtv));

        var srvDesc = new ShaderResourceViewDesc
        {
            Format = desc.Format,
            ViewDimension = D3DSrvDimension.D3DSrvDimensionTexture2D,
            Anonymous = new ShaderResourceViewDescUnion
            {
                Texture2D = { MostDetailedMip = 0, MipLevels = 1 },
            },
        };
        SilkMarshal.ThrowHResult(device.CreateShaderResourceView(_texture, in srvDesc, ref _srv));
    }

    public void Dispose()
    {
        _srv.Dispose();
        _rtv.Dispose();
        _texture.Dispose();
    }
}
