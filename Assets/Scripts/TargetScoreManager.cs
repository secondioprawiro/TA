using UnityEngine;
using TMPro;

public class TargetScoreManager : MonoBehaviour
{
    [Header("Referensi UI")]
    [Tooltip("Seret objek TextMeshPro untuk skor ke sini.")]
    public TextMeshProUGUI scoreText;

    [Header("Pengaturan Skor")]
    [Tooltip("Skor awal.")]
    private int hitCount = 0;
    public int hitsToSucceed = 4;
    private bool challengeCompleted = false;

    void Start()
    {
        UpdateScoreUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ThrowableBall"))
        {
            hitCount++;

            UpdateScoreUI();

            //Destroy(other.gameObject, 0.1f);
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Hit Score: " + hitCount + "/" + hitsToSucceed;

            if (hitCount >= hitsToSucceed)
            {
                scoreText.text += "\nAnda Berhasil!";
                challengeCompleted = true;
            }
        }
    }
}