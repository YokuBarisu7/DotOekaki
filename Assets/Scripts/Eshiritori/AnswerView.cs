using UnityEngine;
using UnityEngine.UI;

public class AnswerView : MonoBehaviour
{
    [SerializeField] InputField inputField;
    [SerializeField] Text errorText;
    public System.Action<string> OnSubmitAnswer;


    private void Start()
    {
        // 1文字入力されるたびに検証して、ダメなら '\0' を返すと入力されない
        inputField.onValidateInput = ValidateKanaOnly;

        // IMEの漢字変換は InputField だけでは完全抑止できないので提出時チェックもする
        inputField.onValueChanged.AddListener(_ => ClearError());
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
    }

    public void CloseAnswerPanel()
    {
        gameObject.SetActive(false);
        inputField.text = string.Empty;
    }

    private char ValidateKanaOnly(string text, int charIndex, char addedChar)
    {
        if (IsAllowedKanaChar(addedChar))
            return addedChar;

        ShowError();
        return '\0'; // 入力を拒否
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
        if (string.IsNullOrEmpty(s)) return false; // 空欄を許すなら true に変えてOK
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
