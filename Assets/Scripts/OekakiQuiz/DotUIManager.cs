using UnityEngine;
using UnityEngine.UI;

public class DotUIManager : MonoBehaviour
{
    [SerializeField] GridGenerator gridGenerator;

    [SerializeField] GameObject dotUI;
    [SerializeField] GameObject blindPanel;
    [SerializeField] Button undoButton;
    [SerializeField] Button redoButton;
    [SerializeField] Button clearButton;
    [SerializeField] Button sizeApplyButton;
    [SerializeField] Button backButton1;
    [SerializeField] Button backButton2;
    [SerializeField] Button gameRestartButton;
    [SerializeField] GameObject penButtonCover;
    [SerializeField] GameObject fillButtonCover;
    [SerializeField] GameObject lineButtonCover;
    [SerializeField] GameObject circleButtonCover;
    [SerializeField] GameObject rectangleButtonCover;
    [SerializeField] Text roleText;
    [SerializeField] Text themeText;
    [SerializeField] SizeInputField widthInputField;
    [SerializeField] SizeInputField heightInputField;
    [SerializeField] QuestionCountInputField questionCountInputField;
    [SerializeField] LimitTimeInputField limitTimeInputField;
    [SerializeField] Image currentColor;
    [SerializeField] Toggle mekakushiToggle;
    [SerializeField] GameObject sizeChangerPanel;
    [SerializeField] GameObject colorSpectrum;
    [SerializeField] GameObject backPanel;
    [SerializeField] ColorPalette palette;

    DrawingManager dm;


    private void Start()
    {
        TryBind();

        backButton1.onClick.AddListener(() =>
        {
            PhotonManager.instance.OnLeaveRoomAndDestroy();
        });
        backButton2.onClick.AddListener(() =>
        {
            PhotonManager.instance.OnLeaveRoomAndDestroy();
        });
        mekakushiToggle.onValueChanged.AddListener(_ => OnMekakushiToggle());
    }

    private void TryBind()
    {
        if (dm == null) dm = DrawingManager.instance;

        dm.OnDrawableChanged += OnDrawableChanged;
        dm.OnToolModeChanged += OnToolModeChanged;
        dm.OnHistoryChanged += OnHistoryChanged;
        dm.OnHasDrawingChanged += OnHasDrawingChanged;
        dm.OnColorChanged += OnColorChanged;
        widthInputField.OnStateChanged += OnSizeInputFieldValueChanged;
        heightInputField.OnStateChanged += OnSizeInputFieldValueChanged;
        questionCountInputField.OnStateChanged += OnSettingInputFieldValueChanged;
        limitTimeInputField.OnStateChanged += OnSettingInputFieldValueChanged;

        RefreshAllUI();
    }

    public void Initialize()
    {
        // 初期化処理
        sizeChangerPanel.SetActive(false);
        colorSpectrum.SetActive(false);
        backPanel.SetActive(false);
        mekakushiToggle.isOn = false;
        DrawingManager.instance.InitializeDrawField();
    }

    private void RefreshAllUI()
    {
        OnDrawableChanged(dm.IsDrawable);
        OnToolModeChanged(dm.currentMode);
        OnHistoryChanged(dm.undoStackCount, dm.redoStackCount);
        OnHasDrawingChanged(dm.HasDrawingCached);
        OnColorChanged(dm.DrawColor);
    }

    private void OnDrawableChanged(bool drawable)
    {
        SetActive(dotUI, drawable);
    }

    private void OnToolModeChanged(DrawingManager.ToolMode mode)
    {
        SetActive(penButtonCover, mode == DrawingManager.ToolMode.Pen);
        SetActive(fillButtonCover, mode == DrawingManager.ToolMode.Fill);
        SetActive(lineButtonCover, mode == DrawingManager.ToolMode.Line);
        SetActive(circleButtonCover, mode == DrawingManager.ToolMode.Circle);
        SetActive(rectangleButtonCover, mode == DrawingManager.ToolMode.Rectangle);
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

    public void SetRoleText(string name)
    {
        roleText.text = $"出題者(描く人)：{name}";
    }

    public void SetThemeText(Role role, string theme)
    {
        themeText.text = role == Role.Questioner ? "お題：" + theme : "お題は何でしょう？";
    }

    public void OnClickSizeApplyButton()
    {
        DrawingManager.instance.ResetDrawFieldSize(widthInputField.inputPixelSize, heightInputField.inputPixelSize);
        gridGenerator.SetGridCount(widthInputField.inputPixelSize, heightInputField.inputPixelSize );
    }

    public void OnClickUndoButton() => dm.UndoButton();
    public void OnClickRedoButton() => dm.RedoButton();
    public void OnClickAllClearButton() => dm.AllClear();
    public void OnClickColor(int index) => dm.ChangeColor(palette.colors[index]);
    public void OnClickToolButton(int index) => dm.ChangeMode((DrawingManager.ToolMode)index);
    public void OnClickEraserButton() => dm.ChangeColor(new Color(0, 0, 0, 0));
    public void ToggleIsDrawable() => dm.SetDrawable(!dm.IsDrawable);

    public void OnSizeInputFieldValueChanged()
    {
        if (heightInputField.IsError || widthInputField.IsError)
        {
            SetInteractable(sizeApplyButton, false);
        }
        else
        {
            SetInteractable(sizeApplyButton, true);
        }
    }

    public void OnSettingInputFieldValueChanged()
    {
        if (questionCountInputField.IsError || limitTimeInputField.IsError)
        {
            SetInteractable(gameRestartButton, false);
        }
        else
        {
            SetInteractable(gameRestartButton, true);
        }
    }

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
