using InfiniteDubhe.Core;
using InfiniteDubhe.Engine;
using InfiniteDubhe.Platform.Windows;
using Microsoft.Extensions.Logging;
using FlappyBird;

// 配置日志输出到控制台。
using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());
Log.SetFactory(loggerFactory);

var config = new GameConfig
{
    Title = "InfiniteDubhe - Flappy Bird",
    Width = 480,
    Height = 720,
    VSync = true,
};

var host = new GameHost(new WindowsPlatformBootstrap());
host.Run(new FlappyBirdGame(config));
