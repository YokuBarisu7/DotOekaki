using UnityEngine;
using UnityEngine.UI;

public class DengonUIManager : MonoBehaviour
{
    [SerializeField] DengonGridGenerator dengonGridGenerator;

    [SerializeField] GameObject dotUI;
    [SerializeField] GameObject blindPanel;
    [SerializeField] Button undoButton;
    [SerializeField] Button redoButton;
    [SerializeField] Button clearButton;
    [SerializeField] Button sizeApplyButton;
    [SerializeField] Button backButton1;
    [SerializeField] Button backButton2;
    [SerializeField] GameObject penButtonCover;
    [SerializeField] GameObject fillButtonCover;
    [SerializeField] GameObject lineButtonCover;
    [SerializeField] GameObject circleButtonCover;
    [SerializeField] GameObject rectangleButtonCover;
    [SerializeField] Text themeText;
    [SerializeField] Text gamefinishText;
    [SerializeField] SizeInputField widthInputField;
    [SerializeField] SizeInputField heightInputField;
    [SerializeField] Image currentColor;
    [SerializeField] Toggle mekakushiToggle;
    [SerializeField] Slider brushSizeSlider;
    [SerializeField] ColorPalette palette;

    [SerializeField] GameObject sizeChangerPanel;
    [SerializeField] GameObject colorSpectrum;
    [SerializeField] GameObject backPanel;

    [Header("タブボタン")]
    [SerializeField] GameObject[] tabs;
    [Header("タブパネル")]
    [SerializeField] GameObject[] panels;

    [SerializeField] Button submitAnswerButton;
    [SerializeField] InputField answerInputField;
    private bool isAnswerPhase = false;
    private bool hasSubmittedAnswer = false;
    private bool isGameFinished = false;

    Transform parentTransform;
    DengonDrawingManager dm;


    private void Start()
    {
        TryBind();

        backButton1.onClick.AddListener(() => { PhotonManager.instance.OnLeaveRoomAndDestroy(); });
        backButton2.onClick.AddListener(() => { PhotonManager.instance.OnLeaveRoomAndDestroy(); });
        brushSizeSlider.onValueChanged.AddListener(OnBrushSizeSliderChanged);
        mekakushiToggle.onValueChanged.AddListener(_ => OnMekakushiToggle());

        parentTransform = tabs[0].transform.parent;

        RefreshAnswerUI();
    }

    private void TryBind()
    {
        if (dm == null) dm = DengonDrawingManager.instance;

        dm.OnDrawableChanged += OnDrawableChanged;
        dm.OnToolModeChanged += OnToolModeChanged;
        dm.OnHistoryChanged += OnHistoryChanged;
        dm.OnHasDrawingChanged += OnHasDrawingChanged;
        dm.OnColorChanged += OnColorChanged;
        dm.OnBrushSizeChanged += OnBrushSizeChanged;
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

    private void OnToolModeChanged(DengonDrawingManager.ToolMode mode)
    {
        SetActive(penButtonCover, mode == DengonDrawingManager.ToolMode.Pen);
        SetActive(fillButtonCover, mode == DengonDrawingManager.ToolMode.Fill);
        SetActive(lineButtonCover, mode == DengonDrawingManager.ToolMode.Line);
        SetActive(circleButtonCover, mode == DengonDrawingManager.ToolMode.Circle);
        SetActive(rectangleButtonCover, mode == DengonDrawingManager.ToolMode.Rectangle);
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

    public void SetAnswerPhase(bool enabled)
    {
        isAnswerPhase = enabled;
        RefreshAnswerUI();
    }

    public void ResetAnswerSubmission()
    {
        hasSubmittedAnswer = false;
        answerInputField.text = "";
        RefreshAnswerUI();
    }

    public void SetAnswerLocked(bool locked)
    { 
        hasSubmittedAnswer = locked;
        RefreshAnswerUI();
    }

    public string GetAnswerText()
    {
        return answerInputField != null ? answerInputField.text : "";
    }

    public void SetGameFinished(bool finished)
    { 
        isGameFinished = finished;
        RefreshAnswerUI() ;
    }

    public void RefreshAnswerUI()
    {
        if (submitAnswerButton == null || answerInputField == null) return;

        // ゲーム終了してる時は操作不可
        if (isGameFinished)
        {
            answerInputField.interactable = false;
            submitAnswerButton.interactable = false;
            return;
        }

        // 回答フェーズ以外はアンサーUIを操作できないようにする
        if (!isAnswerPhase)
        {
            answerInputField.interactable = false;
            submitAnswerButton.interactable = false;
            return;
        }

        bool hasText = !string.IsNullOrWhiteSpace(answerInputField.text);

        if (hasSubmittedAnswer)
        {
            // ロック中：入力不可、でも解除用にボタンは押せる（回答フェーズ中のみ）
            answerInputField.interactable = false;
            submitAnswerButton.interactable = true;
            return;
        }

        // 未ロック：回答フェーズ中だけ入力可
        answerInputField.interactable = true;
        submitAnswerButton.interactable = hasText;
    }


    public void OnClickSizeApplyButton()
    {
        DengonDrawingManager.instance.ResetDrawFieldSize(widthInputField.inputPixelSize, heightInputField.inputPixelSize);
        dengonGridGenerator.SetGridCount(widthInputField.inputPixelSize, heightInputField.inputPixelSize );
    }

    public void OnClickUndoButton() => dm.UndoButton();
    public void OnClickRedoButton() => dm.RedoButton();
    public void OnClickAllClearButton() => dm.AllClear();
    public void OnClickColor(int index) => dm.ChangeColor(palette.colors[index]);
    public void OnClickToolButton(int index) => dm.ChangeMode((DengonDrawingManager.ToolMode)index);
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

    private void OnMekakushiToggle() => blindPanel.SetActive(mekakushiToggle.isOn);

    public void ShowCountdown(string text)
    { 
        gamefinishText.text = text;
    }

    public void OnTabClicked(int tabIndex)
    {
        ResetTabs();

        int panelSibling = panels[tabIndex].transform.GetSiblingIndex();
        tabs[tabIndex].transform.SetSiblingIndex(panelSibling );

        for (int i = 0; i < panels.Length; i++)
        {
            if (i == tabIndex)
            {
                panels[i].SetActive(true);
            }
            else
            {
                panels[i].SetActive(false);
            }
        }
    }

    private void ResetTabs()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].transform.SetSiblingIndex(i);
        }
    }

    public void SetActiveTab(int index)
    { 
        for (int i = 0; i < tabs.Length; i++)
        {
            if (i < index)
            {
                tabs[i].SetActive(true);
                panels[i].SetActive(true);
            }
            else
            {
                tabs[i].SetActive(false);
                panels[i].SetActive(false);
            }
        }
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