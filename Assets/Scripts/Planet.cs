using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Pasang di SETIAP planet. Data & kecepatan terisi OTOMATIS dari nama GameObject:
// Merkurius, Venus, Bumi, Mars, Jupiter, Saturnus, Uranus, Neptunus (opsional: Matahari).
public class Planet : MonoBehaviour
{
    [Header("Data (kosong = diisi otomatis)")]
    public bool isiOtomatis = true;
    public string namaPlanet;
    public string diameter;
    public string jarakDariMatahari;
    public string jumlahSatelit;
    [TextArea] public string ciriKhas;
    [TextArea] public string teksNarasi;     // kosong = "Nama. ciri khas"
    public AudioClip narasi;                 // opsional, kosong = suara TTS HP

    [Header("Gerak")]
    public float kecepatanOrbit = 20f;
    public float kecepatanRotasi = 30f;

    [Header("Animasi Pilih")]
    public float skalaDipilih = 1.4f;
    public Color warnaGlow = new Color(1f, 0.9f, 0.6f);

    public bool Dipilih { get; private set; }

    Vector3 posAwal, skalaAwal;
    Quaternion rotAwal;
    Material mat;
    Color emisiAwal = Color.black;
    float skalaNow = 1f, skalaTarget = 1f, velSkala;
    float glowNow, glowTarget, glowTerakhir = -1f;
    Coroutine resetCo;

    struct Data
    {
        public string d, j, s, c; public float orbit, rotasi;
        public Data(string d, string j, string s, string c, float orbit, float rotasi)
        { this.d = d; this.j = j; this.s = s; this.c = c; this.orbit = orbit; this.rotasi = rotasi; }
    }

    static readonly Dictionary<string, Data> DB = new Dictionary<string, Data>
    {
        { "Matahari",  new Data("1.392.700 km (109 kali Bumi)", "-", "-", "Matahari adalah bintang di pusat Tata Surya, bola gas panas raksasa yang memancarkan cahaya dan panas. Suhu permukaannya sekitar 5.500 derajat Celsius, dan cahayanya butuh sekitar 8 menit untuk sampai ke Bumi.", 0f, 8f) },
        { "Merkurius", new Data("4.879 km", "57,9 juta km", "0", "Planet terkecil dan paling dekat dengan Matahari. Satu tahun di Merkurius hanya 88 hari.", 48f, 6f) },
        { "Venus",     new Data("12.104 km", "108,2 juta km", "0", "Planet terpanas, suhunya sekitar 465 derajat Celsius karena atmosfernya sangat tebal. Venus berputar berlawanan arah dengan planet lain.", 35f, -4f) },
        { "Bumi",      new Data("12.742 km", "149,6 juta km", "1 (Bulan)", "Satu-satunya planet yang diketahui memiliki kehidupan. Sekitar 71 persen permukaannya tertutup air.", 30f, 60f) },
        { "Mars",      new Data("6.779 km", "227,9 juta km", "2 (Phobos & Deimos)", "Disebut planet merah karena tanahnya mengandung karat besi. Di Mars ada Olympus Mons, gunung tertinggi di Tata Surya.", 24f, 58f) },
        { "Jupiter",   new Data("139.820 km", "778,5 juta km", "95+", "Planet terbesar di Tata Surya. Bintik Merah Besarnya adalah badai raksasa yang lebih besar dari Bumi.", 13f, 140f) },
        { "Saturnus",  new Data("116.460 km", "1,43 miliar km", "270+", "Terkenal dengan cincinnya yang indah, tersusun dari es dan batuan. Saturnus punya satelit paling banyak.", 10f, 125f) },
        { "Uranus",    new Data("50.724 km", "2,87 miliar km", "28+", "Planet es berwarna biru kehijauan. Porosnya sangat miring, jadi seolah berputar sambil menggelinding.", 7f, -85f) },
        { "Neptunus",  new Data("49.244 km", "4,5 miliar km", "16", "Planet terjauh dari Matahari. Anginnya paling kencang di Tata Surya, bisa lebih dari 2.000 km per jam.", 5.5f, 90f) },
    };

    void Awake()
    {
        posAwal = transform.localPosition;
        skalaAwal = transform.localScale;
        rotAwal = transform.localRotation;

        Renderer r = GetComponent<Renderer>();
        if (r == null) r = GetComponentInChildren<Renderer>();
        if (r != null)
        {
            mat = r.material;
            if (mat.HasProperty("_EmissionColor")) emisiAwal = mat.GetColor("_EmissionColor");
            mat.EnableKeyword("_EMISSION");
        }
        if (isiOtomatis) IsiData();
    }

    void IsiData()
    {
        string key = gameObject.name.Trim();
        if (!DB.TryGetValue(key, out Data x)) return;
        if (string.IsNullOrEmpty(namaPlanet)) namaPlanet = key;
        if (string.IsNullOrEmpty(diameter)) diameter = x.d;
        if (string.IsNullOrEmpty(jarakDariMatahari)) jarakDariMatahari = x.j;
        if (string.IsNullOrEmpty(jumlahSatelit)) jumlahSatelit = x.s;
        if (string.IsNullOrEmpty(ciriKhas)) ciriKhas = x.c;
        kecepatanOrbit = x.orbit;
        kecepatanRotasi = x.rotasi;
        if (key == "Matahari") skalaDipilih = 1.15f;   // Matahari sudah besar, membesar sedikit saja
    }

    public bool IsMatahari { get { return namaPlanet == "Matahari" || gameObject.name.Trim() == "Matahari"; } }

    void Update()
    {
        skalaNow = Mathf.SmoothDamp(skalaNow, skalaTarget, ref velSkala, 0.1f);
        transform.localScale = skalaAwal * skalaNow;

        glowNow = Mathf.MoveTowards(glowNow, glowTarget, Time.deltaTime * 4f);
        float g = glowNow * (Dipilih ? 0.7f + 0.3f * Mathf.Sin(Time.time * 6f) : 1f);
        if (mat != null && Mathf.Abs(g - glowTerakhir) > 0.005f)
        {
            mat.SetColor("_EmissionColor", emisiAwal + warnaGlow * (g * 0.8f));
            glowTerakhir = g;
        }
    }

    public void Pilih()
    {
        Dipilih = true;
        skalaTarget = skalaDipilih;
        velSkala += 5f;
        glowTarget = 1f;
    }

    public void Lepas()
    {
        Dipilih = false;
        skalaTarget = 1f;
        glowTarget = 0f;
    }

    public void KembaliKeAwal(float durasi)
    {
        HentikanReset();
        resetCo = StartCoroutine(Kembali(durasi));
    }

    public void HentikanReset()
    {
        if (resetCo != null) { StopCoroutine(resetCo); resetCo = null; }
    }

    IEnumerator Kembali(float d)
    {
        Vector3 p0 = transform.localPosition;
        Quaternion r0 = transform.localRotation;
        float a0 = Mathf.Atan2(p0.z, p0.x) * Mathf.Rad2Deg;
        float a1 = Mathf.Atan2(posAwal.z, posAwal.x) * Mathf.Rad2Deg;
        float rad0 = new Vector2(p0.x, p0.z).magnitude;
        float rad1 = new Vector2(posAwal.x, posAwal.z).magnitude;

        for (float t = 0f; t < 1f; t += Time.deltaTime / d)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            float a = Mathf.LerpAngle(a0, a1, e) * Mathf.Deg2Rad;
            float r = Mathf.Lerp(rad0, rad1, e);
            transform.localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Lerp(p0.y, posAwal.y, e), Mathf.Sin(a) * r);
            transform.localRotation = Quaternion.Slerp(r0, rotAwal, e);
            yield return null;
        }
        transform.localPosition = posAwal;
        transform.localRotation = rotAwal;
        resetCo = null;
    }

    public string TeksBicara()
    {
        return !string.IsNullOrEmpty(teksNarasi) ? teksNarasi : namaPlanet + ". " + ciriKhas;
    }

    public string Info()
    {
        if (IsMatahari)
            return "<size=125%><b>" + namaPlanet + "</b></size>\n" +
                   "Jenis: Bintang (pusat Tata Surya)\n" +
                   "Diameter: " + diameter + "\n" +
                   "Suhu permukaan: sekitar 5.500 \u00B0C\n" +
                   "Umur: sekitar 4,6 miliar tahun\n\n" +
                   "Matahari menyimpan 99,8% massa Tata Surya. Cahayanya sampai ke Bumi dalam sekitar 8 menit.";
        return "<size=125%><b>" + namaPlanet + "</b></size>\n" +
               "Diameter: " + diameter + "\n" +
               "Jarak dari Matahari: " + jarakDariMatahari + "\n" +
               "Jumlah satelit: " + jumlahSatelit + "\n\n" +
               ciriKhas;
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}