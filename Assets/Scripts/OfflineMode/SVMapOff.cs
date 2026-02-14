using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SVMapOff : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] RectTransform svCursor; // SV選択カーソル
    [SerializeField] Image svCursorImage;  // 選択した色の表示

    [SerializeField] int texSize = 256;

    RawImage svRawImage;
    Texture2D spectrumTexture;
    RectTransform svRect;
    Color32[] pixels;

    float hue;
    public float s;
    public float v;

    void Start()
    {
        svRawImage = GetComponent<RawImage>();
        svRect = svRawImage.rectTransform;

        // カラースペクトラム用のTexture2Dを作成
        spectrumTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        pixels = new Color32[texSize * texSize];

        svRawImage.texture = spectrumTexture;

        // 初期値の設定
        SetHue(0f);
        SetSV(1f, 1f, true);
    }

    private void OnDestroy()
    {
        if (spectrumTexture != null ) Destroy(spectrumTexture);
    }

    // HueSliderから呼ぶ
    public void SetHue(float newHue)
    {
        hue = Mathf.Repeat(newHue, 1f);
        RedrawSpectrum();
        UpdateCursorColor();
    }

    private void SetSV(float newS, float newV, bool moveCursor)
    {
        s = Mathf.Clamp01(newS);
        v = Mathf.Clamp01(newV);
        UpdateCursorColor();

        if (moveCursor)
        {
            Rect rect = svRect.rect;
            float x = Mathf.Lerp(rect.xMin, rect.xMax, s);
            float y = Mathf.Lerp(rect.yMin, rect.yMax, v);
            svCursor.anchoredPosition = new Vector2(x, y);
        }
    }

    // カラースペクトラムをクリックした時に色を選択
    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateSVByPointer(eventData);
        DrawingManagerOff.instance?.ChangeColor(GetSelectedColor());
    }

    // カラースペクトラムをドラッグした時に色を選択
    public void OnDrag(PointerEventData eventData)
    {
        UpdateSVByPointer(eventData);
        DrawingManagerOff.instance?.ChangeColor(GetSelectedColor());
    }

    private void UpdateSVByPointer(PointerEventData e)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            svRect, e.position, e.pressEventCamera, out localPoint);

        Rect rect = svRect.rect;
        localPoint.x = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax);
        localPoint.y = Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax);

        s = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        v = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

        svCursor.anchoredPosition = localPoint;
        UpdateCursorColor();
    }

    Color GetSelectedColor()
    {
        return Color.HSVToRGB(hue, s, v);
    }

    // カーソルの色を更新
    private void UpdateCursorColor()
    {
        svCursorImage.color = GetSelectedColor();
    }

    private void RedrawSpectrum()
    {
        // y=0 を暗い側にするか明るい側にするかは好み。
        // ここでは v = y/(N-1) にして "上が明るい" になるようにするなら v を反転してもOK。
        int w = texSize;
        int h = texSize;

        for (int y = 0; y < h; y++)
        {
            float vv = (float)y / (h - 1);
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                float ss = (float)x / (w - 1);
                Color c = Color.HSVToRGB(hue, ss, vv);
                pixels[row + x] = (Color32)c;
            }
        }

        spectrumTexture.SetPixels32(pixels);
        spectrumTexture.Apply(false, false);
    }
}
