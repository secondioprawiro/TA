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

    [Tooltip("Tombol UI untuk menambah massa.")]
    public Button increaseMassButton;
    [Tooltip("Tombol UI untuk mengurangi massa.")]
    public Button decreaseMassButton;

    //private MassControlPanel massControlPanel;

    private NetworkObject networkObject;
    private Animator _animator;
    private bool _hasAnimator;
    private bool _canCurrentlyPush = false;
    private InteractiveRigidbody _lastSeenRigidbody;

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

        if (base.IsOwner)
        {
            if (mobilePushButton != null)
            {
                mobilePushButton.onClick.AddListener(OnPushInteraction);
            }
            if (increaseMassButton != null)
            {
                increaseMassButton.onClick.AddListener(OnIncreaseMassPressed);
            }
            if (decreaseMassButton != null)
            {
                decreaseMassButton.onClick.AddListener(OnDecreaseMassPressed);
            }
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _isPushing.OnChange -= OnPushingStateChanged;

        if (base.IsOwner)
        {
            if (mobilePushButton != null)
            {
                mobilePushButton.onClick.RemoveListener(OnPushInteraction);
            }
            if (increaseMassButton != null)
            {
                increaseMassButton.onClick.RemoveListener(OnIncreaseMassPressed);
            }
            if (decreaseMassButton != null)
            {
                decreaseMassButton.onClick.RemoveListener(OnDecreaseMassPressed);
            }
        }
    }
    #endregion

    private void Update()
    {
        if (!base.IsOwner) return;

        bool canSeeObject = Physics.Raycast(
            transform.position + (Vector3.up * 0.5f),
            transform.forward,
            out RaycastHit hit,
            pushDistance,
            pushLayers
        );

        _canCurrentlyPush = canSeeObject || _isPushing.Value;

        if (mobilePushButton != null){
            mobilePushButton.interactable = _canCurrentlyPush;
        }

        if (canSeeObject)
        {
            Debug.Log("Raycast mengenai: " + hit.collider.name);
            InteractiveRigidbody irb = hit.collider.GetComponentInParent<InteractiveRigidbody>();
            _lastSeenRigidbody = hit.collider.GetComponent<InteractiveRigidbody>();
            if (irb != null)
            {
                Debug.Log("BERHASIL menemukan InteractiveRigidbody!");
            }
            else
            {
                Debug.LogError("GAGAL menemukan InteractiveRigidbody pada " + hit.collider.name + " atau induknya!");
            }
        }
        else{
            _lastSeenRigidbody = null;
        }

        if (_lastSeenRigidbody != null && MassControlPanel.Instance != null && !_isPushing.Value)
        {
            MassControlPanel.Instance.ShowPanel(_lastSeenRigidbody);
        }
        else if (MassControlPanel.Instance != null)
        {
            MassControlPanel.Instance.HidePanel();
        }

        HandleMassControlUI();

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
                PushRigidBodies(hit);
            }
        }
    }

    private void PushRigidBodies(ControllerColliderHit hit)
    {
        NetworkObject targetNO = hit.collider.GetComponent<NetworkObject>();
        if (targetNO == null) return;

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);

        CmdApplyForce(targetNO, pushDir * strength);
    }
    private void HandleMassControlUI()
    {
        bool shouldShowMassControls = _lastSeenRigidbody != null && !_isPushing.Value;

        if (increaseMassButton != null)
        {
            increaseMassButton.interactable = shouldShowMassControls;
        }
        if (decreaseMassButton != null)
        {
            decreaseMassButton.interactable = shouldShowMassControls;
        }
    }
    private void OnIncreaseMassPressed()
    {
        if (_lastSeenRigidbody != null)
        {
            _lastSeenRigidbody.AdjustMass(true);
        }
    }
    private void OnDecreaseMassPressed()
    {
        if (_lastSeenRigidbody != null)
        {
            _lastSeenRigidbody.AdjustMass(false);
        }
    }

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

