using System.Collections;
using UnityEngine;
using FishNet.Object;
using SM = UnityEngine.SceneManagement;

public class InteractorSwitcher : NetworkBehaviour
{
    [SerializeField] GameObject rayInteractorObject;
    [SerializeField] GameObject directInteractorObject;
    [SerializeField] string sceneNameToUseDirectInteractor = "Scene2";

    void Awake() { Apply(); }
    public override void OnStartClient() { Apply(); StartCoroutine(ApplyForFrames(10)); }

    void OnEnable()
    {
        SM.SceneManager.sceneLoaded += (_, __) => Apply();
        SM.SceneManager.sceneUnloaded += _ => Apply();
        SM.SceneManager.activeSceneChanged += (_, __) => Apply();
    }
    void OnDisable()
    {
        SM.SceneManager.sceneLoaded -= (_, __) => Apply();
        SM.SceneManager.sceneUnloaded -= _ => Apply();
        SM.SceneManager.activeSceneChanged -= (_, __) => Apply();
    }

    bool IsTargetSceneLoaded()
    {
        var s = SM.SceneManager.GetSceneByName(sceneNameToUseDirectInteractor);
        return s.IsValid() && s.isLoaded;
    }

    IEnumerator ApplyForFrames(int n) { for (int i = 0; i < n; i++) { Apply(); yield return null; } }

    void Apply()
    {
        bool useDirect = IsTargetSceneLoaded();   // <<< kuncinya di sini
        if (rayInteractorObject) rayInteractorObject.SetActive(!useDirect);
        if (directInteractorObject) directInteractorObject.SetActive(useDirect);

        // safety
        if (directInteractorObject)
        {
            var col = directInteractorObject.GetComponent<Collider>();
            if (col) { col.enabled = useDirect; col.isTrigger = true; }
            if (useDirect && !directInteractorObject.TryGetComponent<Rigidbody>(out var rb))
            { rb = directInteractorObject.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false; }
        }

        // debug daftar scene yang sedang terload
        string loaded = "";
        for (int i = 0; i < SM.SceneManager.sceneCount; i++)
            loaded += SM.SceneManager.GetSceneAt(i).name + (i < SM.SceneManager.sceneCount - 1 ? ", " : "");
        Debug.Log($"[InteractorSwitcher] active={SM.SceneManager.GetActiveScene().name} | loaded=[{loaded}] | useDirect={useDirect} rayActive={rayInteractorObject?.activeSelf} directActive={directInteractorObject?.activeSelf}");
    }
}
