using UnityEngine;
using UnityEngine.UI;

public class ImageView : MonoBehaviour
{
    [SerializeField] RawImage rawImage;
    [SerializeField] Text answerText;
    [SerializeField] Text playerNameText;
    [SerializeField] Image marubatsu;

    [SerializeField] Sprite maruSprite;
    [SerializeField] Sprite batsuSprite;

    public void Set(Texture texture)
    {
        rawImage.texture = texture;
        marubatsu.gameObject.SetActive(false);
    }

    public void SetAnswerText(string answer)
    {
        answerText.text = answer;
    }

    public void SetPlayerText(string playerName)
    {
        playerNameText.text = playerName;
    }

    public void SetMaruBatsu(bool isMaru)
    {
        marubatsu.gameObject.SetActive(true);
        marubatsu.sprite = isMaru ? maruSprite : batsuSprite;
    }
}
