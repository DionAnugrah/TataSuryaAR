using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Pasang di ROOT prefab "TataSuryaRoot" (di samping TataSuryaController).
// Menambah: atmosfer bercahaya tiap planet (menyala saat dipilih), korona Matahari,
// sabuk asteroid, dan bintang kecil di sekitar tata surya. Kamera AR tetap terlihat.
public class EfekLuarAngkasa : MonoBehaviour
{
    [Tooltip("Seret file Atmosfer.shader ke sini")]
    public Shader shaderAtmosfer;

    [Header("Atmosfer & Glow")]
    public bool atmosfer = true;
    public float tebalAtmosfer = 1.13f;
    public float glowDipilih = 1.2f;
    public Transform matahari;                       // kosong = cari objek bernama "Matahari"
    public Color warnaKorona = new Color(1f, 0.6f, 0.2f);

    [Header("Sabuk Asteroid (antara Mars & Jupiter)")]
    public bool sabukAsteroid = true;
    public int jumlahAsteroid = 400;
    public Material materialAsteroid;                // kosong = dibuat otomatis
    public float kecepatanSabuk = 4f;

    [Header("Bintang kecil di sekitar tata surya")]
    public bool bintang = true;
    public int jumlahBintang = 160;

    class Atm { public Planet p; public Material m; public float dasar, now; }
    readonly List<Atm> daftar = new List<Atm>();
    readonly List<Material> dibuat = new List<Material>();
    Material matKorona, matBintang;
    Transform sabuk;
    float faktorSabuk;

    static readonly Dictionary<string, Color> WARNA = new Dictionary<string, Color>
    {
        { "Merkurius", new Color(0.70f, 0.70f, 0.70f, 0.00f) },
        { "Venus",     new Color(1.00f, 0.80f, 0.50f, 0.50f) },
        { "Bumi",      new Color(0.35f, 0.60f, 1.00f, 0.90f) },
        { "Mars",      new Color(1.00f, 0.50f, 0.30f, 0.30f) },
        { "Jupiter",   new Color(1.00f, 0.85f, 0.65f, 0.30f) },
        { "Saturnus",  new Color(1.00f, 0.90f, 0.60f, 0.30f) },
        { "Uranus",    new Color(0.60f, 0.95f, 1.00f, 0.50f) },
        { "Neptunus",  new Color(0.40f, 0.60f, 1.00f, 0.60f) },
    };   // nilai alpha = kekuatan cahaya dasar

    void Start()
    {
        if (shaderAtmosfer == null) shaderAtmosfer = Shader.Find("TataSurya/Atmosfer");
        if (shaderAtmosfer == null) Debug.LogWarning("Shader TataSurya/Atmosfer belum diisi di EfekLuarAngkasa.");

        Planet[] semua = GetComponentsInChildren<Planet>(true);

        if (atmosfer && shaderAtmosfer != null)
        {
            foreach (Planet p in semua)
            {
                if (!WARNA.TryGetValue(p.gameObject.name.Trim(), out Color c)) continue;
                Material m = BuatCangkang(p.transform, tebalAtmosfer, new Color(c.r, c.g, c.b), c.a, 2.6f);
                daftar.Add(new Atm { p = p, m = m, dasar = c.a });
            }

            if (matahari == null)
            {
                foreach (Transform t in GetComponentsInChildren<Transform>(true))
                    if (t.name == "Matahari") { matahari = t; break; }
            }
            if (matahari != null) matKorona = BuatCangkang(matahari, 1.5f, warnaKorona, 1.4f, 1.5f);
        }

        Transform mars = null, jupiter = null, merkurius = null;
        float orbitTerjauh = 0f;
        foreach (Planet p in semua)
        {
            string n = p.gameObject.name.Trim();
            if (n == "Mars") mars = p.transform;
            if (n == "Jupiter") jupiter = p.transform;
            if (n == "Merkurius") merkurius = p.transform;
            Vector3 lp = transform.InverseTransformPoint(p.transform.position);
            orbitTerjauh = Mathf.Max(orbitTerjauh, new Vector2(lp.x, lp.z).magnitude);
        }
        float ukRef = merkurius != null ? merkurius.localScale.x : 0.05f;

        if (sabukAsteroid && mars != null && jupiter != null)
        {
            Vector3 a = transform.InverseTransformPoint(mars.position);
            Vector3 b = transform.InverseTransformPoint(jupiter.position);
            float rM = new Vector2(a.x, a.z).magnitude, rJ = new Vector2(b.x, b.z).magnitude;
            BuatSabuk(Mathf.Lerp(rM, rJ, 0.35f), Mathf.Lerp(rM, rJ, 0.6f), ukRef * 0.12f, semua);
        }

        if (bintang && shaderAtmosfer != null && orbitTerjauh > 0f)
            BuatBintang(orbitTerjauh, ukRef);
    }

    Material BuatCangkang(Transform induk, float skala, Color warna, float kekuatan, float pangkat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Atmosfer";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(induk, false);
        go.transform.localScale = Vector3.one * skala;

        Material m = new Material(shaderAtmosfer);
        m.SetColor("_Warna", warna);
        m.SetFloat("_Kekuatan", kekuatan);
        m.SetFloat("_Pangkat", pangkat);
        dibuat.Add(m);

        MeshRenderer r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return m;
    }

    // Gabungan banyak batu kecil dalam SATU mesh (ringan untuk HP)
    Mesh BuatMeshBatu(int jumlah, System.Func<Vector3> posisi, float ukMin, float ukMax)
    {
        Vector3[] o = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        int[] f = { 0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4, 1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2 };
        List<Vector3> v = new List<Vector3>(), nrm = new List<Vector3>();
        List<int> tri = new List<int>();

        for (int i = 0; i < jumlah; i++)
        {
            Vector3 pusat = posisi();
            float uk = Random.Range(ukMin, ukMax);
            Quaternion rot = Random.rotation;
            int basis = v.Count;
            for (int k = 0; k < 6; k++)
            {
                Vector3 d = rot * (o[k] * Random.Range(0.7f, 1.3f));
                v.Add(pusat + d * uk);
                nrm.Add(d.normalized);
            }
            for (int k = 0; k < f.Length; k++) tri.Add(basis + f[k]);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(v);
        mesh.SetNormals(nrm);
        mesh.SetTriangles(tri, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    void BuatSabuk(float rDalam, float rLuar, float ukuran, Planet[] semua)
    {
        Mesh mesh = BuatMeshBatu(jumlahAsteroid, () =>
        {
            float a = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(rDalam, rLuar);
            return new Vector3(Mathf.Cos(a) * r, Random.Range(-0.15f, 0.15f) * (rLuar - rDalam), Mathf.Sin(a) * r);
        }, ukuran * 0.4f, ukuran * 1.2f);

        GameObject go = new GameObject("SabukAsteroid");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = ShadowCastingMode.Off;

        Material m = materialAsteroid;
        if (m == null && semua.Length > 0)
        {
            Renderer contoh = semua[0].GetComponent<Renderer>();
            if (contoh != null)
            {
                m = new Material(contoh.sharedMaterial);
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", null);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.55f, 0.5f, 0.45f));
                if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
                m.DisableKeyword("_EMISSION");
                dibuat.Add(m);
            }
        }
        mr.sharedMaterial = m;
        sabuk = go.transform;
    }

    void BuatBintang(float orbitTerjauh, float ukRef)
    {
        Mesh mesh = BuatMeshBatu(jumlahBintang, () =>
        {
            Vector3 d = Random.onUnitSphere;
            d.y = Mathf.Abs(d.y) * 0.8f + 0.05f;              // hanya di atas kartu
            return d.normalized * orbitTerjauh * Random.Range(1.25f, 1.7f);
        }, ukRef * 0.05f, ukRef * 0.14f);

        GameObject go = new GameObject("Bintang");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        matBintang = new Material(shaderAtmosfer);
        matBintang.SetColor("_Warna", new Color(1f, 0.97f, 0.9f));
        matBintang.SetFloat("_Kekuatan", 1f);
        matBintang.SetFloat("_Pangkat", 0f);                   // 0 = warna polos bercahaya
        dibuat.Add(matBintang);
        mr.sharedMaterial = matBintang;
    }

    void Update()
    {
        float dt = Time.deltaTime, t = Time.time;

        foreach (Atm a in daftar)
        {
            if (a.p == null) continue;
            float target = a.p.Dipilih ? glowDipilih * (0.75f + 0.25f * Mathf.Sin(t * 6f)) : 0f;
            a.now = Mathf.Lerp(a.now, target, 1f - Mathf.Exp(-8f * dt));
            a.m.SetFloat("_Kekuatan", a.dasar + a.now);
        }

        if (matKorona != null) matKorona.SetFloat("_Kekuatan", 1.4f + 0.15f * Mathf.Sin(t * 1.7f));
        if (matBintang != null) matBintang.SetFloat("_Kekuatan", 0.85f + 0.15f * Mathf.Sin(t * 3.1f));

        if (sabuk != null)
        {
            bool jalan = TataSuryaController.Instance != null && TataSuryaController.Instance.OrbitJalan;
            faktorSabuk = Mathf.MoveTowards(faktorSabuk, jalan ? 1f : 0f, dt * 1.5f);
            sabuk.Rotate(Vector3.up, kecepatanSabuk * faktorSabuk * dt, Space.Self);
        }
    }

    void OnDestroy()
    {
        foreach (Material m in dibuat) if (m != null) Destroy(m);
    }
}