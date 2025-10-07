using UnityEngine;
using FishNet.Object;

public class FinishLine : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized) return;

        if (other.CompareTag("PushableCube")) // Pastikan Tag kubus Anda adalah "PushableCube"
        {
            Debug.Log("The winner is: " + other.name);
            GameManager.instance.RaceIsOver();
            gameObject.SetActive(false);
        }
    }
}