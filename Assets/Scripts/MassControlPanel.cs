using UnityEngine;
using UnityEngine.UI;

public class MassControlPanel : MonoBehaviour
{
    public static MassControlPanel Instance { get; private set; }

    [Header("Referensi Tombol")]
    public Button increaseButton;
    public Button decreaseButton;

    private InteractiveRigidbody _targetRigidbody;

    private void Awake()
    {
        // BARU: Logika Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Hancurkan duplikat jika ada
            return;
        }
        Instance = this;

        // Listener tombol tetap sama
        if (increaseButton != null) increaseButton.onClick.AddListener(OnIncreaseMass);
        if (decreaseButton != null) decreaseButton.onClick.AddListener(OnDecreaseMass);

        // Sembunyikan panel secara default
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (gameObject.activeInHierarchy && _targetRigidbody != null)
        {
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) OnIncreaseMass();
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) OnDecreaseMass();
        }
    }

    public void ShowPanel(InteractiveRigidbody target)
    {
        _targetRigidbody = target;
        gameObject.SetActive(true);
    }

    public void HidePanel()
    {
        _targetRigidbody = null;
        gameObject.SetActive(false);
    }

    private void OnIncreaseMass()
    {
        if (_targetRigidbody != null)
        {
            _targetRigidbody.AdjustMass(true);
        }
    }

    private void OnDecreaseMass()
    {
        if (_targetRigidbody != null)
        {
            _targetRigidbody.AdjustMass(false);
        }
    }
}

