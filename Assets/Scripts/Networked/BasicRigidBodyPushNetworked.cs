using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using StarterAssets;

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
    public ThirdPersonControllerNetworked thirdPersonController;

    [Header("Mobile UI")]
    [Tooltip("Seret Tombol Push dari UI Mobile ke sini.")]
    public Button mobilePushButton;

    private NetworkObject networkObject;
    private Animator _animator;
    private bool _hasAnimator;
    private bool _canCurrentlyPush = false;

    private readonly SyncVar<bool> _isPushing = new SyncVar<bool>();

    #region Initialization & Deinitialization
    public override void OnStartClient()
    {
        base.OnStartClient();
        _hasAnimator = TryGetComponent(out _animator);
        networkObject = GetComponent<NetworkObject>();
        if (networkObject == null) Debug.LogError("BasicRigidBodyPushNetworked requires a NetworkObject component.", this);
        if (thirdPersonController == null) thirdPersonController = GetComponent<ThirdPersonControllerNetworked>();
        _isPushing.OnChange += OnPushingStateChanged;

        if (base.IsOwner && mobilePushButton != null)
        {
            mobilePushButton.onClick.AddListener(OnPushInteraction);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _isPushing.OnChange -= OnPushingStateChanged;

        if (base.IsOwner && mobilePushButton != null)
        {
            mobilePushButton.onClick.RemoveListener(OnPushInteraction);
        }
    }
    #endregion

    private void Update()
    {
        if (!base.IsOwner) return;

        bool canSeePushable = Physics.Raycast(
            transform.position + (Vector3.up * 0.5f),
            transform.forward,
            out RaycastHit hit,
            pushDistance,
            pushLayers
        );

        _canCurrentlyPush = canSeePushable || _isPushing.Value;

        if (mobilePushButton != null)
        {
            mobilePushButton.interactable = _canCurrentlyPush;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            OnPushInteraction();
        }
    }

    public void OnPushInteraction()
    {
        if (!_canCurrentlyPush) return;

        if (!_isPushing.Value)
        {
            bool canSeePushable = Physics.Raycast(
                transform.position + (Vector3.up * 0.5f),
                transform.forward,
                pushDistance,
                pushLayers
            );
            if (!canSeePushable) return;
        }

        CmdSetPushingState(!_isPushing.Value);
    }

    [ServerRpc]
    private void CmdSetPushingState(bool newState)
    {
        _isPushing.Value = newState;
    }

    private void OnPushingStateChanged(bool prev, bool next, bool asServer)
    {
        if (_hasAnimator)
        {
            _animator.SetBool("PushBool", next);
        }

        if (base.IsOwner && thirdPersonController != null)
        {
            thirdPersonController.LockRotation(next);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!base.IsOwner) return;
        if (_isPushing.Value)
        {
            var hitLayerMask = 1 << hit.gameObject.layer;
            if ((hitLayerMask & pushLayers.value) != 0)
            {
                // DIUBAH: Panggil fungsi PushRigidBodies di sini
                PushRigidBodies(hit);
            }
        }
    }

    // DIUBAH: Fungsi ini sekarang hanya mengirim permintaan ke server.
    private void PushRigidBodies(ControllerColliderHit hit)
    {
        // Pastikan objek yang ditabrak punya NetworkObject
        NetworkObject targetNO = hit.collider.GetComponent<NetworkObject>();
        if (targetNO == null) return;

        // Dapatkan arah dorongan
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);

        // Kirim permintaan ke server untuk menerapkan gaya
        CmdApplyForce(targetNO, pushDir * strength);
    }

    // BARU: ServerRpc untuk menerapkan gaya di server
    [ServerRpc]
    private void CmdApplyForce(NetworkObject targetObject, Vector3 force)
    {
        // Pastikan objeknya ada dan memiliki Rigidbody
        if (targetObject != null && targetObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            // Terapkan gaya di server. NetworkTransform akan menyinkronkan hasilnya.
            rb.AddForce(force, ForceMode.Impulse);
        }
    }

}

