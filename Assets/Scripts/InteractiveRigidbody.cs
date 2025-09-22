using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro; // Namespace untuk TextMeshPro

[RequireComponent(typeof(Rigidbody))]
public class InteractiveRigidbody : NetworkBehaviour
{
    [Header("Pengaturan Massa")]
    [Tooltip("Massa minimal yang bisa diatur.")]
    public float minMass = 1f;
    [Tooltip("Massa maksimal yang bisa diatur.")]
    public float maxMass = 100f;
    [Tooltip("Berapa banyak massa berubah setiap kali tombol ditekan.")]
    public float massIncrement = 1f;

    [Header("Referensi UI")]
    [Tooltip("Teks untuk menampilkan massa saat ini.")]
    public TextMeshProUGUI massDisplayText;

    // SyncVar untuk menyinkronkan massa ke semua client.
    private readonly SyncVar<float> _currentMass = new SyncVar<float>();

    private Rigidbody _rigidbody;

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Server menginisialisasi massa awal.
        _rigidbody = GetComponent<Rigidbody>();
        _currentMass.Value = _rigidbody.mass;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Semua client perlu me-subscribe untuk memperbarui UI dan Rigidbody.
        _currentMass.OnChange += OnMassChanged;

        // Inisialisasi awal untuk client yang baru bergabung.
        if (TryGetComponent<Rigidbody>(out _rigidbody))
        {
            OnMassChanged(0, _currentMass.Value, false);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        // Selalu unsubscribe.
        _currentMass.OnChange -= OnMassChanged;
    }

    // Fungsi ini dipanggil di SEMUA client setiap kali nilai _currentMass berubah di server.
    private void OnMassChanged(float prev, float next, bool asServer)
    {
        // Perbarui komponen Rigidbody secara lokal.
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.mass = next;

        // Perbarui teks di UI.
        if (massDisplayText != null)
        {
            massDisplayText.text = $"Massa objek\nsaat ini\n{next.ToString("F1")} kg"
;
        }
    }

    // Fungsi yang akan dipanggil oleh pemain untuk mengubah massa.
    // Ini harus berupa ServerRpc karena hanya server yang boleh mengubah SyncVar.
    [ServerRpc(RequireOwnership = false)] // RequireOwnership = false agar siapa saja bisa mengubah.
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

        // Pastikan massa tetap dalam batas min/max.
        _currentMass.Value = Mathf.Clamp(newMass, minMass, maxMass);
    }
}

