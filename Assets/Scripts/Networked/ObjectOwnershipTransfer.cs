using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using FishNet.Connection;
using FishNet.Object;

public class ObjectOwnershipTransfer : NetworkBehaviour
{
    private NetworkObject networkObject;
    private NetworkConnection _ownerConn;

    public override void OnStartClient()
    {
        base.OnStartClient();

        networkObject = GetComponentInParent<NetworkObject>();
        _ownerConn = _ownerConn != null ? networkObject.Owner : null;

        if (networkObject == null)
        {
            Debug.LogError("Tidak menemukan NetworkObject di parent player.");
            return;
        }
        else{
            Debug.Log($"PlayerNOB={networkObject.name}  IsOwner={IsOwner}  OwnerConn={_ownerConn}");
        }
    }
    

    // Method to turn on gravity when the object is selected
    public void EnableTransferOnSelectEntered(SelectEnterEventArgs args)
    {
        var selectedGo = args.interactableObject.transform.gameObject;
        var rb = selectedGo.GetComponent<Rigidbody>();

        Debug.Log($"[OOT][CLIENT] SelectEntered {selectedGo.name}");

        TryTransfer(selectedGo);

        if (rb != null)
        {
            rb.isKinematic = true;
            Debug.Log($"[OOT] Set kinematic ON for {selectedGo.name}");
        }
        else
        {
            Debug.LogError($"[OOT] {selectedGo.name} tidak punya Rigidbody!");
        }

    }


    // Method to turn off gravity when the object is deselected
    public void DisableTransferOnSelectExited(SelectExitEventArgs args)
    {
        var go = args.interactableObject.transform.gameObject;
        var rb = go.GetComponent<Rigidbody>();

        Debug.Log($"[OOT][CLIENT] SelectExited {go.name}");

        TryRemove(go);

        if (rb != null)
        {
            rb.isKinematic = false;
            Debug.Log($"[OOT] Set kinematic OFF for {go.name}");
        }
        else
        {
            Debug.LogError($"[OOT] {go.name} tidak punya Rigidbody!");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TransferObjectOwnershipServer(NetworkObject targetObject, NetworkConnection newOwner)
    {
        if (targetObject == null || !targetObject.IsSpawned)
        {
            Debug.LogWarning("[OOT][SERVER] Target null atau belum Spawned → batal transfer.");
            return;
        }
        if (newOwner == null || !newOwner.IsActive)
        {
            Debug.LogWarning("[OOT][SERVER] newOwner null/tidak aktif.");
            return;
        }

        Debug.Log($"[OOT][SERVER] GiveOwnership {targetObject.name} -> {newOwner.ClientId}");
        targetObject.GiveOwnership(newOwner);
        Debug.Log($"[OOT][SERVER] Transfer OK. CurrentOwner={targetObject.Owner?.ClientId}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveTransferObjectOwnershipServer(NetworkObject targetObject)
    {
        if (targetObject == null || !targetObject.IsSpawned)
        {
            Debug.LogWarning("[OOT][SERVER] Target null atau belum Spawned → batal remove.");
            return;
        }

        Debug.Log($"[OOT][SERVER] RemoveOwnership {targetObject.name}");
        targetObject.RemoveOwnership();
    }

    private void TryTransfer(GameObject go)
    {
        if (networkObject == null || _ownerConn == null)
        {
            Debug.LogWarning("[OOT][CLIENT] PlayerNOB/OwnerConn null → batal transfer.");
            return;
        }

        var targetNob = go.GetComponent<NetworkObject>();
        Debug.Log($"[OOT][CLIENT] TryTransfer {go.name}  HasNOB={targetNob != null}  IsSpawned={(targetNob && targetNob.IsSpawned)}");

        if (targetNob != null)
            TransferObjectOwnershipServer(targetNob, _ownerConn);
    }

    private void TryRemove(GameObject go)
    {
        var targetNob = go.GetComponent<NetworkObject>();
        if (targetNob != null)
            RemoveTransferObjectOwnershipServer(targetNob);
    }

}

