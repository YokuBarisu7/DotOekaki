using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class DengonDrawingManager : MonoBehaviour
{
    public static DengonDrawingManager instance;

    [SerializeField] GameObject drawField;
    [SerializeField] RawImage drawingPanel;
    [SerializeField] int initialWidth;
    [SerializeField] int initialHeight;
    [SerializeField] Color drawColor;
    [SerializeField] int brushSize;

    public Texture2D texture;
    public int CanvasWidth { get; private set; }
    public int CanvasHeight { get; private set; }
    public Color DrawColor => drawColor;
    public int BrushSize => brushSize;
    public ToolMode currentMode { get; private set; } = ToolMode.Pen;
    public int undoStackCount { get { return undoStack.Count; } }
    public int redoStackCount { get { return redoStack.Count; } }
    public bool IsDrawable => isDrawable;
    public bool HasDrawingCached => hasDrawing;

    Color32[] clearBuffer;
    Stack<Color32[]> undoStack; // 元に戻すためのスタック
    Stack<Color32[]> redoStack; // やり直しのためのスタック
    Vector2Int? lastPoint = null; // 前回の描画位置
    Vector2Int? startPoint = null; // 直線モードの始点
    Vector2Int startPixel; // 円モード、長方形モードの始点
    bool isDrawing = false; // 描画中かどうか
    bool isDrawable = false; // 描画可能かどうか
    bool hasDrawing = false; // 何か描かれているかどうか
    const float BaseDrawCanvasSize = 900f;
    DrawingUtils drawer;

    public enum ToolMode
    {
        Pen,
        Fill,
        Line,
        Circle,
        Rectangle,
    }

    public event Action<Color> OnColorChanged;
    public event Action<int> OnBrushSizeChanged;
    public event Action<ToolMode> OnToolModeChanged;
    public event Action<int, int> OnFieldSizeChanged;
    public event Action<int, int> OnHistoryChanged;
    public event Action<bool> OnHasDrawingChanged;
    public event Action<bool> OnDrawableChanged;

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
        // スタックの初期生成
        undoStack = new Stack<Color32[]>();
        redoStack = new Stack<Color32[]>();
    }

    private void Start()
    {
        InitializeDrawField();
    }

    public void InitializeDrawField()
    {
        // Texture2Dを作成
        CreateTexture(initialWidth, initialHeight);
        SetDrawFieldSize(initialWidth, initialHeight);

        // ゲーム開始時は黒色ペンモード
        ChangeColor(Color.black);
        SetBrushSize(1);
        ChangeMode(ToolMode.Pen);
    }

    // テクスチャを作成
    private void CreateTexture(int width, int height)
    {
        if (texture != null)
        {
            Destroy(texture);
            texture = null;
        }

        CanvasWidth = width;
        CanvasHeight = height;
        texture = new Texture2D(CanvasWidth, CanvasHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        ClearCanvas();
        drawingPanel.texture = texture;
        undoStack.Clear();
        redoStack.Clear();
        undoStack.Push(texture.GetPixels32());
        drawer = new DrawingUtils(texture, drawColor, brushSize);
    }

    // 描画領域のサイズを設定
    private void SetDrawFieldSize(int width, int height)
    {
        var rectTransform = drawField.GetComponent<RectTransform>();
        float aspectRatio = (float)width / height;
        rectTransform.sizeDelta = aspectRatio > 1.0f ? new Vector2(BaseDrawCanvasSize, BaseDrawCanvasSize / aspectRatio) : new Vector2(BaseDrawCanvasSize * aspectRatio, BaseDrawCanvasSize);

        OnFieldSizeChanged?.Invoke(width, height);
    }

    public void ResetDrawFieldSize(int width, int height)
    {
        CreateTexture(width, height);
        SetDrawFieldSize(width, height);
    }

    private void Update()
    {
        if (!isDrawable || texture == null) return;
        if (!TryGetMouseLocalPoint(out var localPoint)) return;

        switch (currentMode)
        {
            case ToolMode.Pen:
                UpdatePen(localPoint);
                break;

            case ToolMode.Fill:
                if (Input.GetMouseButtonDown(0) && IsInsideCanvas(localPoint))
                {
                    FloodFill(localPoint);
                }
                break;

            case ToolMode.Line:
                UpdateLine(localPoint);
                break;

            case ToolMode.Circle:
            case ToolMode.Rectangle:
                UpdateShapeDrag(localPoint);
                break;
        }
    }

    private bool TryGetMouseLocalPoint(out Vector2 localPoint)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawingPanel.rectTransform,
            Input.mousePosition,
            null,
            out localPoint
            );
    }
    private bool TryGetPixelPos(Vector2 localPoint, out Vector2Int pixelPos)
    {
        pixelPos = default;

        Rect rect = drawingPanel.rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return false;

        int x = Mathf.FloorToInt((localPoint.x - rect.x) / rect.width * texture.width);
        int y = Mathf.FloorToInt((localPoint.y - rect.y) / rect.height * texture.height);

        // 範囲クランプ（外を触っても落ちないように）
        x = Mathf.Clamp(x, 0, texture.width - 1);
        y = Mathf.Clamp(y, 0, texture.height - 1);

        pixelPos = new Vector2Int(x, y);
        return true;
    }

    private bool IsInsideCanvas(Vector2 localPoint)
    {
        Rect rect = drawingPanel.rectTransform.rect;
        return localPoint.x >= rect.x && localPoint.x <= rect.x + rect.width
            && localPoint.y >= rect.y && localPoint.y <= rect.y + rect.height;
    }

    private void UpdatePen(Vector2 localPoint)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsInsideCanvas(localPoint)) isDrawing = true;
        }

        if (Input.GetMouseButton(0) && isDrawing)
        {
            DrawAtPoint(localPoint);
        }

        if (Input.GetMouseButtonUp(0))
        {
            lastPoint = null;
            if (isDrawing)
            {
                SaveUndo();
                isDrawing = false;
            }
        }
    }

    private void DrawAtPoint(Vector2 localPoint)
    {
        if (!IsInsideCanvas(localPoint)) return;
        if (!TryGetPixelPos(localPoint, out var pixelPos)) return;

        if (lastPoint.HasValue)
        {
            drawer.DrawLine(lastPoint.Value, pixelPos); // 2回目以降の描画
        }
        else
        {
            drawer.DrawPoint(pixelPos); // 最初の描画
        }

        lastPoint = pixelPos;
        texture.Apply();
    }

    private void FloodFill(Vector2 localPoint)
    {
        drawer.FloodFill(drawingPanel, localPoint);
        texture.Apply();
        SaveUndo();
    }

    private void UpdateLine(Vector2 localPoint)
    {
        if (Input.GetMouseButtonUp(0))
        {
            if (!IsInsideCanvas(localPoint)) return;
            if (!TryGetPixelPos(localPoint, out var pixelPos)) return;

            if (startPoint == null)
            {
                startPoint = pixelPos;
                return;
            }

            DrawShape(startPoint.Value, pixelPos);
            SaveUndo();
            startPoint = null;
        }
    }

    private void UpdateShapeDrag(Vector2 localPoint)
    {
        // 始点の設定
        if (Input.GetMouseButtonDown(0) && IsInsideCanvas(localPoint))
        {
            if (!TryGetPixelPos(localPoint, out var pixelPos)) return;
            startPixel = pixelPos;
            isDrawing = true;
        }

        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            if (!IsInsideCanvas(localPoint))
            {
                isDrawing = false;
                return;
            }
            if (!TryGetPixelPos(localPoint, out var endPixel))
            {
                isDrawing = false;
                return;
            }
            DrawShape(startPixel, endPixel);
            SaveUndo();
            isDrawing = false;
        }
    }

    private void DrawShape(Vector2Int start, Vector2Int end)
    {
        switch (currentMode)
        {
            case ToolMode.Line: drawer.DrawLine(start, end); break;
            case ToolMode.Circle: drawer.DrawCircle(start, end); break;
            case ToolMode.Rectangle: drawer.DrawRectangle(start, end); break;
        }
        texture.Apply();
    }

    private void SaveUndo()
    {
        undoStack.Push(texture.GetPixels32()); // 現在の状態を保存
        redoStack.Clear(); // 新しく描画したらRedo履歴はクリア
        NotifyHistory();
        SetHasDrawing(ComputeHasDrawing());
    }

    public void UndoButton() => Undo();
    public void RedoButton() => Redo();

    private void Undo()
    {
        if (undoStackCount > 1)
        {
            redoStack.Push(undoStack.Pop());
            texture.SetPixels32(undoStack.Peek());
            texture.Apply();
            NotifyHistory();
            SetHasDrawing(ComputeHasDrawing());
        }
    }
    private void Redo()
    {
        if (redoStackCount > 0)
        {
            undoStack.Push(redoStack.Pop());
            texture.SetPixels32(undoStack.Peek());
            texture.Apply();
            NotifyHistory();
            SetHasDrawing(ComputeHasDrawing());
        }
    }

    public void AllClear()
    {
        ClearCanvas();
        SaveUndo();
    }

    private void ClearCanvas()
    {
        if (texture == null) return;

        EnsureClearBuffer();
        texture.SetPixels32(clearBuffer);
        texture.Apply();
    }

    private void EnsureClearBuffer()
    {
        int len = texture.width * texture.height;
        if (clearBuffer == null || clearBuffer.Length != len)
        {
            clearBuffer = new Color32[len];
            var t = new Color32(0, 0, 0, 0);
            for (int i = 0; i < len; i++) clearBuffer[i] = t;
        }
    }

    private bool ComputeHasDrawing()
    {
        var pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
            if (pixels[i].a != 0) return true;
        return false;
    }

    private void SetHasDrawing(bool value)
    {
        if (hasDrawing == value) return;
        hasDrawing = value;
        OnHasDrawingChanged?.Invoke(hasDrawing);
    }

    public void SetDrawable(bool value)
    {
        if (isDrawable == value) return;
        isDrawable = value;
        OnDrawableChanged?.Invoke(isDrawable);
    }

    private void NotifyHistory()
    {
        OnHistoryChanged?.Invoke(undoStackCount, redoStackCount);
    }

    public void ChangeMode(ToolMode mode)
    {
        if (currentMode == mode) return;

        currentMode = mode;
        isDrawing = false;
        startPoint = null;
        lastPoint = null;
        startPixel = default;

        OnToolModeChanged?.Invoke(currentMode);
    }

    public void ChangeColor(Color color)
    {
        if (drawColor == color) return;

        drawColor = color;
        drawer = new DrawingUtils(texture, drawColor, brushSize);

        OnColorChanged?.Invoke(drawColor);
    }

    public void SetBrushSize(int size)
    {
        size = Mathf.Clamp(size, 1, 7);
        if (brushSize == size) return;

        brushSize = size;
        drawer = new DrawingUtils(texture, drawColor, brushSize);

        OnBrushSizeChanged?.Invoke(brushSize);
    }

    public byte[] GetPngBytes()
    {
        return texture.EncodeToPNG();
    }
}
