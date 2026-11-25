using System.Collections.Generic;
using UnityEngine;

public class JumpQuestGame : MonoBehaviour
{
    const int T = 32, W = 800, H = 448;
    const float DT = 1f / 60f;
    const string DEATH_TEXT = "SE FUDEU"; // texto da tela de morte 

    [Header("Opcional: fonte pixelada (ex.: Silkscreen). Vazio = fonte padrão.")]
    public Font uiFont;

    static JumpQuestGame inst;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (inst != null) return;
        new GameObject("JumpQuest").AddComponent<JumpQuestGame>();
    }

    // ---------- cores ----------
    static Color Hex(string s) { Color c; ColorUtility.TryParseHtmlString(s, out c); return c; }
    static Color A(Color c, float a) { c.a = a; return c; }
    static float Mod(float a, float m) { return ((a % m) + m) % m; }
    static readonly Color MenuBg = new Color32(0x26, 0x32, 0x4f, 255);
    static readonly Color cInk = Hex("#1d2140"), cWhite = Hex("#f4f0ff"), cYellow = Hex("#ffcf5a"),
        cRed = Hex("#e8553f"), cShadow = Hex("#10132a"), cPts = Hex("#ffe27a"), cHeart = Hex("#ff6b57"),
        cHeartOff = new Color(.35f, .2f, .27f, 1f), cHard = Hex("#d4481a"), cBar = new Color(.063f, .075f, .165f, .55f);

    // ---------- entidades ----------
    class Ent
    {
        public float x, y, w, h, vx, vy, x0, y0, t, kx;
        public int d = -1, hp = 1, ph, life, f = 1, inv, cd, kb, coy;
        public bool on, dead, fly;
        public SpriteRenderer[] sr;
    }
    enum St { Menu, Play, Clear, Dead, Over, Win }
    struct Btn { public string act, label; public Btn(string a, string l) { act = a; label = l; } }

    // ---------- estado ----------
    St st = St.Menu;
    int lv, fr, score, sc0, lives = 3, ct, objT, dt;
    float cam, acc;
    JQLevel d; int[][] g;
    Ent p;
    List<Ent> coins = new List<Ent>(), ens = new List<Ent>(), bl = new List<Ent>(),
        sps = new List<Ent>(), sprs = new List<Ent>(), gys = new List<Ent>();
    bool kL, kR, kJ, jumpPressed, shootPressed;
    Box flagBox;
    struct Box { public float x, y, w, h; }

    // ---------- cena ----------
    Camera mainCam, barCam;
    JQAudio audio;
    Transform screenLayer, backdrop, levelRoot;
    SpriteRenderer vig, bar, dim, vigDead;
    SpriteRenderer[][] hearts;
    SpriteRenderer[] hill1, hill2, waterTop, waterHi;
    SpriteRenderer[][] clouds, trees, cryst;
    SpriteRenderer[] stars, stal, flies, embers, tileEmbers;
    SpriteRenderer volc, volcRim, volcGlow, flagTri;
    List<int> tileEmberX = new List<int>();
    List<float> tileEmberY = new List<float>();

    // ---------- UI (menus) ----------
    bool uiOn; string uiTitle, uiText; Btn[] uiBtns; int sel; Vector2 lastMouse;
    GUIStyle lab, para; bool stylesReady;

    // =====================================================================
    void Awake()
    {
        if (inst != null && inst != this) { Destroy(gameObject); return; }
        inst = this;
        JQGfx.Init();
        audio = gameObject.AddComponent<JQAudio>();
        SetupCameras();
        screenLayer = new GameObject("ScreenLayer").transform;
        BuildHud();
        ShowMenu();
        ApplyVisibility();
    }

    void SetupCameras()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            var go = new GameObject("Main Camera"); go.tag = "MainCamera";
            mainCam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        mainCam.orthographic = true; mainCam.orthographicSize = H / 2f;
        mainCam.clearFlags = CameraClearFlags.SolidColor; mainCam.backgroundColor = MenuBg;
        mainCam.transform.rotation = Quaternion.identity;
        var bg = new GameObject("LetterboxCam");
        barCam = bg.AddComponent<Camera>();
        barCam.clearFlags = CameraClearFlags.SolidColor; barCam.backgroundColor = Color.black;
        barCam.cullingMask = 0; barCam.depth = mainCam.depth - 10;
    }

    void LateUpdate()
    {
        float a = (float)Screen.width / Screen.height, tgt = (float)W / H;
        mainCam.rect = a > tgt ? new Rect((1f - tgt / a) / 2f, 0, tgt / a, 1)
                               : new Rect(0, (1f - a / tgt) / 2f, 1, a / tgt);
        mainCam.transform.position = new Vector3(Mathf.Round(cam) + W / 2f, -H / 2f, -10f);
    }

    // =====================================================================
    // Helpers de desenho
    static SpriteRenderer SR(Transform parent, Sprite s, Color c, int order)
    {
        var go = new GameObject("s"); go.transform.SetParent(parent, false);
        var r = go.AddComponent<SpriteRenderer>(); r.sprite = s; r.color = c; r.sortingOrder = order;
        return r;
    }
    static void Rc(SpriteRenderer r, float x, float y, float w, float h)
    {
        r.transform.localPosition = new Vector3(x + w / 2f, -(y + h / 2f), 0);
        r.transform.localRotation = Quaternion.identity;
        r.transform.localScale = new Vector3(w, Mathf.Max(0f, h), 1);
    }
    // Triângulo isósceles: vértice (ax,ay), meio da base (bx,by), largura da base.
    static void Tri(SpriteRenderer r, float ax, float ay, float bx, float by, float baseLen)
    {
        Vector2 a = new Vector2(ax, -ay), b = new Vector2(bx, -by), dir = a - b;
        float len = dir.magnitude;
        if (len < 0.001f) { r.transform.localScale = Vector3.zero; return; }
        Vector2 c = (a + b) * .5f;
        r.transform.localPosition = new Vector3(c.x, c.y, 0);
        r.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        r.transform.localScale = new Vector3(baseLen, len, 1);
    }
    static void En(SpriteRenderer[] a, bool v) { for (int i = 0; i < a.Length; i++) a[i].enabled = v; }
    SpriteRenderer[] Cols(int n, Color c, int order)
    {
        var a = new SpriteRenderer[n];
        for (int i = 0; i < n; i++) a[i] = SR(backdrop, JQGfx.Square, c, order);
        return a;
    }

    // =====================================================================
    // HUD em sprites (barra, corações, vinheta, efeito de morte)
    void BuildHud()
    {
        vig = SR(screenLayer, JQGfx.Vignette, A(Color.black, .35f), 200); Rc(vig, 0, 0, W, H);
        bar = SR(screenLayer, JQGfx.Square, cBar, 210); Rc(bar, 0, 0, W, 40);
        hearts = new SpriteRenderer[3][];
        for (int i = 0; i < 3; i++)
        {
            float x = 26 + i * 30, y = 14, s = 20;
            var a = new SpriteRenderer[3];
            a[0] = SR(screenLayer, JQGfx.Circle, cHeart, 211); Rc(a[0], x - s * .25f - s * .27f, y - s * .27f, s * .54f, s * .54f);
            a[1] = SR(screenLayer, JQGfx.Circle, cHeart, 211); Rc(a[1], x + s * .25f - s * .27f, y - s * .27f, s * .54f, s * .54f);
            a[2] = SR(screenLayer, JQGfx.Tri, cHeart, 211); Tri(a[2], x, y + s * .7f, x, y + s * .1f, s);
            hearts[i] = a;
        }
        dim = SR(screenLayer, JQGfx.Square, A(Color.black, 0), 300); Rc(dim, 0, 0, W, H); dim.enabled = false;
        vigDead = SR(screenLayer, JQGfx.Vignette, A(Color.black, .75f), 301); Rc(vigDead, 0, 0, W, H); vigDead.enabled = false;
    }

    // =====================================================================
    // Fundo da fase (céu, sol, colinas, decoração, água/lava)
    void BuildBackdrop()
    {
        if (backdrop != null) Destroy(backdrop.gameObject);
        backdrop = new GameObject("Backdrop").transform; backdrop.SetParent(screenLayer, false);
        clouds = trees = cryst = null; stars = stal = flies = embers = null; volc = null;
        JQTheme h = d.th;

        var sky = SR(backdrop, JQGfx.Gradient(h.sky, h.skyBottom), Color.white, 0); Rc(sky, 0, 0, W, H);
        float r = h.sr;
        var glow = SR(backdrop, JQGfx.Circle, A(h.sun, .2f), 1); Rc(glow, 620 - r * 1.7f, 110 - r * 1.7f, r * 3.4f, r * 3.4f);
        var disc = SR(backdrop, JQGfx.Circle, h.sun, 2); Rc(disc, 620 - r, 110 - r, r * 2, r * 2);
        hill1 = Cols(100, h.h1, 3); hill2 = Cols(100, h.h2, 4);

        if (lv == 0)
        {
            clouds = new SpriteRenderer[4][];
            for (int i = 0; i < 4; i++)
                clouds[i] = new[] { SR(backdrop, JQGfx.Square, new Color(1, 1, 1, .55f), 5), SR(backdrop, JQGfx.Square, new Color(1, 1, 1, .55f), 5) };
            trees = new SpriteRenderer[9][];
            for (int i = 0; i < 9; i++)
                trees[i] = new[] {
                    SR(backdrop, JQGfx.Square, Hex("#3b2a4a"), 6),
                    SR(backdrop, JQGfx.Circle, Hex("#2f7a55"), 7), SR(backdrop, JQGfx.Circle, Hex("#2f7a55"), 7), SR(backdrop, JQGfx.Circle, Hex("#2f7a55"), 7),
                    SR(backdrop, JQGfx.HalfCircle, Hex("#2a6a4a"), 7) };
        }
        else if (lv == 1)
        {
            stars = new SpriteRenderer[40];
            for (int i = 0; i < 40; i++) stars[i] = SR(backdrop, JQGfx.Square, Color.white, 5);
            stal = new SpriteRenderer[19];
            for (int i = 0; i < 19; i++) stal[i] = SR(backdrop, JQGfx.Tri, Hex("#3a2552"), 5);
            flies = new SpriteRenderer[8];
            for (int i = 0; i < 8; i++) flies[i] = SR(backdrop, JQGfx.Square, Hex("#8be3ff"), 6);
            cryst = new SpriteRenderer[7][];
            for (int i = 0; i < 7; i++)
                cryst[i] = new[] { SR(backdrop, JQGfx.Tri, Hex("#6fe3ff"), 7), SR(backdrop, JQGfx.Tri, Hex("#b48cff"), 7) };
        }
        else
        {
            volc = SR(backdrop, JQGfx.Trapezoid, Hex("#2a0c10"), 5);
            volcRim = SR(backdrop, JQGfx.Square, Hex("#ff7a33"), 6);
            volcGlow = SR(backdrop, JQGfx.Square, A(Hex("#ff7a33"), .5f), 6);
            embers = new SpriteRenderer[24];
            for (int i = 0; i < 24; i++) embers[i] = SR(backdrop, JQGfx.Square, A(Hex("#ff6b2d"), .7f), 7);
        }
        Color water = lv == 2 ? Hex("#ff5a1f") : lv == 1 ? Hex("#0e2f44") : Hex("#1d6fa8");
        waterTop = Cols(100, water, 10); waterHi = Cols(100, new Color(1, 1, 1, .25f), 11);
    }

    void UpdateHill(SpriteRenderer[] a, float b, float amp, float w, float s)
    {
        for (int k = 0; k < a.Length; k++)
        {
            float x = k * 8;
            Rc(a[k], x, H - b + Mathf.Sin((x + cam * s) / w) * amp, 8, H);
        }
    }

    void UpdateBackdrop()
    {
        float t = Time.time;
        UpdateHill(hill1, 150, 34, 70, .2f);
        UpdateHill(hill2, 100, 26, 50, .45f);

        if (lv == 0)
        {
            float[][] cl = { new float[] { 100, 70, 60 }, new float[] { 360, 110, 80 }, new float[] { 560, 60, 70 }, new float[] { 760, 95, 50 } };
            float m = W + 200;
            for (int i = 0; i < 4; i++)
            {
                float X = Mod(cl[i][0] - cam * .1f - t * 8f * (i + 1), m) - 100;
                Rc(clouds[i][0], X, cl[i][1], cl[i][2], 12);
                Rc(clouds[i][1], X + 10, cl[i][1] - 8, cl[i][2] - 24, 10);
            }
            float o = cam * .6f; int b0 = Mathf.FloorToInt(o / 110f);
            for (int i = 0; i < 9; i++)
            {
                int n = b0 + i; float X = n * 110 - o; int s = (n * 7) & 3;
                var tr = trees[i];
                if (n % 3 != 0)
                {
                    tr[0].enabled = tr[1].enabled = tr[2].enabled = tr[3].enabled = true; tr[4].enabled = false;
                    Rc(tr[0], X + 22, 12 * T - 30, 8, 30);
                    float r1 = 16 + s;
                    Rc(tr[1], X + 26 - r1, 12 * T - 44 - r1, r1 * 2, r1 * 2);
                    Rc(tr[2], X + 14 - 12, 12 * T - 32 - 12, 24, 24);
                    Rc(tr[3], X + 38 - 12, 12 * T - 32 - 12, 24, 24);
                }
                else
                {
                    tr[0].enabled = tr[1].enabled = tr[2].enabled = tr[3].enabled = false; tr[4].enabled = true;
                    Rc(tr[4], X, 12 * T - 4 - 14, 52, 28);
                }
            }
        }
        else if (lv == 1)
        {
            for (int i = 0; i < 40; i++)
            {
                stars[i].color = new Color(1, 1, 1, .4f + .4f * Mathf.Sin(t * 2f + i));
                Rc(stars[i], (i * 97) % W, (i * 53) % 200, 2, 2);
            }
            for (int k = 0; k < 19; k++)
            {
                int x = -20 + 46 * k;
                float X = x - Mod(cam * .5f, 46f); float hh = 30 + ((x * 7) & 31);
                Tri(stal[k], X + 22, hh + 20, X + 22, 0, 44);
            }
            for (int i = 0; i < 8; i++)
            {
                flies[i].color = A(Hex("#8be3ff"), .5f + .5f * Mathf.Sin(t * 3f + i * 2f));
                Rc(flies[i], Mod(i * 131 - cam * .6f, W), 200 + (i * 47) % 150 + Mathf.Sin(t + i) * 8f, 3, 3);
            }
            float o = cam * .8f; int b0 = Mathf.FloorToInt(o / 150f);
            for (int i = 0; i < 7; i++)
            {
                int n = b0 + i; float X = n * 150 - o + ((n * 53) % 40);
                float al = .55f + .25f * Mathf.Sin(t * 2f + n);
                cryst[i][0].color = A(Hex("#6fe3ff"), al); cryst[i][1].color = A(Hex("#b48cff"), al);
                Tri(cryst[i][0], X + 6, 12 * T - 30 - (n % 3) * 10, X + 6, 12 * T, 12);
                Tri(cryst[i][1], X + 16, 12 * T - 20, X + 16, 12 * T, 12);
            }
        }
        else
        {
            float X = Mod(420 - cam * .12f, 1100f) - 250;
            Rc(volc, X, H - 240, 330, 240);
            Rc(volcRim, X + 120, H - 244, 90, 6);
            Rc(volcGlow, X + 160, H - 240, 10, 60);
            for (int i = 0; i < 24; i++)
                Rc(embers[i], Mod(i * 83 - cam * .3f + t * 10f, W), H - Mod(t * 40f + i * 37f, H), 3, 3);
        }

        for (int k = 0; k < 100; k++)
        {
            float x = k * 8, y = H - 58 + Mathf.Sin((x + cam) / 20f + t * 3f) * 4f;
            Rc(waterTop[k], x, y, 8, 80); Rc(waterHi[k], x, y, 8, 2);
        }
    }

    // =====================================================================
    // Objetos da fase (tiles, bandeira, espinhos, molas, gêiseres, inimigos, moedas, jogador)
    void BuildLevelObjects()
    {
        if (levelRoot != null) Destroy(levelRoot.gameObject);
        levelRoot = new GameObject("Level").transform;
        JQTheme h = d.th;
        tileEmberX.Clear(); tileEmberY.Clear();
        var emberList = new List<SpriteRenderer>();

        var cache = new Dictionary<int, Sprite>();
        for (int y = 0; y < 14; y++)
            for (int x = 0; x < d.w; x++)
            {
                int t = g[y][x]; if (t == 0) continue;
                bool top = y == 0 || g[y - 1][x] == 0;
                int key = t * 8 + (top ? 4 : 0) + (y == 13 ? 2 : 0) + (y & 1);
                Sprite sp;
                if (!cache.TryGetValue(key, out sp)) { sp = JQGfx.BakeTile(h, lv, t, top, y == 13, (y & 1) == 1); cache[key] = sp; }
                var tile = SR(levelRoot, sp, Color.white, 20); Rc(tile, x * T, y * T, T, T);
                if (top && t == 1 && lv == 0)
                    for (int i = 0; i < 4; i++)
                    {
                        var bld = SR(levelRoot, JQGfx.Square, h.gt, 21);
                        Rc(bld, x * T + 3 + i * 8, y * T - 3 - ((x + i) & 1) * 2, 2, 4);
                    }
                if (top && t == 1 && lv == 2)
                {
                    var em = SR(levelRoot, JQGfx.Square, Hex("#ffd27a"), 21);
                    emberList.Add(em); tileEmberX.Add(x); tileEmberY.Add(y * T);
                }
            }
        tileEmbers = emberList.ToArray();

        // bandeira
        float fx = d.fl * T;
        var pole = SR(levelRoot, JQGfx.Square, cWhite, 30); Rc(pole, fx + 14, 8 * T, 4, 4 * T);
        flagTri = SR(levelRoot, JQGfx.Tri, cRed, 31);
        var knob = SR(levelRoot, JQGfx.Circle, cYellow, 32); Rc(knob, fx + 16 - 5, 8 * T - 2 - 5, 10, 10);

        foreach (var s in sprs)
        {
            s.sr = new[] { SR(levelRoot, JQGfx.Square, cYellow, 34), SR(levelRoot, JQGfx.Square, h.pt, 34), SR(levelRoot, JQGfx.Square, cRed, 34) };
            Rc(s.sr[0], s.x + 2, s.y, T - 4, 5); Rc(s.sr[1], s.x + 6, s.y + 5, T - 12, 3); Rc(s.sr[2], s.x + 4, s.y + 8, T - 8, 4);
        }
        foreach (var q in gys)
            q.sr = new[] { SR(levelRoot, JQGfx.Square, Hex("#ff6b2d"), 35), SR(levelRoot, JQGfx.Square, Hex("#ffe27a"), 36), SR(levelRoot, JQGfx.Square, Hex("#ff6b2d"), 35) };
        foreach (var s in sps)
        {
            s.sr = new SpriteRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                float x = s.x - 3 + i * 16;
                s.sr[i] = SR(levelRoot, JQGfx.Tri, Hex("#dfe6f2"), 37);
                Tri(s.sr[i], x + 8, s.y, x + 8, s.y + s.h, 16);
            }
        }
        foreach (var c in coins)
            c.sr = new[] { SR(levelRoot, JQGfx.Circle, A(cPts, .22f), 40), SR(levelRoot, JQGfx.Star, cPts, 41) };
        foreach (var e in ens) MakeEnemySR(e);

        // jogador (21 peças)
        var ps = new SpriteRenderer[21];
        Sprite[] spr = { JQGfx.Square, JQGfx.Square, JQGfx.Circle, JQGfx.Circle, JQGfx.Square, JQGfx.Square, JQGfx.Square, JQGfx.Square, JQGfx.Square,
            JQGfx.Square, JQGfx.Square, JQGfx.Square, JQGfx.Square, JQGfx.Circle, JQGfx.Circle, JQGfx.HalfCircle, JQGfx.Square, JQGfx.Circle,
            JQGfx.Ring, JQGfx.Square, JQGfx.Square };
        string[] col = { "#6a4fb3", "#6a4fb3", "#f2a33a", "#ffbf5e", "#d9862a", "#f4f0ff", "#f4f0ff", "#1d2140", "#1d2140",
            "#f2a33a", "#f2a33a", "#c98f63", "#c98f63", "#c98f63", "#b57a50", "#1fb5a6", "#168f84", "#ffe27a",
            "#1d2140", "#1d2140", "#7a3b2a" };
        for (int i = 0; i < 21; i++) ps[i] = SR(levelRoot, spr[i], Hex(col[i]), 100 + i);
        p.sr = ps;
    }

    void MakeEnemySR(Ent e)
    {
        JQTheme h = d.th;
        if (e.fly)
            e.sr = new[] { SR(levelRoot, JQGfx.Tri, h.e, 50), SR(levelRoot, JQGfx.Tri, h.e, 50), SR(levelRoot, JQGfx.Square, h.e, 51),
                           SR(levelRoot, JQGfx.Square, cShadow, 52), SR(levelRoot, JQGfx.Square, cShadow, 52) };
        else
            e.sr = new[] { SR(levelRoot, JQGfx.Square, h.e, 50), SR(levelRoot, JQGfx.Square, h.e, 50), SR(levelRoot, JQGfx.Square, cShadow, 52),
                           SR(levelRoot, JQGfx.Square, cShadow, 52), SR(levelRoot, JQGfx.Square, cShadow, 52) };
    }

    static void KillSR(Ent e) { if (e.sr != null) for (int i = 0; i < e.sr.Length; i++) if (e.sr[i] != null) Destroy(e.sr[i].gameObject); }

    void UpdateWorld()
    {
        JQTheme h = d.th;
        float tt = Time.time * 1000f / 300f;

        for (int i = 0; i < tileEmbers.Length; i++)
            Rc(tileEmbers[i], tileEmberX[i] * T + 4 + (((tileEmberX[i] * 5 + (fr >> 3)) & 3) * 7), tileEmberY[i] - 4, 3, 4);

        float fx = d.fl * T;
        Tri(flagTri, fx + 18 + 26 + Mathf.Sin(fr / 8f) * 3f, 8 * T + 12, fx + 18, 8 * T + 12, 24);

        foreach (var q in gys)
        {
            int ph = (fr + q.ph) % 150; var s = q.sr;
            if (ph < 60)
            {
                float k = Mathf.Min(1f, ph / 8f);
                s[0].enabled = s[1].enabled = true; s[2].enabled = false;
                Rc(s[0], q.x, q.y + q.h * (1 - k), q.w, q.h * k);
                Rc(s[1], q.x + 8, q.y + q.h * (1 - k) + 6, 12, Mathf.Max(0, q.h * k - 6));
            }
            else if (ph > 120)
            {
                s[0].enabled = s[1].enabled = false; s[2].enabled = true;
                Rc(s[2], q.x + 6, q.y + q.h - 6 - (ph % 6), 16, 6);
            }
            else En(s, false);
        }

        foreach (var c in coins)
        {
            float cy = c.y + 8 + Mathf.Sin(tt + c.x) * 2f;
            Rc(c.sr[0], c.x + 8 - 13, cy - 13, 26, 26);
            Rc(c.sr[1], c.x + 8 - 9, cy - 9, 18, 18);
        }

        foreach (var e in ens)
        {
            var s = e.sr;
            if (e.fly)
            {
                float w = Mathf.Sin(e.t * 4f) * 6f;
                Tri(s[0], e.x - 4, e.y + 3 + w, e.x + 8.5f, e.y + 11.5f, 10.3f);
                Tri(s[1], e.x + 30, e.y + 3 + w, e.x + 17.5f, e.y + 11.5f, 10.3f);
                Rc(s[2], e.x + 7, e.y + 3, 12, 14);
                Rc(s[3], e.x + 9, e.y + 8, 3, 3); Rc(s[4], e.x + 15, e.y + 8, 3, 3);
            }
            else
            {
                Color c = e.hp > 1 ? cHard : h.e; s[0].color = c; s[1].color = c;
                Rc(s[0], e.x, e.y + 4, e.w, e.h - 4); Rc(s[1], e.x + 4, e.y, e.w - 8, 6);
                Rc(s[2], e.x + (e.d > 0 ? 16 : 5), e.y + 9, 6, 6);
                Rc(s[3], e.x + 3, e.y + e.h - 4, 8, 4); Rc(s[4], e.x + e.w - 11, e.y + e.h - 4, 8, 4);
            }
        }
        foreach (var b in bl) Rc(b.sr[0], b.x, b.y, b.w, b.h);

        UpdatePlayer();
    }

    void UpdatePlayer()
    {
        var s = p.sr; float x = p.x, y = p.y; int f = p.f;
        bool vis = !(p.inv > 0 && (p.inv / 4) % 2 == 1);
        En(s, vis); if (!vis) return;
        bool mv = p.on && p.vx != 0;
        int a = mv ? (fr / 6) % 2 * 3 : 0, b = mv ? 3 - a : 0;
        Rc(s[0], x + 4, y + 21, 8, 6); Rc(s[1], x + 16, y + 21, 8, 6);
        Rc(s[2], x, y + 9, 28, 20);
        Rc(s[3], x + 5, y + 11, 18, 12);
        Rc(s[4], x + 2, y + 21, 24, 3);
        Rc(s[5], x + 1, y + 26 - a, 11, 4); Rc(s[6], x + 16, y + 26 - b, 11, 4);
        Rc(s[7], x + 1, y + 29 - a, 11, 1); Rc(s[8], x + 16, y + 29 - b, 11, 1);
        Rc(s[9], x + 1, y + 13, 5, 8); Rc(s[10], x + 22, y + 13, 5, 8);
        Rc(s[11], x + 1, y + 20, 5, 4); Rc(s[12], x + 22, y + 20, 5, 4);
        Rc(s[13], x + 5, y, 18, 16);
        float ex = f > 0 ? 23 : 5; Rc(s[14], x + ex - 2.5f, y + 11 - 2.5f, 5, 5);
        Rc(s[15], x + 14 - 9.5f, y + 6 - 9.5f, 19, 19);
        Rc(s[16], x + 4, y + 4, 20, 4);
        Rc(s[17], x + 14 - 3.5f, y - 2 - 3.5f, 7, 7);
        float gx = x + (f > 0 ? 19 : 9);
        Rc(s[18], gx - 4.25f, y + 11 - 4.25f, 8.5f, 8.5f); Rc(s[19], gx, y + 10, 2, 2);
        Rc(s[20], x + (f > 0 ? 15 : 9), y + 15, 6, 1);
    }

    void UpdateHud()
    {
        for (int i = 0; i < 3; i++)
            for (int k = 0; k < 3; k++) hearts[i][k].color = i < lives ? cHeart : cHeartOff;
        bool dead = st == St.Dead;
        dim.enabled = vigDead.enabled = dead;
        if (dead) dim.color = A(Color.black, Mathf.Min(1f, dt / 40f) * .55f);
    }

    // =====================================================================
    // Visibilidade / render
    bool InGame() { return st != St.Menu && levelRoot != null; }

    void ApplyVisibility()
    {
        bool gm = InGame();
        if (levelRoot != null) levelRoot.gameObject.SetActive(gm);
        screenLayer.gameObject.SetActive(gm);
        mainCam.backgroundColor = gm ? Color.black : MenuBg;
    }

    void Render()
    {
        if (!InGame()) return;
        screenLayer.position = new Vector3(Mathf.Round(cam), 0, 0);
        UpdateBackdrop(); UpdateWorld(); UpdateHud();
    }

    // =====================================================================
    // Loop principal
    void Update()
    {
        if (JQInput.MuteDown) audio.ToggleMute();
        kL = JQInput.Left; kR = JQInput.Right; kJ = JQInput.JumpHeld;

        if (uiOn && uiBtns != null && uiBtns.Length > 0)
        {
            int n = uiBtns.Length;
            if (JQInput.UpDown) sel = (sel + n - 1) % n;
            if (JQInput.DownDown) sel = (sel + 1) % n;
            if (JQInput.ConfirmDown) Act(uiBtns[sel].act);
        }
        else if (st == St.Play)
        {
            if (JQInput.JumpDown) jumpPressed = true;
            if (JQInput.ShootDown) shootPressed = true;
        }

        acc += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        while (acc >= DT) { Step(); acc -= DT; }
        ApplyVisibility(); Render();
    }

    // =====================================================================
    // Lógica do jogo (port direto do JS)
    bool Solid(int a, int b)
    {
        if (a < 0 || a >= d.w) return true;
        if (b < 0 || b >= 14) return false;
        return g[b][a] > 0;
    }

    bool HitX(Ent o, out int ca, out int cb)
    {
        int x0 = Mathf.FloorToInt(o.x / T), x1 = Mathf.FloorToInt((o.x + o.w - 1) / T);
        int y0 = Mathf.FloorToInt(o.y / T), y1 = Mathf.FloorToInt((o.y + o.h - 1) / T);
        for (int a = x0; a <= x1; a++)
            for (int b = y0; b <= y1; b++)
                if (Solid(a, b)) { ca = a; cb = b; return true; }
        ca = cb = 0; return false;
    }

    void Move(Ent o, out bool wall, out bool ground)
    {
        wall = false; ground = false; int ca, cb;
        o.x += o.vx;
        if (HitX(o, out ca, out cb))
        {
            if (o.vx > 0) o.x = ca * T - o.w; else o.x = (ca + 1) * T;
            wall = true;
        }
        o.y += o.vy;
        if (HitX(o, out ca, out cb))
        {
            if (o.vy > 0) { o.y = cb * T - o.h; ground = true; } else o.y = (cb + 1) * T;
            o.vy = 0;
        }
    }

    static bool Ov(Ent a, Ent b) { return a.x < b.x + b.w && a.x + a.w > b.x && a.y < b.y + b.h && a.y + a.h > b.y; }

    void Snd(float f, float t, JQAudio.Wave w = JQAudio.Wave.Square) { audio.Sfx(f, t, w); }

    void Over() { st = St.Dead; dt = 0; uiOn = false; Snd(90, .9f, JQAudio.Wave.Saw); }

    void Over2()
    {
        st = St.Over;
        Show("VOCÊ PERDEU", "Pontos: " + score, new[] { new Btn("again", "TENTAR NOVAMENTE"), new Btn("menu", "MENU") });
    }

    void Hurt()
    {
        if (p.inv > 0) return;
        lives--; Snd(120, .25f, JQAudio.Wave.Saw);
        if (lives <= 0) { Over(); return; }
        p.inv = 90; p.vy = -6; p.kb = 12; p.kx = -p.f * 3;
    }

    void ResetPlayer()
    {
        p.x = 2 * T; p.y = 12 * T - 30; p.w = 28; p.h = 30; p.vx = p.vy = 0;
        p.on = true; p.f = 1; p.inv = 90; p.cd = 0; p.kb = 0; p.kx = 0; p.coy = 0;
    }

    void Step()
    {
        if (st == St.Clear)
        {
            if (--ct <= 0)
            {
                if (lv < 2) Load(lv + 1);
                else { st = St.Win; Show("VOCÊ VENCEU!", "Pontos: " + score, new[] { new Btn("play", "JOGAR DE NOVO"), new Btn("menu", "MENU") }); }
            }
            return;
        }
        if (st == St.Dead) { if (++dt > 170) Over2(); return; }
        if (st != St.Play) return;
        objT--; fr++;

        if (p.inv > 0) p.inv--;
        if (p.cd > 0) p.cd--;
        float ax = (kR ? 1 : 0) - (kL ? 1 : 0);
        if (p.kb > 0) { p.kb--; p.vx = p.kx; }
        else { p.vx = ax * 3.2f; if (ax != 0) p.f = (int)ax; }
        p.coy = p.on ? 6 : p.coy - 1;
        if (jumpPressed && p.coy > 0) { p.vy = -11; p.coy = 0; Snd(520, .1f); }
        jumpPressed = false;
        if (!kJ && p.vy < -4) p.vy = -4;
        if (shootPressed && p.cd <= 0)
        {
            bl.Add(new Ent { x = p.x + (p.f > 0 ? p.w : -8), y = p.y + 10, w = 8, h = 6, vx = p.f * 7, life = 70,
                             sr = new[] { SR(levelRoot, JQGfx.Square, Color.white, 60) } });
            p.cd = 18; Snd(300, .06f, JQAudio.Wave.Saw);
        }
        shootPressed = false;

        bool wl, gr;
        p.vy = Mathf.Min(p.vy + .5f, 12); Move(p, out wl, out gr); p.on = gr;
        if (p.y > H + 40)
        {
            lives--; Snd(120, .3f, JQAudio.Wave.Saw);
            if (lives <= 0) { Over(); return; }
            ResetPlayer();
        }

        // tiros
        for (int i = 0; i < bl.Count; i++)
        {
            var b = bl[i]; int ca, cb;
            b.x += b.vx; b.life--;
            if (HitX(b, out ca, out cb)) b.life = 0;
            foreach (var e in ens)
                if (b.life > 0 && Ov(b, e))
                {
                    b.life = 0; e.hp--; Snd(200, .08f);
                    if (e.hp <= 0) { e.dead = true; score += 100; }
                }
        }
        bl.RemoveAll(b => { if (b.life <= 0) { KillSR(b); return true; } return false; });

        // inimigos
        foreach (var e in ens)
        {
            bool wall = false, ground = false;
            if (e.fly)
            {
                e.t += .05f; e.x += e.d * 1.2f; e.y = e.y0 + Mathf.Sin(e.t) * 28f;
                if (Mathf.Abs(e.x - e.x0) > 110) e.d *= -1;
            }
            else
            {
                e.vy = Mathf.Min(e.vy + .5f, 12); e.vx = e.d * .9f; Move(e, out wall, out ground);
            }
            e.on = ground;
            if (wall) e.d *= -1;
            else if (e.on && !Solid(Mathf.FloorToInt((e.x + (e.d > 0 ? e.w + 2 : -2)) / T), Mathf.FloorToInt((e.y + e.h + 2) / T))) e.d *= -1;
            if (e.y > H + 80) e.dead = true;
            if (!e.dead && Ov(p, e))
            {
                if (p.vy > 0 && p.y + p.h - e.y < 16) { e.dead = true; p.vy = -8; score += 100; Snd(660, .1f); }
                else Hurt();
            }
        }
        ens.RemoveAll(e => { if (e.dead) { KillSR(e); return true; } return false; });

        foreach (var s in sps) if (Ov(p, s)) Hurt();
        foreach (var s in sprs) if (Ov(p, s) && p.vy >= 0) { p.vy = -17; p.on = false; Snd(700, .15f); }
        foreach (var q in gys) if ((fr + q.ph) % 150 < 60 && Ov(p, q)) Hurt();

        for (int i = coins.Count - 1; i >= 0; i--)
        {
            var c = coins[i];
            if (Mathf.Abs(c.x + 8 - (p.x + 14)) < 22 && Mathf.Abs(c.y + 8 - (p.y + 15)) < 24)
            {
                score += 10; Snd(880, .07f); KillSR(c); coins.RemoveAt(i);
            }
        }

        var fl = new Ent { x = flagBox.x, y = flagBox.y, w = flagBox.w, h = flagBox.h };
        if (Ov(p, fl)) { st = St.Clear; ct = 110; score += 200; Snd(988, .3f); }

        cam += (Mathf.Max(0, Mathf.Min(p.x - W / 2f + 11, d.w * T - W)) - cam) * .15f;
    }

    // =====================================================================
    // Carregar fase
    void Load(int n)
    {
        audio.Music(n);
        lv = n; d = JQData.Levels[n]; sc0 = score;
        g = new int[14][];
        for (int y = 0; y < 14; y++) g[y] = new int[d.w];
        for (int x = 0; x < d.w; x++)
        {
            bool pit = false;
            foreach (var q in d.pits) if (x >= q[0] && x <= q[1]) pit = true;
            if (!pit) { g[12][x] = 1; g[13][x] = 1; }
        }

        foreach (var c in coins) KillSR(c);
        coins = new List<Ent>();
        foreach (var pl in d.pl)
            for (int i = 0; i < pl[2]; i++)
            {
                g[pl[1]][pl[0] + i] = 2;
                coins.Add(new Ent { x = (pl[0] + i) * T + 8, y = (pl[1] - 1) * T + 8 });
            }
        for (int x = 6; x < d.w - 4; x += 7)
            if (g[12][x] != 0) coins.Add(new Ent { x = x * T + 8, y = 11 * T + 8 });

        ens = new List<Ent>();
        foreach (var t in d.en) ens.Add(new Ent { x = t * T + 2, y = 12 * T - 28, w = 28, h = 28, d = -1, hp = n == 2 ? 2 : 1 });
        foreach (var b in d.bat) ens.Add(new Ent { x = b[0] * T, x0 = b[0] * T, y = b[1] * T, y0 = b[1] * T, w = 26, h = 18, d = 1, t = b[0], hp = 1, fly = true });
        sprs = new List<Ent>();
        foreach (var t in d.spr) sprs.Add(new Ent { x = t * T, y = 12 * T - 12, w = T, h = 12 });
        gys = new List<Ent>();
        for (int i = 0; i < d.gey.Length; i++) gys.Add(new Ent { x = d.gey[i] * T + 2, y = 9 * T, w = 28, h = 3 * T, ph = i * 50 });
        sps = new List<Ent>();
        foreach (var t in d.sp) sps.Add(new Ent { x = t * T + 3, y = 11 * T + 14, w = 26, h = 18 });
        bl = new List<Ent>();
        flagBox = new Box { x = d.fl * T, y = 8 * T, w = T, h = 4 * T };

        p = new Ent(); ResetPlayer();
        BuildBackdrop(); BuildLevelObjects();
        cam = 0; objT = 260; st = St.Play; uiOn = false;
        ApplyVisibility();
    }

    // =====================================================================
    // Menus
    void Show(string title, string text, Btn[] b) { uiTitle = title; uiText = text; uiBtns = b; sel = 0; uiOn = true; }

    void ShowMenu()
    {
        st = St.Menu;
        Show("JUMP QUEST", "Jogo 2D de plataforma · Cloud 10",
            new[] { new Btn("play", "JOGAR"), new Btn("help", "CONTROLES"), new Btn("bye", "SAIR") });
    }

    void Act(string a)
    {
        audio.Music(lv); Snd(440, .08f);
        switch (a)
        {
            case "play": score = 0; lives = 3; Load(0); break;
            case "again": score = sc0; lives = 3; Load(lv); break;
            case "menu": ShowMenu(); break;
            case "help":
                Show("CONTROLES", "Mover: setas ou A / D\nPular: espaço, seta para cima ou W\nAtirar: X ou K\nPule em cima dos inimigos para derrotá-los.\nChegue à bandeira para concluir a fase.",
                    new[] { new Btn("menu", "VOLTAR") });
                break;
            case "bye":
                Show("ATÉ LOGO!", "Obrigado por jogar Jump Quest.",
                    new[] { new Btn("menu", "VOLTAR AO MENU"), new Btn("quit", "FECHAR O JOGO") });
                break;
            case "quit":
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    // =====================================================================
    // Texto e menus com IMGUI (escala automática para 800x448)
    void InitStyles()
    {
        stylesReady = true;
        lab = new GUIStyle(GUI.skin.label) { wordWrap = false, clipping = TextClipping.Overflow, richText = false };
        if (uiFont != null) lab.font = uiFont;
        para = new GUIStyle(lab) { wordWrap = true, alignment = TextAnchor.UpperCenter, fontSize = 16 };
        para.normal.textColor = cWhite;
    }

    static void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }

    void Txt(string s, float x, float y, TextAnchor al, Color c, int size)
    {
        lab.fontSize = size; lab.alignment = al;
        float w = 700f, left = al == TextAnchor.UpperCenter ? x - w / 2f : al == TextAnchor.UpperRight ? x - w : x;
        var r = new Rect(left, y - size * .9f, w, size * 1.6f);
        lab.normal.textColor = new Color(cShadow.r, cShadow.g, cShadow.b, c.a);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, lab);
        lab.normal.textColor = c;
        GUI.Label(r, s, lab);
    }

    void OnGUI()
    {
        if (!stylesReady) InitStyles();
        float sc = Mathf.Min(Screen.width / (float)W, Screen.height / (float)H);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - W * sc) / 2f, (Screen.height - H * sc) / 2f, 0),
                                   Quaternion.identity, new Vector3(sc, sc, 1));

        if (InGame() && st != St.Dead)
        {
            Txt("FASE " + (lv + 1) + "/3", W / 2f, 26, TextAnchor.UpperCenter, cWhite, 16);
            Txt("PONTOS " + score, W - 52, 26, TextAnchor.UpperRight, cPts, 16);
            if (objT > 0 && st == St.Play)
            {
                Txt(d.th.n, W / 2f, 60, TextAnchor.UpperCenter, cWhite, 16);
                Txt("OBJETIVO: CHEGUE À BANDEIRA", W / 2f, 84, TextAnchor.UpperCenter, cPts, 16);
            }
            if (st == St.Clear) Txt("FASE CONCLUÍDA!", W / 2f, H / 2f, TextAnchor.UpperCenter, cPts, 24);
        }

        if (st == St.Dead)
        {
            float z = 1f + Mathf.Max(0f, 1f - dt / 50f) * .35f, al = Mathf.Min(1f, dt / 30f);
            int size = Mathf.RoundToInt(64 * z);
            lab.fontSize = size; lab.alignment = TextAnchor.MiddleCenter;
            var r = new Rect(W / 2f - 400, H / 2f - 70, 800, 140);
            lab.normal.textColor = new Color(0, 0, 0, al);
            GUI.Label(new Rect(r.x + 4, r.y + 4, r.width, r.height), DEATH_TEXT, lab);
            lab.normal.textColor = new Color(.769f, .086f, .11f, al);
            GUI.Label(r, DEATH_TEXT, lab);
        }

        if (uiOn && uiBtns != null)
        {
            Fill(new Rect(0, 0, W, H), new Color(cShadow.r, cShadow.g, cShadow.b, .86f));
            var btns = uiBtns;
            float ph = para.CalcHeight(new GUIContent(uiText), 560f);
            float total = 56 + 12 + ph + 14 + btns.Length * 54 - 10;
            float y = (H - total) / 2f;

            lab.fontSize = 44; lab.alignment = TextAnchor.MiddleCenter; lab.normal.textColor = cYellow;
            GUI.Label(new Rect(0, y, W, 56), uiTitle, lab);
            GUI.Label(new Rect((W - 560) / 2f, y + 68, 560, ph + 4), uiText, para);

            Vector2 mp = Event.current.mousePosition; bool moved = mp != lastMouse;
            float by = y + 56 + 12 + ph + 14;
            for (int i = 0; i < btns.Length; i++)
            {
                var r = new Rect((W - 260) / 2f, by + i * 54, 260, 44);
                if (moved && r.Contains(mp)) sel = i;
                bool on = i == sel;
                Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), cWhite);
                Fill(r, on ? cYellow : cRed);
                lab.fontSize = 18; lab.alignment = TextAnchor.MiddleCenter; lab.normal.textColor = on ? cInk : Color.white;
                GUI.Label(r, btns[i].label, lab);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { Act(btns[i].act); break; }
            }
            lastMouse = mp;
        }

        // botão de som (tecla M)
        var mr = new Rect(W - 40, 6, 34, 34);
        float ma = audio != null && audio.Muted ? .45f : 1f;
        Fill(new Rect(mr.x - 2, mr.y - 2, mr.width + 4, mr.height + 4), new Color(cWhite.r, cWhite.g, cWhite.b, ma));
        Fill(mr, new Color(cShadow.r, cShadow.g, cShadow.b, .85f));
        lab.fontSize = 16; lab.alignment = TextAnchor.MiddleCenter; lab.normal.textColor = new Color(cWhite.r, cWhite.g, cWhite.b, ma);
        GUI.Label(mr, "M", lab);
        if (GUI.Button(mr, GUIContent.none, GUIStyle.none)) audio.ToggleMute();
    }
}
