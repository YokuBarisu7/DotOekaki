using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using System;

public class DrawingManager : MonoBehaviourPunCallbacks
{
    public static DrawingManager instance;

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
    Vector2Int? lastPoint = null; // 前回の描画位置
    Vector2Int? startPoint = null; // 直線モードの始点
    Vector2Int startPixel; // 円モード、長方形モードの始点
    bool isDrawing = false; // 描画中かどうか
    bool isDrawable = false; // 描画可能かどうか
    bool hasDrawing = false; // 何か描かれているかどうか
    const float BaseDrawCanvasSize = 900f;
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
        SetDrawFieldSize(initialWidth, initialHeight);

        // ゲーム開始時は黒色ペンモード
        ChangeColor(Color.black);
        SetBrushSize(1);
        ChangeMode(ToolMode.Pen);
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
    }

    // 描画領域のサイズを設定
    [PunRPC]
    private void SetDrawFieldSize(int width, int height)
    {
        var rectTransform = drawField.GetComponent<RectTransform>();
        float aspectRatio = (float)width / height;
        rectTransform.sizeDelta = aspectRatio > 1.0f ? new Vector2(BaseDrawCanvasSize, BaseDrawCanvasSize / aspectRatio) : new Vector2(BaseDrawCanvasSize * aspectRatio, BaseDrawCanvasSize);

        OnFieldSizeChanged?.Invoke(width, height);
    }

    public void ResetDrawFieldSize(int width, int height)
    { 
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("CreateTexture", RpcTarget.All, width, height);
            photonView.RPC("SetDrawFieldSize", RpcTarget.All, width, height);
        }
        else
        {
            CreateTexture(width, height);
            SetDrawFieldSize(width, height);
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

            if (PhotonNetwork.InRoom)
            {
                int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                photonView.RPC("DrawAtPixelRPC", RpcTarget.All,
                    actorNumber, p.x, p.y,
                    drawColor.r, drawColor.g, drawColor.b, drawColor.a,
                    brushSize);
            }
            else
            {
                // 将来的には DrawAtPixel(p) 推奨
                DrawAtPixel(p);
            }
        }

        if (Input.GetMouseButtonUp(0) && isDrawing)
        {
            isDrawing = false;
            lastPoint = null;

            if (PhotonNetwork.InRoom)
            {
                int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                photonView.RPC("ResetLastPoint", RpcTarget.All, actorNumber);
            }
            else
            {
                SaveUndo();
            }
        }
    }

    private void UpdateFill(Vector2 localPoint)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsInsideCanvas(localPoint)) return;
            if (!TryGetPixelPos(localPoint, out var p)) return;

            if (PhotonNetwork.InRoom)
            {
                int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                photonView.RPC("FloodFillRPC", RpcTarget.All,
                    actorNumber, p.x, p.y,
                    drawColor.r, drawColor.g, drawColor.b, drawColor.a,
                    brushSize);
            }
            else
            {
                FloodFill(localPoint);
            }
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

            if (PhotonNetwork.InRoom)
            {
                int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                photonView.RPC("DrawShapeRPC", RpcTarget.All,
                    actorNumber, (int)currentMode, startPoint.Value.x, startPoint.Value.y, p.x, p.y,
                    drawColor.r, drawColor.g, drawColor.b, drawColor.a, brushSize);
            }
            else
            {
                DrawShape(startPoint.Value, p);
            }
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

            if (PhotonNetwork.InRoom)
            {
                int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                photonView.RPC("DrawShapeRPC", RpcTarget.All,
                    actorNumber, (int)currentMode, startPixel.x, startPixel.y, end.x, end.y,
                    drawColor.r, drawColor.g, drawColor.b, drawColor.a, brushSize);
            }
            else
            {
                DrawShape(startPixel, end);
            }
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

    private void DrawAtPixel(Vector2Int p)
    {
        if (lastPoint.HasValue) drawer.DrawLine(lastPoint.Value, p);
        else drawer.DrawPoint(p);

        lastPoint = p;
        texture.Apply();
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
        SaveUndo();
    }

    [PunRPC]
    private void DrawShapeRPC(int actorNumber, int modeInt, int startX, int startY, int endX, int endY, float r, float g, float b, float a, int size)
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

    private void FloodFill(Vector2 localPoint)
    {
        drawer.FloodFill(drawingPanel, localPoint);
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
        undoStack.Push(texture.GetPixels32()); // 現在の状態を保存
        redoStack.Clear(); // 新しく描画したらRedo履歴はクリア
        NotifyHistory();
        SetHasDrawing(ComputeHasDrawing());
    }

    public void UndoButton()
    {
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("Undo", RpcTarget.All);
        }
        else
        {
            Undo();
        }
    }
    public void RedoButton() 
    {
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("Redo", RpcTarget.All);
        }
        else
        {
            Redo();
        }
    }

    public void AllClearButton()
    {
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("AllClear", RpcTarget.All);
        }
        else
        {
            AllClear();
        }
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
    private void AllClear()
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
            for (int i = 0; i < len;i++) clearBuffer[i] = t;
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
            ChangeModeRPC(mode);
        }
    }

    [PunRPC]
    private void ChangeModeRPC(ToolMode mode)
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
