using UnityEngine;

public class TriggerDebug : MonoBehaviour
{
    // Fungsi ini akan mencatat APAPUN yang masuk ke trigger.
    private void OnTriggerEnter(Collider other)
    {
        // Gunakan LogError agar pesannya berwarna merah dan mudah terlihat.
        Debug.LogError("--- TRIGGER DIMASUKI ---");
        Debug.LogWarning("Objek yang masuk: " + other.gameObject.name);
        Debug.LogWarning("Tag dari objek tersebut: '" + other.gameObject.tag + "'");
        Debug.LogWarning("Layer dari objek tersebut: " + LayerMask.LayerToName(other.gameObject.layer));

        // Kita juga cek induknya untuk informasi tambahan
        if (other.transform.parent != null)
        {
            Debug.Log("Induk objek adalah: " + other.transform.parent.name +
                      " dengan tag: '" + other.transform.parent.tag + "'");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.LogError("--- TRIGGER DITINGGALKAN ---");
        Debug.LogWarning("Objek yang keluar: " + other.gameObject.name);
    }
}