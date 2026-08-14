using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.Rendering;

/// <summary>GPU 纹理（D3D11）。由 <see cref="Renderer.CreateTexture"/> 创建，实现 <see cref="ITexture"/>。</summary>
public sealed unsafe class Texture2D : ITexture, IDisposable
{
    private ComPtr<ID3D11Texture2D> _texture;
    private ComPtr<ID3D11ShaderResourceView> _srv;

    public int Width { get; }
    public int Height { get; }

    internal ComPtr<ID3D11ShaderResourceView> Srv => _srv;

    public Texture2D(ComPtr<ID3D11Device> device, int width, int height, ReadOnlySpan<byte> rgba)
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
            BindFlags = (uint)BindFlag.ShaderResource,
            Usage = Usage.Default,
            CPUAccessFlags = 0,
            MiscFlags = (uint)ResourceMiscFlag.None,
            SampleDesc = new SampleDesc(1, 0),
        };

        fixed (byte* p = rgba)
        {
            var srd = new SubresourceData
            {
                PSysMem = p,
                SysMemPitch = (uint)(width * 4),
                SysMemSlicePitch = 0,
            };
            SilkMarshal.ThrowHResult(device.CreateTexture2D(in desc, in srd, ref _texture));
        }

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
        _texture.Dispose();
    }
}
