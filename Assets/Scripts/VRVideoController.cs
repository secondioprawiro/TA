using UnityEngine;
using UnityEngine.Video;
// Kita tidak butuh 'using UnityEngine.UI;' lagi untuk sementara

public class VRVideoController : MonoBehaviour
{
    [Tooltip("Seret komponen Video Player yang ingin dikontrol ke sini.")]
    public VideoPlayer videoPlayer;

    // Tidak ada lagi fungsi Update, slider, atau scrubbing.

    /// <summary>
    /// Fungsi ini hanya akan Play jika video berhenti, dan Stop jika video berjalan.
    /// </summary>
    public void TogglePlayStop()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("Video Player belum di-assign di Inspector!");
            return;
        }

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
            Debug.Log("Perintah STOP dikirim ke Video Player.");
        }
        else
        {
            videoPlayer.Play();
            Debug.Log("Perintah PLAY dikirim ke Video Player.");
        }
    }
}