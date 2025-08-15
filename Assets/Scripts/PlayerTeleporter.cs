using UnityEngine;
using FishNet.Object;
using System.Collections;
using FishNet.Connection;

public class PlayerTeleporter : NetworkBehaviour
{
    [TargetRpc]
    public void RpcTeleport(NetworkConnection ownerConnection, Vector3 position, Quaternion rotation)
    {
        StartCoroutine(Teleport_Coroutine(position, rotation));
    }

    private IEnumerator Teleport_Coroutine(Vector3 position, Quaternion rotation)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        yield return null;

        transform.position = position;
        transform.rotation = rotation;

        yield return null;

        if (cc != null)
            cc.enabled = true;
    }
}