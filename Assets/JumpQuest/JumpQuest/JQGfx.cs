using System;
using UnityEngine;

public static class JQGfx
{
    const int W = 800, H = 448, T = 32;
    public static Sprite Square, Circle, HalfCircle, Ring, Tri, Star, Trapezoid, Vignette;

    public static void Init()
    {
        if (Square != null) return;
        Square = Make(1, 1, FilterMode.Point, (x, y) => 1f);
        Circle = Make(64, 64, FilterMode.Bilinear, (x, y) => Cov(32f - Dist(x, y)));
        HalfCircle = Make(64, 64, FilterMode.Bilinear, (x, y) => y < 32 ? 0f : Cov(32f - Dist(x, y)));
        Ring = Make(64, 64, FilterMode.Bilinear, (x, y) => { float d = Dist(x, y); return Cov(32f - d) * Cov(d - 20.7f); });
        Tri = Make(64, 64, FilterMode.Bilinear, (x, y) =>
        {
            float half = 32f * (1f - (y + .5f) / 64f);
            return Cov(half - Mathf.Abs(x + .5f - 32f));
        });

        var star = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            float ang = i * Mathf.PI / 4f, r = (i % 2 == 0) ? 32f : 12.8f;
            star[i] = new Vector2(32f + Mathf.Cos(ang) * r, 32f + Mathf.Sin(ang) * r);
        }
        Star = Make(64, 64, FilterMode.Bilinear, (x, y) => Super(star, x, y));

        var trap = new[] { new Vector2(0, 0), new Vector2(64, 0), new Vector2(40.7f, 64), new Vector2(23.3f, 64) };
        Trapezoid = Make(64, 64, FilterMode.Bilinear, (x, y) => Super(trap, x, y));

        Vignette = Make(64, 64, FilterMode.Bilinear, (x, y) =>
        {
            float u = (x + .5f) / 64f * W, v = (y + .5f) / 64f * H;
            float d = Mathf.Sqrt((u - W / 2f) * (u - W / 2f) + (v - H / 2f) * (v - H / 2f));
            return Mathf.Clamp01((d - .35f * H) / (.62f * W - .35f * H));
        });
    }

    static float Dist(int x, int y) { float dx = x + .5f - 32f, dy = y + .5f - 32f; return Mathf.Sqrt(dx * dx + dy * dy); }
    static float Cov(float v) { return Mathf.Clamp01(v + .5f); }

    static float Super(Vector2[] p, int x, int y)
    {
        int n = 0;
        if (In(p, x + .25f, y + .25f)) n++;
        if (In(p, x + .75f, y + .25f)) n++;
        if (In(p, x + .25f, y + .75f)) n++;
        if (In(p, x + .75f, y + .75f)) n++;
        return n / 4f;
    }

    static bool In(Vector2[] p, float x, float y)
    {
        bool c = false;
        for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
            if ((p[i].y > y) != (p[j].y > y) &&
                x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x) c = !c;
        return c;
    }

    static Sprite Make(int w, int h, FilterMode fm, Func<int, int, float> a)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = fm; tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color(1, 1, 1, a(x, y));
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), w);
    }

    // Degradê vertical (céu).
    public static Sprite Gradient(Color top, Color bottom)
    {
        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        {
            Color c = Color.Lerp(bottom, top, y / 63f);
            for (int x = 0; x < 64; x++) px[y * 64 + x] = c;
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64);
    }

    static void Paint(Color[] px, float x, float y, float w, float h, Color c)
    {
        int x0 = Mathf.Max(0, Mathf.RoundToInt(x)), x1 = Mathf.Min(T, Mathf.RoundToInt(x + w));
        int y0 = Mathf.Max(0, Mathf.RoundToInt(y)), y1 = Mathf.Min(T, Mathf.RoundToInt(y + h));
        var src = new Color(c.r, c.g, c.b, 1f);
        for (int yy = y0; yy < y1; yy++)
            for (int xx = x0; xx < x1; xx++)
            {
                int i = (T - 1 - yy) * T + xx;
                px[i] = Color.Lerp(px[i], src, c.a);
            }
    }

    // Tile 32x32
    public static Sprite BakeTile(JQTheme h, int lv, int t, bool top, bool row13, bool odd)
    {
        var tex = new Texture2D(T, T, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[T * T];
        Color body = t == 1 ? h.g : h.p; body.a = 1f;
        for (int i = 0; i < px.Length; i++) px[i] = body;

        Color k22 = new Color(0, 0, 0, .22f), k2 = new Color(0, 0, 0, .2f);
        if (top) Paint(px, 0, 0, T, 6, t == 1 ? h.gt : h.pt);
        Paint(px, 0, 6, T, 2, new Color(1, 1, 1, .1f));
        if (row13) Paint(px, 0, 0, T, T, new Color(0, 0, 0, .25f));
        if (t == 2) { Paint(px, 15, 8, 2, T - 8, k22); Paint(px, 0, T - 9, T, 2, k22); }

        if (lv == 1) { Paint(px, 0, 15, T, 2, k2); Paint(px, odd ? 8 : 20, 6, 2, 9, k2); }
        else if (lv == 2 && t == 1)
        {
            var lava = new Color(1f, .51f, .157f, .55f);
            Paint(px, 10, 12, 2, 12, lava); Paint(px, 10, 22, 8, 2, lava);
        }
        else if (t == 1) { Paint(px, 6, 14, 3, 3, k2); Paint(px, 20, 22, 3, 3, k2); }
        Paint(px, 0, T - 2, T, 2, new Color(0, 0, 0, .18f));

        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, T, T), new Vector2(.5f, .5f), T);
    }
}
