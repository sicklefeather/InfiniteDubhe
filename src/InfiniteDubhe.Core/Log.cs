using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace InfiniteDubhe.Core;

/// <summary>
/// 引擎日志门面，底层基于 Microsoft.Extensions.Logging。Host 通过 <see cref="SetFactory"/> 注入输出目标。
/// </summary>
public static class Log
{
    private static ILoggerFactory _factory = NullLoggerFactory.Instance;

    private static ILogger Default => _factory.CreateLogger("InfiniteDubhe");

    /// <summary>替换底层日志工厂（如控制台/文件输出）。传 null 回退到空实现。</summary>
    public static void SetFactory(ILoggerFactory factory)
        => _factory = factory ?? NullLoggerFactory.Instance;

    /// <summary>按类别获取日志器。</summary>
    public static ILogger Get(string category) => _factory.CreateLogger(category);

    public static void Debug(string message, params object?[] args) => Default.LogDebug(message, args);
    public static void Info(string message, params object?[] args) => Default.LogInformation(message, args);
    public static void Warn(string message, params object?[] args) => Default.LogWarning(message, args);
    public static void Error(string message, params object?[] args) => Default.LogError(message, args);
    public static void Error(Exception exception, string message, params object?[] args) => Default.LogError(exception, message, args);
}
