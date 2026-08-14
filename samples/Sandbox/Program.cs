using InfiniteDubhe.Core;
using InfiniteDubhe.Engine;
using InfiniteDubhe.Platform.Windows;
using Microsoft.Extensions.Logging;
using Sandbox;

// 配置日志输出到控制台。
using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());
Log.SetFactory(loggerFactory);

var config = new GameConfig
{
    Title = "InfiniteDubhe Sandbox (M0)",
    Width = 1280,
    Height = 720,
    VSync = true,
};

var host = new GameHost(new WindowsPlatformBootstrap());
host.Run(new SandboxGame(config));
