using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Leitura de teclado que funciona com o Input Manager antigo E com o novo Input System.
public static class JQInput
{
#if ENABLE_INPUT_SYSTEM
    static bool H(params Key[] ks)
    {
        var k = Keyboard.current; if (k == null) return false;
        for (int i = 0; i < ks.Length; i++) if (k[ks[i]].isPressed) return true;
        return false;
    }
    static bool D(params Key[] ks)
    {
        var k = Keyboard.current; if (k == null) return false;
        for (int i = 0; i < ks.Length; i++) if (k[ks[i]].wasPressedThisFrame) return true;
        return false;
    }
    public static bool Left { get { return H(Key.LeftArrow, Key.A); } }
    public static bool Right { get { return H(Key.RightArrow, Key.D); } }
    public static bool JumpHeld { get { return H(Key.Space, Key.UpArrow, Key.W); } }
    public static bool JumpDown { get { return D(Key.Space, Key.UpArrow, Key.W); } }
    public static bool ShootDown { get { return D(Key.X, Key.K, Key.J); } }
    public static bool MuteDown { get { return D(Key.M); } }
    public static bool UpDown { get { return D(Key.UpArrow, Key.W); } }
    public static bool DownDown { get { return D(Key.DownArrow, Key.S); } }
    public static bool ConfirmDown { get { return D(Key.Enter, Key.NumpadEnter, Key.Space); } }
#else
    static bool H(params KeyCode[] ks)
    {
        for (int i = 0; i < ks.Length; i++) if (Input.GetKey(ks[i])) return true;
        return false;
    }
    static bool D(params KeyCode[] ks)
    {
        for (int i = 0; i < ks.Length; i++) if (Input.GetKeyDown(ks[i])) return true;
        return false;
    }
    public static bool Left { get { return H(KeyCode.LeftArrow, KeyCode.A); } }
    public static bool Right { get { return H(KeyCode.RightArrow, KeyCode.D); } }
    public static bool JumpHeld { get { return H(KeyCode.Space, KeyCode.UpArrow, KeyCode.W); } }
    public static bool JumpDown { get { return D(KeyCode.Space, KeyCode.UpArrow, KeyCode.W); } }
    public static bool ShootDown { get { return D(KeyCode.X, KeyCode.K, KeyCode.J); } }
    public static bool MuteDown { get { return D(KeyCode.M); } }
    public static bool UpDown { get { return D(KeyCode.UpArrow, KeyCode.W); } }
    public static bool DownDown { get { return D(KeyCode.DownArrow, KeyCode.S); } }
    public static bool ConfirmDown { get { return D(KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space); } }
#endif
}
