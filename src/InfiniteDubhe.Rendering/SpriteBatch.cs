using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.Rendering;

/// <summary>
/// 精灵批处理器。收集 <see cref="SpriteDrawCommand"/>，按（层、层内深度、纹理）排序，
/// 同纹理合并为单次 Draw Call。顶点在 CPU 侧预变换到裁剪空间（无常量缓冲）。
/// </summary>
public sealed unsafe class SpriteBatch : ICollection<SpriteDrawCommand>, IDisposable
{
    private const int MaxSprites = 2048;
    private const int MaxVertices = MaxSprites * 4;
    private const int MaxIndices = MaxSprites * 6;

    private readonly ComPtr<ID3D11Device> _device;
    private readonly ComPtr<ID3D11DeviceContext> _context;

    private ComPtr<ID3D11VertexShader> _vertexShader;
    private ComPtr<ID3D11PixelShader> _pixelShader;
    private ComPtr<ID3D11InputLayout> _inputLayout;
    private ComPtr<ID3D11BlendState> _blendState;
    private ComPtr<ID3D11RasterizerState> _rasterizerState;
    private ComPtr<ID3D11SamplerState> _samplerState;

    private readonly List<SpriteDrawCommand> _commands = new();
    private readonly SpriteVertex[] _verts = new SpriteVertex[MaxVertices];
    private readonly uint[] _indices = new uint[MaxIndices];
    private Camera? _camera;

    private const string ShaderSource = @"
struct vs_in {
    float3 position : POSITION;
    float4 color    : COLOR0;
    float2 texcoord : TEXCOORD0;
};

struct vs_out {
    float4 position_clip : SV_POSITION;
    float4 color         : COLOR0;
    float2 texcoord      : TEXCOORD0;
};

vs_out vs_main(vs_in input) {
    vs_out output;
    output.position_clip = float4(input.position, 1.0);
    output.color = input.color;
    output.texcoord = input.texcoord;
    return output;
}

Texture2D tex : register(t0);
SamplerState samp : register(s0);

float4 ps_main(vs_out input) : SV_TARGET {
    return tex.Sample(samp, input.texcoord) * input.color;
}
";

    [StructLayout(LayoutKind.Sequential)]
    private struct SpriteVertex
    {
        public float X, Y, Z;
        public float R, G, B, A;
        public float U, V;
    }

    public SpriteBatch(ComPtr<ID3D11Device> device, ComPtr<ID3D11DeviceContext> context)
    {
        _device = device;
        _context = context;
        CreateDeviceObjects();
    }

    public void Begin(Camera camera)
    {
        _camera = camera;
        _commands.Clear();
    }

    public void Draw(ITexture texture, Vector2 position, Color color)
    {
        if (texture is null) return;
        _commands.Add(new SpriteDrawCommand
        {
            Texture = texture,
            SourceRect = default,
            Position = position,
            Rotation = 0f,
            Origin = Vector2.Zero,
            Scale = Vector2.One,
            Color = color,
            Effects = SpriteEffects.None,
            Layer = 0,
            LayerDepth = 0f,
        });
    }

    public void Draw(in SpriteDrawCommand command) => _commands.Add(command);

    public void End()
    {
        if (_commands.Count == 0 || _camera is null) return;

        _commands.Sort(static (a, b) =>
        {
            var c = a.Layer.CompareTo(b.Layer);
            if (c != 0) return c;
            c = a.LayerDepth.CompareTo(b.LayerDepth);
            if (c != 0) return c;
            return RuntimeHelpers.GetHashCode(a.Texture!).CompareTo(RuntimeHelpers.GetHashCode(b.Texture!));
        });

        BindPipeline();
        var view = _camera.ViewMatrix;

        var vertCount = 0;
        var indexCount = 0;
        ITexture? currentTexture = null;

        foreach (var cmd in _commands)
        {
            var tex = cmd.Texture;
            if (tex is null) continue;

            if (currentTexture is not null && !ReferenceEquals(currentTexture, tex))
            {
                Flush(currentTexture, vertCount, indexCount);
                vertCount = 0;
                indexCount = 0;
            }
            currentTexture = tex;

            EmitQuad(in cmd, view, ref vertCount, ref indexCount);
        }

        if (currentTexture is not null && vertCount > 0)
            Flush(currentTexture, vertCount, indexCount);
    }

    public void Dispose()
    {
        _vertexShader.Dispose();
        _pixelShader.Dispose();
        _inputLayout.Dispose();
        _blendState.Dispose();
        _rasterizerState.Dispose();
        _samplerState.Dispose();
    }

    private void Flush(ITexture texture, int vertCount, int indexCount)
    {
        // MVP：每次同纹理批次重建缓冲（简化、正确优先）。后续用动态缓冲池优化（NFR-03）。
        ComPtr<ID3D11Buffer> vb = default;
        var vbDesc = new BufferDesc
        {
            ByteWidth = (uint)(vertCount * sizeof(SpriteVertex)),
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.VertexBuffer,
        };
        fixed (SpriteVertex* v = _verts)
        {
            var srd = new SubresourceData { PSysMem = v };
            SilkMarshal.ThrowHResult(_device.CreateBuffer(in vbDesc, in srd, ref vb));
        }

        ComPtr<ID3D11Buffer> ib = default;
        var ibDesc = new BufferDesc
        {
            ByteWidth = (uint)(indexCount * sizeof(uint)),
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.IndexBuffer,
        };
        fixed (uint* i = _indices)
        {
            var srd = new SubresourceData { PSysMem = i };
            SilkMarshal.ThrowHResult(_device.CreateBuffer(in ibDesc, in srd, ref ib));
        }

        uint stride = (uint)sizeof(SpriteVertex), offset = 0;
        _context.IASetVertexBuffers(0, 1, ref vb, in stride, in offset);
        _context.IASetIndexBuffer(ib, Format.FormatR32Uint, 0);
        var srv = ((Texture2D)texture).Srv;
        _context.PSSetShaderResources(0, 1, ref srv);
        _context.DrawIndexed((uint)indexCount, 0, 0);
        Profiler.RecordDrawCall();

        vb.Dispose();
        ib.Dispose();
    }

    private void EmitQuad(in SpriteDrawCommand cmd, Matrix3x2 view, ref int vertCount, ref int indexCount)
    {
        var tex = cmd.Texture!;
        int sx = cmd.SourceRect.X;
        int sy = cmd.SourceRect.Y;
        int sw = cmd.SourceRect.IsEmpty ? tex.Width : cmd.SourceRect.Width;
        int sh = cmd.SourceRect.IsEmpty ? tex.Height : cmd.SourceRect.Height;

        float u0 = (float)sx / tex.Width;
        float v0 = (float)sy / tex.Height;
        float u1 = (float)(sx + sw) / tex.Width;
        float v1 = (float)(sy + sh) / tex.Height;

        if ((cmd.Effects & SpriteEffects.FlipHorizontally) != 0) (u0, u1) = (u1, u0);
        if ((cmd.Effects & SpriteEffects.FlipVertically) != 0) (v0, v1) = (v1, v0);

        float w = sw * cmd.Scale.X;
        float h = sh * cmd.Scale.Y;
        float cos = MathF.Cos(cmd.Rotation);
        float sin = MathF.Sin(cmd.Rotation);

        var tl = Vector2.Transform(Rotate(-cmd.Origin.X, -cmd.Origin.Y, cos, sin) + cmd.Position, view);
        var tr = Vector2.Transform(Rotate(w - cmd.Origin.X, -cmd.Origin.Y, cos, sin) + cmd.Position, view);
        var br = Vector2.Transform(Rotate(w - cmd.Origin.X, h - cmd.Origin.Y, cos, sin) + cmd.Position, view);
        var bl = Vector2.Transform(Rotate(-cmd.Origin.X, h - cmd.Origin.Y, cos, sin) + cmd.Position, view);

        uint baseIndex = (uint)vertCount;

        verts(vertCount++, tl, u0, v0, cmd.Color);
        verts(vertCount++, tr, u1, v0, cmd.Color);
        verts(vertCount++, br, u1, v1, cmd.Color);
        verts(vertCount++, bl, u0, v1, cmd.Color);

        _indices[indexCount++] = baseIndex + 0;
        _indices[indexCount++] = baseIndex + 1;
        _indices[indexCount++] = baseIndex + 2;
        _indices[indexCount++] = baseIndex + 0;
        _indices[indexCount++] = baseIndex + 2;
        _indices[indexCount++] = baseIndex + 3;
    }

    private void verts(int i, Vector2 p, float u, float v, Color color)
    {
        _verts[i] = new SpriteVertex { X = p.X, Y = p.Y, Z = 0f, R = color.R, G = color.G, B = color.B, A = color.A, U = u, V = v };
    }

    private static Vector2 Rotate(float x, float y, float cos, float sin)
        => new(cos * x - sin * y, sin * x + cos * y);

    private void BindPipeline()
    {
        _context.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
        _context.IASetInputLayout(_inputLayout);
        _context.VSSetShader(_vertexShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
        _context.PSSetShader(_pixelShader, ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
        _context.PSSetSamplers(0, 1, ref _samplerState);
        _context.OMSetBlendState(_blendState, null, 0xffffffff);
        _context.RSSetState(_rasterizerState);
    }

    private void CreateDeviceObjects()
    {
        CreateShaders();
        CreateBlendState();
        CreateRasterizerState();
        CreateSamplerState();
    }

    private void CreateShaders()
    {
        using var compiler = D3DCompiler.GetApi();
        var shaderBytes = Encoding.ASCII.GetBytes(ShaderSource);

        ComPtr<ID3D10Blob> vsCode = default, vsErrors = default;
        var hr = compiler.Compile(in shaderBytes[0], (nuint)shaderBytes.Length, "SpriteShader", null,
            ref Unsafe.NullRef<ID3DInclude>(), "vs_main", "vs_5_0", 0, 0, ref vsCode, ref vsErrors);
        if (hr < 0)
        {
            if (vsErrors.Handle is not null)
                Console.Error.WriteLine(SilkMarshal.PtrToString((nint)vsErrors.GetBufferPointer()));
            SilkMarshal.ThrowHResult(hr);
        }
        SilkMarshal.ThrowHResult(_device.CreateVertexShader(vsCode.GetBufferPointer(), vsCode.GetBufferSize(),
            ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref _vertexShader));

        ComPtr<ID3D10Blob> psCode = default, psErrors = default;
        hr = compiler.Compile(in shaderBytes[0], (nuint)shaderBytes.Length, "SpriteShader", null,
            ref Unsafe.NullRef<ID3DInclude>(), "ps_main", "ps_5_0", 0, 0, ref psCode, ref psErrors);
        if (hr < 0)
        {
            if (psErrors.Handle is not null)
                Console.Error.WriteLine(SilkMarshal.PtrToString((nint)psErrors.GetBufferPointer()));
            SilkMarshal.ThrowHResult(hr);
        }
        SilkMarshal.ThrowHResult(_device.CreatePixelShader(psCode.GetBufferPointer(), psCode.GetBufferSize(),
            ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref _pixelShader));

        CreateInputLayout(vsCode);

        vsCode.Dispose();
        vsErrors.Dispose();
        psCode.Dispose();
        psErrors.Dispose();
    }

    private void CreateInputLayout(ComPtr<ID3D10Blob> vsCode)
    {
        var posName = Encoding.ASCII.GetBytes("POSITION\0");
        var colName = Encoding.ASCII.GetBytes("COLOR\0");
        var texName = Encoding.ASCII.GetBytes("TEXCOORD\0");

        fixed (byte* pos = posName, col = colName, tex = texName)
        {
            var elements = new InputElementDesc[]
            {
                new()
                {
                    SemanticName = pos,
                    SemanticIndex = 0,
                    Format = Format.FormatR32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0,
                },
                new()
                {
                    SemanticName = col,
                    SemanticIndex = 0,
                    Format = Format.FormatR32G32B32A32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 12,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0,
                },
                new()
                {
                    SemanticName = tex,
                    SemanticIndex = 0,
                    Format = Format.FormatR32G32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 28,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0,
                },
            };

            SilkMarshal.ThrowHResult(_device.CreateInputLayout(in elements[0], (uint)elements.Length,
                vsCode.GetBufferPointer(), vsCode.GetBufferSize(), ref _inputLayout));
        }
    }

    private void CreateBlendState()
    {
        var desc = new BlendDesc
        {
            AlphaToCoverageEnable = false,
            IndependentBlendEnable = false,
        };
        desc.RenderTarget[0] = new RenderTargetBlendDesc
        {
            BlendEnable = true,
            SrcBlend = Blend.SrcAlpha,
            DestBlend = Blend.InvSrcAlpha,
            BlendOp = BlendOp.Add,
            SrcBlendAlpha = Blend.One,
            DestBlendAlpha = Blend.InvSrcAlpha,
            BlendOpAlpha = BlendOp.Add,
            RenderTargetWriteMask = 0xF,
        };
        SilkMarshal.ThrowHResult(_device.CreateBlendState(in desc, ref _blendState));
    }

    private void CreateRasterizerState()
    {
        var desc = new RasterizerDesc
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None,
            FrontCounterClockwise = false,
            DepthClipEnable = true,
            ScissorEnable = false,
            MultisampleEnable = false,
        };
        SilkMarshal.ThrowHResult(_device.CreateRasterizerState(in desc, ref _rasterizerState));
    }

    private void CreateSamplerState()
    {
        var desc = new SamplerDesc
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MipLODBias = 0,
            MaxAnisotropy = 1,
            MinLOD = float.MinValue,
            MaxLOD = float.MaxValue,
        };
        SilkMarshal.ThrowHResult(_device.CreateSamplerState(in desc, ref _samplerState));
    }

    // ICollection<SpriteDrawCommand>（供 IRenderable.Submit 直接收集指令）
    public int Count => _commands.Count;
    public bool IsReadOnly => false;
    public void Add(SpriteDrawCommand item) => _commands.Add(item);
    public void Clear() => _commands.Clear();
    public bool Contains(SpriteDrawCommand item) => _commands.Contains(item);
    public void CopyTo(SpriteDrawCommand[] array, int arrayIndex) => _commands.CopyTo(array, arrayIndex);
    public bool Remove(SpriteDrawCommand item) => _commands.Remove(item);
    public IEnumerator<SpriteDrawCommand> GetEnumerator() => _commands.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _commands.GetEnumerator();
}
