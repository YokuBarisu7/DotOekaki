using UnityEngine;
using UnityEngine.UI;

public class DengonGridGenerator : MonoBehaviour
{
    [SerializeField] RawImage gridPanel;
    [SerializeField] Toggle gridToggle;

    [Header("グリッドの細かさ(縦×横)")]
    [SerializeField] int cols;
    [SerializeField] int rows;

    [Header("グリッド線の太さ")]
    [SerializeField] float thicknessPx;

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

        DengonDrawingManager.instance.OnFieldSizeChanged += SetGridCount;
    }

    private void OnDestroy()
    {
        if (runtimeMat != null) Destroy(runtimeMat);
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

    // ドット数変更時に呼ばれる
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
        runtimeMat.SetFloat(ThicknessID, thicknessPx);
    }
}
