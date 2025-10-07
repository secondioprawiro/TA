using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class InteractiveRigidbody : NetworkBehaviour
{
    [Header("Identitas Objek")]
    [Tooltip("Nama yang akan ditampilkan di UI, contoh: OBJEK A")]
    public string objectName = "OBJEK";

    [Header("Pengaturan Massa")]
    [Tooltip("Massa minimal yang bisa diatur.")]
    public float minMass = 1f;
    [Tooltip("Massa maksimal yang bisa diatur.")]
    public float maxMass = 100f;
    [Tooltip("Berapa banyak massa berubah setiap kali tombol ditekan.")]
    public float massIncrement = 1f;

    [Header("Referensi UI")]
    [Tooltip("Teks untuk menampilkan info objek ini.")]
    public TextMeshProUGUI massDisplayText;

    private readonly SyncVar<float> _currentMass = new SyncVar<float>();
    private Rigidbody _rigidbody;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _rigidbody = GetComponent<Rigidbody>();
        _currentMass.Value = _rigidbody.mass;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _currentMass.OnChange += OnMassChanged;
        if (TryGetComponent<Rigidbody>(out _rigidbody))
        {
            OnMassChanged(0, _currentMass.Value, false);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _currentMass.OnChange -= OnMassChanged;
    }

    private void OnMassChanged(float prev, float next, bool asServer)
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.mass = next;

        float currentDrag = _rigidbody.drag;

        // Script ini sekarang HANYA mengupdate teksnya sendiri.
        if (massDisplayText != null)
        {
            massDisplayText.text = $"{objectName}\n\nMassa: {next.ToString("F1")} kg\nHambatan: {currentDrag.ToString("F1")}";
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AdjustMass(bool increase)
    {
        float newMass = _currentMass.Value;
        if (increase)
        {
            newMass += massIncrement;
        }
        else
        {
            newMass -= massIncrement;
        }
        _currentMass.Value = Mathf.Clamp(newMass, minMass, maxMass);
    }
}