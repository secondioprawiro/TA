using UnityEngine;
using UnityEngine.UI; // Ditambahkan untuk mengakses komponen Button
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

    // --- Sisa kode Anda tidak diubah ---
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!base.IsOwner) return;
        if (_isPushing.Value)
        {
            var hitLayerMask = 1 << hit.gameObject.layer;
            if ((hitLayerMask & pushLayers.value) != 0)
            {
                PushRigidBodies(hit);
            }
        }
    }

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
    public void TransferObjectOwnership(GameObject selectedObject) {
        if (networkObject == null || networkObject.Owner == null) 
            return; 

        NetworkObject objectNetworkObject = selectedObject.GetComponent<NetworkObject>(); 

        if (objectNetworkObject != null) { 
            TransferObjectOwnershipServer(objectNetworkObject, networkObject.Owner); 
        } 
    }
    public void RemoveTransferObjectOwnership(GameObject selectedObject) { 
        if (networkObject == null || networkObject.Owner == null) 
            return;
        
        NetworkObject ObjectNetworkObject = selectedObject.GetComponent<NetworkObject>(); 

        if (ObjectNetworkObject != null) { 
            RemoveTransferObjectOwnershipServer(ObjectNetworkObject); 
        } 
    }
    [ServerRpc(RequireOwnership = false)]
    private void TransferObjectOwnershipServer(NetworkObject targetObject, NetworkConnection newOwner) { 
        targetObject.GiveOwnership(newOwner); 
    }
    [ServerRpc(RequireOwnership = false)]
    private void RemoveTransferObjectOwnershipServer(NetworkObject targetObject) { 
        targetObject.RemoveOwnership(); 
    }
    #endregion
}

