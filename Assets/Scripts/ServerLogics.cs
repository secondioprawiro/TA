using UnityEngine;
using FishNet;
using FishNet.Object;
using FishNet.Connection;
using System.Collections;
using System.Collections.Generic;
using FishNet.Transporting;

public class ServerLogics : NetworkBehaviour
{
    // Singleton telah dihapus.

    [Header("Player Prefabs")]
    [SerializeField]
    private List<GameObject> playerPrefabs = new List<GameObject>();

    // --- PING SYSTEM ---
    [Header("Ping System")]
    [Tooltip("Berapa lama (dalam detik) server akan menunggu sebelum men-disconnect client yang tidak aktif.")]
    [SerializeField] private float _clientTimeout = 20f;
    private Dictionary<NetworkConnection, float> _lastPingTimes = new Dictionary<NetworkConnection, float>();
    private Coroutine _checkPingsCoroutine;

    private void Awake()
    {
        // Kode Singleton telah dihapus.
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _checkPingsCoroutine = StartCoroutine(CheckClientPings());
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (_checkPingsCoroutine != null)
        {
            StopCoroutine(_checkPingsCoroutine);
            _checkPingsCoroutine = null;
        }
    }

    private void OnEnable()
    {
        if (base.IsServerInitialized)
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }
    }

    private void OnDisable()
    {
        if (base.IsServerInitialized)
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }
    }

    private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            _lastPingTimes[connection] = Time.time;
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            _lastPingTimes.Remove(connection);
            Debug.Log($"Client {connection.ClientId} telah disconnect. Mencari objek untuk dihapus...");
            foreach (NetworkObject nob in new List<NetworkObject>(connection.Objects))
            {
                if (nob != null)
                {
                    Despawn(nob);
                }
            }
        }
    }

    // Fungsi ini sekarang akan dipanggil oleh RPC dari skrip lain.
    public void ReceivePing(NetworkConnection sender)
    {
        if (sender != null)
        {
            _lastPingTimes[sender] = Time.time;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void LogdeviceModel(string deviceModel)
    {
        Debug.Log($"Device Model: {deviceModel}");
    }

    private IEnumerator CheckClientPings()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            List<NetworkConnection> clientsToDisconnect = new List<NetworkConnection>();
            foreach (var entry in _lastPingTimes)
            {
                if (Time.time > entry.Value + _clientTimeout)
                {
                    clientsToDisconnect.Add(entry.Key);
                }
            }

            foreach (var client in clientsToDisconnect)
            {
                Debug.LogWarning($"Client {client.ClientId} timed out. Disconnecting.");
                client.Disconnect(true);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnRequestServerRpc(int spawnIndex, NetworkConnection conn, GameObject playerInstances)
    {
        NetworkConnection connectionsave = conn;
        DespawnRequestServerRpc(playerInstances);
        // Ensure spawn index is valid
        if (spawnIndex < 0 || spawnIndex >= playerPrefabs.Count)
        {
            Debug.LogError("Invalid spawn index");
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
            playerPrefabs[spawnIndex],
            SpawnPoint.instance.transform.position,
            Quaternion.identity
        );

        // Spawn the instantiated player on the network
        Spawn(playerInstance, connectionsave);
        // Transfer ownership of the targetObject to the newOwner

    }


    public void DespawnRequestServerRpc(GameObject playerInstances)
    {
        // Transfer ownership of the targetObject to the newOwner
        Despawn(playerInstances.gameObject, DespawnType.Destroy);
    }
}
