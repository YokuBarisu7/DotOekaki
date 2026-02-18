using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;

public class CooperateChatManager : MonoBehaviourPunCallbacks
{
    [SerializeField] InputField chatInputField;
    [SerializeField] ScrollRect chatScrollRect;

    [SerializeField] GameObject textPrefab;
    [SerializeField] Transform chatTransform;
    [SerializeField] ColorPalette textColors;

    int textColorIndex;

    private void Start()
    {
        textColorIndex = PlayerPrefs.GetInt("TextColorIndex", 0);
        textColorIndex = Mathf.Clamp(textColorIndex, 0, textColors.colors.Length - 1);
    }

    void Update()
    {
        // エンターキーまたはテンキーのエンターキーが押されたらメッセージを送信
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            string answer = chatInputField.text;
            if (!string.IsNullOrEmpty(answer))
            {
                string senderName = PhotonNetwork.LocalPlayer.NickName; // 自分の名前取得
                photonView.RPC("SendChatMessage", RpcTarget.All, answer, senderName, textColorIndex);
                chatInputField.text = ""; // チャット入力欄をリセット

                // 出題者以外の場合は回答を提出
                if (PhotonNetwork.LocalPlayer.ActorNumber != CooperateGameManager.instance.QuestionerNumbers[0] && PhotonNetwork.LocalPlayer.ActorNumber != CooperateGameManager.instance.QuestionerNumbers[1])
                {
                    CooperateGameManager.instance.SubmitAnswer(answer);
                }
            }
            // チャット入力欄にフォーカスを移す
            chatInputField.Select();
            chatInputField.ActivateInputField();
        }
    }

    [PunRPC]
    private void SendChatMessage(string message, string senderName, int index)
    {
        GameObject newTextObject = Instantiate(textPrefab, chatTransform);
        Text newText = newTextObject.GetComponent<Text>();

        newText.text = $"{senderName}: {message}";
        index = Mathf.Clamp(index, 0, textColors.colors.Length - 1);
        newText.color = textColors.colors[index]; // テキストの色を設定

        Canvas.ForceUpdateCanvases();
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null; // 次のフレームまで待機
        chatScrollRect.verticalNormalizedPosition = 0f; // スクロールを最下部に移動
    }
}
