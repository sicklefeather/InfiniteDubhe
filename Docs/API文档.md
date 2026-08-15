# 无限天枢引擎 API 文档

> 文档版本：M2
> 更新日期：2026-08-15
> 对应代码：M0 + M1 + M2（含 `Sandbox`、`FlappyBird` 两个示例）
> 前置文档：`需求文档.md`、`设计文档.md`

本文档是**已实现代码**的公开 API 参考，按程序集（命名空间）组织。粒度到「类型 + 公开成员 + 一句话说明」，不含内部实现。与 `设计文档.md` 的差异以**代码为准**。

---

## 1. 快速上手

一个最小可运行的游戏：

```csharp
using InfiniteDubhe.Core;
using InfiniteDubhe.Engine;
using InfiniteDubhe.Platform.Windows;

var config = new GameConfig { Title = "Hello", Width = 800, Height = 600 };
var host = new GameHost(new WindowsPlatformBootstrap());
host.Run(new MyGame(config));

sealed class MyGame : Game
{
    public MyGame(GameConfig config) : base(config) { }

    protected override void LoadContent()
    {
        // GetService<T>() 解析子系统：SceneManager / ResourceManager / Renderer
        var scene = new Scene("Main");
        GetService<SceneManager>()!.Load(scene);
        scene.CreateObject("Player"); // AddComponent<T>() 挂组件
    }
}
```

- 生命周期钩子（`Initialize` / `LoadContent` / `Update` / `FixedUpdate` / `Render` / `UnloadContent` / `Shutdown`）由 `GameHost` 驱动。
- 子系统访问：`GetService<SceneManager>()`、`GetService<ResourceManager>()`、`GetService<Renderer>()`。

---

## 2. 程序集总览

| 程序集 | 职责 | 依赖 |
|--------|------|------|
| `InfiniteDubhe.Core` | 基础：数学/时间/日志/事件/协程/补间/调试绘制/渲染契约 | 无 |
| `InfiniteDubhe.Platform` | PAL 抽象接口（窗口/图形/输入/文件/时钟） | Core |
| `InfiniteDubhe.Platform.Windows` | Windows PAL 实现（Silk.NET + D3D11） | Core + Platform |
| `InfiniteDubhe.Scene` | GameObject/Component/Scene/Transform/SpriteRenderer/帧动画 | Core |
| `InfiniteDubhe.Rendering` | 渲染器/SpriteBatch/Camera/Texture2D | Core + Platform |
| `InfiniteDubhe.Input` | 输入静态门面 + 动作映射 | Core + Platform |
| `InfiniteDubhe.Resources` | 资源管理 + 序列化 + 纹理加载 | Core + Platform + Scene + Rendering |
| `InfiniteDubhe.Physics` | Aether.Physics2D 封装 | Scene |
| `InfiniteDubhe.Audio` | 音频（OpenAL 后端） | Core + Platform |
| `InfiniteDubhe.UI` | UI 控件/布局/事件/内置字体 | Core + Scene + Rendering + Input |
| `InfiniteDubhe.Engine` | 引擎运行时：`GameHost` 主循环 | Core + Platform + Rendering |

宿主程序集（`Sandbox` / `FlappyBird`）依赖以上全部。

---

## 3. 全局约定（使用前必读）

1. **坐标系统**：屏幕坐标，**+y 向下**，(0,0) 在左上角，单位像素。默认相机下世界与屏幕 1:1。
2. **角度单位**：用户 API 用**度**（`Transform.RotationDeg`、`Camera.RotationDeg`）；内部热路径（`SpriteDrawCommand.Rotation`）用**弧度**。
3. **生命周期**（`Component`）：`AddComponent` → 立即 `Awake` → 激活 `OnEnable` → 首帧 `Start` → 每帧 `Update` / 固定步 `FixedUpdate` → `OnDisable` → `OnDestroy`。回调不传 `dt`，统一读 `Time`。
4. **时间**：`Time.DeltaTime`（未缩放）、`Time.ScaledDeltaTime`（已缩放）、`Time.FixedDeltaTime`、`Time.TimeScale`、`Time.TotalTime`。
5. **静态门面**：`Time` / `Input` / `Audio` / `Events` / `Coroutines` / `Tween` / `Debug` / `Physics2D` / `Log` 均为全局静态门面，由 `GameHost` 每帧推进。
6. **渲染契约**：`SpriteRenderer` 只持有 `ITexture` 句柄（实现 `IRenderable`），不持有具体 GPU 实现；渲染数据经 `SpriteDrawCommand` 提交。
7. **音频降级**：OpenAL 未初始化时 `Audio.IsAvailable == false`，所有播放调用安全变为空操作（不抛异常）。原生库 `soft_oal.dll` 已随 `Silk.NET.OpenAL.Soft.Native` 打包进输出目录。

---

## 4. InfiniteDubhe.Core

### 4.1 Game（抽象基类）

继承它写游戏逻辑，在 `Main` 里交给 `GameHost.Run`。

```csharp
public abstract class Game
{
    public GameConfig Config { get; }
    public IServiceProvider? Services { get; }   // 由 GameHost 注入
    protected T? GetService<T>() where T : class; // 解析子系统

    protected virtual void Initialize();
    protected virtual void LoadContent();
    protected virtual void Update(float dt);
    protected virtual void FixedUpdate(float dt);
    protected virtual void Render();
    protected virtual void UnloadContent();
    protected virtual void Shutdown();
}
```

### 4.2 GameConfig

| 成员 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `Title` | string | "InfiniteDubhe" | 窗口标题 |
| `Width` / `Height` | int | 1280 / 720 | 分辨率 |
| `VSync` | bool | true | 垂直同步 |
| `FixedTimestep` | float | 1/60 | 固定步长（秒） |
| `MaxDeltaTime` | float | 0.25 | 帧间隔上限，防窗口拖动导致 dt 巨大 |

### 4.3 Time（静态）

| 成员 | 说明 |
|------|------|
| `DeltaTime` | 当前帧原始间隔（秒，未缩放） |
| `FixedDeltaTime` | 固定步长 |
| `TimeScale` | 时间缩放（0 = 暂停） |
| `TotalTime` | 累计运行时间（未缩放） |
| `ScaledDeltaTime` | `DeltaTime * TimeScale` |

### 4.4 Log（静态）

| 成员 | 说明 |
|------|------|
| `SetFactory(ILoggerFactory)` | 替换底层日志工厂（控制台/文件） |
| `Get(string category)` | 按类别取日志器 |
| `Debug / Info / Warn / Error` | 分级输出；`Error(Exception, string, ...)` 带异常 |

### 4.5 Events / IEventBus / EventBus

```csharp
public interface IEventBus
{
    IDisposable Subscribe<TEvent>(Action<TEvent> handler); // 返回可释放句柄（释放即退订）
    void Unsubscribe<TEvent>(Action<TEvent> handler);
    void Publish<TEvent>(TEvent e);
}
public static class Events
{
    public static IEventBus Global { get; }
    public static void Publish<TEvent>(TEvent e);
    public static IDisposable Subscribe<TEvent>(Action<TEvent> handler);
}
```

### 4.6 协程与延时（Coroutines / Scheduler / Coroutine）

```csharp
public static class Coroutines
{
    public static Scheduler Global { get; }
    public static Coroutine Start(IEnumerator routine);
    public static Coroutine Invoke(Action action, float delaySeconds = 0f);
    public static void Stop(Coroutine coroutine);
    public static void StopAll();
}

public sealed class Scheduler
{
    public int ActiveCount { get; }
    public Coroutine StartCoroutine(IEnumerator routine);
    public Coroutine Invoke(Action action, float delaySeconds);
    public void Stop(Coroutine coroutine);
    public void StopAll();
}
public sealed class Coroutine { public bool IsFinished { get; } }

// yield 指令：
public readonly struct WaitForSeconds { public WaitForSeconds(float seconds); }
public readonly struct WaitForFixedUpdate { }
```

协程返回 `null` 表示等待一帧；`WaitForSeconds` 为缩放时间。

### 4.7 补间（Tween / TweenHandle / Easing / Ease）

```csharp
public static class Tween
{
    // 三组重载：float / Vector2 / Color
    public static TweenHandle To(Func<float> getter, Action<float> setter, float to, float duration,
        Ease ease = Ease.Linear, Action? onComplete = null);
    public static TweenHandle FromTo(Action<float> setter, float from, float to, float duration, ...);
    // 同构：Vector2 / Color 版本
    public static void StopAll();
}

public sealed class TweenHandle
{
    public bool IsPlaying { get; }
    public Action? OnComplete { get; set; }
    public void Pause(); public void Resume(); public void Stop();
}

public enum Ease { Linear, InQuad, OutQuad, InOutQuad, InCubic, ..., OutElastic, InOutElastic }
public static class Easing { public static float Apply(Ease ease, float t); }
```

### 4.8 调试绘制（Debug 静态）

| 成员 | 说明 |
|------|------|
| `DrawLine(from, to, color, thickness)` | 画线段（世界坐标像素） |
| `DrawRay(origin, direction, color, length, thickness)` | 沿方向画射线 |
| `DrawRect(center, size, color, thickness)` | 轴对齐矩形边框 |
| `DrawCircle(center, radius, color, segments, thickness)` | 多边形近似圆 |
| `DrawPoint(position, color, size)` | 画点（小方块） |

> 在 `Update`/`Render` 中累积，由渲染器每帧在精灵之后绘制并清空（`Layer = int.MaxValue`）。

### 4.9 基础类型

| 类型 | 关键成员 | 说明 |
|------|---------|------|
| `Color`（readonly struct） | `R/G/B/A`、`FromRgb(r,g,b,a)`、`Lerp(a,b,t)`、`Black/White/CornflowerBlue` | RGBA float 分量（0–1） |
| `Rectangle`（readonly struct） | `X/Y/Width/Height`、`Right/Bottom`、`IsEmpty`、`Empty` | 整数矩形（源矩形用） |
| `SpriteEffects`（enum） | `None / FlipHorizontally / FlipVertically` | 精灵翻转 |
| `Key`（enum） | `Space`、`Escape`、`Enter`、字母、数字、方向键、F1–F12 … | 平台无关键码 |
| `MouseButton`（enum） | `Left / Middle / Right` | 鼠标键 |
| `ITexture` | `Width / Height` | 纹理句柄（渲染契约） |
| `IRenderable` | `Submit(ICollection<SpriteDrawCommand>)` | 可渲染对象契约 |
| `SpriteDrawCommand`（struct） | `Texture/SourceRect/Position/Rotation/Origin/Scale/Color/Effects/Layer/LayerDepth` | 单条绘制指令 |
| `ISerializer` | `Serialize<T>`、`Deserialize<T>`、`Deserialize(json, type)` | 序列化门面 |

### 4.10 编辑器特性标注

供编辑器 Inspector 使用，对游戏运行时零影响：

| 类型 | 说明 |
|------|------|
| `HideInInspectorAttribute` | 标记属性不显示在 Inspector |
| `RangeAttribute(min, max)` | 数值滑杆范围 |
| `HeaderAttribute(label)` | 属性分组标题 |
| `TooltipAttribute(text)` | 属性悬停提示 |

### 4.11 Profiler（性能分析，M3）

| 成员 | 说明 |
|------|------|
| `Enabled` | 是否开启采样（false 时零开销） |
| `FrameTimeMs` | 当前帧总耗时（毫秒） |
| `FixedUpdateTimeMs` / `UpdateTimeMs` / `RenderTimeMs` | 分阶段耗时（FixedUpdate 多次求和） |
| `DrawCalls` | 本帧 Draw Call 数量（由 SpriteBatch 上报） |
| `AllocatedBytes` | 本帧主线程 GC 分配字节数 |
| `Fps` | 按帧耗时推算的 FPS |

### 4.12 ObjectPool&lt;T&gt;（对象池，M3）

```csharp
public sealed class ObjectPool<T> where T : class
{
    public ObjectPool(Func<T> factory, Action<T>? onRent = null, Action<T>? onReturn = null);
    public int Count { get; }
    public T Rent();
    public void Return(T item);
}
```

> 复用实例避免每帧分配（对应 NFR-03）。

---

## 5. InfiniteDubhe.Engine

### 5.1 GameHost

```csharp
public sealed class GameHost
{
    public GameHost(IPlatformBootstrap platform);
    public void Run(Game game);   // 装配平台/渲染/场景/资源/输入并驱动主循环
}
```

主循环：固定步长累积（`FixedUpdate`）+ 可变渲染；每帧推进 `Coroutines`、`Tween`、`Audio`、`Scene`。`Esc` 或关闭窗口即退出。

---

## 6. InfiniteDubhe.Platform（PAL 接口）

宿主通常只依赖 `IPlatformBootstrap`；自定义平台时实现以下接口。

| 接口 | 关键成员 |
|------|---------|
| `IWindow` | `Title/Width/Height/IsClosing`、`event Load/Resized/Closing`、`Initialize()`、`ProcessEvents()` |
| `IGraphicsContext` | `Clear(Color)`、`SwapBuffers()`、`MakeCurrent()`、`Native`（后端句柄） |
| `IInputSource` | `IsKeyDown/IsKeyPressed(Key)`、`MousePosition`、`IsMouseButtonDown/Pressed(MouseButton)`、`MouseWheel`、`Update()` |
| `IFileSystem` | `OpenRead(path)`、`Exists(path)`、`GetFullPath(relative)` |
| `IClock` | `Tick()`、`TotalSeconds` |
| `IPlatformBootstrap` | `CreateWindow/CreateGraphicsContext/CreateInput/CreateFileSystem/CreateClock` |

`InfiniteDubhe.Platform.Windows` 提供现成实现：`new WindowsPlatformBootstrap()`（对应 `IWindow`/`IGraphicsContext`/`IInputSource`/`IFileSystem`/`IClock` 的 Windows 版）。

---

## 7. InfiniteDubhe.Scene

### 7.1 Scene / SceneManager

```csharp
public sealed class Scene
{
    public string Name { get; }
    public IReadOnlyList<GameObject> RootObjects { get; }
    public GameObject CreateObject(string name); // 创建根级对象
}
public sealed class SceneManager
{
    public Scene? Global { get; }           // 常驻全局场景（跨关卡存活的对象）
    public Scene? Current { get; }          // 当前关卡场景
    public void SetGlobal(Scene? scene);    // 设置全局场景（传 null 清除）
    public void Load(Scene? scene);         // 切换关卡场景（传 null 清除）
}
```

### 7.2 GameObject

| 成员 | 说明 |
|------|------|
| `Name` / `Id`(Guid) / `Scene` / `Transform` | 标识与变换 |
| `Active` | 激活开关（影响子节点） |
| `AddComponent<T>()` | 添加组件并立即 `Awake` |
| `GetComponent<T>()` / `TryGetComponent<T>(out)` / `GetComponents()` | 查询组件 |
| `CreateChild(name)` | 创建子对象 |
| `Destroy()` | 延迟到本帧末销毁（含子对象） |

### 7.3 Component

| 成员 | 说明 |
|------|------|
| `GameObject` / `Transform` | 所属对象与变换（快捷） |
| `Enabled` | 组件开关（控制 Update/FixedUpdate 是否调用） |
| 生命周期（`protected virtual`） | `Awake / OnEnable / Start / Update / FixedUpdate / OnDisable / OnDestroy` |

### 7.4 Transform

| 成员 | 说明 |
|------|------|
| `Position` | 本地位置 |
| `RotationDeg` | 旋转（度） |
| `Scale` | 缩放，默认 (1,1) |
| `Parent` / `Children` | 父子层级（只读视图） |
| `SetParent(Transform?)` | 重新挂载，传 null 移到根级 |
| `WorldPosition` | 累加父级平移的世界坐标 |
| `LocalToWorld` | 本地→世界矩阵（Matrix3x2） |

### 7.5 SpriteRenderer（实现 IRenderable）

| 成员 | 说明 |
|------|------|
| `Texture` | 纹理（`ITexture` 句柄） |
| `Color` | 颜色/透明度 tint |
| `SourceRect` | 源矩形；空表示整张纹理 |
| `Origin` | 旋转/缩放原点 |
| `Effects` | 翻转 |
| `Layer` / `LayerDepth` | 排序层 / 同层深度 |

### 7.6 帧动画（SpriteAnimator / SpriteAnimationClip）

```csharp
public readonly struct SpriteAnimationFrame
{
    public Rectangle SourceRect { get; }   // 源矩形
    public float DurationSeconds { get; }  // 显示时长（秒）
}
public sealed class SpriteAnimationClip
{
    public string Name { get; } public IReadOnlyList<SpriteAnimationFrame> Frames { get; } public bool Loop { get; }
    public static SpriteAnimationClip FromRow(string name, Rectangle firstFrame, int frameCount, int framesPerSecond, bool loop = true);
    public static SpriteAnimationClip FromGrid(string name, Rectangle firstFrame, int columns, int rows, int frameCount, int framesPerSecond, bool loop = true);
}
public sealed class SpriteAnimator : Component
{
    public SpriteRenderer? Renderer { get; set; }  // Awake 自动挂接同对象 SpriteRenderer
    public SpriteAnimationClip? CurrentClip { get; } public int CurrentFrame { get; } public bool IsPlaying { get; }
    public float Speed { get; set; }               // 播放速度倍率（1 = 原速）
    public void AddClip(SpriteAnimationClip clip);
    public bool RemoveClip(string name);
    public void Play(string name); public void Stop(); public void Pause(); public void Resume();
}
```

---

## 8. InfiniteDubhe.Rendering

### 8.1 Renderer

| 成员 | 说明 |
|------|------|
| `Camera` | 默认正交相机 |
| `ClearColor` | 清屏色，默认 CornflowerBlue |
| `Initialize()` | 获取后端句柄并创建 GPU 资源（由 GameHost 调用） |
| `CreateTexture(width, height, rgba)` | 从 RGBA 字节创建纹理（程序化纹理/上传） |
| `CreateRenderTarget(width, height)` | 创建离屏渲染目标（编辑器视口等用） |
| `Clear(color)` | 清屏 |
| `Draw(IReadOnlyList<IRenderable>)` | 收集指令 → 批处理 → 绘制（到后备缓冲） |
| `Draw(renderables, RenderTarget2D)` | 渲染到离屏目标（视口） |
| `Present()` | 提交渲染帧 |

### 8.2 Camera

| 成员 | 说明 |
|------|------|
| `Position` | 世界坐标中位于屏幕中心的点 |
| `Zoom` | 缩放，默认 1 |
| `RotationDeg` | 旋转（度） |
| `ViewportWidth` / `ViewportHeight` | 视口尺寸 |
| `ViewMatrix` | 世界 → 裁剪空间（正交投影 + 相机变换） |

### 8.3 SpriteBatch / Texture2D

```csharp
public sealed class SpriteBatch : ICollection<SpriteDrawCommand>, IDisposable
{
    public void Begin(Camera camera);
    public void Draw(ITexture texture, Vector2 position, Color color);
    public void Draw(in SpriteDrawCommand command);
    public void End(); // 按 (Layer, LayerDepth, 纹理) 排序后合并 Draw Call
}
public sealed class Texture2D : ITexture, IDisposable { public int Width { get; } public int Height { get; } }
```

> 使用者一般不必直接接触 `SpriteBatch`/`Texture2D`——通过 `Renderer.CreateTexture` 与 `SpriteRenderer` 即可；`Renderer.Draw` 内部完成批处理。

### 8.4 RenderTarget2D

```csharp
public sealed class RenderTarget2D : ITexture, IDisposable
{
    public int Width { get; }
    public int Height { get; }
}
```

> 离屏渲染目标：`Renderer.CreateRenderTarget` 创建，`Renderer.Draw(renderables, target)` 渲染进它，之后可作普通纹理（`ITexture`）采样（编辑器视口即用此呈现）。

---

## 9. InfiniteDubhe.Input

### 9.1 Input（静态门面）

| 成员 | 说明 |
|------|------|
| `IsKeyDown(Key)` / `IsKeyPressed(Key)` | 持续 / 本帧刚按下（边沿） |
| `MousePosition` | 鼠标屏幕坐标 |
| `IsMouseButtonDown/Pressed(MouseButton)` | 持续 / 边沿 |
| `MouseWheel` | 本帧滚轮累计值（正值向上） |

### 9.2 InputActions（动作映射）

| 成员 | 说明 |
|------|------|
| `Bind(name, params Key[])` / `Bind(name, params MouseButton[])` | 绑定动作到键/鼠标键（可追加） |
| `Unbind(name)` | 解除全部绑定 |
| `IsDown(name)` | 任一绑定按下 |
| `WasPressed(name)` | 任一绑定本帧刚按下 |

---

## 10. InfiniteDubhe.Resources

### 10.1 ResourceManager

| 成员 | 说明 |
|------|------|
| `RegisterLoader<T>(IResourceLoader<T>)` | 注册某类资源的加载器 |
| `Load<T>(path)` | 同步加载，带引用计数，同路径复用 |
| `LoadAsync<T>(path)` | 异步加载（后台线程包装同步） |
| `Unload(path)` | 释放一次引用，归零才真正释放 |
| `GetPath(resource)` | 反向查询某已缓存资源的加载路径（未缓存则 null），供场景序列化把纹理句柄还原为路径 |
| `Reload<T>(path)` | 热重载单个资源（重新加载并替换缓存实例，触发 `ResourceChanged`） |
| `ReloadAll<T>()` | 热重载所有已缓存且属于该类型的资源 |
| `event ResourceChanged` | 资源变更/热重载触发点（M3 落地） |

### 10.2 资源加载器

```csharp
public interface IResourceLoader<T> where T : class { T Load(string path); }
public sealed class TextureLoader : IResourceLoader<ITexture>   // StbImageSharp 解码 PNG/JPG 等
```

> 使用方式：`resources.Load<ITexture>(path)`（`TextureLoader` 已由 GameHost 注册）。

### 10.3 场景序列化

```csharp
public sealed class SceneSerializer
{
    public SceneSerializer(ResourceManager resources, IAssetGuidResolver? guidResolver = null);
    public string Serialize(Scene scene);      // 活对象 → SceneFile → JSON
    public Scene Deserialize(string json);     // JSON → SceneFile → 活对象 + 引用重连
}

public interface IAssetGuidResolver
{
    Guid GetGuid(string path);                 // 取路径对应 GUID（无则创建 .meta）
    string? GetPath(Guid guid);                // 按 GUID 解析当前路径
}

public sealed class SceneLoader
{
    public SceneLoader(IFileSystem fileSystem, SceneSerializer serializer);
    public Scene LoadScene(string path);
    public void SaveScene(Scene scene, string path);
    public bool Exists(string path);
}
```

> 场景文件为扁平 DTO（GUID 父子引用 + 组件类型全名 + 公开可写属性值）；纹理存 **GUID + 相对路径**（提供 `IAssetGuidResolver` 时以 GUID 主引用，改名/移动后经 GUID 重连；无解析器则退化为纯路径）。组件经反射重建（`AssemblyQualifiedName` + 公开可写属性），统一覆盖内置与自定义组件；`SpriteAnimator` 片段与 `Canvas` UI 树暂不覆盖。

### 10.4 AssetBundle（资源包，M3）

```csharp
public sealed class AssetBundle
{
    public static void Pack(string rootDir, string outputPath, Func<string, bool>? filter = null);   // 打包目录 → .dubhe 文件
    public static AssetBundle PackInMemory(string rootDir, Func<string, bool>? filter = null);        // 打包到内存（构建/测试用）
    public static AssetBundle Load(string path);                   // 从包文件加载
    public IReadOnlyCollection<string> Paths { get; }              // 包内资源路径
    public bool Contains(string path);
    public IFileSystem CreateFileSystem();                         // 作为 IFileSystem 供引擎使用
}
```

> 把资源目录打成单个 `.dubhe` 包；`CreateFileSystem()` 返回的 `IFileSystem` 可无缝替换磁盘文件系统（`ResourceManager`/`SceneLoader` 直接复用），游戏发布时用包替代松散的 `Content/` 目录。

---

## 11. InfiniteDubhe.Physics

### 11.1 PhysicsWorld2D（Component）

挂到场景的任一对象上，`Awake` 时登记为 `Physics2D.ActiveWorld`。

| 成员 | 说明 |
|------|------|
| `Gravity` | 重力（像素/秒²，+y 向下） |

### 11.2 Rigidbody2D（Component）

| 成员 | 说明 |
|------|------|
| `Type`（`BodyType2D`） | `Static / Kinematic / Dynamic` |
| `LinearDamping` / `AngularDamping` / `FixedRotation` / `IgnoreGravity` | 阻尼/锁旋转/忽略重力 |
| `Velocity` / `AngularVelocity` | 线速度（像素/秒）/ 角速度（度/秒） |
| `Mass` | 质量（只读，由形状+密度决定） |
| `AddForce(force)` / `AddImpulse(impulse)` / `AddTorque(torque)` | 施力/冲量/扭矩 |
| `event CollisionEnter/Stay/Exit` | 碰撞回调（`Action<Collision2D>`） |

### 11.3 碰撞体

```csharp
public abstract class Collider2D : Component
{
    public float Density / Friction / Restitution { get; set; }
    public bool IsSensor { get; set; }   // 触发器（只检测、不产生响应）
}
public sealed class BoxCollider2D : Collider2D { public Vector2 Size { get; set; } }  // 中心对齐完整宽高
public sealed class CircleCollider2D : Collider2D { public float Radius { get; set; } }
```

### 11.4 Physics2D（静态门面）与查询

```csharp
public static class Physics2D
{
    public static RaycastHit2D? Raycast(Vector2 from, Vector2 to);
    public static int OverlapPoint(Vector2 point, ICollection<Collider2D> results);
}
public readonly struct RaycastHit2D { public Vector2 Point; public Vector2 Normal; public float Fraction; public Collider2D Collider; }
public readonly struct Collision2D { public Collider2D Other; public Rigidbody2D? OtherRigidbody; }
```

> 换算：1 物理单位 = 100 像素（内部自动换算，用户面一律用像素）。

---

## 12. InfiniteDubhe.Audio

### 12.1 Audio（静态门面）

| 成员 | 说明 |
|------|------|
| `IsAvailable` | 后端是否可用（OpenAL 已初始化） |
| `MasterVolume` | 主音量（0–1） |
| `BgmVoice` | 当前背景音乐实例（无则 null） |
| `PlaySfx(clip, volume, pitch)` | 播放一次性音效 |
| `Play(clip, loop, volume, pitch)` | 播放声音（可循环） |
| `PlayBgm(clip, volume, fadeInSeconds)` | 切换背景音乐（循环，可淡入） |
| `StopBgm(fadeOutSeconds)` | 停止 BGM（可淡出） |
| `SetBgmVolume(volume)` | 实时调整 BGM 音量 |

### 12.2 AudioClip

| 成员 | 说明 |
|------|------|
| `Samples` | 16-bit 交错 PCM |
| `SampleRate` / `Channels` / `Duration` | 采样率 / 声道数（1/2）/ 时长 |
| `FromWav(path)` / `FromWav(stream)` | 从 WAV 载入（8/16-bit，单/双声道） |
| `Create(short[], sampleRate, channels)` | 用原始 PCM 程序化构造 |

### 12.3 AudioVoice

| 成员 | 说明 |
|------|------|
| `Clip` | 所属音频片段 |
| `Volume` / `Pitch` / `Loop` | 音量 / 音调（1=原速）/ 循环 |
| `IsPlaying` | 是否播放中 |
| `Play()` / `Pause()` / `Stop()` | 控制 |
| `FadeTo(target, duration, onComplete)` | 渐变音量 |
| `FadeOut(duration)` | 淡出到静音并释放 |

---

## 13. InfiniteDubhe.UI

### 13.1 Canvas（Component + IRenderable）

挂在 GameObject 上，`Initialize(Renderer)` 生成白色纹理与内置字体图集。

| 成员 | 说明 |
|------|------|
| `SortingLayer` | 渲染层（默认 1000，置顶于世界精灵） |
| `Font` | 内置字体图集 |
| `Roots` | 根元素（只读） |
| `Initialize(Renderer)` | 生成纹理/字体（创建后、渲染前调用一次） |
| `Add<T>(T)` / `Remove(UIElement)` | 增删根元素 |

### 13.2 UIElement（抽象基类）

| 分组 | 成员 | 说明 |
|------|------|------|
| 布局 | `Anchor` / `Pivot` / `Position` / `Size` | 锚点(父空间0–1) / 枢轴(自身0–1) / 像素偏移 / 尺寸 |
| 布局 | `Layout`（`LayoutDirection`）/ `Spacing` / `Padding` | 流式排列（`None/Vertical/Horizontal`） |
| 外观 | `Color` / `Visible` | 颜色 / 可见 |
| 交互 | `Interactable` / `IsHovered` / `IsPressed` | 是否参与命中测试 / 悬停 / 按压 |
| 事件 | `event Clicked / PointerEnter / PointerExit` | `Action<UIElement>` |
| 子元素 | `Children` / `AddChild(child)` / `AddChild<T>(child)` / `RemoveChild` / `RemoveAllChildren` | 子元素树 |

> 计算规则：`topLeft = 父左上 + 父尺寸 × Anchor + Position − 自身尺寸 × Pivot`。`Layout` 非 `None` 时子元素按 `Spacing`/`Padding` 自动堆叠。

### 13.3 控件

| 类型 | 关键成员 | 说明 |
|------|---------|------|
| `Text` | `Content`、`Scale` | 用内置位图字体绘制；尺寸自动 |
| `Button` | `Label`、`TextColor`、`FontScale`、`BackgroundColor/HoverColor/PressedColor` | 背景随状态变色 + 居中文本；`Interactable=true` |
| `Image` | `Texture` | 纹理（拉伸到 `Size`）；无纹理时退化为纯色 |
| `Panel` | （继承 UIElement） | 纯色矩形（背景/容器） |

### 13.4 BitmapFont / LayoutDirection

```csharp
public enum LayoutDirection { None, Vertical, Horizontal }

public sealed class BitmapFont
{
    public const int GlyphWidth = 5, GlyphHeight = 7, CellWidth = 6, CellHeight = 8, Columns = 16, Rows = 6;
    public ITexture Texture { get; }
    public int Advance { get; }               // 等宽推进（像素）
    public Rectangle GetGlyph(char c);        // 字符源矩形（不支持则 Empty）
    public static BitmapFont Build(Renderer renderer);
}
```

> 内置 5×7 字体覆盖 ASCII 32–126；`Canvas.Font` 由 `Initialize` 自动构建。

---

## 14. 示例

| 项目 | 演示内容 |
|------|---------|
| `samples/Sandbox` | M2 全子系统：物理（重力/刚体/碰撞/射线）、音频、动画（帧动画+补间）、UI（控件/布局/事件）、调试绘制 |
| `samples/FlappyBird` | 完整小游戏：程序化素材、三态流程、扑翅帧动画、循环 BGM、难度递增、音效 |

运行：`dotnet run --project samples/Sandbox` / `dotnet run --project samples/FlappyBird`。
