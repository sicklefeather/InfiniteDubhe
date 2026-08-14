namespace InfiniteDubhe.Core;

/// <summary>引擎抽象键位（与平台无关）。由 PAL 从平台键码映射。</summary>
public enum Key
{
    Unknown = 0,

    Escape,
    Enter,
    Space,
    Tab,
    Backspace,

    LeftShift,
    RightShift,
    LeftControl,
    RightControl,
    LeftAlt,
    RightAlt,

    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    Up,
    Down,
    Left,
    Right,

    Home,
    End,
    PageUp,
    PageDown,
    Insert,
    Delete,
}
