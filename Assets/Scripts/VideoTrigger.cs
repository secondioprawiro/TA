using UnityEngine;
using UnityEngine.Video;

public class VideoTrigger : MonoBehaviour
{
    [Tooltip("Seret objek yang memiliki komponen Video Player ke sini (misal: papan tulis).")]
    public VideoPlayer videoPlayer;

    private void OnTriggerEnter(Collider other)
    {
        // Cek apakah yang masuk ke dalam trigger adalah objek dengan Tag "Player"
        if (other.CompareTag("Player"))
        {
            // Jika video player ada dan belum berputar, putar videonya.
            if (videoPlayer != null && !videoPlayer.isPlaying)
            {
                Debug.Log("Player entered trigger, playing video.");
                videoPlayer.Play();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Cek apakah yang keluar dari trigger adalah objek dengan Tag "Player"
        if (other.CompareTag("Player"))
        {
            // Jika video player ada dan sedang berputar, hentikan videonya.
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                Debug.Log("Player exited trigger, stopping video.");
                videoPlayer.Stop();
            }
        }
    }
}