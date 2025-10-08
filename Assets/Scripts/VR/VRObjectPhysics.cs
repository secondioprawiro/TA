using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using FishNet.Object;
using FishNet.Connection;

public class VRObjectPhysics : NetworkBehaviour
{
    private Rigidbody rb;
    private XRBaseInteractable interactable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        interactable = GetComponent<XRBaseInteractable>();

        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (!base.IsOwner)
        {
            ServerRequestOwnership();
        }
        else
        {
            ServerSetKinematic(true);
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (!base.IsOwner) return;

        ServerSetKinematic(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerRequestOwnership(NetworkConnection sender = null)
    {
        base.GiveOwnership(sender);
        ServerSetKinematic(true);
    }

    [ServerRpc]
    private void ServerSetKinematic(bool isKinematic)
    {
        rb.isKinematic = isKinematic;
        ObserversSetKinematic(isKinematic);
    }

    [ObserversRpc]
    private void ObserversSetKinematic(bool isKinematic)
    {
        //if (base.IsOwner) return;
        rb.isKinematic = isKinematic;
    }
}