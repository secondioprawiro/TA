using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using FishNet.Managing.Scened;
using FishNet.Connection;
using System.Collections.Generic;
using FishNet;

public class VR_SceneChangeTrigger : NetworkBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Nama scene yang akan dimuat. Harus ada di Build Settings.")]
    [SerializeField]
    private string _targetSceneName;

    [Header("Teleport Settings")]
    [Tooltip("Tag yang digunakan oleh spawn point di scene tujuan.")]
    [SerializeField]
    private string _spawnPointTag = "Respawn";

    [Header("UI Settings")]
    [Tooltip("Tag yang digunakan oleh Tombol UI untuk pindah scene.")]
    [SerializeField]
    private string _sceneChangeButtonTag = "SceneChangeButton";

    // --- Logika Koneksi ke Tombol UI (Sudah Benar) ---

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            SetupButtonListener();
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (base.IsOwner)
        {
            RemoveButtonListener();
        }
    }

    private void SetupButtonListener()
    {
        GameObject buttonObject = GameObject.FindWithTag(_sceneChangeButtonTag);
        if (buttonObject != null)
        {
            Button sceneChangeButton = buttonObject.GetComponent<Button>();
            if (sceneChangeButton != null)
            {
                sceneChangeButton.onClick.AddListener(RequestSceneChange);
            }
        }
    }

    private void RemoveButtonListener()
    {
        GameObject buttonObject = GameObject.FindWithTag(_sceneChangeButtonTag);
        if (buttonObject != null)
        {
            Button sceneChangeButton = buttonObject.GetComponent<Button>();
            if (sceneChangeButton != null)
            {
                sceneChangeButton.onClick.RemoveListener(RequestSceneChange);
            }
        }
    }

    public void RequestSceneChange()
    {
        if (!base.IsOwner) return;
        Debug.Log($"Client adalah Owner. Mengirim permintaan pindah scene ke '{_targetSceneName}'...");
        CmdChangeScene(_targetSceneName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdChangeScene(string sceneName, NetworkConnection sender = null)
    {
        if (string.IsNullOrEmpty(sceneName) || sender.FirstObject == null) return;

        // --- PERBAIKAN: Simpan nama scene lama sebelum pindah ---
        string oldSceneName = sender.FirstObject.gameObject.scene.name;

        SceneLoadData sld = new SceneLoadData(sceneName);
        sld.MovedNetworkObjects = new NetworkObject[sender.Objects.Count];
        sender.Objects.CopyTo(sld.MovedNetworkObjects, 0);

        System.Action<SceneLoadEndEventArgs> onLoadEndAction = null;
        onLoadEndAction = (args) =>
        {
            InstanceFinder.SceneManager.OnLoadEnd -= onLoadEndAction;

            GameObject spawnPoint = GameObject.FindWithTag(_spawnPointTag);
            if (spawnPoint == null)
            {
                Debug.LogError($"[Server] Tidak dapat menemukan GameObject dengan tag '{_spawnPointTag}' di scene yang baru.");
                return;
            }

            foreach (NetworkObject nob in sender.Objects)
            {
                if (nob.TryGetComponent(out PlayerTeleporter teleporter))
                {
                    Debug.Log($"[Server] Menteleportasi pemain milik ClientId {nob.Owner.ClientId}.");
                    teleporter.RpcTeleport(nob.Owner, spawnPoint.transform.position, spawnPoint.transform.rotation);
                }
            }

            // --- PERBAIKAN: Setelah teleportasi selesai, unload scene yang lama ---
            Debug.Log($"[Server] Melepaskan (unloading) scene '{oldSceneName}' untuk ClientId {sender.ClientId}.");
            SceneUnloadData sud = new SceneUnloadData(oldSceneName);
            InstanceFinder.SceneManager.UnloadConnectionScenes(sender, sud);
        };

        InstanceFinder.SceneManager.OnLoadEnd += onLoadEndAction;

        InstanceFinder.SceneManager.LoadConnectionScenes(sender, sld);
    }
}
