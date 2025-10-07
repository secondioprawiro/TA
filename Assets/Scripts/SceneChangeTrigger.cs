using UnityEngine;
using FishNet.Object;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Connection;
using System.Collections.Generic;

public class SceneChangeTrigger : NetworkBehaviour
{
    [Tooltip("Scene yang akan dimuat saat pemain masuk ke trigger ini.")]
    [SerializeField]
    private string _targetSceneName = "GameScene";

    [Header("Teleport Settings")]
    [Tooltip("Tag yang digunakan oleh spawn point di scene tujuan.")]
    [SerializeField]
    private string _spawnPointTag = "Respawn";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<NetworkObject>(out NetworkObject nob))
            return;

        if (nob.IsOwner)
        {
            Debug.Log($"Pemain {nob.Owner.ClientId} masuk ke trigger, meminta pindah ke scene {_targetSceneName}.");
            CmdChangeScene(_targetSceneName);
        }
    }

    // Fungsi ini berjalan di server untuk memindahkan satu pemain.
    [ServerRpc(RequireOwnership = false)]
    private void CmdChangeScene(string sceneName, NetworkConnection sender = null)
    {
        if (string.IsNullOrEmpty(sceneName) || sender.Objects.Count == 0) return;

        string oldSceneName = gameObject.scene.name;

        SceneLoadData sld = new SceneLoadData(sceneName);
        sld.MovedNetworkObjects = new NetworkObject[sender.Objects.Count];
        sender.Objects.CopyTo(sld.MovedNetworkObjects, 0);

        System.Action<SceneLoadEndEventArgs> onLoadEndAction = null;
        onLoadEndAction = (args) =>
        {
            InstanceFinder.SceneManager.OnLoadEnd -= onLoadEndAction;

            GameObject spawnPoint = GameObject.FindWithTag(_spawnPointTag);
            if (spawnPoint == null)
            {
                Debug.LogError($"[Server] Tidak dapat menemukan GameObject dengan tag '{_spawnPointTag}' di scene yang baru.");
                return;
            }

            foreach (NetworkObject movedNob in sender.Objects)
            {
                if (movedNob.TryGetComponent(out PlayerTeleporter teleporter))
                {
                    Debug.Log($"[Server] Menteleportasi pemain milik ClientId {movedNob.Owner.ClientId}.");
                    teleporter.RpcTeleport(movedNob.Owner, spawnPoint.transform.position, spawnPoint.transform.rotation);
                }
            }

            Debug.Log($"[Server] Melepaskan (unloading) scene '{oldSceneName}' untuk ClientId {sender.ClientId}.");
            SceneUnloadData sud = new SceneUnloadData(oldSceneName);
            InstanceFinder.SceneManager.UnloadConnectionScenes(sender, sud);
        };

        InstanceFinder.SceneManager.OnLoadEnd += onLoadEndAction;

        InstanceFinder.SceneManager.LoadConnectionScenes(sender, sld);
    }
}
