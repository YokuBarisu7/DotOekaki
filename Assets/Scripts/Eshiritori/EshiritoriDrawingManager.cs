using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using System;

public class EshiritoriDrawingManager : MonoBehaviourPunCallbacks
{
    public static EshiritoriDrawingManager instance;


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

    Color32[] clearBuffer; // ClearCanvasで使用
    Stack<Color32[]> undoStack; // 元に戻すためのスタック
    Stack<Color32[]> redoStack; // やり直しのためのスタック
    Vector2Int? startPoint = null; // 直線モードの始点
    Vector2Int startPixel; // 円モード、長方形モードの始点
    bool isDrawing = false; // 描画中かどうか
    bool isDrawable = false; // 描画可能かどうか
    bool hasDrawing = false; // 何か描かれているかどうか
    DrawingUtils drawer;

    Dictionary<int, Vector2Int?> lastPoints = new Dictionary<int, Vector2Int?>();

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

        // ゲーム開始時は黒色ペンモード
        ChangeColor(Color.black);
        SetBrushSize(1);
        ChangeMode(ToolMode.Pen);
    }

    // テクスチャを作成
    [PunRPC]
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

        OnFieldSizeChanged?.Invoke(width, height);
    }

    public void ResetDrawFieldSize(int size)
    {
        photonView.RPC("CreateTexture", RpcTarget.All, size, size);
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
                UpdateFill(localPoint);
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

    private void UpdatePen(Vector2 localPoint)
    {
        if (Input.GetMouseButtonDown(0) && IsInsideCanvas(localPoint))
            isDrawing = true;

        if (Input.GetMouseButton(0) && isDrawing)
        {
            if (!IsInsideCanvas(localPoint)) return;
            if (!TryGetPixelPos(localPoint, out var p)) return;

            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            photonView.RPC("DrawAtPixelRPC", RpcTarget.All,
                actorNumber, p.x, p.y,
                drawColor.r, drawColor.g, drawColor.b, drawColor.a,
                brushSize);
        }

        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            isDrawing = false;

            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            photonView.RPC("ResetLastPoint", RpcTarget.All, actorNumber);
        }
    }

    private void UpdateFill(Vector2 localPoint)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsInsideCanvas(localPoint)) return;
            if (!TryGetPixelPos(localPoint, out var p)) return;

            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            photonView.RPC("FloodFillRPC", RpcTarget.All,
                actorNumber, p.x, p.y,
                drawColor.r, drawColor.g, drawColor.b, drawColor.a,
                brushSize);
        }
    }

    private void UpdateLine(Vector2 localPoint)
    {
        if (Input.GetMouseButtonUp(0))
        {
            if (!IsInsideCanvas(localPoint)) return;
            if (!TryGetPixelPos(localPoint, out var p)) return;

            // 一回目のクリックで始点を設定
            if (startPoint == null)
            {
                startPoint = p;
                return;
            }

            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            photonView.RPC("DrawShapeRPC", RpcTarget.All,
                actorNumber, startPoint.Value.x, startPoint.Value.y, p.x, p.y,
                 (int)currentMode, drawColor.r, drawColor.g, drawColor.b, drawColor.a, brushSize);

            startPoint = null;
        }
    }

    private void UpdateShapeDrag(Vector2 localPoint)
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 始点の設定
            if (!IsInsideCanvas(localPoint)) return;
            if (!TryGetPixelPos(localPoint, out var p)) return;

            startPixel = p;
            isDrawing = true;
        }

        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            isDrawing = false;
            if (!IsInsideCanvas(localPoint)) return;
            if (!TryGetPixelPos(localPoint, out var end)) return;

            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            photonView.RPC("DrawShapeRPC", RpcTarget.All,
                actorNumber, startPixel.x, startPixel.y, end.x, end.y,
                 (int)currentMode, drawColor.r, drawColor.g, drawColor.b, drawColor.a, brushSize);
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

    [PunRPC]
    private void DrawAtPixelRPC(int actorNumber, int x, int y, float r, float g, float b, float a, int size)
    {
        x = Mathf.Clamp(x, 0, texture.width - 1);
        y = Mathf.Clamp(y, 0, texture.height - 1);

        var point = new Vector2Int(x, y);
        var color = new Color(r, g, b, a);

        if (!lastPoints.ContainsKey(actorNumber))
        {
            lastPoints[actorNumber] = null;
        }

        // 一時的にそのプレイヤー設定でDrawingUtilsを使う
        var tempDrawer = new DrawingUtils(texture, color, size);

        if (lastPoints[actorNumber].HasValue)
        {
            tempDrawer.DrawLine(lastPoints[actorNumber].Value, point); // 2回目以降の描画
        }
        else
        {
            tempDrawer.DrawPoint(point); // 最初の描画
        }

        lastPoints[actorNumber] = point;
        texture.Apply();
    }

    [PunRPC]
    private void ResetLastPoint(int actorNumber)
    {
        if (lastPoints.ContainsKey(actorNumber))
        {
            lastPoints[actorNumber] = null;
        }
        SaveUndo();
    }

    [PunRPC]
    private void DrawShapeRPC(int actorNumber, int startX, int startY, int endX, int endY, int modeInt, float r, float g, float b, float a, int size)
    {
        var color = new Color(r, g, b, a);
        var tempDrawer = new DrawingUtils(texture, color, size);
        var mode = (ToolMode)modeInt;

        switch (mode)
        {
            case ToolMode.Line: tempDrawer.DrawLine(new Vector2Int(startX, startY), new Vector2Int(endX, endY)); break;
            case ToolMode.Circle: tempDrawer.DrawCircle(new Vector2Int(startX, startY), new Vector2Int(endX, endY)); break;
            case ToolMode.Rectangle: tempDrawer.DrawRectangle(new Vector2Int(startX, startY), new Vector2Int(endX, endY)); break;
        }

        texture.Apply();
        SaveUndo();
    }

    [PunRPC]
    private void FloodFillRPC(int actorNumber, int x, int y, float r, float g, float b, float a, int size)
    {
        x = Mathf.Clamp(x, 0, texture.width - 1);
        y = Mathf.Clamp(y, 0, texture.height - 1);

        Color color = new Color(r, g, b, a);

        // 一時的にそのプレイヤー設定でDrawingUtilsを使う
        var tempDrawer = new DrawingUtils(texture, color, size);

        tempDrawer.FloodFillPixel(x, y);
        texture.Apply();
        SaveUndo();
    }

    private void SaveUndo()
    {
        Color32[] currentPixels = texture.GetPixels32();

        if (undoStack.Count > 0 && ArePixelsEqual(undoStack.Peek(), currentPixels)) return;

        undoStack.Push(texture.GetPixels32()); // 現在の状態を保存
        redoStack.Clear(); // 新しく描画したらRedo履歴はクリア
        NotifyHistory();
        SetHasDrawing(ComputeHasDrawing());
    }

    private bool ArePixelsEqual(Color32[] a, Color32[] b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (!a[i].Equals(b[i])) return false;
        }

        return true;
    }

    public void UndoButton()
    {
        photonView.RPC("Undo", RpcTarget.All);
    }
    public void RedoButton() 
    {
        photonView.RPC("Redo", RpcTarget.All);
    }

    public void AllClearButton()
    {
        photonView.RPC("AllClear", RpcTarget.All);
    }

    [PunRPC]
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
    [PunRPC]
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

    [PunRPC]
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
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("ChangeModeRPC", RpcTarget.All, mode);
        }
        else
        {
            currentMode = mode;
        }
    }

    [PunRPC]
    private void ChangeModeRPC(ToolMode mode)
    {
        if (currentMode == mode) return;

        currentMode = mode;
        isDrawing = false;
        startPoint = null;
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
}
