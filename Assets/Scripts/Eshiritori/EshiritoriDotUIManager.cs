using UnityEngine;
using UnityEngine.UI;

public class EshiritoriDotUIManager : MonoBehaviour
{

    [SerializeField] GameObject dotUI;
    [SerializeField] Button undoButton;
    [SerializeField] Button redoButton;
    [SerializeField] Button clearButton;
    [SerializeField] Button backButton;
    [SerializeField] GameObject penButtonCover;
    [SerializeField] GameObject eraserButtonCover;
    [SerializeField] Text roleText;
    [SerializeField] Text themeText;

    private void Start()
    {
        backButton.onClick.AddListener(() =>
        {
            PhotonManager.instance.OnLeaveRoomAndDestroy();
        });
    }

    private void Update()
    {
        SetActive(dotUI, EshiritoriDrawingManager.instance.isDrawable);
        SetActive(penButtonCover, EshiritoriDrawingManager.instance.currentMode == EshiritoriDrawingManager.ToolMode.Pen);
        SetActive(eraserButtonCover, EshiritoriDrawingManager.instance.currentMode == EshiritoriDrawingManager.ToolMode.Eraser);
        SetInteractable(undoButton, EshiritoriDrawingManager.instance.undoStackCount > 1);
        SetInteractable(redoButton, EshiritoriDrawingManager.instance.redoStackCount > 0);
        SetInteractable(clearButton, EshiritoriDrawingManager.instance.HasDrawing());
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

    public void SetRoleText(bool isQuestioner)
    {
        roleText.text = isQuestioner == true ? "あなたは描き手です" : "あなたは回答者です";
    }

    public void OnClickUndoButton()
    {
        EshiritoriDrawingManager.instance.UndoButton();
    }

    public void OnClickRedoButton()
    {
        EshiritoriDrawingManager.instance.RedoButton();
    }

    public void ToggleIsDrawable()
    { 
        EshiritoriDrawingManager.instance.isDrawable = !EshiritoriDrawingManager.instance.isDrawable;
    }
    public void OnClickPenButton()
    {
        EshiritoriDrawingManager.instance.ChangeMode(EshiritoriDrawingManager.ToolMode.Pen);
        EshiritoriDrawingManager.instance.ChangeColor(Color.black);
    }
    public void OnClickEraserButton()
    {
        EshiritoriDrawingManager.instance.ChangeMode(EshiritoriDrawingManager.ToolMode.Eraser);
        EshiritoriDrawingManager.instance.ChangeColor(new Color(0, 0, 0, 0));
    }

    public void OnClickAllClearButton()
    {
        EshiritoriDrawingManager.instance.AllClear();
    }
}
