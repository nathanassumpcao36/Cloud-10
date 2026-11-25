using System.Collections.Generic;
using UnityEngine;

// Áudio 100% sintetizado (igual ao WebAudio do HTML): efeitos + música procedural por fase.
public class JQAudio : MonoBehaviour
{
    public enum Wave { Square, Saw, Sine, Triangle }

    const int SR = 44100;
    const int POOL = 28;
    const float SFX_VOL = 0.12f;
    const float MUSIC_BOOST = 2.2f;

    static readonly int[][] SC = { new[] { 0, 2, 4, 7, 9 }, new[] { 0, 3, 5, 7, 10 }, new[] { 0, 1, 5, 7, 8 } };
    static readonly int[] RT = { 57, 50, 45 };
    static readonly int[] BP = { 84, 62, 104 };

    AudioSource sfx;
    AudioSource[] pool;
    int pi;
    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    int level = -1;
    bool playing;
    double nt;
    int beat;

    public bool Muted { get; private set; }

    void Awake()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false; sfx.spatialBlend = 0f;
        pool = new AudioSource[POOL];
        for (int i = 0; i < POOL; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false; s.spatialBlend = 0f;
            pool[i] = s;
        }
    }

    public void ToggleMute()
    {
        Muted = !Muted;
        AudioListener.volume = Muted ? 0f : 1f;
    }

    public void Sfx(float freq, float dur, Wave w)
    {
        if (Muted) return;
        sfx.PlayOneShot(SfxClip(freq, dur, w), SFX_VOL);
    }

    public void Music(int n)
    {
        if (level == n) return;
        level = n; beat = 0;
        for (int i = 0; i < POOL; i++) pool[i].Stop();
        nt = AudioSettings.dspTime + 0.1;
        playing = true;
    }

    void Update()
    {
        if (!playing) return;
        double now = AudioSettings.dspTime;
        if (nt < now - 1.0) nt = now + 0.1;
        int guard = 0;
        while (nt < now + 0.5 && guard++ < 64) Beat();
    }

    static int S(int[] sc, int i) { return sc[i % 5] + 12 * (i / 5); }

    void Beat()
    {
        int n = level; int[] sc = SC[n]; int r = RT[n]; float bt = 60f / BP[n];
        if (beat % 8 == 0)
        {
            for (int k = 0; k < 3; k++) Tone(r + S(sc, k * 2), nt, bt * 8f, Wave.Triangle, .05f, bt * 2f);
            Tone(r - 12, nt, bt * 8f, Wave.Sine, .12f, bt);
        }
        if (Random.value < .65f)
            Tone(r + 12 + S(sc, Random.Range(0, 8)), nt, bt * 2.5f, n == 2 ? Wave.Saw : Wave.Triangle,
                 n == 2 ? .025f : .05f, .02f);
        if (n == 2 && beat % 2 == 0) Tone(r - 24, nt, .2f, Wave.Sine, .25f, .01f);
        if (n == 1 && Random.value < .2f)
            Tone(r + 36 + S(sc, Random.Range(0, 5)), nt, bt * 3f, Wave.Sine, .03f, .01f);
        nt += bt; beat++;
    }

    void Tone(int midi, double when, float du, Wave w, float vo, float att)
    {
        float f = 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        var s = pool[pi]; pi = (pi + 1) % POOL;
        s.Stop();
        s.clip = NoteClip(f, du, w, att, vo);
        s.volume = vo * 0.6f * MUSIC_BOOST;
        s.PlayScheduled(when);
    }

    static float Osc(Wave w, float f, int i)
    {
        double ph = (f * (double)i / SR) % 1.0;
        switch (w)
        {
            case Wave.Square: return ph < 0.5 ? 1f : -1f;
            case Wave.Saw: return (float)(2.0 * ph - 1.0);
            case Wave.Sine: return Mathf.Sin((float)(ph * 2.0 * Mathf.PI));
            default: return (float)(4.0 * System.Math.Abs(ph - 0.5) - 1.0);
        }
    }

    AudioClip SfxClip(float f, float dur, Wave w)
    {
        string key = "s|" + (int)(f * 10) + "|" + (int)(dur * 1000) + "|" + (int)w;
        AudioClip c;
        if (cache.TryGetValue(key, out c)) return c;
        int n = Mathf.Max(1, (int)(dur * SR));
        var data = new float[n];
        float k = Mathf.Log(0.0025f); // decaimento exponencial 0.04 -> 0.0001
        for (int i = 0; i < n; i++)
        {
            float s = (float)i / SR;
            data[i] = Osc(w, f, i) * Mathf.Exp(k * s / dur);
        }
        c = AudioClip.Create(key, n, 1, SR, false); c.SetData(data, 0);
        cache[key] = c; return c;
    }

    AudioClip NoteClip(float f, float du, Wave w, float att, float vo)
    {
        string key = "n|" + (int)(f * 10) + "|" + (int)(du * 1000) + "|" + (int)w + "|" + (int)(att * 1000) + "|" + (int)(vo * 1000);
        AudioClip c;
        if (cache.TryGetValue(key, out c)) return c;
        int n = Mathf.Max(1, (int)(du * SR));
        var data = new float[n];
        float k = Mathf.Log(0.0001f / vo);
        float span = Mathf.Max(0.001f, du - att);
        for (int i = 0; i < n; i++)
        {
            float s = (float)i / SR;
            float env = s < att ? s / att : Mathf.Exp(k * (s - att) / span);
            data[i] = Osc(w, f, i) * env;
        }
        c = AudioClip.Create(key, n, 1, SR, false); c.SetData(data, 0);
        cache[key] = c; return c;
    }
}
