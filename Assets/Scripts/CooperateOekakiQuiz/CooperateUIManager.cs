using UnityEngine;
using UnityEngine.UI;

public class CooperateUIManager : MonoBehaviour
{
    [SerializeField] CooperateGridGenerator cooperateGridGenerator;

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

    [SerializeField] bool isBlind;

    [SerializeField] GameObject sizeChangerPanel;
    [SerializeField] GameObject colorSpectrum;
    [SerializeField] GameObject backPanel;


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
    }

    public void Initialize()
    {
        // 初期化処理
        cooperateGridGenerator.InitializeGridToggle();
        sizeChangerPanel.SetActive(false);
        colorSpectrum.SetActive(false);
        backPanel.SetActive(false);
        mekakushiToggle.isOn = false;
        CooperateDrawingManager.instance.InitializeDrawField();
    }

    private void Update()
    {
        SetActive(dotUI, CooperateDrawingManager.instance.isDrawable);
        SetActive(blindPanel, isBlind);
        SetActive(penButtonCover, CooperateDrawingManager.instance.currentMode == CooperateDrawingManager.ToolMode.Pen);
        SetActive(fillButtonCover, CooperateDrawingManager.instance.currentMode == CooperateDrawingManager.ToolMode.Fill);
        SetActive(lineButtonCover, CooperateDrawingManager.instance.currentMode == CooperateDrawingManager.ToolMode.Line);
        SetActive(circleButtonCover, CooperateDrawingManager.instance.currentMode == CooperateDrawingManager.ToolMode.Circle);
        SetActive(rectangleButtonCover, CooperateDrawingManager.instance.currentMode == CooperateDrawingManager.ToolMode.Rectangle);

        currentColor.color = CooperateDrawingManager.instance.drawColor;

        isBlind = mekakushiToggle.isOn;

        if (heightInputField.IsError || widthInputField.IsError)
        {
            SetInteractable(sizeApplyButton, false);
        }
        else
        {
            SetInteractable(sizeApplyButton, true);
        }

        if (cooperateCountInputField.IsError || cooperateTimeInputField.IsError)
        {
            SetInteractable(gameRestartButton, false);
        }
        else
        {
            SetInteractable(gameRestartButton, true);
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
        if (CooperateDrawingManager.instance.CanvasWidth > 50 || CooperateDrawingManager.instance.CanvasHeight > 50)
        {
            cooperateGridGenerator.ChangeInteractableGridToggle(false);
        }
        else
        {
            cooperateGridGenerator.ChangeInteractableGridToggle(true);
        }
    }

    public void ToggleIsDrawable()
    {
        CooperateDrawingManager.instance.isDrawable = !CooperateDrawingManager.instance.isDrawable;
    }

    // ツールボタン
    public void OnClickToolButton(int index)
    {
        switch (index)
        {
            case 0:
                CooperateDrawingManager.instance.ChangeMode(CooperateDrawingManager.ToolMode.Pen);
                break;
            case 1:
                CooperateDrawingManager.instance.ChangeMode(CooperateDrawingManager.ToolMode.Fill);
                break;
            case 2:
                CooperateDrawingManager.instance.ChangeMode(CooperateDrawingManager.ToolMode.Line);
                break;
            case 3:
                CooperateDrawingManager.instance.ChangeMode(CooperateDrawingManager.ToolMode.Circle);
                break;
            case 4:
                CooperateDrawingManager.instance.ChangeMode(CooperateDrawingManager.ToolMode.Rectangle);
                break;
        }
    }

    public void OnClickBlack()
    {
        CooperateDrawingManager.instance.ChangeColor(Color.black);
    }
    public void OnClickRed()
    {
        CooperateDrawingManager.instance.ChangeColor(Color.red);
    }
    public void OnClickBlue()
    {
        CooperateDrawingManager.instance.ChangeColor(Color.blue);
    }
    public void OnClickGreen()
    {
        CooperateDrawingManager.instance.ChangeColor(Color.green);
    }
    public void OnClickYellow()
    {
        CooperateDrawingManager.instance.ChangeColor(Color.yellow);
    }
    public void OnClickMagenta()
    {
        CooperateDrawingManager.instance.ChangeColor(Color.magenta);
    }
    public void OnClickCyan()
    {
        CooperateDrawingManager.instance.ChangeColor(Color.cyan);
    }
    public void OnClickGray()
    {
        CooperateDrawingManager.instance.ChangeColor(Color.gray);
    }
    public void OnClickBeige()
    {
        CooperateDrawingManager.instance.ChangeColor(new Color32(246, 184, 148, 255));
    }
    public void OnClickWhite()
    {
        CooperateDrawingManager.instance.ChangeColor(Color.white);
    }
    public void OnClickEraserButton()
    {
        CooperateDrawingManager.instance.ChangeColor(new Color(0, 0, 0, 0));
    }
}
