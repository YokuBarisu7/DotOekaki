using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.IO;

public class DrawingManagerOff : MonoBehaviour
{
    public static DrawingManagerOff instance;

    [Header("Drawing")]
    [SerializeField] GameObject drawField;
    [SerializeField] RawImage drawingPanel;
    [SerializeField] int initialWidth;
    [SerializeField] int initialHeight;
    [SerializeField] int brushSize;

    [Header("Export")]
    [SerializeField] private string exportDefaultName = "oekaki_dot";
    [SerializeField, Range(1, 100)] private int jpgQuality = 90;
    [SerializeField, Range(1, 64)] private int exportScale = 10;

    public int CanvasWidth { get; private set; }
    public int CanvasHeight { get; private set; }
    public Color DrawColor => drawColor;
    public int BrushSize => brushSize;
    public ToolMode currentMode { get; private set; } = ToolMode.Pen;
    public int undoStackCount { get { return undoStack.Count; } }
    public int redoStackCount { get { return redoStack.Count; } }

    Texture2D texture;
    Color drawColor; // ペンの色
    Color32[] clearBuffer; // ClearCanvasで使用
    Stack<Color[]> undoStack; // 元に戻すためのスタック
    Stack<Color[]> redoStack; // やり直しのためのスタック
    Vector2Int? lastPoint; // 前回の描画位置
    Vector2Int? startPoint; // 直線モードの始点
    Vector2Int startPixel; // 円モード、長方形モードの始点
    bool isDrawing = false; // 描画中かどうか
    bool isDrawable = false; // 描画可能かどうか
    bool hasDrawing = false; // 何か描かれているかどうか
    const float BaseDrawCanvasSize = 900f;
    DrawingUtils drawer;

    public bool IsDrawable => isDrawable;
    public bool HasDrawingCached => hasDrawing;

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
        undoStack = new Stack<Color[]>();
        redoStack = new Stack<Color[]>();
    }

    private void Start()
    {
        // ゲーム開始時は黒色ペンモード
        drawColor = Color.black;
        brushSize = 1;
        currentMode = ToolMode.Pen;

        // Texture2Dを作成
        CreateTexture(initialWidth, initialHeight);
        SetDrawable(true);
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

    // マウス座標からおえかきパネル座標、テクスチャ座標に変換
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

    // カーソル位置がおえかきパネル上にあるかどうか
    private bool IsInsideCanvas(Vector2 localPoint)
    {
        Rect rect = drawingPanel.rectTransform.rect;
        return localPoint.x >= rect.x && localPoint.x <= rect.x + rect.width
            && localPoint.y >= rect.y && localPoint.y <= rect.y + rect.height;
    }

    //　各おえかきモードのメソッド
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

    private void FloodFill(Vector2 localPoint)
    {
        drawer.FloodFill(drawingPanel, localPoint);
        texture.Apply();
        SaveUndo();
    }

    private void UpdateLine(Vector2 localPoint)
    {
        if (Input.GetMouseButtonUp (0))
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
        undoStack.Push(texture.GetPixels());
        drawer = new DrawingUtils(texture, drawColor, brushSize);
        NotifyHistory();
        SetHasDrawing(false);
    }

    // 描画領域のサイズを設定
    private void SetDrawFieldSize(int width, int height)
    {
        var rectTransform = drawField.GetComponent<RectTransform>();
        float aspectRatio = (float)width / height;
        rectTransform.sizeDelta = aspectRatio > 1.0f ? new Vector2(BaseDrawCanvasSize, BaseDrawCanvasSize / aspectRatio) : new Vector2(BaseDrawCanvasSize * aspectRatio, BaseDrawCanvasSize);
    }

    public void ResetDrawFieldSize(int width, int height)
    {
        CreateTexture(width, height);
        SetDrawFieldSize(width, height);
        OnFieldSizeChanged?.Invoke(width, height);
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
            drawer.DrawPoint(pixelPos) ; // 最初の描画
        }

        lastPoint = pixelPos;
        texture.Apply();
    }

    private void DrawShape(Vector2Int start, Vector2Int end)
    {
        if (currentMode == ToolMode.Line)
        {
            drawer.DrawLine(start, end);
        }
        else if (currentMode == ToolMode.Circle)
        {
            drawer.DrawCircle(start, end);
        }
        else if (currentMode == ToolMode.Rectangle)
        {
            drawer.DrawRectangle(start, end);
        }
        texture.Apply();
    }

    private void SaveUndo(bool has)
    {
        undoStack.Push(texture.GetPixels()); // 現在の状態を保存
        redoStack.Clear(); // 新しく描画したらRedo履歴はクリア
        NotifyHistory();
        SetHasDrawing(has);
    }

    private void SaveUndo() => SaveUndo(true);

    public void UndoButton() => Undo();
    public void RedoButton() => Redo();

    private void Undo()
    {
        if (undoStackCount > 1)
        {
            redoStack.Push(undoStack.Pop());
            texture.SetPixels(undoStack.Peek());
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
            texture.SetPixels(undoStack.Peek());
            texture.Apply();
            NotifyHistory();
            SetHasDrawing(ComputeHasDrawing());
        }
    }

    public void AllClear()
    {
        ClearCanvas();
        SaveUndo(false);
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

        OnToolModeChanged?.Invoke(currentMode);
    }

    public void ChangeColor(Color color)
    {
        if (drawColor == color) return;

        drawColor = color;
        drawer = new DrawingUtils(texture, drawColor, brushSize);

        OnColorChanged?.Invoke(drawColor);
    }

    public void OnValueChangedBrushSize(Slider slider)
    {
        int newSize = (int)slider.value;
        if (brushSize == newSize) return;

        brushSize = newSize;
        drawer = new DrawingUtils(texture, drawColor, brushSize);

        OnBrushSizeChanged?.Invoke(brushSize);
    }


    // 書き出し関連

    public void ExportPng(string baseName)
    {
        if (texture == null) return;

        string safeBase = SanitizeBaseName(baseName);
        if (string.IsNullOrWhiteSpace(safeBase)) safeBase = exportDefaultName;

        string fileName = EnsureExtension(safeBase, ".png");

        Texture2D scaled = ScaleNearest(texture, exportScale, keepAlpha: true);
        byte[] bytes = scaled.EncodeToPNG();
        Destroy(scaled);

        SaveBytes(bytes, fileName);
    }

    public void ExportJpg(string baseName)
    {
        if (texture == null) return;

        string safeBase = SanitizeBaseName(baseName);
        if (string.IsNullOrWhiteSpace(safeBase)) safeBase = exportDefaultName;

        string fileName = EnsureExtension(safeBase, ".jpg");

        Texture2D flattened = FlattenOnWhite(texture); // RGB24(不透明)
        Texture2D scaled = ScaleNearest(flattened, exportScale, keepAlpha: false);
        Destroy(flattened);

        byte[] bytes = scaled.EncodeToJPG(jpgQuality);
        Destroy(scaled);

        SaveBytes(bytes, fileName);
    }

    private static string EnsureExtension(string name, string ext)
    {
        // "foo.png" を入れても ".png" が二重にならないように
        if (name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return name;
        // ".jpg"入れてきた人も救う
        name = Path.GetFileNameWithoutExtension(name);
        return name + ext;
    }

    private static string SanitizeBaseName(string name)
    {
        if (name == null) return "";
        name = name.Trim();

        // パス混入対策：フォルダ区切りは除去
        name = name.Replace("/", "_").Replace("\\", "_");

        // OS的に禁止文字を除去
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");

        // 空白連続を軽く整える（任意）
        name = string.Join(" ", name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        return name;
    }

    private static Texture2D FlattenOnWhite(Texture2D src)
    {
        int w = src.width, h = src.height;
        Color32[] sp = src.GetPixels32();
        Color32[] dp = new Color32[sp.Length];
        Color32 bg = new Color32(255, 255, 255, 255);

        for (int i = 0; i < sp.Length; i++)
        {
            Color32 s = sp[i];
            int a = s.a;
            dp[i] = new Color32(
                (byte)((s.r * a + bg.r * (255 - a)) / 255),
                (byte)((s.g * a + bg.g * (255 - a)) / 255),
                (byte)((s.b * a + bg.b * (255 - a)) / 255),
                255
            );
        }

        var dst = new Texture2D(w, h, TextureFormat.RGB24, false);
        dst.filterMode = FilterMode.Point;
        dst.SetPixels32(dp);
        dst.Apply();
        return dst;
    }

    private static Texture2D ScaleNearest(Texture2D src, int scale, bool keepAlpha)
    {
        if (scale <= 1)
        {
            // コピーを返す（元textureをそのままEncodeすると、後でDestroyできないので）
            return CopyTexture(src, keepAlpha);
        }

        int sw = src.width;
        int sh = src.height;
        int dw = sw * scale;
        int dh = sh * scale;

        Color32[] sp = src.GetPixels32();
        Color32[] dp = new Color32[dw * dh];

        for (int y = 0; y < dh; y++)
        {
            int sy = y / scale;
            int spRow = sy * sw;
            int dpRow = y * dw;

            for (int x = 0; x < dw; x++)
            {
                int sx = x / scale;
                dp[dpRow + x] = sp[spRow + sx];
            }
        }

        // PNG用はRGBA、JPG用はRGBで十分
        var format = keepAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24;
        var dst = new Texture2D(dw, dh, format, false);
        dst.filterMode = FilterMode.Point;
        dst.SetPixels32(dp);
        dst.Apply(false, false);
        return dst;
    }

    private static Texture2D CopyTexture(Texture2D src, bool keepAlpha)
    {
        Color32[] sp = src.GetPixels32();
        var format = keepAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24;

        var dst = new Texture2D(src.width, src.height, format, false);
        dst.filterMode = FilterMode.Point;
        dst.SetPixels32(sp);
        dst.Apply(false, false);
        return dst;
    }

    private void SaveBytes(byte[] bytes, string fileName)
    {
        // Steam(Standalone)想定：ユーザーの「ピクチャ」フォルダに保存
        string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        // フォルダが無い/取れない環境対策（念のため）
        if (string.IsNullOrEmpty(dir))
            dir = Application.persistentDataPath;

        // サブフォルダを作って散らからないように（任意）
        //string appDir = Path.Combine(dir, "DotOekaki");
        //Directory.CreateDirectory(appDir);

        string path = MakeUniquePath(dir, fileName);
        File.WriteAllBytes(path, bytes);
        Debug.Log($"Saved: {path}");
    }

    private static string MakeUniquePath(string dir, string fileName)
    {
        string path = Path.Combine(dir, fileName);
        if (!File.Exists(path)) return path;

        string name = Path.GetFileNameWithoutExtension(fileName);
        string ext = Path.GetExtension(fileName);

        for (int i = 2; i < 10000; i++)
        {
            string candidate = Path.Combine(dir, $"{name}_{i}{ext}");
            if (!File.Exists(candidate)) return candidate;
        }
        return path;
    }
}
