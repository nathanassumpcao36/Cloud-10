using UnityEngine;

public class JQTheme
{
    public Color e, sky, skyBottom, sun, h1, h2, g, gt, p, pt;
    public int sr;
    public string n;
}

public class JQLevel
{
    public int w, fl;
    public int[][] pits, pl, bat;
    public int[] sp, en, spr, gey;
    public JQTheme th;
}

public static class JQData
{
    static Color C(string hex) { Color c; ColorUtility.TryParseHtmlString(hex, out c); return c; }
    static int[] I(params int[] a) { return a; }
    static int[][] A(params int[][] a) { return a; }

    public static readonly JQLevel[] Levels = Build();

    static JQLevel[] Build()
    {
        var l0 = new JQLevel
        {
            w = 60, fl = 57,
            pits = A(I(14, 16), I(30, 32)),
            pl = A(I(8, 9, 3), I(18, 9, 3), I(22, 7, 4), I(36, 9, 3), I(40, 7, 3), I(46, 9, 4)),
            sp = I(), en = I(12, 26, 44, 50), spr = I(), bat = A(), gey = I(),
            th = new JQTheme
            {
                e = C("#b6e35c"), n = "FASE 1 · CAMPO", sky = C("#2f3f86"), skyBottom = C("#f19a6e"),
                sun = C("#f2b441"), sr = 46, h1 = C("#34456b"), h2 = C("#41568a"),
                g = C("#5b3b4a"), gt = C("#f2b441"), p = C("#2f8f8a"), pt = C("#7be0c3")
            }
        };
        var l1 = new JQLevel
        {
            w = 70, fl = 67,
            pits = A(I(10, 12), I(30, 32), I(50, 52)),
            pl = A(I(16, 10, 2), I(19, 8, 2), I(22, 6, 3), I(26, 8, 2), I(34, 10, 2), I(37, 8, 2), I(40, 6, 3),
                   I(44, 8, 2), I(47, 10, 2), I(56, 9, 3), I(60, 7, 3), I(64, 9, 3)),
            sp = I(14, 15, 28, 29, 36, 54, 55), en = I(8, 18, 28, 35, 42, 58, 62), spr = I(21, 43),
            bat = A(I(30, 5), I(48, 4), I(62, 5)), gey = I(),
            th = new JQTheme
            {
                e = C("#c88bff"), n = "FASE 2 · CAVERNA", sky = C("#1a1020"), skyBottom = C("#4a2f66"),
                sun = C("#c9d6ff"), sr = 22, h1 = C("#2a1b3d"), h2 = C("#3a2552"),
                g = C("#3b3350"), gt = C("#8f7bd6"), p = C("#6b4f3a"), pt = C("#d9a066")
            }
        };
        var l2 = new JQLevel
        {
            w = 80, fl = 77,
            pits = A(I(8, 10), I(18, 26), I(44, 54), I(66, 68)),
            pl = A(I(13, 9, 3), I(19, 10, 2), I(22, 9, 2), I(25, 10, 2), I(30, 9, 3), I(34, 7, 3), I(38, 9, 3),
                   I(45, 10, 2), I(48, 9, 2), I(51, 8, 2), I(58, 9, 2), I(61, 7, 3), I(72, 9, 3)),
            sp = I(14, 15, 29, 40, 41, 57, 63, 64, 73), en = I(6, 16, 30, 36, 56, 60, 64, 71, 74), spr = I(12),
            bat = A(I(22, 6), I(36, 4), I(50, 5), I(68, 5)), gey = I(34, 43, 55, 62, 70),
            th = new JQTheme
            {
                e = C("#ff9a3c"), n = "FASE 3 · VULCÃO", sky = C("#3a0f14"), skyBottom = C("#9a2a22"),
                sun = C("#ff7a33"), sr = 70, h1 = C("#5a1a1c"), h2 = C("#7a2420"),
                g = C("#2b1a1a"), gt = C("#ff6b2d"), p = C("#8a8f9c"), pt = C("#d6dbe6")
            }
        };
        return new[] { l0, l1, l2 };
    }
}
