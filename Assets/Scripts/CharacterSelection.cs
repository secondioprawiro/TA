using FishNet.Connection;
using FishNet.Object;
using Unity.Mathematics;
using UnityEngine;
using FishNet.Component.Spawning;

public class CharacterSelection : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;    // Standard player prefab
    [SerializeField] private GameObject vrPlayerPrefab;  // VR player prefab

    // Variabel untuk menyimpan referensi ke PlayerSpawner agar tidak perlu dicari berulang kali.
    private PlayerSpawner _playerSpawner;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Automatically spawn character if the client is the owner
        if (IsOwner)
        {
            SpawnCharacter();
        }
    }

    // Automatically spawn a character based on device type
    private void SpawnCharacter()
    {
        string deviceModel = SystemInfo.deviceModel;
        bool isVR = deviceModel.Contains("Pico");        

        Debug.Log($"Device Model: {deviceModel}. Spawning {(isVR ? "VR" : "standard")} character.");
        // Kirim spawnIndex (0 untuk non-VR, 1 untuk VR) ke server.
        SpawnRequest(isVR ? 1 : 0, LocalConnection);
    }

    // Sends a request to the server to spawn a character
    [ServerRpc(RequireOwnership = false)]
    private void SpawnRequest(int spawnIndex, NetworkConnection conn)
    {
        GameObject prefabToSpawn = spawnIndex == 1 ? vrPlayerPrefab : playerPrefab;

        // Ensure the prefab is valid
        if (prefabToSpawn == null)
        {
            Debug.LogError($"Invalid spawn index: {spawnIndex}. Prefab is not assigned.");
            return;
        }

       
        if (_playerSpawner == null)
        {
            _playerSpawner = FindObjectOfType<PlayerSpawner>();
            if (_playerSpawner == null)
            {
                Debug.LogError("Tidak dapat menemukan PlayerSpawner di scene!");
                return;
            }
        }
       
        if (_playerSpawner.Spawns == null || _playerSpawner.Spawns.Length == 0)
        {
            Debug.LogError("Daftar 'Spawns' di PlayerSpawner kosong!");
            return;
        }
       
        if (spawnIndex < 0 || spawnIndex >= _playerSpawner.Spawns.Length)
        {
            Debug.LogError($"Spawn index {spawnIndex} di luar jangkauan daftar 'Spawns'!");
            return;
        }
        
        Transform spawnPointTransform = _playerSpawner.Spawns[spawnIndex];

        if (spawnPointTransform == null)
        {
            Debug.LogError($"Spawn point pada index {spawnIndex} adalah null!");
            return;
        }
        
        GameObject playerInstance = Instantiate(
            prefabToSpawn,
            spawnPointTransform.position, 
            spawnPointTransform.rotation  
        );

        Spawn(playerInstance, conn);
        Debug.Log($"Spawned {prefabToSpawn.name} for connection {conn.ClientId} at {spawnPointTransform.name}.");
    }
}
