using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class DengonShowPanelManager : MonoBehaviourPun
{
    [SerializeField] GameObject showField;
    [SerializeField] RawImage showingPanel;

    [SerializeField] Text fromText;
    [SerializeField] Text toText;

    [SerializeField] RawImage gridPanel;
    [SerializeField] Toggle gridToggle;

    int cols;
    int rows;
    float thicknessPx = 0.5f;

    static readonly int RectSizeID = Shader.PropertyToID("_RectSize");
    static readonly int GridCountID = Shader.PropertyToID("_GridCount");
    static readonly int ThicknessID = Shader.PropertyToID("_GridThickness");
    static readonly int EnabledID = Shader.PropertyToID("_GridEnabled");

    Material runtimeMat;


    private void Start()
    {
        runtimeMat = Instantiate(gridPanel.material);
        gridPanel.material = runtimeMat;

        if (gridToggle != null)
        {
            gridToggle.isOn = false;
            gridToggle.onValueChanged.AddListener(OnToggle);
            OnToggle(false);
        }
        else
        {
            SetEnabled(false);
        }
        ApplyParams();
    }

    private void OnDestroy()
    {
        if (runtimeMat != null) Destroy(runtimeMat);
    }

    public void SetShowPanel(Texture texture)
    {
        showingPanel.texture = texture;
        SetShowFieldSize(texture.width, texture.height);
        SetGridCount(texture.width, texture.height);
    }

    public void SetFromText(string text)
    {
        fromText.text = text;
    }

    public void SetToText(string text)
    {
        toText.text = text;
    }

    private void SetShowFieldSize(int width, int height)
    {
        RectTransform rectTransform = showField.GetComponent<RectTransform>();
        float aspectRatio = (float)width / height;

        if (aspectRatio > 1)
        {
            rectTransform.sizeDelta = new Vector2(800, 800 / aspectRatio);
        }
        else
        {
            rectTransform.sizeDelta = new Vector2(800 * aspectRatio, 800);
        }
    }

    private void OnToggle(bool on)
    {
        SetEnabled(on);
        if (on)
        {
            ApplyParams();
        }
    }

    private void SetEnabled(bool on)
    {
        if (runtimeMat == null) return;
        runtimeMat.SetFloat(EnabledID, on ? 1f : 0f);
    }

    public void SetGridCount(int newCols, int newRows)
    {
        cols = Mathf.Max(1, newCols);
        rows = Mathf.Max(1, newRows);
        ApplyParams();
    }

    private void ApplyParams()
    {
        if (runtimeMat == null) return;

        cols = Mathf.Max(1, cols);
        rows = Mathf.Max(1, rows);

        RectTransform rt = gridPanel.rectTransform;
        Vector2 size = rt.rect.size;

        Canvas canvas = gridPanel.canvas;
        float sf = canvas != null ? canvas.scaleFactor : 1f;

        Vector2 rectPixels = size * sf;

        runtimeMat.SetVector(RectSizeID, new Vector4(rectPixels.x, rectPixels.y, 0, 0));
        runtimeMat.SetVector(GridCountID, new Vector4(cols, rows, 0, 0));
        runtimeMat.SetFloat(ThicknessID, thicknessPx = 0.5f);
    }
}
