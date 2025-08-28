using UnityEngine;
using FishNet.Object;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Connection;
using System.Collections.Generic; // Diperlukan untuk List

// Pastikan skrip ini menempel pada GameObject trigger di scene lobi Anda.
public class SceneChangeTrigger : NetworkBehaviour
{
    [Tooltip("Scene yang akan dimuat saat pemain masuk ke trigger ini.")]
    [SerializeField]
    private string _targetSceneName = "GameScene";

    [Header("Teleport Settings")]
    [Tooltip("Tag yang digunakan oleh spawn point di scene tujuan.")]
    [SerializeField]
    private string _spawnPointTag = "Respawn";

    // Fungsi ini akan berjalan saat ada objek lain yang masuk ke dalam trigger.
    private void OnTriggerEnter(Collider other)
    {
        // 1. Dapatkan komponen NetworkObject dari objek yang masuk.
        if (!other.TryGetComponent<NetworkObject>(out NetworkObject nob))
            return;

        // 2. Pastikan objek tersebut adalah milik client lokal (IsOwner).
        // Ini mencegah pemain lain memicu trigger untuk Anda.
        if (nob.IsOwner)
        {
            Debug.Log($"Pemain {nob.Owner.ClientId} masuk ke trigger, meminta pindah ke scene {_targetSceneName}.");
            // 3. Minta server untuk memindahkan scene kita.
            CmdChangeScene(_targetSceneName);
        }
    }

    // Fungsi ini berjalan di server untuk memindahkan satu pemain.
    [ServerRpc(RequireOwnership = false)]
    private void CmdChangeScene(string sceneName, NetworkConnection sender = null)
    {
        if (string.IsNullOrEmpty(sceneName) || sender.Objects.Count == 0) return;

        // --- PERBAIKAN: Ambil nama scene dari GameObject trigger ini, bukan dari objek pemain. ---
        // Ini jauh lebih andal karena trigger ini pasti berada di scene yang ingin kita unload.
        string oldSceneName = gameObject.scene.name;

        // Siapkan data untuk memuat scene baru.
        SceneLoadData sld = new SceneLoadData(sceneName);
        // Pindahkan semua objek milik koneksi ini ke scene baru.
        sld.MovedNetworkObjects = new NetworkObject[sender.Objects.Count];
        sender.Objects.CopyTo(sld.MovedNetworkObjects, 0);

        // Definisikan Aksi yang akan dijalankan setelah scene selesai dimuat.
        System.Action<SceneLoadEndEventArgs> onLoadEndAction = null;
        onLoadEndAction = (args) =>
        {
            // 1. Berhenti berlangganan event agar tidak berjalan lagi untuk pemain lain.
            InstanceFinder.SceneManager.OnLoadEnd -= onLoadEndAction;

            // 2. Cari spawn point di scene yang baru menggunakan tag.
            GameObject spawnPoint = GameObject.FindWithTag(_spawnPointTag);
            if (spawnPoint == null)
            {
                Debug.LogError($"[Server] Tidak dapat menemukan GameObject dengan tag '{_spawnPointTag}' di scene yang baru.");
                return;
            }

            // 3. Teleportasi HANYA pemain yang baru saja pindah.
            foreach (NetworkObject movedNob in sender.Objects)
            {
                if (movedNob.TryGetComponent(out PlayerTeleporter teleporter))
                {
                    Debug.Log($"[Server] Menteleportasi pemain milik ClientId {movedNob.Owner.ClientId}.");
                    teleporter.RpcTeleport(movedNob.Owner, spawnPoint.transform.position, spawnPoint.transform.rotation);
                }
            }

            // 4. Setelah teleportasi selesai, unload scene yang lama untuk koneksi ini.
            Debug.Log($"[Server] Melepaskan (unloading) scene '{oldSceneName}' untuk ClientId {sender.ClientId}.");
            SceneUnloadData sud = new SceneUnloadData(oldSceneName);
            InstanceFinder.SceneManager.UnloadConnectionScenes(sender, sud);
        };

        // Berlangganan (subscribe) ke event OnLoadEnd.
        InstanceFinder.SceneManager.OnLoadEnd += onLoadEndAction;

        // Jalankan proses load scene HANYA untuk koneksi yang meminta.
        InstanceFinder.SceneManager.LoadConnectionScenes(sender, sld);
    }
}
