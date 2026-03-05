using UnityEngine;
using UnityEngine.UI;

public class DotUIManagerOff : MonoBehaviour
{
    [SerializeField] GridGeneratorOffline gridGeneratorOff;

    [SerializeField] GameObject dotUI;
    [SerializeField] GameObject blindPanel;
    [SerializeField] Image currentColor;
    [SerializeField] Toggle mekakushiToggle;
    [SerializeField] Button undoButton;
    [SerializeField] Button redoButton;
    [SerializeField] Button clearButton;
    [SerializeField] Button sizeApplyButton;
    [SerializeField] Button backButton;
    [SerializeField] GameObject penButtonCover;
    [SerializeField] GameObject fillButtonCover;
    [SerializeField] GameObject lineButtonCover;
    [SerializeField] GameObject circleButtonCover;
    [SerializeField] GameObject rectangleButtonCover;
    [SerializeField] SizeInputField widthInputField;
    [SerializeField] SizeInputField heightInputField;
    [SerializeField] InputField pngFileNameInputField;
    [SerializeField] InputField jpgFileNameInputField;
    [SerializeField] ColorPalette palette;

    DrawingManagerOff dm;


    private void Start()
    {
        TryBind();

        backButton.onClick.AddListener(() => SceneController.instance.LoadScene("Title"));
        mekakushiToggle.onValueChanged.AddListener(_ => OnMekakushiToggle());
    }

    private void TryBind()
    {
        if (dm == null) dm = DrawingManagerOff.instance;
        if (dm == null) return;

        dm.OnDrawableChanged += OnDrawableChanged;
        dm.OnToolModeChanged += OnToolModeChanged;
        dm.OnHistoryChanged += OnHistoryChanged;
        dm.OnHasDrawingChanged += OnHasDrawingChanged;
        dm.OnColorChanged += OnColorChanged;
        widthInputField.OnStateChanged += OnSizeInputFieldValueChanged;
        heightInputField.OnStateChanged += OnSizeInputFieldValueChanged;

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

    private void OnDrawableChanged(bool drawable)
    {
        SetActive(dotUI, drawable);
    }

    private void OnToolModeChanged(DrawingManagerOff.ToolMode mode)
    {
        SetActive(penButtonCover, mode == DrawingManagerOff.ToolMode.Pen);
        SetActive(fillButtonCover, mode == DrawingManagerOff.ToolMode.Fill);
        SetActive(lineButtonCover, mode == DrawingManagerOff.ToolMode.Line);
        SetActive(circleButtonCover, mode == DrawingManagerOff.ToolMode.Circle);
        SetActive(rectangleButtonCover, mode == DrawingManagerOff.ToolMode.Rectangle);
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
        color.a = 1;
        currentColor.color = color;
    }

    public void OnClickSizeApplyButton()
    {
        dm.ResetDrawFieldSize(widthInputField.inputPixelSize, heightInputField.inputPixelSize);
        gridGeneratorOff.SetGridCount(widthInputField.inputPixelSize, heightInputField.inputPixelSize);
    }

    public void OnClickUndoButton() => dm.UndoButton();
    public void OnClickRedoButton() => dm.RedoButton();
    public void OnClickAllClearButton() => dm.AllClear();
    public void OnClickColor(int index) => dm.ChangeColor(palette.colors[index]);
    public void OnClickEraserButton() => dm.ChangeColor(new Color32(83, 83, 83, 0));
    public void OnClickToolButton(int index) => dm.ChangeMode((DrawingManagerOff.ToolMode)index);
    public void ToggleIsDrawable() => dm.SetDrawable(!dm.IsDrawable);
    public void OnMekakushiToggle() => blindPanel.SetActive(mekakushiToggle.isOn);

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

    public void OnClickExportPng()
    {
        string name = pngFileNameInputField.text;
        dm.ExportPng(name);
    }

    public void OnClickExportJpg()
    {
        string name = jpgFileNameInputField.text;
        dm.ExportJpg(name);
    }


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
