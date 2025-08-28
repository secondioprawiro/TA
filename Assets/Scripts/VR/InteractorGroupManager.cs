using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit; // Namespace ini penting untuk mengakses XR Interaction Manager

public class InteractorGroupManager : MonoBehaviour
{
    [Tooltip("Seret GameObject anak 'Direct Interactor' ke sini.")]
    [SerializeField]
    private GameObject directInteractorObject;

    [Tooltip("Seret GameObject anak 'Ray Interactor' ke sini.")]
    [SerializeField]
    private GameObject rayInteractorObject;

    // Start sekarang akan memanggil Coroutine
    void Start()
    {
        // Memulai Coroutine untuk mendaftarkan lalu mengaktifkan interactor.   
        StartCoroutine(RegisterThenActivateInteractors());
    }

    // Coroutine ini akan mendaftarkan komponen saat GameObject masih nonaktif.
    private IEnumerator RegisterThenActivateInteractors()
    {
        // Tunggu satu frame untuk memastikan semua komponen lain sudah siap.
        yield return null;

        // --- PERBAIKAN BERDASARKAN LOG ERROR ---
        // Cari XR Interaction Manager di scene.
        XRInteractionManager interactionManager = FindObjectOfType<XRInteractionManager>();

        if (interactionManager == null)
        {
            Debug.LogError("GAGAL: Tidak dapat menemukan XRInteractionManager di scene!");
            yield break; // Hentikan coroutine jika tidak ada manager.
        }

        Debug.Log("XRInteractionManager ditemukan. Mendaftarkan interactor secara manual...");

        // Ambil komponen interactor dari GameObject anak yang MASIH NONAKTIF.
        IXRInteractor directInteractor = directInteractorObject?.GetComponent<IXRInteractor>();
        IXRInteractor rayInteractor = rayInteractorObject?.GetComponent<IXRInteractor>();

        // 1. DAFTARKAN DULU saat mereka masih nonaktif.
        if (directInteractor != null)
        {
            interactionManager.RegisterInteractor(directInteractor);
            Debug.Log("Direct Interactor berhasil didaftarkan.");
        }

        if (rayInteractor != null)
        {
            interactionManager.RegisterInteractor(rayInteractor);
            Debug.Log("Ray Interactor berhasil didaftarkan.");
        }

        // 2. BARU AKTIFKAN GameObject-nya setelah berhasil terdaftar.
        if (directInteractorObject != null)
        {
            directInteractorObject.SetActive(true);
        }

        if (rayInteractorObject != null)
        {
            rayInteractorObject.SetActive(true);
        }
    }
}
