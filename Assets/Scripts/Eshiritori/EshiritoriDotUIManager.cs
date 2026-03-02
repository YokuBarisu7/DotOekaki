using UnityEngine;
using UnityEngine.UI;

public class EshiritoriDotUIManager : MonoBehaviour
{
    [SerializeField] EshiritoriGridGenerator gridGenerator;

    [SerializeField] GameObject dotUI;
    [SerializeField] GameObject blindPanel;
    [SerializeField] Button undoButton;
    [SerializeField] Button redoButton;
    [SerializeField] Button clearButton;
    [SerializeField] Button sizeApplyButton;
    [SerializeField] Button backButton;
    [SerializeField] GameObject turnEndButton;
    [SerializeField] GameObject resultDisplayButton;
    [SerializeField] GameObject penButtonCover;
    [SerializeField] GameObject fillButtonCover;
    [SerializeField] GameObject lineButtonCover;
    [SerializeField] GameObject circleButtonCover;
    [SerializeField] GameObject rectangleButtonCover;
    [SerializeField] GameObject eraserButtonCover;
    [SerializeField] Text roleText;
    [SerializeField] SizeInputField sizeInputField;
    [SerializeField] Image currentColor;
    [SerializeField] Toggle mekakushiToggle;
    [SerializeField] GameObject sizeChangerPanel;
    [SerializeField] GameObject colorSpectrum;
    [SerializeField] GameObject backPanel;
    [SerializeField] Slider brushSizeSlider;
    [SerializeField] ColorPalette palette;

    EshiritoriDrawingManager dm;

    private void Start()
    {
        TryBind();

        backButton.onClick.AddListener(() => { PhotonManager.instance.OnLeaveRoomAndDestroy(); });
        brushSizeSlider.onValueChanged.AddListener(OnBrushSizeSliderChanged);
        mekakushiToggle.onValueChanged.AddListener(_ => OnMekakushiToggle());
    }

    private void TryBind()
    {
        if (dm == null) dm = EshiritoriDrawingManager.instance;

        dm.OnDrawableChanged += OnDrawableChanged;
        dm.OnToolModeChanged += OnToolModeChanged;
        dm.OnHistoryChanged += OnHistoryChanged;
        dm.OnHasDrawingChanged += OnHasDrawingChanged;
        dm.OnColorChanged += OnColorChanged;
        dm.OnBrushSizeChanged += OnBrushSizeChanged;
        sizeInputField.OnStateChanged += OnSizeInputFieldValueChanged;
        EshiritoriManager.instance.OnTurnStarted += SetTurnEndButton;

        RefreshAllUI();
    }

    private void RefreshAllUI()
    {
        OnDrawableChanged(dm.IsDrawable);
        OnToolModeChanged(dm.currentMode);
        OnHistoryChanged(dm.undoStackCount, dm.redoStackCount);
        OnHasDrawingChanged(dm.HasDrawingCached);
        OnColorChanged(dm.DrawColor);
    }

    // 初期化処理
    public void Initialize()
    {
        sizeChangerPanel.SetActive(false);
        colorSpectrum.SetActive(false);
        backPanel.SetActive(false);
        mekakushiToggle.isOn = false;
        turnEndButton.SetActive(false);
        resultDisplayButton.SetActive(false);
    }

    private void OnDrawableChanged(bool drawable)
    {
        SetActive(dotUI, drawable);
    }

    private void OnToolModeChanged(EshiritoriDrawingManager.ToolMode mode)
    {
        SetActive(penButtonCover, mode == EshiritoriDrawingManager.ToolMode.Pen);
        SetActive(fillButtonCover, mode == EshiritoriDrawingManager.ToolMode.Fill);
        SetActive(lineButtonCover, mode == EshiritoriDrawingManager.ToolMode.Line);
        SetActive(circleButtonCover, mode == EshiritoriDrawingManager.ToolMode.Circle);
        SetActive(rectangleButtonCover, mode == EshiritoriDrawingManager.ToolMode.Rectangle);
    }

    private void OnHistoryChanged(int undoCount, int redoCount)
    {
        SetInteractable(undoButton, undoCount > 1);
        SetInteractable(redoButton, redoCount > 0);
    }

    private void OnHasDrawingChanged(bool hasDrawing)
    {
        SetInteractable(clearButton, hasDrawing);
    }

    private void OnColorChanged(Color color)
    {
        currentColor.color = color;
    }

    private void OnBrushSizeChanged(int size)
    {
        if ((int)brushSizeSlider.value != size)
            brushSizeSlider.SetValueWithoutNotify(size);
    }

    private void SetTurnEndButton(bool isDrawer)
    { 
        turnEndButton.SetActive(isDrawer);
    }

    public void SetResultDisplayButton(bool isMaster)
    { 
        resultDisplayButton.SetActive(isMaster);
    }

    public void OnClickSizeApplyButton()
    {
        EshiritoriDrawingManager.instance.ResetDrawFieldSize(sizeInputField.inputPixelSize);
    }

    public void SetRoleText(bool isDrawer)
    {
        roleText.text = isDrawer == true ? "あなたが描き手です" : "";
    }

    public void OnClickUndoButton() => dm.UndoButton();
    public void OnClickRedoButton() => dm.RedoButton();
    public void OnClickAllClearButton() => dm.AllClearButton();
    public void OnClickColor(int index) => dm.ChangeColor(palette.colors[index]);
    public void OnClickToolButton(int index) => dm.ChangeMode((EshiritoriDrawingManager.ToolMode)index);
    public void OnClickEraserButton() => dm.ChangeColor(new Color(0, 0, 0, 0));
    public void ToggleIsDrawable() => dm.SetDrawable(!dm.IsDrawable);
    public void OnBrushSizeSliderChanged(float v) => dm.SetBrushSize((int)v);

    public void OnSizeInputFieldValueChanged()
    {
        SetInteractable(sizeApplyButton, !sizeInputField.IsError);
    }

    public void OnClickTurnEndButton() => EshiritoriManager.instance.AdvanceNextTurn();
    public void OnClickResultDisplayButton() => EshiritoriManager.instance.GameFinish();
    public void OnClickRestartButton() => EshiritoriManager.instance.RestartGame();

    private void OnMekakushiToggle() => blindPanel.SetActive(mekakushiToggle.isOn);

    private void SetActive(GameObject obj, bool isActive)
    {
        if (obj.activeSelf != isActive)
        {
            obj.SetActive(isActive);
        }
    }

    private void SetInteractable(Button button, bool isInteractable)
    {
        if (button.interactable != isInteractable)
        {
            button.interactable = isInteractable;
        }
    }
}
