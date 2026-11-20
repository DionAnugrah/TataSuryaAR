using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Pasang di ROOT prefab "TataSuryaRoot" (bersama AudioSource).
// Input memakai Input System BARU (aman untuk Android).
public class TataSuryaController : MonoBehaviour
{
    public static TataSuryaController Instance;

    [Header("Objek")]
    public Planet[] planets;
    public Planet bumi;
    [Tooltip("Kosong = dicari otomatis (objek bernama Matahari)")]
    public Planet matahari;
    public Transform bulan;
    public float jarakBulan = 0.6f;
    public float kecepatanBulan = 120f;

    [Header("Garis Orbit")]
    public Material materialGaris;
    public float lebarGaris = 0.002f;

    [Header("Audio (opsional, kosong = suara bawaan)")]
    public AudioSource audioSource;
    public AudioClip klikClip;
    public AudioClip benarClip;
    public AudioClip salahClip;

    [Header("Musik Latar")]
    [Tooltip("Seret file Musik_Latar_Luar_Angkasa ke sini")]
    public AudioClip musikLatar;
    [Range(0f, 1f)] public float volumeMusik = 0.35f;

    [Header("Animasi")]
    public float kecepatanGlobal = 1f;
    public float durasiMuncul = 0.8f;
    public float tinggiMelayang = 0.004f;
    public float zoomMin = 0.3f;
    public float zoomMax = 3f;
    [Tooltip("Centang = planet langsung mengorbit saat muncul. Hilangkan centang = diam dulu sampai tombol Mulai Orbit ditekan.")]
    public bool orbitSaatMuncul = true;
    [Tooltip("Kecepatan rotasi planet pada porosnya saat orbit di-pause (0 = diam total).")]
    [Range(0f, 1f)] public float rotasiSaatDiam = 0.25f;

    [Header("Narasi")]
    [TextArea] public string pembukaNarasi = "Tata Surya terdiri dari Matahari sebagai pusatnya, dan delapan planet yang mengelilinginya. Ayo kita kenali satu per satu.";
    [TextArea] public string penutupNarasi = "Itulah delapan planet di Tata Surya kita. Coba tekan tombol Kuis untuk menguji ingatanmu!";

    public bool OrbitJalan { get; private set; }
    public Planet Terpilih { get; private set; }
    public bool SedangNarasi { get { return narasiCo != null; } }

    Vector3 skalaAwal, posAwalRoot, skalaBulan;
    bool sudahStart;
    float muncul, zoomNow = 1f, zoomTarget = 1f;
    float faktorOrbit, garisNow, garisTarget;
    float bulanNow, bulanTarget, sudutBulan;
    Coroutine narasiCo;
    Transform garisRoot;
    readonly List<LineRenderer> garis = new List<LineRenderer>();

    Vector2 posMulai;
    float waktuMulai, jarakPinch;
    bool bisaTap;
    static readonly List<RaycastResult> hasilUI = new List<RaycastResult>();

    void Awake()
    {
        Instance = this;
        skalaAwal = transform.localScale;
        if (matahari == null)
            foreach (Planet p in GetComponentsInChildren<Planet>(true))
                if (p.gameObject.name.Trim() == "Matahari") { matahari = p; break; }
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (bulan != null)
        {
            skalaBulan = bulan.localScale;
            bulan.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        Instance = this;
        muncul = 0f;
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        posAwalRoot = transform.localPosition;
        sudahStart = true;
        if (materialGaris != null) BuatGarisOrbit();
        if (orbitSaatMuncul)
        {
            OrbitJalan = true;
            garisTarget = 1f;
        }

        // Kartu terdeteksi: bunyi "pop" + musik latar mulai pelan-pelan
        SuaraManager.Pop();
        if (musikLatar != null) SuaraManager.MainkanMusik(musikLatar, volumeMusik);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SuaraManager.HentikanMusik();
    }

    public static float EaseOutBack(float x)
    {
        x = Mathf.Clamp01(x);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // Muncul (pop) + zoom halus + melayang
        muncul = Mathf.MoveTowards(muncul, 1f, dt / Mathf.Max(0.01f, durasiMuncul));
        zoomNow = Mathf.Lerp(zoomNow, zoomTarget, 1f - Mathf.Exp(-12f * dt));
        transform.localScale = skalaAwal * Mathf.Max(0.001f, zoomNow * EaseOutBack(muncul));
        if (sudahStart && tinggiMelayang > 0f)
            transform.localPosition = posAwalRoot + Vector3.up * (Mathf.Sin(Time.time * 1.5f) * tinggiMelayang);

        // Orbit (mulai/berhenti halus) + rotasi pada poros (tetap pelan saat diam)
        faktorOrbit = Mathf.MoveTowards(faktorOrbit, OrbitJalan ? 1f : 0f, dt * 1.5f);
        float k = faktorOrbit * faktorOrbit * (3f - 2f * faktorOrbit) * kecepatanGlobal;
        foreach (Planet p in planets)
        {
            if (p == null) continue;
            if (k > 0f) p.transform.RotateAround(transform.position, transform.up, p.kecepatanOrbit * k * dt);
            float rot = rotasiSaatDiam + (1f - rotasiSaatDiam) * k;
            p.transform.Rotate(Vector3.up, p.kecepatanRotasi * rot * dt, Space.Self);
        }

        // Garis orbit muncul pelan
        garisNow = Mathf.MoveTowards(garisNow, garisTarget, dt * 2f);
        if (garisRoot != null)
        {
            bool aktif = garisNow > 0.001f;
            if (garisRoot.gameObject.activeSelf != aktif) garisRoot.gameObject.SetActive(aktif);
            if (aktif) foreach (LineRenderer lr in garis) lr.widthMultiplier = garisNow;
        }

        // Bulan muncul (pop) & mengorbit Bumi
        if (bulan != null && bumi != null)
        {
            bulanNow = Mathf.MoveTowards(bulanNow, bulanTarget, dt * 3f);
            bool aktif = bulanNow > 0.001f;
            if (bulan.gameObject.activeSelf != aktif) bulan.gameObject.SetActive(aktif);
            if (aktif)
            {
                bulan.localScale = skalaBulan * Mathf.Max(0.001f, EaseOutBack(bulanNow));
                sudutBulan += kecepatanBulan * dt;
                Vector3 lokal = Quaternion.AngleAxis(sudutBulan, Vector3.up) * Vector3.right * jarakBulan;
                bulan.position = bumi.transform.position + transform.TransformVector(lokal);
            }
        }

        HandleInput();
    }

    // ================= INPUT (Input System baru) =================
    void HandleInput()
    {
        var sentuhan = Touch.activeTouches;
        if (sentuhan.Count == 0) { InputMouse(); return; }

        if (sentuhan.Count >= 2)
        {
            bisaTap = false;   // sedang pinch -> jangan dianggap tap
            Touch a = sentuhan[0], b = sentuhan[1];
            float jarak = Vector2.Distance(a.screenPosition, b.screenPosition);
            if (a.phase == TouchPhase.Began || b.phase == TouchPhase.Began || jarakPinch <= 0f)
            {
                jarakPinch = jarak;
                return;
            }
            if (jarakPinch > 1f)
                zoomTarget = Mathf.Clamp(zoomTarget * (jarak / jarakPinch), zoomMin, zoomMax);
            jarakPinch = jarak;
            return;
        }

        jarakPinch = 0f;
        Touch t = sentuhan[0];
        float batasGeser = (Screen.dpi > 0 ? Screen.dpi : 160f) * 0.15f;

        switch (t.phase)
        {
            case TouchPhase.Began:
                posMulai = t.screenPosition;
                waktuMulai = Time.time;
                bisaTap = !PointerDiUI(t.screenPosition);
                break;
            case TouchPhase.Moved:
                if ((t.screenPosition - posMulai).magnitude > batasGeser) bisaTap = false;
                break;
            case TouchPhase.Ended:
                if (bisaTap && Time.time - waktuMulai < 0.4f) CobaTap(t.screenPosition);
                bisaTap = false;
                break;
            case TouchPhase.Canceled:
                bisaTap = false;
                break;
        }
    }

    // Untuk tes di Unity Editor pakai mouse: klik = tap, scroll = zoom
    void InputMouse()
    {
        Mouse m = Mouse.current;
        if (m == null) return;

        Vector2 pos = m.position.ReadValue();
        if (m.leftButton.wasPressedThisFrame)
        {
            posMulai = pos;
            bisaTap = !PointerDiUI(pos);
        }
        if (m.leftButton.wasReleasedThisFrame && bisaTap && (pos - posMulai).magnitude < 10f)
            CobaTap(pos);

        float scroll = m.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            zoomTarget = Mathf.Clamp(zoomTarget * (1f + Mathf.Sign(scroll) * 0.1f), zoomMin, zoomMax);
    }

    bool PointerDiUI(Vector2 pos)
    {
        if (EventSystem.current == null) return false;
        PointerEventData data = new PointerEventData(EventSystem.current) { position = pos };
        hasilUI.Clear();
        EventSystem.current.RaycastAll(data, hasilUI);
        foreach (RaycastResult r in hasilUI)
            if (r.module is GraphicRaycaster) return true;
        return false;
    }

    void CobaTap(Vector2 pos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Planet p = null;
        if (Physics.Raycast(cam.ScreenPointToRay(pos), out RaycastHit hit, 100f))
            p = hit.collider.GetComponentInParent<Planet>();
        if (p == null) p = PlanetTerdekatDiLayar(cam, pos);
        if (p != null) PilihPlanet(p);
    }

    Planet PlanetTerdekatDiLayar(Camera cam, Vector2 pos)
    {
        float min = (Screen.dpi > 0 ? Screen.dpi : 160f) * 0.35f;
        Planet terbaik = null;
        foreach (Planet p in planets)
        {
            if (p == null) continue;
            Vector3 sp = cam.WorldToScreenPoint(p.transform.position);
            if (sp.z < 0f) continue;
            float d = Vector2.Distance(pos, sp);
            if (d < min) { min = d; terbaik = p; }
        }
        return terbaik;
    }

    // ================= PILIH PLANET =================
    public void PilihPlanet(Planet p)
    {
        BatalkanNarasi();
        PilihInternal(p, true);
    }

    void PilihInternal(Planet p, bool bicara)
    {
        if (Terpilih != null && Terpilih != p) Terpilih.Lepas();
        Terpilih = p;
        p.Pilih();
        SuaraManager.Pop();
        if (bicara) Ucap(p.TeksBicara(), p.narasi);
        if (p == bumi) bulanTarget = 1f;
    }

    // ================= ORBIT =================
    public bool ToggleOrbit()
    {
        OrbitJalan = !OrbitJalan;
        if (OrbitJalan)
        {
            garisTarget = 1f;
            foreach (Planet p in planets) if (p != null) p.HentikanReset();
            SuaraManager.Pop();
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
            if (p == null) continue;
            Vector3 pos = p.transform.localPosition;
            float r = new Vector2(pos.x, pos.z).magnitude;

            GameObject go = new GameObject("Orbit_" + p.name);
            go.transform.SetParent(garisRoot, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.material = materialGaris;
            lr.startWidth = lebarGaris;
            lr.endWidth = lebarGaris;
            lr.widthMultiplier = 0f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            int n = 64;
            lr.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, pos.y, Mathf.Sin(a) * r));
            }
            garis.Add(lr);
        }
        garisRoot.gameObject.SetActive(false);
    }

    // ================= NARASI =================
    public bool ToggleNarasi()
    {
        if (SedangNarasi) { HentikanNarasi(); return false; }
        MulaiNarasi();
        return true;
    }

    public void MulaiNarasi()
    {
        BatalkanNarasi();
        narasiCo = StartCoroutine(NarasiRoutine());
    }

    public void HentikanNarasi()
    {
        BatalkanNarasi();
        BerhentiBicara();
        if (Terpilih != null) { Terpilih.Lepas(); Terpilih = null; }
    }

    IEnumerator NarasiRoutine()
    {
        Ucap(pembukaNarasi, null);
        yield return TungguBicara();

        // Matahari dulu, baru planet-planet
        if (matahari != null)
        {
            PilihInternal(matahari, false);
            Ucap(matahari.TeksBicara(), matahari.narasi);
            yield return TungguBicara();
            yield return new WaitForSeconds(0.3f);
        }

        foreach (Planet p in planets)
        {
            if (p == null) continue;
            PilihInternal(p, false);
            Ucap(p.TeksBicara(), p.narasi);
            yield return TungguBicara();
            yield return new WaitForSeconds(0.3f);
        }

        if (Terpilih != null) { Terpilih.Lepas(); Terpilih = null; }
        Ucap(penutupNarasi, null);
        yield return TungguBicara();
        narasiCo = null;
    }

    IEnumerator TungguBicara()
    {
        yield return null;
        yield return null;
        while (MasihBicara()) yield return null;
    }

    void BatalkanNarasi()
    {
        if (narasiCo != null) { StopCoroutine(narasiCo); narasiCo = null; }
    }

    void Ucap(string teks, AudioClip clip)
    {
        BerhentiBicara();
        if (clip != null && audioSource != null) { audioSource.clip = clip; audioSource.Play(); }
        else SuaraManager.Ucapkan(teks);
    }

    void BerhentiBicara()
    {
        SuaraManager.Berhenti();
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }

    bool MasihBicara()
    {
        if (audioSource != null && audioSource.isPlaying) return true;
        return SuaraManager.SedangBicara;
    }

    // ================= RESET =================
    public void ResetSemua()
    {
        BatalkanNarasi();
        BerhentiBicara();

        OrbitJalan = false;
        faktorOrbit = 0f;
        if (Terpilih != null) Terpilih.Lepas();
        Terpilih = null;

        foreach (Planet p in planets)
        {
            if (p == null) continue;
            p.Lepas();
            p.KembaliKeAwal(0.7f);
        }

        zoomTarget = 1f;
        garisTarget = 0f;
        bulanTarget = 0f;
        SuaraManager.Pop();
    }

    // ================= EFEK SUARA =================
    public void PlayKlik()  { if (klikClip != null && audioSource != null) audioSource.PlayOneShot(klikClip);  else SuaraManager.Klik(); }
    public void PlayBenar() { if (benarClip != null && audioSource != null) audioSource.PlayOneShot(benarClip); else SuaraManager.Benar(); }
    public void PlaySalah() { if (salahClip != null && audioSource != null) audioSource.PlayOneShot(salahClip); else SuaraManager.Salah(); }
}