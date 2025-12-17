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

    [SerializeField] bool isBlind;

    [SerializeField] GameObject sizeChangerPanel;
    [SerializeField] GameObject colorSpectrum;
    [SerializeField] GameObject backPanel;

    [Header("タブボタン")]
    [SerializeField] GameObject[] tabs;
    [Header("パネル")]
    [SerializeField] GameObject[] panels;

    [SerializeField] Button submitAnswerButton;
    [SerializeField] InputField answerInputField;
    private bool isAnswerPhase = false;
    private bool hasSubmittedAnswer = false;

    Transform parentTransform;


    private void Start()
    {
        backButton1.onClick.AddListener(() =>
        {
            PhotonManager.instance.OnLeaveRoomAndDestroy();
        });
        backButton2.onClick.AddListener(() =>
        {
            PhotonManager.instance.OnLeaveRoomAndDestroy();
        });

        parentTransform = tabs[0].transform.parent;

        RefreshAnswerUI();
    }

    public void Initialize()
    {
        // 初期化処理
        dengonGridGenerator.InitializeGridToggle();
        sizeChangerPanel.SetActive(false);
        colorSpectrum.SetActive(false);
        backPanel.SetActive(false);
        mekakushiToggle.isOn = false;
        DengonDrawingManager.instance.InitializeDrawField();
    }

    private void Update()
    {
        SetActive(blindPanel, isBlind);
        SetActive(penButtonCover, DengonDrawingManager.instance.currentMode == DengonDrawingManager.ToolMode.Pen);
        SetActive(fillButtonCover, DengonDrawingManager.instance.currentMode == DengonDrawingManager.ToolMode.Fill);
        SetActive(lineButtonCover, DengonDrawingManager.instance.currentMode == DengonDrawingManager.ToolMode.Line);
        SetActive(circleButtonCover, DengonDrawingManager.instance.currentMode == DengonDrawingManager.ToolMode.Circle);
        SetActive(rectangleButtonCover, DengonDrawingManager.instance.currentMode == DengonDrawingManager.ToolMode.Rectangle);

        SetInteractable(undoButton, DengonDrawingManager.instance.undoStackCount > 1);
        SetInteractable(redoButton, DengonDrawingManager.instance.redoStackCount > 0);
        SetInteractable(clearButton, DengonDrawingManager.instance.HasDrawing());

        currentColor.color = DengonDrawingManager.instance.drawColor;

        isBlind = mekakushiToggle.isOn;

        if (heightInputField.IsError || widthInputField.IsError)
        {
            SetInteractable(sizeApplyButton, false);
        }
        else
        {
            SetInteractable(sizeApplyButton, true);
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

    public void LockAnswerSubmission()
    {
        hasSubmittedAnswer = true;
        RefreshAnswerUI();
    }

    public string GetAnswerText()
    {
        return answerInputField != null ? answerInputField.text : "";
    }

    private void RefreshAnswerUI()
    {
        if (submitAnswerButton == null || answerInputField == null) return;

        if (hasSubmittedAnswer)
        {
            // 提出済み：編集不可＆ボタン不可
            submitAnswerButton.interactable = false;
            answerInputField.interactable = false;
            return;
        }

        // 未提出：回答フェーズ中だけ入力可能、かつ文字がある時だけ押せる
        bool hasText = !string.IsNullOrWhiteSpace(answerInputField.text);

        answerInputField.interactable = isAnswerPhase;
        submitAnswerButton.interactable = isAnswerPhase && hasText;
    }

    public void OnClickSizeApplyButton()
    {
        DengonDrawingManager.instance.ResetDrawFieldSize(widthInputField.inputPixelSize, heightInputField.inputPixelSize);
        if (DengonDrawingManager.instance.CanvasWidth > 50 || DengonDrawingManager.instance.CanvasHeight > 50)
        {
            dengonGridGenerator.ChangeInteractableGridToggle(false);
        }
        else
        {
            dengonGridGenerator.ChangeInteractableGridToggle(true);
        }
    }

    public void OnClickUndoButton()
    {
        DengonDrawingManager.instance.UndoButton();
    }

    public void OnClickRedoButton()
    {
        DengonDrawingManager.instance.RedoButton();
    }
    public void OnClickAllClearButton()
    {
        DengonDrawingManager.instance.AllClear();
    }

    public void ToggleIsDrawable()
    {
        DengonDrawingManager.instance.isDrawable = !DengonDrawingManager.instance.isDrawable;
    }

    // ツールボタン
    public void OnClickToolButton(int index)
    {
        switch (index)
        {
            case 0:
                DengonDrawingManager.instance.ChangeMode(DengonDrawingManager.ToolMode.Pen);
                break;
            case 1:
                DengonDrawingManager.instance.ChangeMode(DengonDrawingManager.ToolMode.Fill);
                break;
            case 2:
                DengonDrawingManager.instance.ChangeMode(DengonDrawingManager.ToolMode.Line);
                break;
            case 3:
                DengonDrawingManager.instance.ChangeMode(DengonDrawingManager.ToolMode.Circle);
                break;
            case 4:
                DengonDrawingManager.instance.ChangeMode(DengonDrawingManager.ToolMode.Rectangle);
                break;
        }
    }

    public void OnClickColorButton(int index)
    {
        switch (index)
        {
            case 0:
                DengonDrawingManager.instance.ChangeColor(Color.black);
                break;
            case 1:
                DengonDrawingManager.instance.ChangeColor(Color.white);
                break;
            case 2:
                DengonDrawingManager.instance.ChangeColor(Color.red);
                break;
            case 3:
                DengonDrawingManager.instance.ChangeColor(Color.blue);
                break;
            case 4:
                DengonDrawingManager.instance.ChangeColor(Color.green);
                break;
            case 5:
                DengonDrawingManager.instance.ChangeColor(Color.yellow);
                break;
            case 6:
                DengonDrawingManager.instance.ChangeColor(Color.magenta);
                break;
            case 7:
                DengonDrawingManager.instance.ChangeColor(Color.cyan);
                break;
            case 8:
                DengonDrawingManager.instance.ChangeColor(Color.gray);
                break;
            case 9:
                DengonDrawingManager.instance.ChangeColor(new Color32(246, 184, 148, 255));
                break;
        }
    }

    public void OnClickEraserButton()
    {
        DengonDrawingManager.instance.ChangeColor(new Color(0, 0, 0, 0));
    }

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
}