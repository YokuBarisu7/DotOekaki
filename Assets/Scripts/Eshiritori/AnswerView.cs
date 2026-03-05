using UnityEngine;
using UnityEngine.UI;

public class AnswerView : MonoBehaviour
{
    [SerializeField] InputField inputField;
    [SerializeField] Text errorText;
    [SerializeField] Button submitButton;
    public System.Action<string> OnSubmitAnswer;


    private void Start()
    {
        inputField.onValueChanged.AddListener(OnInputChanged);
    }

    public void OnSubmit()
    {
        // 入力されたテキストを取得
        string inputText = inputField.text;
        if (!IsKanaOnly(inputText)) return;
        OnSubmitAnswer?.Invoke(inputText);
        // 入力フィールドをクリア
        inputField.text = string.Empty;
        CloseAnswerPanel();
    }

    public void OpenAnswerPanel()
    {
        gameObject.SetActive(true);
        ClearError();
        inputField.ActivateInputField();
    }

    public void CloseAnswerPanel()
    {
        gameObject.SetActive(false);
        inputField.text = string.Empty;
        ClearError();
    }

    private void OnInputChanged(string text)
    {
        bool valid = IsKanaOnly(text);
        submitButton.interactable = valid;

        if (valid)
            ClearError();
        else
            ShowError();
    }

    public static bool IsAllowedKanaChar(char c)
    {
        // ひらがな
        if (c >= 'ぁ' && c <= 'ゖ') return true;

        // カタカナ（ァ〜ヺあたりまで広めに許可）
        if (c >= 'ァ' && c <= 'ヺ') return true;

        // 長音（必要なら許可）
        if (c == 'ー') return true;

        return false;
    }

    public static bool IsKanaOnly(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (char c in s)
        {
            if (!IsAllowedKanaChar(c)) return false;
        }
        return true;
    }

    private void ShowError()
    {
        if (!errorText) return;
        errorText.gameObject.SetActive(true);
    }

    private void ClearError()
    {
        if (!errorText) return;
        errorText.gameObject.SetActive(false);
    }
}
