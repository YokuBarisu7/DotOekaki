using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class EshiritoriManager : MonoBehaviourPunCallbacks
{
    public static EshiritoriManager instance;

    private int questionerNumber = 0;
    EshiritoriDotUIManager dotUIManager;

    [SerializeField] TimerController timerController;
    [SerializeField] ImagePanelController imagePanelController;
    [SerializeField] AnsweView answerView;

    [SerializeField] GameObject finishButton;

    private int questionCount;
    private List<string> answers = new List<string>();
    private int maxTurnNum = 4;
    private int currentTurnNum = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 設定
        dotUIManager = FindAnyObjectByType<EshiritoriDotUIManager>();
        questionCount = 100;
        answerView.OnSubmitAnswer = SetAnswer;

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("GameStart", RpcTarget.All);
            photonView.RPC("TurnStart", RpcTarget.All);
        }
    }

    [PunRPC]
    private void GameStart()
    {
        Debug.Log("ゲーム開始");
        imagePanelController.CreateHiragana("か", false);
        for (int i = 0; i < maxTurnNum; i++)
        {
            Texture2D texture = CopyTexture(EshiritoriDrawingManager.instance.texture);
            imagePanelController.CreateNewImage(texture);
        }
        imagePanelController.CreateHiragana("え",true);
    }

    [PunRPC]
    private void TurnStart()
    {
        Debug.Log("ターン開始");
        currentTurnNum++;
        Debug.Log("ターン" + currentTurnNum + "開始");
        // タイマーをリセット
        timerController.ResetTimer();
        // 出題者決定
        questionerNumber = GetNextQuestioner();
        // 出題者の役割を表示
        dotUIManager.SetRoleText(IsQuestioner());
        EshiritoriDrawingManager.instance.ResetDrawField();
        if (PhotonNetwork.LocalPlayer.ActorNumber == questionerNumber)
        {
            EshiritoriDrawingManager.instance.isDrawable = true;
        }
        else
        {
            EshiritoriDrawingManager.instance.isDrawable = false;
        }
    }

    [PunRPC]
    private void TurnEnd(int questionerNumber)
    {
        Texture2D texture = CopyTexture(EshiritoriDrawingManager.instance.texture);
        imagePanelController.SetTexture(texture, currentTurnNum);
        // 回答パネル表示
        if (IsQuestioner())
        {
            answerView.OpenAnswerPanel();
        }
    }

    private void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (answers.Count >= maxTurnNum)
                {
                    //Debug.Log("ゲーム終了");
                    finishButton.SetActive(true);
                    return;
                }
                if (timerController.GetRemainingTime() <= 0)
                {
                    if (currentTurnNum >= maxTurnNum)
                    {
                        photonView.RPC("TurnEnd", RpcTarget.All, questionerNumber);
                        return;
                    }
                    photonView.RPC("TurnEnd", RpcTarget.All, questionerNumber);
                    photonView.RPC("TurnStart", RpcTarget.All);
                }
            }
        }
    }

    private Texture2D CopyTexture(Texture2D texture)
    {
        Texture2D copy = new Texture2D(texture.width, texture.height, texture.format, false);
        copy.SetPixels(texture.GetPixels());
        copy.Apply();
        return copy;
    }

    private int GetNextQuestioner()
    {
        if (questionerNumber == 0)
        {
            return 1;
        }
        if (questionerNumber == PhotonNetwork.PlayerList.Length)
        {
            return 1;
        }
        return questionerNumber + 1;
    }

    // 前の出題者の番号
    public int GetPreviousQuestionerNumber()
    {
        if (questionerNumber == 1)
        {
            return PhotonNetwork.PlayerList.Length;
        }
        return questionerNumber - 1;
    }

    public void SetAnswer(string answer) 
    {
        int senderNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        photonView.RPC("SendAnswer", RpcTarget.All, answer, senderNumber);
    }

    [PunRPC]
    private void SendAnswer(string answer, int senderNumber)
    {
        answers.Add(answer);
        bool isSender = senderNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        string displayAnswer = isSender ? answer : new string('●', answer.Length);
        imagePanelController.SetText(displayAnswer, answers.Count);
    }

    public void OnClickTurnEnd()
    {
        if (currentTurnNum >= maxTurnNum)
        {
            photonView.RPC("TurnEnd", RpcTarget.All, questionerNumber);
            return;
        }
        photonView.RPC("TurnEnd", RpcTarget.All, questionerNumber);
        photonView.RPC("TurnStart", RpcTarget.All);
    }

    private bool IsQuestioner()
    {
        return questionerNumber == PhotonNetwork.LocalPlayer.ActorNumber;
    }

    [PunRPC]
    private void GameEnd()
    {
        answers.Insert(0, "か");
        answers.Add("え");
        imagePanelController.DisplayResult(answers);

    }

    public void OnClickFinish()
    {
        photonView.RPC("GameEnd", RpcTarget.All);
    }
}
