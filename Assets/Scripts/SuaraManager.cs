using UnityEngine;

// Suara narasi (Text-to-Speech HP Android) + efek klik/benar/salah dibuat lewat kode.
// TIDAK perlu dipasang manual: otomatis dibuat saat aplikasi mulai.
public class SuaraManager : MonoBehaviour
{
    public static SuaraManager Instance { get; private set; }

    [Range(0.5f, 1.5f)] public float kecepatanBicara = 0.95f;
    [Range(0.5f, 2f)]   public float nadaBicara = 1.05f;
    [Range(0f, 1f)]     public float volumeEfek = 0.7f;

    AudioSource sfx;
    AudioClip cKlik, cPop, cBenar, cSalah, cSelesai;
    float minSampai, maksSampai, estimasiSampai;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBuat()
    {
        if (Instance == null) new GameObject("SuaraManager").AddComponent<SuaraManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;

        cKlik    = Nada("klik",    new[] { 1500f }, 0.045f, 0.45f, false);
        cPop     = Nada("pop",     new[] { 700f, 1050f }, 0.06f, 0.40f, false);
        cBenar   = Nada("benar",   new[] { 523.25f, 659.25f, 783.99f }, 0.11f, 0.50f, false);
        cSalah   = Nada("salah",   new[] { 233f, 175f }, 0.20f, 0.35f, true);
        cSelesai = Nada("selesai", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.13f, 0.50f, false);

        InitTTS();
    }

    static AudioClip Nada(string nama, float[] frek, float durPerNada, float vol, bool kasar)
    {
        const int sr = 44100;
        int nPer = (int)(sr * durPerNada);
        float[] data = new float[nPer * frek.Length];
        for (int k = 0; k < frek.Length; k++)
        {
            for (int i = 0; i < nPer; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Min(1f, i / (sr * 0.005f)) * Mathf.Exp(-t * 5f / durPerNada);
                float s = Mathf.Sin(2f * Mathf.PI * frek[k] * t);
                if (kasar) s = Mathf.Clamp(s * 3f, -1f, 1f) * 0.6f;
                data[k * nPer + i] = s * env * vol;
            }
        }
        AudioClip c = AudioClip.Create(nama, data.Length, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    void Main(AudioClip c) { if (c != null && sfx != null) sfx.PlayOneShot(c, volumeEfek); }

    public static void Klik()    { if (Instance) Instance.Main(Instance.cKlik); }
    public static void Pop()     { if (Instance) Instance.Main(Instance.cPop); }
    public static void Benar()   { if (Instance) Instance.Main(Instance.cBenar); }
    public static void Salah()   { if (Instance) Instance.Main(Instance.cSalah); }
    public static void Selesai() { if (Instance) Instance.Main(Instance.cSelesai); }
    public static void Ucapkan(string teks) { if (Instance) Instance.UcapkanI(teks); }
    public static void Berhenti() { if (Instance) Instance.BerhentiI(); }
    public static bool SedangBicara { get { return Instance && Instance.SedangBicaraI(); } }

    void UcapkanI(string teks)
    {
        if (string.IsNullOrEmpty(teks)) return;
        float perkiraan = teks.Length / (13f * kecepatanBicara) + 0.8f;
        minSampai = Time.time + 0.7f;
        estimasiSampai = Time.time + perkiraan;
        maksSampai = Time.time + perkiraan + 3f;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (siap) BicaraTTS(teks); else antrean = teks;
#else
        Debug.Log("[Narasi] " + teks);
#endif
    }

    void BerhentiI()
    {
        minSampai = maksSampai = estimasiSampai = 0f;
#if UNITY_ANDROID && !UNITY_EDITOR
        antrean = null;
        if (siap && tts != null) { try { tts.Call<int>("stop"); } catch { } }
#endif
    }

    bool SedangBicaraI()
    {
        if (Time.time < minSampai) return true;
        if (Time.time > maksSampai) return false;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (siap && tts != null) { try { return tts.Call<bool>("isSpeaking"); } catch { } }
#endif
        return Time.time < estimasiSampai;
    }

    void OnApplicationPause(bool jeda) { if (jeda) BerhentiI(); }

#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaObject tts;
    volatile int statusInit = int.MinValue;
    bool siap, gagal;
    string antrean;
    int nomor;

    class PendengarInit : AndroidJavaProxy
    {
        readonly SuaraManager m;
        public PendengarInit(SuaraManager m) : base("android.speech.tts.TextToSpeech$OnInitListener") { this.m = m; }
        public void onInit(int status) { m.statusInit = status; }
    }

    void InitTTS()
    {
        try
        {
            using (AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject act = up.GetStatic<AndroidJavaObject>("currentActivity");
                tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", act, new PendengarInit(this));
            }
        }
        catch (System.Exception e) { gagal = true; Debug.LogWarning("TTS tidak bisa dibuat: " + e.Message); }
    }

    void Update()
    {
        if (siap || gagal || tts == null) return;
        int st = statusInit;
        if (st == int.MinValue) return;
        if (st != 0) { gagal = true; Debug.LogWarning("TTS gagal init, status " + st); return; }

        AturBahasa();
        siap = true;
        if (antrean != null) { BicaraTTS(antrean); antrean = null; }
    }

    void AturBahasa()
    {
        try
        {
            int r;
            using (AndroidJavaObject loc = new AndroidJavaObject("java.util.Locale", "id", "ID"))
                r = tts.Call<int>("setLanguage", loc);
            if (r < 0)
                using (AndroidJavaObject loc = new AndroidJavaObject("java.util.Locale", "in", "ID"))
                    r = tts.Call<int>("setLanguage", loc);
            if (r < 0) Debug.LogWarning("Suara Bahasa Indonesia belum terpasang di HP (kode " + r + ")");
            tts.Call<int>("setSpeechRate", kecepatanBicara);
            tts.Call<int>("setPitch", nadaBicara);
        }
        catch (System.Exception e) { Debug.LogWarning("TTS setLanguage: " + e.Message); }
    }

    void BicaraTTS(string teks)
    {
        try
        {
            using (AndroidJavaObject b = new AndroidJavaObject("android.os.Bundle"))
                tts.Call<int>("speak", teks, 0, b, "narasi" + (nomor++));
        }
        catch (System.Exception e) { Debug.LogWarning("TTS speak: " + e.Message); }
    }

    void OnDestroy()
    {
        if (tts != null) { try { tts.Call<int>("stop"); tts.Call("shutdown"); } catch { } tts.Dispose(); tts = null; }
        if (Instance == this) Instance = null;
    }
#else
    void InitTTS() { }
    void OnDestroy() { if (Instance == this) Instance = null; }
#endif
}