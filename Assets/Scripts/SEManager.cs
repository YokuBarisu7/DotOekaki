using UnityEngine;

public class SEManager : MonoBehaviour
{
    public static SEManager instance;

    AudioSource audioSource;
    [SerializeField] AudioClip buttonClickSE;
    [SerializeField] AudioClip cancelButtonClickSE;
    [SerializeField] AudioClip lightSE;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayButtonClickSE() => audioSource.PlayOneShot(buttonClickSE);
    public void PlayCancelButtonClickSE() => audioSource.PlayOneShot(cancelButtonClickSE);
    public void PlayLightSE() => audioSource.PlayOneShot(lightSE);
}
