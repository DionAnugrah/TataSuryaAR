using UnityEngine;

// Pasang di SETIAP planet (Merkurius s/d Neptunus, termasuk Bumi).
// Planet harus punya Collider (Sphere sudah otomatis punya).
public class Planet : MonoBehaviour
{
    [Header("Data")]
    public string namaPlanet;
    public string diameter;
    public string jarakDariMatahari;
    public string jumlahSatelit;
    [TextArea] public string ciriKhas;
    public AudioClip narasi;

    [Header("Gerak")]
    public float kecepatanOrbit = 20f;   // derajat per detik mengelilingi Matahari
    public float kecepatanRotasi = 30f;  // derajat per detik pada porosnya

    Vector3 posAwal, skalaAwal;
    Quaternion rotAwal;
    Renderer rend;

    void Awake()
    {
        posAwal = transform.localPosition;
        skalaAwal = transform.localScale;
        rotAwal = transform.localRotation;
        rend = GetComponentInChildren<Renderer>();
    }

    public void Pilih()
    {
        transform.localScale = skalaAwal * 1.4f;
        SetGlow(true);
    }

    public void Lepas()
    {
        transform.localScale = skalaAwal;
        SetGlow(false);
    }

    public void ResetPosisi()
    {
        transform.localPosition = posAwal;
        transform.localRotation = rotAwal;
        transform.localScale = skalaAwal;
        SetGlow(false);
    }

    void SetGlow(bool on)
    {
        if (rend == null) return;
        Material m = rend.material;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", on ? Color.white * 0.5f : Color.black);
    }

    public string Info()
    {
        return namaPlanet + "\n" +
               "Diameter: " + diameter + "\n" +
               "Jarak dari Matahari: " + jarakDariMatahari + "\n" +
               "Jumlah satelit: " + jumlahSatelit + "\n\n" +
               ciriKhas;
    }
}
