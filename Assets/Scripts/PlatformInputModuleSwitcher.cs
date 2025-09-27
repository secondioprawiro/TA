using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit.UI;
using SM = UnityEngine.SceneManagement;

[RequireComponent(typeof(EventSystem))]
public class PlatformInputModuleSwitcher : MonoBehaviour
{
    [Header("Modules on this EventSystem")]
    public InputSystemUIInputModule standardInputModule;
    public XRUIInputModule vrInputModule;               

    void Awake()
    {
        if (!standardInputModule) standardInputModule = GetComponent<InputSystemUIInputModule>();
        if (!vrInputModule) vrInputModule = GetComponent<XRUIInputModule>();

        if (!standardInputModule && !vrInputModule)
        {
            Debug.LogError("[ES Switcher] Tidak menemukan modul UI pada EventSystem.");
            return;
        }

        DontDestroyOnLoad(gameObject);   
        Apply(IsXRActive());             
        StartCoroutine(WaitXRThenApply());  
    }

    void OnEnable()
    {
        SM.SceneManager.activeSceneChanged += (_, __) => Apply(IsXRActive());
        SM.SceneManager.sceneLoaded += (_, __) => Apply(IsXRActive());
    }

    void OnDisable()
    {
        SM.SceneManager.activeSceneChanged -= (_, __) => Apply(IsXRActive());
        SM.SceneManager.sceneLoaded -= (_, __) => Apply(IsXRActive());
    }

    IEnumerator WaitXRThenApply()
    {
        for (int i = 0; i < 20; i++)
        {
            Apply(IsXRActive());
            yield return null;
        }

        float t = 0f;
        while (t < 3f)
        {
            Apply(IsXRActive());
            yield return new WaitForSeconds(0.25f);
            t += 0.25f;
        }
    }

    bool IsXRActive()
    {
        var mgr = XRGeneralSettings.Instance ? XRGeneralSettings.Instance.Manager : null;
        bool initialized = mgr && mgr.isInitializationComplete;
        return (initialized && XRSettings.isDeviceActive);
    }

    void Apply(bool xrMode)
    {
        if (vrInputModule) vrInputModule.enabled = xrMode;      
        if (standardInputModule) standardInputModule.enabled = !xrMode;     
    }
}
