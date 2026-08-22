using System.Runtime.CompilerServices;

// 信任兄弟工具（沿用引擎 G-04 模式）：
// 编辑器视口拾取/拖拽需要读取 UIElement.ComputedPosition/ComputedSize（每帧由 Canvas.Submit 布局刷新）。
[assembly: InternalsVisibleTo("InfiniteDubheEditor")]
