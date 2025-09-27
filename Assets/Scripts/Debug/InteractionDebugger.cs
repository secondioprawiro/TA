using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRDirectInteractor))]
[RequireComponent(typeof(Collider))]
public class InteractorDebugger : MonoBehaviour
{
    XRDirectInteractor _interactor;

    void Awake()
    {
        _interactor = GetComponent<XRDirectInteractor>();

        // Pastikan collider adalah trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // Agar OnTriggerEnter/Stay/Exit muncul di Console, wajib ada Rigidbody di salah satu pihak
        if (!TryGetComponent<Rigidbody>(out var rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;     // jangan mengganggu tracking tangan
            rb.useGravity = false;
        }

        // Daftarkan event XRI
        _interactor.hoverEntered.AddListener(LogHoverEnter);
        _interactor.hoverExited.AddListener(LogHoverExit);
        _interactor.selectEntered.AddListener(LogSelectEnter);
        _interactor.selectExited.AddListener(LogSelectExit);
    }

    public void LogHoverEnter(HoverEnterEventArgs args)
        => Debug.Log($"[XRI] HOVER ENTER: {args.interactableObject.transform.name}");

    public void LogHoverExit(HoverExitEventArgs args)
        => Debug.Log($"[XRI] HOVER EXIT: {args.interactableObject.transform.name}");

    public void LogSelectEnter(SelectEnterEventArgs args)
        => Debug.Log($"[XRI] SELECT ENTER: {args.interactableObject.transform.name}");

    public void LogSelectExit(SelectExitEventArgs args)
        => Debug.Log($"[XRI] SELECT EXIT: {args.interactableObject.transform.name}");

    void OnTriggerEnter(Collider other) => Debug.Log($"[TRIGGER] Enter {other.name}");
    void OnTriggerExit(Collider other) => Debug.Log($"[TRIGGER] Exit {other.name}");
}
