using TMPro;
using UnityEngine;

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

    [Header("Kuis")]
    public GameObject panelKuis;
    public TMP_Text teksSoal;
    public TMP_Text[] teksPilihan;    // isi 3 teks (dari 3 tombol pilihan)
    public TMP_Text teksHasil;

    class Soal
    {
        public string tanya;
        public string[] pilihan;
        public int benar;
        public Soal(string t, string[] p, int b) { tanya = t; pilihan = p; benar = b; }
    }

    readonly Soal[] daftar =
    {
        new Soal("Planet terbesar di Tata Surya?", new[] { "Saturnus", "Jupiter", "Neptunus" }, 1),
        new Soal("Planet terdekat dengan Matahari?", new[] { "Merkurius", "Venus", "Mars" }, 0),
        new Soal("Planet yang dijuluki planet merah?", new[] { "Mars", "Jupiter", "Venus" }, 0),
        new Soal("Planet dengan cincin paling terkenal?", new[] { "Uranus", "Saturnus", "Mars" }, 1),
        new Soal("Berapa jumlah planet di Tata Surya?", new[] { "7", "8", "9" }, 1),
    };

    int indeks, skor;
    bool menunggu;
    Planet terakhir;

    TataSuryaController C { get { return TataSuryaController.Instance; } }

    void Update()
    {
        if (C == null) return;
        if (C.Terpilih != terakhir)
        {
            terakhir = C.Terpilih;
            RefreshInfo();
        }
    }

    void RefreshInfo()
    {
        Planet p = C != null ? C.Terpilih : null;

        if (teksInfo != null)
            teksInfo.text = p != null ? p.Info() : "Tap sebuah planet untuk melihat infonya.";

        if (teksFakta != null)
        {
            bool adalahBumi = p != null && C != null && p == C.bumi;
            teksFakta.gameObject.SetActive(adalahBumi);
            teksFakta.text = faktaBumi;
        }
    }

    // ===== Dipanggil dari OnClick tombol =====

    public void ToggleInfo()
    {
        panelInfo.SetActive(!panelInfo.activeSelf);
        RefreshInfo();
        if (C != null) C.PlayKlik();
    }

    public void ToggleOrbit()
    {
        if (C == null) return;
        bool jalan = C.ToggleOrbit();
        teksTombolOrbit.text = jalan ? "Pause" : "Mulai Orbit";
    }

    public void Narasi()
    {
        if (C != null) C.MulaiNarasi();
    }

    public void ResetSemua()
    {
        if (C != null) C.ResetSemua();
        teksTombolOrbit.text = "Mulai Orbit";
        panelInfo.SetActive(false);
        panelKuis.SetActive(false);
        CancelInvoke();
        RefreshInfo();
    }

    // ===== Kuis =====

    public void ToggleKuis()
    {
        CancelInvoke();
        bool buka = !panelKuis.activeSelf;
        panelKuis.SetActive(buka);
        if (buka)
        {
            skor = 0;
            indeks = 0;
            TampilSoal();
        }
    }

    void TampilSoal()
    {
        menunggu = false;
        teksHasil.text = "";
        Soal s = daftar[indeks];
        teksSoal.text = "(" + (indeks + 1) + "/" + daftar.Length + ") " + s.tanya;
        for (int i = 0; i < teksPilihan.Length; i++)
            teksPilihan[i].text = s.pilihan[i];
    }

    // OnClick tombol pilihan: isi parameter 0, 1, atau 2
    public void JawabKuis(int pilihan)
    {
        if (menunggu) return;
        menunggu = true;

        bool benar = pilihan == daftar[indeks].benar;
        if (benar) skor++;
        teksHasil.text = benar ? "Benar!" : "Salah!";

        if (C != null)
        {
            if (benar) C.PlayBenar(); else C.PlaySalah();
        }
        Invoke(nameof(SoalBerikutnya), 1.2f);
    }

    void SoalBerikutnya()
    {
        indeks++;
        if (indeks >= daftar.Length)
        {
            teksSoal.text = "Selesai! Skor kamu " + skor + "/" + daftar.Length;
            teksHasil.text = "Tekan tombol Kuis lagi untuk menutup";
            foreach (TMP_Text t in teksPilihan) t.text = "";
            return;
        }
        TampilSoal();
    }
}
