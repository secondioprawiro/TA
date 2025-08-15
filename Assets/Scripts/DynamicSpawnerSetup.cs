using UnityEngine;
using FishNet;
using FishNet.Managing;
using FishNet.Component.Spawning;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Transporting;

public class DynamicSpawnerSetup : MonoBehaviour
{
    private PlayerSpawner _playerSpawner;

    private void Awake()
    {
        // Dapatkan referensi ke PlayerSpawner saat game dimulai.
        _playerSpawner = GetComponent<PlayerSpawner>();
        if (_playerSpawner == null)
        {
            Debug.LogError("PlayerSpawner tidak ditemukan di objek ini! Script tidak akan berjalan.", this);
            return;
        }

        // Berlangganan ke event yang terjadi di server dan klien.
        InstanceFinder.ServerManager.OnServerConnectionState += OnServerConnectionState;
        InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;
    }

    private void OnDestroy()
    {
        // Penting untuk berhenti berlangganan saat objek dihancurkan.
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    /// <summary>
    /// Dipanggil setiap kali status koneksi server berubah.
    /// </summary>
    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        // Kita hanya peduli saat server sudah benar-benar dimulai.
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            UpdateSpawnPoints();
        }
    }

    /// <summary>
    /// Dipanggil setiap kali status koneksi klien berubah.
    /// </summary>
    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        // Kita hanya peduli saat klien sudah benar-benar dimulai.
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            UpdateSpawnPoints();
        }
    }

    /// <summary>
    /// Fungsi inti untuk mencari dan mendaftarkan SpawnPoint yang aktif saat ini.
    /// </summary>
    private void UpdateSpawnPoints()
    {
        // 1. Cari SEMUA GameObject di scene yang memiliki tag "SpawnPoint".
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");

        if (spawnPointObjects.Length == 0)
        {
            Debug.LogWarning("Tidak ada GameObject dengan tag 'SpawnPoint' yang ditemukan di scene saat ini. Mengosongkan daftar spawn.");
            // Buat array kosong untuk menggantikan yang lama.
            _playerSpawner.Spawns = new Transform[0];
            return;
        }

        // 2. Buat array Transform baru dengan ukuran yang sesuai.
        Transform[] newSpawns = new Transform[spawnPointObjects.Length];

        // 3. Isi array baru dengan transform dari setiap spawn point yang ditemukan.
        for (int i = 0; i < spawnPointObjects.Length; i++)
        {
            newSpawns[i] = spawnPointObjects[i].transform;
        }

        // 4. Ganti seluruh daftar spawn yang lama dengan yang baru.
        _playerSpawner.Spawns = newSpawns;
        Debug.Log($"PlayerSpawner telah diperbarui dengan {newSpawns.Length} spawn point baru.");
    }
}
