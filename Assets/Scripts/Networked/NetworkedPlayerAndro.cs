using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using FishNet.Connection;
using FishNet.Object;

public class NetworkedPlayerAndro : NetworkBehaviour
{

    [Header("Components to Disable")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    [SerializeField] private Camera clientCamera;
    [SerializeField] private AudioListener clientAudioListener;

    [SerializeField] private GameObject Joystick;
    [SerializeField] private GameObject UISystem;

    private string myPrediction = "";


    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            EnableLocalPlayerInput();
        }
        else
        {
            DisableOtherPlayersInput();
        }
    }

    public void SetMyPrediction(string choice)
    {
        // Hanya player yang memiliki kontrol (IsOwner) yang bisa mengatur prediksinya sendiri
        if (!base.IsOwner) return;

        myPrediction = choice;
        Debug.Log("Pilihan saya disimpan: " + myPrediction);
    }

    public void CheckMyResult(string actualWinner)
    {
        // Hanya player yang memiliki kontrol (IsOwner) yang perlu mengecek hasilnya.
        if (!base.IsOwner) return;

        if (myPrediction == actualWinner)
        {
            Debug.Log("Hasil: Prediksi SAYA BENAR!");
            // Nanti kita akan panggil fungsi di GameManager untuk menampilkan UI "Benar"
            GameManager.instance.ShowResultPanel(true, actualWinner, myPrediction);
        }
        else
        {
            Debug.Log("Hasil: Prediksi SAYA SALAH.");
            // Nanti kita akan panggil fungsi di GameManager untuk menampilkan UI "Salah"
            GameManager.instance.ShowResultPanel(false, actualWinner, myPrediction);
        }
    }

    public void EnableLocalPlayerInput()
    {
        if (clientCamera != null)
            clientCamera.enabled = true;

        if (clientAudioListener != null)
            clientAudioListener.enabled = true;

#if UNITY_IOS || (UNITY_ANDROID && !UNITY_EDITOR)
        Debug.Log("This code runs on Android!");
        if (Joystick != null)
            Joystick.SetActive(true);

        if (UISystem != null)
            UISystem.SetActive(true);
#elif UNITY_EDITOR
        Debug.Log("This code runs on Editor!");
        if (Joystick != null)
            Joystick.SetActive(false);

        if (UISystem != null)
            UISystem.SetActive(false);
#else
        Debug.Log("This code not runs on Server Or Windows!");
        if (Joystick != null)
            Joystick.SetActive(false);

        if (UISystem != null)
            UISystem.SetActive(false);
#endif

        if (scriptsToDisable != null)
        {
            foreach (var script in scriptsToDisable)
            {
                if (script != null)
                {
                    script.enabled = true;
                }
            }
        }

    }

    public void DisableOtherPlayersInput()
    {
        if (clientCamera != null)
            clientCamera.enabled = false;

        if (clientAudioListener != null)
            clientAudioListener.enabled = false;

        if (Joystick != null)
            Joystick.SetActive(false);

        if (UISystem != null)
            UISystem.SetActive(false);

        if (scriptsToDisable != null)
        {
            foreach (var scriptdisable in scriptsToDisable)
            {
                if (scriptdisable != null)
                {
                    scriptdisable.enabled = false;
                }
            }
        }

    }
}