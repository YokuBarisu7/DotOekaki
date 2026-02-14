using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HueSlider : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    RawImage hueRawImage; // HueスライダーのRawImage
    RectTransform hueRect; // HueスライダーのRectTransform
    Texture2D hueTexture;

    [SerializeField] Image hueCursorImage; // Hueスライダーのカーソル
    [SerializeField] RectTransform hueCursor; // Hueスライダーのカーソル
    [SerializeField] SVMap svMap; // SVマップ

    private float hue;

    void Start()
    {
        hueRawImage = GetComponent<RawImage>();
        hueRect = hueRawImage.rectTransform;

        CreateHueTexture();
        hueRawImage.texture = hueTexture;

        // 初期値を設定
        SetHue(0f);
    }

    private void OnDestroy()
    {
        if (hueTexture != null) Destroy(hueTexture);
    }

    // Hueスライダー用のTexture2Dを作成
    private void CreateHueTexture()
    {
        int width = 256;
        hueTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var px = new Color32[width];
        for (int x = 0; x < width; x++)
        {
            float h = (float)x / (width - 1);
            px[x] = (Color32)Color.HSVToRGB(h, 1f, 1f);
        }
        hueTexture.SetPixels32(px);
        hueTexture.Apply(false, false);
    }

    // クリック時の処理
    public void OnPointerClick(PointerEventData eventData) => UpdateHue(eventData);
    // ドラッグ時の処理
    public void OnDrag(PointerEventData eventData) => UpdateHue(eventData);

    // H値を更新する共通処理
    private void UpdateHue(PointerEventData eventData)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hueRect, eventData.position, eventData.pressEventCamera, out local);

        Rect rect = hueRect.rect;
        local.x = Mathf.Clamp(local.x, rect.xMin, rect.xMax);

        float h = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
        SetHue(h);

        // SVMapへ通知
        svMap.SetHue(hue);
    }

    private void SetHue(float h)
    {
        hue = Mathf.Repeat(h, 1f);

        Color c = Color.HSVToRGB(hue, 1f, 1f);
        hueCursorImage.color = c;

        // カーソルの位置更新
        Rect rect = hueRect.rect;
        float x = Mathf.Lerp(rect.xMin, rect.xMax, hue);
        hueCursor.anchoredPosition = new Vector2(x, hueCursor.anchoredPosition.y);
    }
}
