using System.Numerics;
using InfiniteDubhe.Platform;
using Silk.NET.Input;

namespace InfiniteDubhe.Platform.Windows;

/// <summary>键盘 + 鼠标输入源（事件驱动）。手柄/触摸随 M2+ 扩展。</summary>
public sealed class WindowsInputSource : IInputSource, IDisposable
{
    private readonly HashSet<InfiniteDubhe.Core.Key> _held = new();
    private readonly HashSet<InfiniteDubhe.Core.Key> _pressed = new();
    private readonly HashSet<InfiniteDubhe.Core.MouseButton> _mouseHeld = new();
    private readonly HashSet<InfiniteDubhe.Core.MouseButton> _mousePressed = new();
    private Vector2 _mousePos;
    private float _mouseWheel;
    private string _textInput = "";
    private IInputContext? _input;

    public WindowsInputSource(WindowsWindow window)
    {
        window.Load += () => Initialize(window);
    }

    public bool IsKeyDown(InfiniteDubhe.Core.Key key) => _held.Contains(key);
    public bool IsKeyPressed(InfiniteDubhe.Core.Key key) => _pressed.Contains(key);
    public Vector2 MousePosition => _mousePos;
    public bool IsMouseButtonDown(InfiniteDubhe.Core.MouseButton button) => _mouseHeld.Contains(button);
    public bool IsMouseButtonPressed(InfiniteDubhe.Core.MouseButton button) => _mousePressed.Contains(button);
    public float MouseWheel => _mouseWheel;
    public string TextInput => _textInput;

    public void Update()
    {
        _pressed.Clear();
        _mousePressed.Clear();
        _mouseWheel = 0f;
        _textInput = "";
    }

    public void Dispose() => _input?.Dispose();

    private void Initialize(WindowsWindow window)
    {
        _input = window.Silk.CreateInput();

        foreach (var keyboard in _input.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
            keyboard.KeyChar += (_, c) => _textInput += c;
        }

        foreach (var mouse in _input.Mice)
        {
            mouse.MouseMove += (_, pos) => _mousePos = new Vector2(pos.X, pos.Y);
            mouse.MouseDown += (_, button) =>
            {
                var mapped = Map(button);
                if (mapped is null) return;
                _mouseHeld.Add(mapped.Value);
                _mousePressed.Add(mapped.Value);
            };
            mouse.MouseUp += (_, button) =>
            {
                var mapped = Map(button);
                if (mapped is null) return;
                _mouseHeld.Remove(mapped.Value);
            };
            mouse.Scroll += (_, wheel) => _mouseWheel += wheel.Y;
        }
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        var mapped = Map(key);
        if (mapped == InfiniteDubhe.Core.Key.Unknown) return;
        _held.Add(mapped);
        _pressed.Add(mapped);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        var mapped = Map(key);
        if (mapped == InfiniteDubhe.Core.Key.Unknown) return;
        _held.Remove(mapped);
    }

    private static InfiniteDubhe.Core.MouseButton? Map(MouseButton button) => button switch
    {
        MouseButton.Left => InfiniteDubhe.Core.MouseButton.Left,
        MouseButton.Right => InfiniteDubhe.Core.MouseButton.Right,
        MouseButton.Middle => InfiniteDubhe.Core.MouseButton.Middle,
        _ => null,
    };

    private static InfiniteDubhe.Core.Key Map(Key key) => key switch
    {
        Key.Escape => InfiniteDubhe.Core.Key.Escape,
        Key.Enter => InfiniteDubhe.Core.Key.Enter,
        Key.Space => InfiniteDubhe.Core.Key.Space,
        Key.Tab => InfiniteDubhe.Core.Key.Tab,
        Key.Backspace => InfiniteDubhe.Core.Key.Backspace,

        Key.ShiftLeft => InfiniteDubhe.Core.Key.LeftShift,
        Key.ShiftRight => InfiniteDubhe.Core.Key.RightShift,
        Key.ControlLeft => InfiniteDubhe.Core.Key.LeftControl,
        Key.ControlRight => InfiniteDubhe.Core.Key.RightControl,
        Key.AltLeft => InfiniteDubhe.Core.Key.LeftAlt,
        Key.AltRight => InfiniteDubhe.Core.Key.RightAlt,

        Key.A => InfiniteDubhe.Core.Key.A,
        Key.B => InfiniteDubhe.Core.Key.B,
        Key.C => InfiniteDubhe.Core.Key.C,
        Key.D => InfiniteDubhe.Core.Key.D,
        Key.E => InfiniteDubhe.Core.Key.E,
        Key.F => InfiniteDubhe.Core.Key.F,
        Key.G => InfiniteDubhe.Core.Key.G,
        Key.H => InfiniteDubhe.Core.Key.H,
        Key.I => InfiniteDubhe.Core.Key.I,
        Key.J => InfiniteDubhe.Core.Key.J,
        Key.K => InfiniteDubhe.Core.Key.K,
        Key.L => InfiniteDubhe.Core.Key.L,
        Key.M => InfiniteDubhe.Core.Key.M,
        Key.N => InfiniteDubhe.Core.Key.N,
        Key.O => InfiniteDubhe.Core.Key.O,
        Key.P => InfiniteDubhe.Core.Key.P,
        Key.Q => InfiniteDubhe.Core.Key.Q,
        Key.R => InfiniteDubhe.Core.Key.R,
        Key.S => InfiniteDubhe.Core.Key.S,
        Key.T => InfiniteDubhe.Core.Key.T,
        Key.U => InfiniteDubhe.Core.Key.U,
        Key.V => InfiniteDubhe.Core.Key.V,
        Key.W => InfiniteDubhe.Core.Key.W,
        Key.X => InfiniteDubhe.Core.Key.X,
        Key.Y => InfiniteDubhe.Core.Key.Y,
        Key.Z => InfiniteDubhe.Core.Key.Z,

        Key.Number0 => InfiniteDubhe.Core.Key.D0,
        Key.Number1 => InfiniteDubhe.Core.Key.D1,
        Key.Number2 => InfiniteDubhe.Core.Key.D2,
        Key.Number3 => InfiniteDubhe.Core.Key.D3,
        Key.Number4 => InfiniteDubhe.Core.Key.D4,
        Key.Number5 => InfiniteDubhe.Core.Key.D5,
        Key.Number6 => InfiniteDubhe.Core.Key.D6,
        Key.Number7 => InfiniteDubhe.Core.Key.D7,
        Key.Number8 => InfiniteDubhe.Core.Key.D8,
        Key.Number9 => InfiniteDubhe.Core.Key.D9,

        Key.F1 => InfiniteDubhe.Core.Key.F1,
        Key.F2 => InfiniteDubhe.Core.Key.F2,
        Key.F3 => InfiniteDubhe.Core.Key.F3,
        Key.F4 => InfiniteDubhe.Core.Key.F4,
        Key.F5 => InfiniteDubhe.Core.Key.F5,
        Key.F6 => InfiniteDubhe.Core.Key.F6,
        Key.F7 => InfiniteDubhe.Core.Key.F7,
        Key.F8 => InfiniteDubhe.Core.Key.F8,
        Key.F9 => InfiniteDubhe.Core.Key.F9,
        Key.F10 => InfiniteDubhe.Core.Key.F10,
        Key.F11 => InfiniteDubhe.Core.Key.F11,
        Key.F12 => InfiniteDubhe.Core.Key.F12,

        Key.Up => InfiniteDubhe.Core.Key.Up,
        Key.Down => InfiniteDubhe.Core.Key.Down,
        Key.Left => InfiniteDubhe.Core.Key.Left,
        Key.Right => InfiniteDubhe.Core.Key.Right,

        Key.Home => InfiniteDubhe.Core.Key.Home,
        Key.End => InfiniteDubhe.Core.Key.End,
        Key.PageUp => InfiniteDubhe.Core.Key.PageUp,
        Key.PageDown => InfiniteDubhe.Core.Key.PageDown,
        Key.Insert => InfiniteDubhe.Core.Key.Insert,
        Key.Delete => InfiniteDubhe.Core.Key.Delete,

        _ => InfiniteDubhe.Core.Key.Unknown,
    };
}
