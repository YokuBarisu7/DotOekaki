using UnityEngine;
using UnityEngine.UI;

public class CooperateUIManager : MonoBehaviour
{
    [SerializeField] CooperateGridGenerator gridGenerator;

    [SerializeField] GameObject dotUI;
    [SerializeField] GameObject blindPanel;
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
    [SerializeField] CooperateCountInputField cooperateCountInputField;
    [SerializeField] CooperateTimeInputField cooperateTimeInputField;
    [SerializeField] Image currentColor;
    [SerializeField] Toggle mekakushiToggle;
    [SerializeField] GameObject sizeChangerPanel;
    [SerializeField] GameObject colorSpectrum;
    [SerializeField] GameObject backPanel;
    [SerializeField] Slider brushSizeSlider;
    [SerializeField] ColorPalette palette;

    CooperateDrawingManager dm;


    private void Start()
    {
        TryBind();

        backButton1.onClick.AddListener(() => { PhotonManager.instance.OnLeaveRoomAndDestroy(); } );
        backButton2.onClick.AddListener(() => { PhotonManager.instance.OnLeaveRoomAndDestroy(); });
        brushSizeSlider.onValueChanged.AddListener(OnBrushSizeSliderChanged);
        mekakushiToggle.onValueChanged.AddListener(_ => OnMekakushiToggle());
    }

    private void TryBind()
    {
        if (dm == null) dm = CooperateDrawingManager.instance;

        dm.OnDrawableChanged += OnDrawableChanged;
        dm.OnToolModeChanged += OnToolModeChanged;
        dm.OnColorChanged += OnColorChanged;
        dm.OnBrushSizeChanged += OnBrushSizeChanged;
        widthInputField.OnStateChanged += OnSizeInputFieldValueChanged;
        heightInputField.OnStateChanged += OnSizeInputFieldValueChanged;
        cooperateCountInputField.OnStateChanged += OnSettingInputFieldValueChanged;
        cooperateTimeInputField.OnStateChanged += OnSettingInputFieldValueChanged;

        RefreshAllUI();
    }

    private void RefreshAllUI()
    {
        OnDrawableChanged(dm.IsDrawable);
        OnToolModeChanged(dm.currentMode);
        OnColorChanged(dm.DrawColor);
    }

    // 初期化処理
    public void Initialize()
    {
        sizeChangerPanel.SetActive(false);
        colorSpectrum.SetActive(false);
        backPanel.SetActive(false);
        mekakushiToggle.isOn = false;
    }

    private void OnDrawableChanged(bool drawable)
    {
        SetActive(dotUI, drawable);
    }

    private void OnToolModeChanged(CooperateDrawingManager.ToolMode mode)
    {
        SetActive(penButtonCover, mode == CooperateDrawingManager.ToolMode.Pen);
        SetActive(fillButtonCover, mode == CooperateDrawingManager.ToolMode.Fill);
        SetActive(lineButtonCover, mode == CooperateDrawingManager.ToolMode.Line);
        SetActive(circleButtonCover, mode == CooperateDrawingManager.ToolMode.Circle);
        SetActive(rectangleButtonCover, mode == CooperateDrawingManager.ToolMode.Rectangle);
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

    public void SetRoleText(string name1, string name2)
    {
        roleText.text = $"Drawer：{name1} & {name2}";
    }

    public void SetThemeText(bool isQuestioner, string theme)
    {
        themeText.text = isQuestioner == true ? "お題：" + theme : "お題は何でしょう？";
    }

    public void OnClickSizeApplyButton()
    {
        CooperateDrawingManager.instance.ResetDrawFieldSize(widthInputField.inputPixelSize, heightInputField.inputPixelSize);
        gridGenerator.SetGridCount(widthInputField.inputPixelSize, heightInputField.inputPixelSize);
    }

    public void OnClickColor(int index) => dm.ChangeColor(palette.colors[index]);
    public void OnClickToolButton(int index) => dm.ChangeMode((CooperateDrawingManager.ToolMode)index);
    public void OnClickEraserButton() => dm.ChangeColor(new Color(0, 0, 0, 0));
    public void ToggleIsDrawable() => dm.SetDrawable(!dm.IsDrawable);
    public void OnBrushSizeSliderChanged(float v) => dm.SetBrushSize((int)v);

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
        if (cooperateCountInputField.IsError || cooperateTimeInputField.IsError)
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
