using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EshiritoriManager : MonoBehaviourPunCallbacks
{
    public static EshiritoriManager instance;

    [SerializeField] EshiritoriDotUIManager dotUIManager;
    [SerializeField] TimerController timerController;
    [SerializeField] ImagePanelController imagePanelController;
    [SerializeField] AnswerView answerView;

    [SerializeField] int timeLimit;
    [SerializeField] int drawerNumber;
    [SerializeField] int[] drawerOrder;
    [SerializeField] int answererNumber;
    [SerializeField] int maxTurnNum;
    [SerializeField] int currentTurnNum = 0;
    [SerializeField] int answerTurnNum = 0;

    private string firstchar; // 最初の文字
    private string lastchar;　// 最後の文字
    private List<string> answerList = new List<string>(); // 回答のリスト(最後の正誤判定に使用)
    private bool gameStarted = false;

    private const string ROOM_FIRST = "firstChar";
    private const string ROOM_LAST = "lastChar";
    private const string ROOM_ORDER = "drawerOrder";
    private const string ROOM_ROUND = "roundId";
    private int appliedRoundId = -1;

    public event Action<bool> OnTurnStarted;


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
        answerView.OnSubmitAnswer += SetAnswer;

        if (PhotonNetwork.IsMasterClient)
        {
            EnsureRoomSetup();
        }
        TryApplyRoomSetup();
    }

    private void EnsureRoomSetup()
    {
        int nextRoundId = 0;
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (props.TryGetValue(ROOM_ROUND, out var rObj) && rObj is int r)
            nextRoundId = r + 1;

        string first = imagePanelController.GetRandomHiragana().ToString();
        string last = imagePanelController.GetRandomHiragana().ToString();

        var list = new List<int>();
        foreach (var p in PhotonNetwork.PlayerList)
        {
            list.Add(p.ActorNumber);
        }
        Shuffle(list);

        int n = list.Count;
        int[] twoRoundOrder = new int[n * 2];
        for (int i = 0; i < n; i++)
        {
            twoRoundOrder[i] = list[i];
            twoRoundOrder[i + n] = list[i];
        }

        var hash = new ExitGames.Client.Photon.Hashtable
        {
            [ROOM_ROUND] = nextRoundId,
            [ROOM_FIRST] = first,
            [ROOM_LAST] = last,
            [ROOM_ORDER] = twoRoundOrder
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }
    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        TryApplyRoomSetup();
    }

    private void TryApplyRoomSetup()
    {
        SetReady(false);

        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        if (!props.TryGetValue(ROOM_ROUND, out var roundObj)) return;
        if (!(roundObj is int roundId)) return;

        if (appliedRoundId == roundId) return;

        if (!props.TryGetValue(ROOM_FIRST, out var fObj)) return;
        if (!props.TryGetValue(ROOM_LAST, out var lObj)) return;
        if (!props.TryGetValue(ROOM_ORDER, out var oObj)) return;

        appliedRoundId = roundId;

        firstchar = (string)fObj;
        lastchar = (string)lObj;
        drawerOrder = (int[])oObj;

        ResetLocalGameState();

        // UI反映
        imagePanelController.SetHiragana(firstchar, lastchar);

        SetReady(true);
    }

    private void ResetLocalGameState()
    {
        gameStarted = false;
        currentTurnNum = 0;
        answerTurnNum = 0;
        answerList.Clear();

        imagePanelController.HideText();
        imagePanelController.ClearAllImageView();
        drawerNumber = 0;
        answererNumber = 0;
        dotUIManager.Initialize();
    }

    private void SetReady(bool ready)
    {
        var hash = new ExitGames.Client.Photon.Hashtable();
        hash["isReady"] = ready;
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    private void CheckAllReady()
    {
        if (!PhotonNetwork.IsMasterClient || gameStarted) return;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!IsTrue(p, "isReady")) return;
        }

        gameStarted = true;
        timeLimit = PlayerPrefs.GetInt("ShiritoriTime", 60);
        photonView.RPC("SyncTimeLimit", RpcTarget.All, timeLimit);
        photonView.RPC("GameStart", RpcTarget.All);
        photonView.RPC("TurnStart", RpcTarget.All);
    }
    private bool IsTrue(Player p, string key)
    {
        if (p.CustomProperties == null) return false;
        if (!p.CustomProperties.TryGetValue(key, out var v)) return false;
        return v is bool b && b;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("isReady"))
        {
            CheckAllReady();
        }
    }

    [PunRPC]
    private void SyncTimeLimit(int time)
    {
        timeLimit = time;
    }

    [PunRPC]
    private void GameStart()
    {
        Debug.Log("ゲーム開始");
        maxTurnNum = drawerOrder.Length;

        answerList.Clear();
        for (int i =  0; i < maxTurnNum; i++) 
        {
            answerList.Add("");
        }
        imagePanelController.ClearAllImageView();

        for (int i = 0; i < maxTurnNum; i++)
        {
            imagePanelController.CreateNewImageView();
        }
    }

    [PunRPC]
    private void TurnStart()
    {
        currentTurnNum++;
        Debug.Log("ターン" + currentTurnNum + "開始");
        // 出題者決定
        drawerNumber = drawerOrder[currentTurnNum - 1];
        // 出題者の役割を表示
        dotUIManager.SetRoleText(IsDrawer);
        dotUIManager.Initialize();
        EshiritoriDrawingManager.instance.InitializeDrawField();
        EshiritoriDrawingManager.instance.SetDrawable(IsDrawer);
        // タイマーをリセット
        timerController.StartTimer(timeLimit);

        OnTurnStarted?.Invoke(IsDrawer);
    }

    [PunRPC]
    private void TurnEnd()
    {
        answerTurnNum++;

        Texture2D texture = CopyTexture(EshiritoriDrawingManager.instance.texture);
        imagePanelController.SetTexture(texture, currentTurnNum);

        // 回答パネル表示
        if (IsDrawer)
        {
            answerView.OpenAnswerPanel();
        }

        // 回答者が回答する前にターンが終わってしまった場合、強制終了する
        if (PhotonNetwork.LocalPlayer.ActorNumber == answererNumber)
        {
            answerView.CloseAnswerPanel();
        }

        answererNumber = drawerOrder[currentTurnNum - 1];
    }

    private Texture2D CopyTexture(Texture2D texture)
    {
        var copy = new Texture2D(texture.width, texture.height, texture.format, false)
        {
            filterMode = FilterMode.Point
        };
        copy.SetPixels32(texture.GetPixels32());
        copy.Apply();
        return copy;
    }

    public void TimeUp()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        AdvanceNextTurn();
    }

    public void AdvanceNextTurn()
    {
        if (currentTurnNum >= maxTurnNum)
        {
            photonView.RPC("TurnEnd", RpcTarget.All);
            return;
        }
        photonView.RPC("TurnEnd", RpcTarget.All);
        photonView.RPC("TurnStart", RpcTarget.All);
    }

    public void SetAnswer(string answer)
    {
        int senderNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        int answerIndex = answerTurnNum - 1;

        photonView.RPC("SendAnswer", RpcTarget.All, answer, answerIndex, senderNumber);
    }

    [PunRPC]
    private void SendAnswer(string answer, int answerIndex, int senderNumber)
    {
        if (answerIndex < 0 || answerIndex >= answerList.Count) return;

        answerList[answerIndex] = answer;
        bool isMe = senderNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        string displayAnswer = isMe ? answer : new string('●', answer.Length); // 自分以外には伏字
        string senderName = PhotonNetwork.CurrentRoom.GetPlayer(senderNumber).NickName;
        imagePanelController.SetText(displayAnswer, senderName, answerIndex);

        if (PhotonNetwork.IsMasterClient && answerTurnNum >= maxTurnNum)
        {
            Debug.Log("ゲーム終了");
            dotUIManager.SetResultDisplayButton(true);
            photonView.RPC("FinishSetting", RpcTarget.All);
        }
    }

    [PunRPC]
    private void FinishSetting()
    { 
        timerController.StopTimer();
        EshiritoriDrawingManager.instance.SetDrawable(false);
    }

    private bool IsDrawer => PhotonNetwork.LocalPlayer.ActorNumber == drawerNumber;

    public void GameFinish()
    {
        photonView.RPC("ResultDisplay", RpcTarget.All);
    }

    [PunRPC]
    private void ResultDisplay()
    {
        gameStarted = false;
        answerList.Insert(0, firstchar);
        answerList.Add(lastchar);
        imagePanelController.DisplayResult(answerList);
    }

    public void RestartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        EnsureRoomSetup();
    }
}
