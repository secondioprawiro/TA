using UnityEngine;
using FishNet.Object;

public class PredictionZone : NetworkBehaviour
{
    public string zoneChoice;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            var playerScript = other.GetComponent<NetworkedPlayerAndro>();
            if (playerScript != null)
            {
                playerScript.SetMyPrediction(zoneChoice);
            }
            ServerSignalReady();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerSignalReady()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.PlayerIsReady();
        }
    }
}