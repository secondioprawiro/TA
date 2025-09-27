using FishNet.Object;
using UnityEngine;

public class NetworkSpawnLogger : NetworkBehaviour
{
    public override void OnStartServer()
    {
        Debug.Log($"[SERVER] Spawned: {name}  IsSpawned={NetworkObject.IsSpawned}  Owner={NetworkObject.Owner}");
    }

    public override void OnStopServer()
    {
        Debug.Log($"[SERVER] Despawned: {name}");
    }

    public override void OnStartClient()
    {
        Debug.Log($"[CLIENT] Seen: {name}  IsOwner={IsOwner}  IsServer={IsServerInitialized}  IsSpawned={NetworkObject.IsSpawned}");
    }
}
