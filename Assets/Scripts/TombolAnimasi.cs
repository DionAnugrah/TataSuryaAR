using UnityEngine;
using UnityEngine.EventSystems;

// Efek tombol mengecil saat ditekan & memantul saat dilepas.
// Dipasang otomatis oleh UIManager ke semua Button.
public class TombolAnimasi : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float skalaTekan = 0.9f;

    Vector3 awal;
    float now = 1f, target = 1f, vel;

    void Awake() { awal = transform.localScale; }

    void OnDisable()
    {
        now = target = 1f; vel = 0f;
        transform.localScale = awal;
    }

    public void OnPointerDown(PointerEventData e) { target = skalaTekan; }
    public void OnPointerUp(PointerEventData e)   { target = 1f; vel = 4f; }
    public void OnPointerExit(PointerEventData e)  { target = 1f; }

    public void Pop(float dari) { now = dari; vel = 0f; target = 1f; }

    void Update()
    {
        now = Mathf.SmoothDamp(now, target, ref vel, 0.07f, Mathf.Infinity, Time.unscaledDeltaTime);
        transform.localScale = awal * now;
    }
}