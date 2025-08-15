using FishNet.Connection;
using FishNet.Object;
using Unity.Mathematics;
using UnityEngine;

public class CharacterSelection : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;  // Standard player prefab
    [SerializeField] private GameObject vrPlayerPrefab; // VR player prefab

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
        GameObject serverObject = GameObject.FindWithTag("Server"); // Ensure the GameObject has the "Server" tag
        if (serverObject != null)
        {
            ServerLogics logics = serverObject.GetComponent<ServerLogics>();
            if (logics != null)
            {

                logics.LogdeviceModel(deviceModel);

            }
            else
            {
                Debug.LogError("ServerLogics component not found on the GameObject with 'Server' tag!");
            }
        }
        Debug.Log($"Device Model: {deviceModel}. Spawning {(isVR ? "VR" : "standard")} character.");
        SpawnRequest(isVR ? 1 : 0, LocalConnection);  // Request to spawn the appropriate prefab
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

        // Ensure the spawn point exists
        if (SpawnPoint.instance == null)
        {
            Debug.LogError("Spawn point is not defined");
            return;
        }

        // Instantiate the player prefab at the designated spawn point
        GameObject playerInstance = Instantiate(
            prefabToSpawn,
            SpawnPoint.instance.transform.position,
            quaternion.identity
        );

        // Spawn the instantiated player on the network
        Spawn(playerInstance, conn);
        Debug.Log($"Spawned {prefabToSpawn.name} for connection {conn.ClientId}.");
    }
}
