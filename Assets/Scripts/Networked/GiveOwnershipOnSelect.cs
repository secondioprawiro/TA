using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GiveOwnershipOnSelect : NetworkBehaviour
{
    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        var playerNob = args.interactorObject.transform.GetComponentInParent<NetworkObject>();
        if (playerNob && playerNob.Owner != null)
            RequestGiveOwnership(playerNob.Owner);
    }

    public void OnSelectExited(SelectExitEventArgs args)
        => ReturnOwnershipServer();

    [ServerRpc(RequireOwnership = false)]
    private void RequestGiveOwnership(NetworkConnection newOwner)
    {
        if (!NetworkObject || !NetworkObject.IsSpawned || newOwner == null || !newOwner.IsActive) return;
        NetworkObject.GiveOwnership(newOwner);
        Debug.Log($"[SERVER] Ownership -> {newOwner.ClientId} for {name}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReturnOwnershipServer()
    {
        if (!NetworkObject || !NetworkObject.IsSpawned) return;
        NetworkObject.RemoveOwnership();
    }
}
