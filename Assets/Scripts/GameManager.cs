using FishNet.Object;
using UnityEngine.UI;
using FishNet;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    [Header("UI Elements")]
    public TextMeshProUGUI instructionText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    [Header("Challenge Settings")]
    public string correctWinnerName;

    [Header("Game Elements")]
    public GameObject predictionZoneKiri;
    public GameObject predictionZoneKanan;

    [HideInInspector]
    public NetworkedPlayerAndro localPlayerController;

    private readonly SyncVar<int> readyPlayerCount = new SyncVar<int>();


    void Awake()
    {
        // Cek jika instance sudah ada
        if (instance == null)
        {
            // Jika belum, jadikan script ini sebagai instance
            instance = this;
            Debug.Log("GameManager has awakened and set its instance.");
        }
        else
        {
            Debug.LogWarning("Another GameManager instance was found, destroying this one.");
            Destroy(gameObject);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!base.IsClientInitialized)
            return;

        // Cari semua object player yang ada di scene
        var players = FindObjectsOfType<NetworkedPlayerAndro>();
        foreach (var player in players)
        {
            if (player.IsOwner)
            {
                localPlayerController = player;
                Debug.Log("GameManager (Client) has successfully found and registered the local player: " + player.name);
                break;
            }
        }

        if (localPlayerController == null)
        {
            Debug.LogError("GameManager (Client) started, but could not find the local player object in the scene!");
        }
    }

    [Server]
    public void PlayerIsReady()
    {
        readyPlayerCount.Value++;

        if (readyPlayerCount.Value >= InstanceFinder.ServerManager.Clients.Count)
        {
            Debug.Log("Semua pemain sudah memilih. Memulai balapan!");
            ObserverStartRace();
        }
    }

    [Server]
    public void RaceIsOver()
    {
        ObserverEvaluateResults();
    }

    [ObserversRpc]
    private void ObserverEvaluateResults()
    {
        NetworkedPlayerAndro localPlayer = FindMyPlayer();
        if (localPlayer != null)
        {
            localPlayer.CheckMyResult(correctWinnerName);
        }
    }

    private NetworkedPlayerAndro FindMyPlayer() {
        return localPlayerController;
    }

    public void ShowResultPanel(bool wasCorrect, string winnerName, string playerPrediction)
    {
        resultPanel.SetActive(true);
        if (wasCorrect)
        {
            resultText.text = "Jawaban Anda BENAR!\n" +
                              "Pilihan anda " + playerPrediction;
                              
        }
        else
        {
            resultText.text = "Jawaban Anda SALAH.\n\n" +
                              "Pilihan Anda: " + playerPrediction;
        }

        StartCoroutine(HideResultPanelAfterDelay(10f));

    }

    [ObserversRpc]
    private void ObserverStartRace()
    {
        // Memulai Coroutine countdown untuk semua pemain
        StartCoroutine(StartRaceCountdown());
    }
    private IEnumerator StartRaceCountdown()
    {
        if (predictionZoneKiri != null) predictionZoneKiri.SetActive(false);
        if (predictionZoneKanan != null) predictionZoneKanan.SetActive(false);

        instructionText.gameObject.SetActive(true);
        instructionText.text = "3";
        yield return new WaitForSeconds(1f);

        instructionText.text = "2";
        yield return new WaitForSeconds(1f);

        instructionText.text = "1";
        yield return new WaitForSeconds(1f);

        instructionText.text = "MULAI!";

        NetworkedPlayerAndro localPlayer = FindMyPlayer();
        if (localPlayer != null)
        {
            var pusher = localPlayer.GetComponent<BasicRigidBodyPushNetworked>();
            if (pusher != null)
            {
                pusher.canPush = true;
                Debug.Log("Pusher enabled for player: " + localPlayer.name);
            }
        }

        yield return new WaitForSeconds(1.5f);
        instructionText.gameObject.SetActive(false);
    }

    private IEnumerator HideResultPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        resultPanel.SetActive(false);
    }
}