using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing; // Namespace penting untuk SyncVar modern
using StarterAssets; // Namespace untuk ThirdPersonController, sesuaikan jika perlu

public class BasicRigidBodyPushNetworked : NetworkBehaviour
{
    [Header("Push Settings")]
    public LayerMask pushLayers;
    [Range(0.5f, 5f)] public float strength = 1.1f;

    [Header("Raycast Settings")]
    [Tooltip("Jarak maksimal dari pemain untuk bisa mulai mendorong.")]
    public float pushDistance = 1.5f;

    [Header("Dependencies")]
    [Tooltip("Masukkan komponen Third Person Controller Networked (atau script yang mengatur rotasi) di sini.")]
    // DIUBAH: Tipe data diubah ke ThirdPersonControllerNetworked
    public ThirdPersonControllerNetworked thirdPersonController;

    // Komponen internal
    private NetworkObject networkObject;
    private Animator _animator;
    private bool _hasAnimator;

    // Menggunakan tipe SyncVar<bool> yang baru untuk menyinkronkan status 'mendorong'
    // ke semua client. Server yang akan mengubah nilainya.
    private readonly SyncVar<bool> _isPushing = new SyncVar<bool>();

    #region Initialization & Deinitialization

    // OnStartClient dipanggil pada semua client (termasuk server/host) saat objek ini muncul.
    public override void OnStartClient()
    {
        base.OnStartClient();

        // Mendapatkan referensi komponen yang dibutuhkan.
        _hasAnimator = TryGetComponent(out _animator);
        networkObject = GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("BasicRigidBodyPushNetworked requires a NetworkObject component.", this);
        }

        // Jika referensi controller belum di-set di Inspector, coba cari otomatis.
        if (thirdPersonController == null)
        {
            // DIUBAH: Mencari komponen dengan nama yang benar
            thirdPersonController = GetComponent<ThirdPersonControllerNetworked>();
        }

        // Mendaftarkan fungsi OnPushingStateChanged agar dipanggil setiap kali
        // nilai _isPushing berubah di server. Ini penting agar client bisa mengupdate animasinya.
        _isPushing.OnChange += OnPushingStateChanged;
    }

    // OnStopClient dipanggil saat objek dihancurkan/hilang dari network.
    public override void OnStopClient()
    {
        base.OnStopClient();
        // Selalu batalkan pendaftaran (unsubscribe) untuk menghindari memory leak atau error.
        _isPushing.OnChange -= OnPushingStateChanged;
    }

    #endregion

    // Update dipanggil setiap frame.
    private void Update()
    {
        // Blokade agar hanya client pemilik objek (local player) yang bisa mengirim input.
        if (!base.IsOwner)
            return;

        // Kirim Raycast ke depan dari posisi tengah tubuh pemain
        bool canSeePushable = Physics.Raycast(
            transform.position + (Vector3.up * 0.5f), // Titik awal raycast
            transform.forward,                         // Arah ke depan
            out RaycastHit hit,                        // Informasi tabrakan
            pushDistance,                              // Jarak maksimal
            pushLayers                                 // Hanya deteksi layer yang ditentukan
        );

        // Jika Raycast mengenai objek yang bisa didorong DAN pemain menekan 'E'
        if (canSeePushable && Input.GetKeyDown(KeyCode.E) && _hasAnimator)
        {
            // Kirim permintaan ke server untuk MENGUBAH status (toggle).
            CmdSetPushingState(!_isPushing.Value);
        }

        // Fitur keamanan: Jika sedang mendorong tapi sudah tidak melihat objek (misal berbalik), hentikan dorongan.
        if (_isPushing.Value && !canSeePushable)
        {
            CmdSetPushingState(false);
        }
    }

    // ServerRpc: Fungsi ini dipanggil oleh client, tapi dieksekusi di SERVER.
    [ServerRpc]
    private void CmdSetPushingState(bool newState)
    {
        // Server mengubah nilai SyncVar. Perubahan ini akan otomatis dikirim
        // ke semua client yang terhubung.
        _isPushing.Value = newState;
    }

    // Fungsi ini dipanggil secara otomatis di SEMUA client setiap kali
    // _isPushing.Value berubah di server.
    private void OnPushingStateChanged(bool prev, bool next, bool asServer)
    {
        if (_hasAnimator)
        {
            // Perbarui parameter 'isPushing' di Animator sesuai nilai terbaru.
            _animator.SetBool("PushBool", next);
        }

        // Logika baru untuk mengunci/membuka rotasi player
        if (base.IsOwner && thirdPersonController != null)
        {
            // Jika 'next' adalah true, berarti kita mulai mendorong -> kunci rotasi
            // Jika 'next' adalah false, berarti kita berhenti -> buka kunci rotasi
            thirdPersonController.LockRotation(next);
        }
    }

    // OnControllerColliderHit dipanggil saat CharacterController menabrak collider lain.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Pastikan hanya client pemilik yang menerapkan fisika dorongan.
        if (!base.IsOwner)
            return;

        // Hanya terapkan dorongan fisik jika sedang dalam state 'mendorong'.
        if (_isPushing.Value)
        {
            // Pastikan kita hanya mendorong objek yang ada di pushLayers
            var hitLayerMask = 1 << hit.gameObject.layer;
            if ((hitLayerMask & pushLayers.value) != 0)
            {
                PushRigidBodies(hit);
            }
        }
    }

    // Fungsi untuk menerapkan gaya dorong ke Rigidbody.
    // Logika ini tidak perlu diubah.
    private void PushRigidBodies(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;

        var bodyLayerMask = 1 << body.gameObject.layer;
        if ((bodyLayerMask & pushLayers.value) == 0) return;

        if (hit.moveDirection.y < -0.3f) return;

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);
        GameObject selectedObject = hit.collider.gameObject;

        TransferObjectOwnership(selectedObject);
        body.AddForce(pushDir * strength, ForceMode.Impulse);
        RemoveTransferObjectOwnership(selectedObject);
    }

    #region Ownership Transfer
    // Semua fungsi di bawah ini untuk mentransfer kepemilikan objek yang didorong
    // agar fisika tersinkronisasi dengan benar. Logika ini sudah benar dan tidak perlu diubah.
    public void TransferObjectOwnership(GameObject selectedObject)
    {
        if (networkObject == null || networkObject.Owner == null) return;
        NetworkObject objectNetworkObject = selectedObject.GetComponent<NetworkObject>();
        if (objectNetworkObject != null)
        {
            TransferObjectOwnershipServer(objectNetworkObject, networkObject.Owner);
        }
    }

    public void RemoveTransferObjectOwnership(GameObject selectedObject)
    {
        if (networkObject == null || networkObject.Owner == null) return;
        NetworkObject ObjectNetworkObject = selectedObject.GetComponent<NetworkObject>();
        if (ObjectNetworkObject != null)
        {
            RemoveTransferObjectOwnershipServer(ObjectNetworkObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TransferObjectOwnershipServer(NetworkObject targetObject, NetworkConnection newOwner)
    {
        targetObject.GiveOwnership(newOwner);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveTransferObjectOwnershipServer(NetworkObject targetObject)
    {
        targetObject.RemoveOwnership();
    }
    #endregion
}

