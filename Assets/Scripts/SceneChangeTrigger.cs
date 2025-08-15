using UnityEngine;
using FishNet.Object; // Diperlukan untuk NetworkBehaviour
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Connection;
using System.Collections; // Diperlukan untuk Coroutine (IEnumerator)
using UnityEngine.SceneManagement; // Diperlukan untuk mengakses Scene

// PERBAIKAN: Ganti MonoBehaviour menjadi NetworkBehaviour
public class SceneChangeTrigger : NetworkBehaviour
{
    [Tooltip("Scene yang akan dimuat saat pemain masuk ke trigger ini.")]
    [SerializeField]
    private string _targetSceneName = "GameScene"; // Pastikan nama ini sama persis dengan nama file scene kedua Anda.

    // Fungsi ini akan berjalan saat ada objek lain yang masuk ke dalam trigger.
    private void OnTriggerEnter(Collider other)
    {
        // 1. Dapatkan komponen NetworkObject dari objek yang masuk.
        NetworkObject nob = other.GetComponent<NetworkObject>();

        // 2. Jika objek itu adalah objek jaringan DAN kita adalah pemiliknya (ini penting!).
        if (nob != null && nob.IsOwner)
        {
            Debug.Log($"Pemain {nob.Owner.ClientId} masuk ke trigger, meminta pindah ke scene {_targetSceneName}.");

            // 3. Minta server untuk memindahkan scene kita, kirim juga NetworkObject pemain.
            ServerRequestSceneChange(nob);
        }
    }

    // Fungsi ini hanya akan berjalan di server.
    [ServerRpc(RequireOwnership = false)]
    private void ServerRequestSceneChange(NetworkObject playerObject, NetworkConnection sender = null)
    {
        // Siapkan data untuk memuat scene baru.
        SceneLoadData sld = new SceneLoadData(_targetSceneName);
        // PENTING: Beritahu FishNet objek mana yang harus ikut pindah ke scene baru.
        sld.MovedNetworkObjects = new NetworkObject[] { playerObject };

        // Dapatkan scene lama dari objek pemain sebelum dipindahkan.
        string oldScene = playerObject.gameObject.scene.name;

        // --- LOGIKA BARU UNTUK MENANGANI EVENT SETELAH LOAD SELESAI ---

        // Definisikan Aksi yang akan dijalankan setelah scene selesai dimuat.
        System.Action<SceneLoadEndEventArgs> onLoadEndAction = null;
        onLoadEndAction = (args) =>
        {
            // 1. Langsung berhenti berlangganan event agar tidak berjalan lagi untuk scene lain.
            InstanceFinder.SceneManager.OnLoadEnd -= onLoadEndAction;

            // 2. Jalankan Coroutine untuk memindahkan pemain ke SpawnPoint.
            StartCoroutine(MovePlayerToSpawnPoint(playerObject, _targetSceneName));

            // 3. Setelah semua beres, baru lepas (unload) scene yang lama.
            SceneUnloadData sud = new SceneUnloadData(oldScene);
            InstanceFinder.SceneManager.UnloadConnectionScenes(sender, sud);
        };

        // Berlangganan (subscribe) ke event OnLoadEnd.
        InstanceFinder.SceneManager.OnLoadEnd += onLoadEndAction;

        // Jalankan proses load scene untuk koneksi yang bersangkutan.
        InstanceFinder.SceneManager.LoadConnectionScenes(sender, sld);
    }

    /// <summary>
    /// Coroutine yang berjalan di server untuk memindahkan pemain ke SpawnPoint.
    /// </summary>
    private IEnumerator MovePlayerToSpawnPoint(NetworkObject playerObject, string sceneName)
    {
        // Tunggu satu frame untuk memastikan scene sudah "tenang" dan semua objeknya aktif.
        yield return new WaitForEndOfFrame();

        if (playerObject == null)
            yield break;

        // Cari SpawnPoint di scene yang baru dimuat.
        GameObject spawnPoint = null;
        Scene targetScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
        if (targetScene.IsValid())
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(targetScene);
            foreach (GameObject rootObj in targetScene.GetRootGameObjects())
            {
                if (rootObj.name == "SpawnPoint")
                {
                    spawnPoint = rootObj;
                    break;
                }
            }
        }

        if (spawnPoint != null)
        {
            // Nonaktifkan CharacterController sementara jika ada, untuk menghindari konflik.
            CharacterController cc = playerObject.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Pindahkan posisi dan rotasi pemain.
            playerObject.transform.position = spawnPoint.transform.position;
            playerObject.transform.rotation = spawnPoint.transform.rotation;

            PlayerTeleporter teleporter = playerObject.GetComponent<PlayerTeleporter>();
            if (teleporter != null)
            {
                teleporter.RpcTeleport(playerObject.Owner, spawnPoint.transform.position, spawnPoint.transform.rotation);
            }

            // Aktifkan kembali CharacterController.
            if (cc != null) cc.enabled = true;

            Debug.Log($"[Server] Pemain {playerObject.Owner.ClientId} berhasil dipindahkan ke SpawnPoint di scene {sceneName}.");
        }
        else
        {
            Debug.LogWarning($"[Server] Peringatan: SpawnPoint tidak ditemukan di scene '{sceneName}'. Pemain tidak dipindahkan.");
        }
    }
}
