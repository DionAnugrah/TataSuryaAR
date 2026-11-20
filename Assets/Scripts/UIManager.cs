using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pasang di GameObject kosong "UIManager" di scene (BUKAN di prefab).
public class UIManager : MonoBehaviour
{
    [Header("Info Planet")]
    public GameObject panelInfo;
    public TMP_Text teksInfo;

    [Header("Fakta Bumi")]
    public TMP_Text teksFakta;
    [TextArea]
    public string faktaBumi = "Bumi satu-satunya planet yang diketahui memiliki kehidupan. Sekitar 71% permukaannya tertutup air.";

    [Header("Orbit")]
    public TMP_Text teksTombolOrbit;

    [Header("Narasi (opsional)")]
    public TMP_Text teksTombolNarasi;

    [Header("Kuis")]
    public GameObject panelKuis;
    public TMP_Text teksSoal;
    public TMP_Text[] teksPilihan;
    public TMP_Text teksHasil;
    public int jumlahSoal = 5;
    public bool bacakanSoal = true;
    public Color warnaBenar = new Color(0.30f, 0.85f, 0.40f);
    public Color warnaSalah = new Color(0.95f, 0.35f, 0.35f);

    class Soal
    {
        public string tanya; public string[] pilihan; public int benar;
        public Soal(string t, string[] p, int b) { tanya = t; pilihan = p; benar = b; }
    }

    readonly Soal[] bank =
    {
        new Soal("Planet terbesar di Tata Surya?", new[] { "Saturnus", "Jupiter", "Neptunus" }, 1),
        new Soal("Planet terdekat dengan Matahari?", new[] { "Merkurius", "Venus", "Mars" }, 0),
        new Soal("Planet yang dijuluki planet merah?", new[] { "Mars", "Jupiter", "Venus" }, 0),
        new Soal("Planet dengan cincin paling terkenal?", new[] { "Uranus", "Saturnus", "Mars" }, 1),
        new Soal("Berapa jumlah planet di Tata Surya?", new[] { "7", "8", "9" }, 1),
        new Soal("Planet paling panas di Tata Surya?", new[] { "Merkurius", "Venus", "Mars" }, 1),
        new Soal("Satu-satunya planet yang diketahui memiliki kehidupan?", new[] { "Mars", "Bumi", "Venus" }, 1),
        new Soal("Apa nama satelit alami Bumi?", new[] { "Phobos", "Titan", "Bulan" }, 2),
        new Soal("Planet terjauh dari Matahari?", new[] { "Uranus", "Saturnus", "Neptunus" }, 2),
        new Soal("Planet yang berputar seperti menggelinding?", new[] { "Uranus", "Neptunus", "Jupiter" }, 0),
        new Soal("Apa pusat dari Tata Surya?", new[] { "Bumi", "Matahari", "Jupiter" }, 1),
        new Soal("Planet dengan satelit paling banyak?", new[] { "Jupiter", "Mars", "Saturnus" }, 2),
    };

    readonly List<Soal> sesi = new List<Soal>();
    int indeks, skor, jawabanBenar;
    bool menunggu, infoBuka, kuisBuka, narasiTerakhir;
    Planet terakhir;
    Button[] tombolPilihan;
    Image[] gambarPilihan;
    Color[] warnaAsli;
    Coroutine kuisCo;
    readonly Dictionary<GameObject, Coroutine> anim = new Dictionary<GameObject, Coroutine>();
    readonly Dictionary<GameObject, Vector3> skalaPanel = new Dictionary<GameObject, Vector3>();

    TataSuryaController C { get { return TataSuryaController.Instance; } }

    void Start()
    {
        infoBuka = panelInfo != null && panelInfo.activeSelf;
        kuisBuka = panelKuis != null && panelKuis.activeSelf;
        if (teksFakta != null) teksFakta.gameObject.SetActive(false);

        int n = teksPilihan != null ? teksPilihan.Length : 0;
        tombolPilihan = new Button[n];
        gambarPilihan = new Image[n];
        warnaAsli = new Color[n];
        for (int i = 0; i < n; i++)
        {
            tombolPilihan[i] = teksPilihan[i].GetComponentInParent<Button>(true);
            if (tombolPilihan[i] != null)
            {
                gambarPilihan[i] = tombolPilihan[i].targetGraphic as Image;
                if (gambarPilihan[i] != null) warnaAsli[i] = gambarPilihan[i].color;
            }
        }

        // Tambah animasi tekan ke SEMUA tombol secara otomatis
        Canvas cv = panelInfo != null ? panelInfo.GetComponentInParent<Canvas>(true) : null;
        if (cv != null)
            foreach (Button b in cv.rootCanvas.GetComponentsInChildren<Button>(true))
                if (b.GetComponent<TombolAnimasi>() == null) b.gameObject.AddComponent<TombolAnimasi>();

        RefreshInfo(false);
    }

    void Update()
    {
        if (C == null) return;

        if (C.Terpilih != terakhir)
        {
            terakhir = C.Terpilih;
            RefreshInfo(true);
        }

        // Tulisan tombol Orbit selalu sesuai keadaan orbit
        if (teksTombolOrbit != null)
        {
            string t = C.OrbitJalan ? "Pause" : "Mulai Orbit";
            if (teksTombolOrbit.text != t) teksTombolOrbit.text = t;
        }

        bool nar = C.SedangNarasi;
        if (teksTombolNarasi != null && nar != narasiTerakhir)
        {
            narasiTerakhir = nar;
            teksTombolNarasi.text = nar ? "Stop Narasi" : "Narasi";
        }
    }

    bool Siap()
    {
        if (C != null) return true;
        SuaraManager.Ucapkan("Arahkan kamera ke kartu tata surya dulu ya.");
        return false;
    }

    void RefreshInfo(bool animasi)
    {
        Planet p = C != null ? C.Terpilih : null;

        if (teksInfo != null)
        {
            teksInfo.text = p != null ? p.Info() : "Tap sebuah planet untuk melihat infonya.";
            if (animasi && infoBuka) MulaiAnim(teksInfo.gameObject, FadeTeks(teksInfo));
        }

        if (teksFakta != null)
        {
            bool adalahBumi = p != null && C != null && p == C.bumi;
            teksFakta.text = faktaBumi;
            if (adalahBumi && !teksFakta.gameObject.activeSelf)
            {
                teksFakta.gameObject.SetActive(true);
                MulaiAnim(teksFakta.gameObject, FadeTeks(teksFakta));
            }
            else if (!adalahBumi) teksFakta.gameObject.SetActive(false);
        }
    }

    // ===== Dipanggil dari OnClick tombol =====

    public void ToggleInfo()
    {
        SuaraManager.Klik();
        infoBuka = !infoBuka;
        if (infoBuka && kuisBuka) TutupKuis();
        RefreshInfo(false);
        TampilPanel(panelInfo, infoBuka);
    }

    public void ToggleOrbit()
    {
        if (!Siap()) return;
        bool jalan = C.ToggleOrbit();
        if (teksTombolOrbit != null) teksTombolOrbit.text = jalan ? "Pause" : "Mulai Orbit";
    }

    public void Narasi()
    {
        SuaraManager.Klik();
        if (!Siap()) return;
        C.ToggleNarasi();
    }

    public void ResetSemua()
    {
        if (C != null) C.ResetSemua();
        if (teksTombolOrbit != null) teksTombolOrbit.text = "Mulai Orbit";
        if (infoBuka) { infoBuka = false; TampilPanel(panelInfo, false); }
        if (kuisBuka) TutupKuis();
        RefreshInfo(false);
    }

    // ===== Kuis =====

    public void ToggleKuis()
    {
        SuaraManager.Klik();
        if (kuisBuka) { TutupKuis(); return; }

        kuisBuka = true;
        if (infoBuka) { infoBuka = false; TampilPanel(panelInfo, false); }
        if (C != null && C.SedangNarasi) C.HentikanNarasi();

        AcakSesi();
        skor = 0;
        indeks = 0;
        TampilPanel(panelKuis, true);
        TampilSoal();
    }

    void TutupKuis()
    {
        kuisBuka = false;
        if (kuisCo != null) { StopCoroutine(kuisCo); kuisCo = null; }
        SuaraManager.Berhenti();
        TampilPanel(panelKuis, false);
    }

    void AcakSesi()
    {
        sesi.Clear();
        sesi.AddRange(bank);
        for (int i = sesi.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Soal tmp = sesi[i]; sesi[i] = sesi[j]; sesi[j] = tmp;
        }
        int jumlah = Mathf.Clamp(jumlahSoal, 1, sesi.Count);
        sesi.RemoveRange(jumlah, sesi.Count - jumlah);
    }

    void TampilSoal()
    {
        menunggu = false;
        Soal s = sesi[indeks];
        teksHasil.text = "";
        teksSoal.text = "(" + (indeks + 1) + "/" + sesi.Count + ") " + s.tanya;
        MulaiAnim(teksSoal.gameObject, FadeTeks(teksSoal));

        int n = Mathf.Min(teksPilihan.Length, s.pilihan.Length);
        int[] urut = new int[n];
        for (int i = 0; i < n; i++) urut[i] = i;
        for (int i = n - 1; i > 0; i--) { int j = Random.Range(0, i + 1); int t = urut[i]; urut[i] = urut[j]; urut[j] = t; }

        for (int i = 0; i < teksPilihan.Length; i++)
        {
            bool ada = i < n;
            if (tombolPilihan[i] != null) tombolPilihan[i].gameObject.SetActive(ada);
            if (!ada) continue;
            teksPilihan[i].text = s.pilihan[urut[i]];
            if (urut[i] == s.benar) jawabanBenar = i;
            if (gambarPilihan[i] != null) gambarPilihan[i].color = warnaAsli[i];
            StartCoroutine(PopTerlambat(i, 0.08f * i));
        }

        if (bacakanSoal) SuaraManager.Ucapkan(s.tanya);
    }

    // OnClick tombol pilihan: parameter 0, 1, atau 2
    public void JawabKuis(int pilihan)
    {
        if (!kuisBuka || menunggu || indeks >= sesi.Count) return;
        if (pilihan < 0 || pilihan >= teksPilihan.Length) return;
        menunggu = true;

        bool benar = pilihan == jawabanBenar;
        if (benar) skor++;

        Warnai(jawabanBenar, warnaBenar);
        Pop(jawabanBenar, 1.2f);
        if (!benar)
        {
            Warnai(pilihan, warnaSalah);
            if (tombolPilihan[pilihan] != null) StartCoroutine(Goyang((RectTransform)tombolPilihan[pilihan].transform));
        }

        string kunci = teksPilihan[jawabanBenar].text;
        teksHasil.text = benar ? "<color=#4CD964>Benar!</color>"
                               : "<color=#FF5A5A>Kurang tepat</color>\nJawaban: " + kunci;
        MulaiAnim(teksHasil.gameObject, FadeTeks(teksHasil));

        if (C != null) { if (benar) C.PlayBenar(); else C.PlaySalah(); }
        else { if (benar) SuaraManager.Benar(); else SuaraManager.Salah(); }
        SuaraManager.Ucapkan(benar ? "Benar!" : "Kurang tepat. Jawabannya " + kunci);

        kuisCo = StartCoroutine(Lanjut(benar ? 1.4f : 2.4f));
    }

    IEnumerator Lanjut(float jeda)
    {
        yield return new WaitForSecondsRealtime(jeda);
        indeks++;
        if (indeks >= sesi.Count) SelesaiKuis();
        else TampilSoal();
        kuisCo = null;
    }

    void SelesaiKuis()
    {
        int total = sesi.Count;
        string pesan = skor == total ? "Sempurna!" : (skor >= total * 0.6f ? "Hebat!" : "Ayo coba lagi!");
        teksSoal.text = "Kuis selesai!\nSkor kamu " + skor + "/" + total;
        teksHasil.text = pesan + "\nTekan Kuis untuk menutup";
        MulaiAnim(teksSoal.gameObject, FadeTeks(teksSoal));
        foreach (Button b in tombolPilihan) if (b != null) b.gameObject.SetActive(false);
        SuaraManager.Selesai();
        SuaraManager.Ucapkan("Kuis selesai. Skor kamu " + skor + " dari " + total + ". " + pesan);
    }

    void Warnai(int i, Color c)
    {
        if (i >= 0 && i < gambarPilihan.Length && gambarPilihan[i] != null) gambarPilihan[i].color = c;
    }

    void Pop(int i, float dari)
    {
        if (i < 0 || i >= tombolPilihan.Length || tombolPilihan[i] == null) return;
        TombolAnimasi ta = tombolPilihan[i].GetComponent<TombolAnimasi>();
        if (ta != null) ta.Pop(dari);
    }

    IEnumerator PopTerlambat(int i, float jeda)
    {
        yield return new WaitForSecondsRealtime(jeda);
        Pop(i, 0.6f);
    }

    IEnumerator Goyang(RectTransform rt)
    {
        Vector2 asal = rt.anchoredPosition;
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            float x = Mathf.Sin(t * 50f) * 12f * (1f - t / 0.4f);
            rt.anchoredPosition = asal + new Vector2(x, 0f);
            yield return null;
        }
        rt.anchoredPosition = asal;
    }

    // ===== Animasi panel & teks =====

    void MulaiAnim(GameObject kunci, IEnumerator rutin)
    {
        if (anim.TryGetValue(kunci, out Coroutine lama) && lama != null) StopCoroutine(lama);
        anim[kunci] = StartCoroutine(rutin);
    }

    void TampilPanel(GameObject panel, bool tampil)
    {
        if (panel == null) return;
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        if (tampil && !panel.activeSelf) { cg.alpha = 0f; panel.SetActive(true); }
        MulaiAnim(panel, AnimPanel(panel, cg, tampil));
    }

    IEnumerator AnimPanel(GameObject panel, CanvasGroup cg, bool tampil)
    {
        if (!skalaPanel.TryGetValue(panel, out Vector3 s)) { s = panel.transform.localScale; skalaPanel[panel] = s; }
        cg.interactable = tampil;
        cg.blocksRaycasts = tampil;

        float a0 = cg.alpha, a1 = tampil ? 1f : 0f;
        float d = tampil ? 0.28f : 0.18f;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / d)
        {
            cg.alpha = Mathf.Lerp(a0, a1, t);
            float k = tampil ? Mathf.LerpUnclamped(0.85f, 1f, TataSuryaController.EaseOutBack(t))
                             : Mathf.Lerp(1f, 0.9f, t);
            panel.transform.localScale = s * k;
            yield return null;
        }
        cg.alpha = a1;
        panel.transform.localScale = s;
        if (!tampil) panel.SetActive(false);
    }

    IEnumerator FadeTeks(TMP_Text t)
    {
        for (float x = 0f; x < 1f; x += Time.unscaledDeltaTime / 0.25f)
        {
            t.alpha = x;
            yield return null;
        }
        t.alpha = 1f;
    }
}