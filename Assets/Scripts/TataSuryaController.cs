using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Pasang di ROOT prefab "TataSuryaRoot" (bersama komponen AudioSource).
public class TataSuryaController : MonoBehaviour
{
    public static TataSuryaController Instance;

    [Header("Objek")]
    public Planet[] planets;          // Merkurius ... Neptunus (8 planet)
    public Planet bumi;               // referensi ke planet Bumi
    public Transform bulan;           // Bulan (boleh dinonaktifkan di awal)
    public float jarakBulan = 0.6f;   // jarak Bulan dari Bumi (satuan lokal)
    public float kecepatanBulan = 120f;

    [Header("Garis Orbit")]
    public Material materialGaris;    // material sederhana (mis. Unlit putih)
    public float lebarGaris = 0.002f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip klikClip;
    public AudioClip benarClip;
    public AudioClip salahClip;

    public bool OrbitJalan { get; private set; }
    public Planet Terpilih { get; private set; }

    Vector3 skalaAwal;
    float sudutBulan;
    Coroutine narasiCo;
    Transform garisRoot;

    void Awake()
    {
        Instance = this;
        skalaAwal = transform.localScale;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (bulan != null) bulan.gameObject.SetActive(false);
    }

    void Start()
    {
        if (materialGaris != null) BuatGarisOrbit();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (OrbitJalan)
        {
            foreach (Planet p in planets)
            {
                p.transform.RotateAround(transform.position, transform.up, p.kecepatanOrbit * Time.deltaTime);
                p.transform.Rotate(Vector3.up, p.kecepatanRotasi * Time.deltaTime, Space.Self);
            }
        }

        if (bulan != null && bumi != null && bulan.gameObject.activeSelf)
        {
            sudutBulan += kecepatanBulan * Time.deltaTime;
            Vector3 lokal = Quaternion.AngleAxis(sudutBulan, Vector3.up) * Vector3.right * jarakBulan;
            bulan.position = bumi.transform.position + transform.TransformVector(lokal);
        }

        HandleInput();
    }

    // ---------- Input: tap planet & pinch zoom ----------
    void HandleInput()
    {
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase != TouchPhase.Began) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId)) return;

            Ray ray = Camera.main.ScreenPointToRay(t.position);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Planet p = hit.collider.GetComponentInParent<Planet>();
                if (p != null) PilihPlanet(p);
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);
            float sekarang = (a.position - b.position).magnitude;
            float sebelum = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
            if (sebelum > 0.01f)
            {
                float s = transform.localScale.x * (sekarang / sebelum);
                s = Mathf.Clamp(s, skalaAwal.x * 0.3f, skalaAwal.x * 3f);
                transform.localScale = Vector3.one * s;
            }
        }
    }

    // ---------- Pilih planet ----------
    public void PilihPlanet(Planet p)
    {
        BatalkanNarasi();
        PilihInternal(p);
    }

    void PilihInternal(Planet p)
    {
        if (Terpilih != null) Terpilih.Lepas();
        Terpilih = p;
        p.Pilih();

        if (audioSource != null)
        {
            audioSource.Stop();
            if (p.narasi != null) audioSource.PlayOneShot(p.narasi);
        }

        if (p == bumi && bulan != null) bulan.gameObject.SetActive(true);
    }

    // ---------- Orbit ----------
    public bool ToggleOrbit()
    {
        OrbitJalan = !OrbitJalan;
        if (OrbitJalan)
        {
            if (garisRoot != null) garisRoot.gameObject.SetActive(true);
        }
        else
        {
            PlayKlik();
        }
        return OrbitJalan;
    }

    void BuatGarisOrbit()
    {
        garisRoot = new GameObject("GarisOrbit").transform;
        garisRoot.SetParent(transform, false);

        foreach (Planet p in planets)
        {
            Vector3 pos = p.transform.localPosition;
            float r = new Vector2(pos.x, pos.z).magnitude;

            GameObject go = new GameObject("Orbit_" + p.namaPlanet);
            go.transform.SetParent(garisRoot, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.material = materialGaris;
            lr.startWidth = lebarGaris;
            lr.endWidth = lebarGaris;

            int n = 64;
            lr.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, pos.y, Mathf.Sin(a) * r));
            }
        }
        garisRoot.gameObject.SetActive(false);
    }

    // ---------- Narasi ----------
    public void MulaiNarasi()
    {
        BatalkanNarasi();
        narasiCo = StartCoroutine(NarasiRoutine());
    }

    IEnumerator NarasiRoutine()
    {
        foreach (Planet p in planets)
        {
            PilihInternal(p);
            float durasi = p.narasi != null ? p.narasi.length : 2f;
            yield return new WaitForSeconds(durasi + 0.5f);
        }
        if (Terpilih != null) { Terpilih.Lepas(); Terpilih = null; }
        narasiCo = null;
    }

    void BatalkanNarasi()
    {
        if (narasiCo != null) { StopCoroutine(narasiCo); narasiCo = null; }
    }

    // ---------- Reset ----------
    public void ResetSemua()
    {
        BatalkanNarasi();
        if (audioSource != null) audioSource.Stop();

        OrbitJalan = false;
        Terpilih = null;
        foreach (Planet p in planets) p.ResetPosisi();

        transform.localScale = skalaAwal;
        sudutBulan = 0f;
        if (bulan != null) bulan.gameObject.SetActive(false);
        if (garisRoot != null) garisRoot.gameObject.SetActive(false);
    }

    // ---------- Efek suara ----------
    public void PlayKlik()  { Mainkan(klikClip); }
    public void PlayBenar() { Mainkan(benarClip); }
    public void PlaySalah() { Mainkan(salahClip); }

    void Mainkan(AudioClip c)
    {
        if (audioSource != null && c != null) audioSource.PlayOneShot(c);
    }
}
