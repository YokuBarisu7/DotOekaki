using UnityEngine;
using UnityEngine.UI;
using static CooperateDrawingManager;

public class CooperatePreviewGenerator : MonoBehaviour
{
    [SerializeField] RawImage previewPanel;
    [SerializeField] int initialPreviewWidth;
    [SerializeField] int initialPreviewHeight;

    Texture2D previewTexture;
    Vector2Int? startPoint = null; // 直線モードの始点
    Vector2Int startPixel; // 円モード、長方形モードの始点
    bool isDrawing = false; // 描画中かどうか
    Color previewColor;
    Color32[] clearBuffer;
    ToolMode currentPreviewMode;
    int previewBrushSize;
    DrawingUtils drawer;

    private void Start()
    {
        previewColor = Color.black;
        previewBrushSize = 1;
        currentPreviewMode = ToolMode.Pen;

        CreatePreviewTexture(initialPreviewWidth, initialPreviewHeight);

        CooperateDrawingManager.instance.OnColorChanged += HandleColorChanged;
        CooperateDrawingManager.instance.OnBrushSizeChanged += HandleBrushSizeChanged;
        CooperateDrawingManager.instance.OnToolModeChanged += HandleToolModeChanged;
        CooperateDrawingManager.instance.OnFieldSizeChanged += HandleFieldResized;
    }

    private void CreatePreviewTexture(int width, int height)
    {
        if (previewTexture != null)
        {
            Destroy(previewTexture);
            previewTexture = null;
        }

        previewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        previewTexture.filterMode = FilterMode.Point;
        ClearCanvas();
        previewPanel.texture = previewTexture;
        drawer = new DrawingUtils(previewTexture, previewColor, previewBrushSize);
    }

    private void Update()
    {
        if (previewTexture == null || drawer == null) return;

        if (!TryGetMouseLocalPoint(out var localPoint)) return;

        switch (currentPreviewMode)
        {
            case ToolMode.Line:
                UpdateLine(localPoint);
                break;
            case ToolMode.Circle:
            case ToolMode.Rectangle:
                UpdateShapeDrag(localPoint);
                break;
            default:
                if (isDrawing) CancelPreview();
                break;
        }
    }

    private bool TryGetMouseLocalPoint(out Vector2 localPoint)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            previewPanel.rectTransform,
            Input.mousePosition,
            null,
            out localPoint
            );
    }

    private bool TryGetPixelPos(Vector2 localPoint, out Vector2Int pixelPos)
    {
        pixelPos = default;
        if (!IsInsideCanvas(localPoint)) return false;

        Rect rect = previewPanel.rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return false;

        int x = Mathf.FloorToInt((localPoint.x - rect.x) / rect.width * previewTexture.width);
        int y = Mathf.FloorToInt((localPoint.y - rect.y) / rect.height * previewTexture.height);

        // 範囲クランプ（外を触っても落ちないように）
        x = Mathf.Clamp(x, 0, previewTexture.width - 1);
        y = Mathf.Clamp(y, 0, previewTexture.height - 1);

        pixelPos = new Vector2Int(x, y);
        return true;
    }

    private void UpdateLine(Vector2 localPoint)
    {
        if (Input.GetMouseButtonUp(0))
        {
            if (!IsInsideCanvas(localPoint)) return;
            if (!TryGetPixelPos(localPoint, out var pixelPos)) return;

            if (!isDrawing)
            {
                startPoint = pixelPos;
                isDrawing = true;
                return;
            }

            CancelPreview();
        }

        if (isDrawing && startPoint.HasValue)
        {
            if (!TryGetPixelPos(localPoint, out var endPoint)) return;
            DrawShape(startPoint.Value, endPoint);
        }
    }

    private void UpdateShapeDrag(Vector2 localPoint)
    {
        if (Input.GetMouseButtonDown(0) && IsInsideCanvas(localPoint))
        {
            // 始点を設定
            if (!TryGetPixelPos(localPoint, out var pixelPos)) return;
            startPixel = pixelPos;
            isDrawing = true;
        }

        if (Input.GetMouseButton(0) && isDrawing)
        {
            if (!TryGetPixelPos(localPoint, out var endPixel)) return;
            DrawShape(startPixel, endPixel);
        }

        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            ClearCanvas();
            isDrawing = false;
        }
    }

    private void DrawShape(Vector2Int start, Vector2Int end)
    {
        ClearCanvas();

        switch (currentPreviewMode)
        {
            case ToolMode.Line: drawer.DrawLine(start, end); break;
            case ToolMode.Circle: drawer.DrawCircle(start, end); break;
            case ToolMode.Rectangle: drawer.DrawRectangle(start, end); break;
        }

        previewTexture.Apply();
    }

    private void EnsureClearBuffer()
    {
        int len = previewTexture.width * previewTexture.height;
        if (clearBuffer == null || clearBuffer.Length != len)
        {
            clearBuffer = new Color32[len];
            var t = new Color32(0, 0, 0, 0);
            for (int i = 0; i < len; i++) clearBuffer[i] = t;
        }
    }

    private void ClearCanvas()
    {
        if (previewTexture == null) return;

        EnsureClearBuffer();
        previewTexture.SetPixels32(clearBuffer);
        previewTexture.Apply();
    }

    private void CancelPreview()
    {
        isDrawing = false;
        startPoint = null;
        ClearCanvas();
    }

    private bool IsInsideCanvas(Vector2 localPoint)
    {
        Rect rect = previewPanel.rectTransform.rect;
        return localPoint.x >= rect.x && localPoint.x <= rect.x + rect.width
            && localPoint.y >= rect.y && localPoint.y <= rect.y + rect.height;
    }

    private void HandleColorChanged(Color c)
    {
        c.a = 1;
        previewColor = c;
        if (previewTexture != null)
            drawer = new DrawingUtils(previewTexture, previewColor, previewBrushSize);
    }

    private void HandleBrushSizeChanged(int size)
    {
        previewBrushSize = size;
        if (previewTexture != null)
            drawer = new DrawingUtils(previewTexture, previewColor, previewBrushSize);
    }

    private void HandleToolModeChanged(ToolMode mode)
    {
        currentPreviewMode = mode;

        // 途中状態は全部キャンセル（モード跨ぎバグ対策）
        CancelPreview();
    }

    private void HandleFieldResized(int width, int height)
    {
        CreatePreviewTexture(width, height);
        CancelPreview();
    }
}
