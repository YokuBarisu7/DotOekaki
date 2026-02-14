using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DengonGameManager : MonoBehaviourPunCallbacks
{
    public static DengonGameManager instance;

    [SerializeField] DengonUIManager dengonUIManager;
    [SerializeField] DengonShowPanelManager dengonShowPanelManager;
    [SerializeField] DengonThemeGenerator dengonThemeGenerator;
    DengonTheme myTheme;

    [Header("UIパーツ")]
    [SerializeField] GameObject panels;
    [SerializeField] GameObject loadObj;
    [SerializeField] Text themeText;
    [SerializeField] Text drawingTimerText;
    [SerializeField] Text answerTimerText; 
    [SerializeField] Text submitDrawingButtonText;
    [SerializeField] Text submitAnswerButtonText;

    [Header("ゲームパラメータ")]
    [SerializeField] int currentRound; // 現在のラウンド数
    [SerializeField] float timeRemaining;
    [SerializeField] bool isDrawingPhase; // お絵かきフェーズか回答フェーズかどうか
    [SerializeField] bool isGameFinished;

    public int drawingTime; // お絵かき時間
    public int answerTime; // 回答時間
    private Coroutine timerCoroutine;
    private bool timerStopped;
    private bool gameStarted;
    List<int> myOrderList; // 自分の順番リスト
    int currentOwnerID;

    private readonly HashSet<int> drawingReadyPlayers = new HashSet<int>();
    private readonly HashSet<int> answerReadyPlayers = new HashSet<int>();

    private bool myDrawingReady;
    private bool myAnswerReady;

    private bool phaseTransitioning;
    

    [SerializeField] Text debugText;

    private void Awake()
    {
        if (instance == null) instance = this;

        if (!dengonUIManager) dengonUIManager = FindAnyObjectByType<DengonUIManager>();
        if (!dengonShowPanelManager) dengonShowPanelManager = FindAnyObjectByType<DengonShowPanelManager>();
    }

    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            currentOwnerID = PhotonNetwork.LocalPlayer.ActorNumber;

            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("SetLoadObj", RpcTarget.All, true);

                drawingTime = PlayerPrefs.GetInt("DengonTime", 180);
                answerTime = PlayerPrefs.GetInt("DengonAnswerTime", 60);
                photonView.RPC("SyncTimeOption", RpcTarget.All, drawingTime, answerTime); // 制限時間の同期


                var actors = PhotonNetwork.PlayerList.Select(p => p.ActorNumber).OrderBy(x => x).ToList();

                Dictionary<int, List<int>> playerOrder = GenerateOrder(actors);

                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    int playerId = player.ActorNumber;

                    if (!playerOrder.TryGetValue(playerId, out var orderList))
                    {
                        Debug.LogError($"Order not found for actor {playerId}. actors=[{string.Join(",", actors)}]");
                        continue;
                    }

                    string orderJson = JsonUtility.ToJson(new SerializableOrderList(orderList));
                    photonView.RPC("ReceiveOrderList", player, orderJson);
                }

                // デバッグ用に自分の順番リストを表示
                var lines = new[] { $"自分の番号：{currentOwnerID}", $"順番:{playerOrder[PhotonNetwork.LocalPlayer.ActorNumber]}" };
                debugText.text += "\n" + string.Join("\n", lines);

                restartButton.SetActive(true); // リザルト画面の設定再利用ボタンを表示
                photonView.RPC("ShowTabPanel", RpcTarget.All, PhotonNetwork.CurrentRoom.PlayerCount); // プレイヤーの人数に応じて結果タブを表示
            }
        }
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient && !gameStarted)
        {
            CheckAllPlayerReady();
        }
    }

    private void CheckAllPlayerReady()
    {
        if (gameStarted) return;

        int roomVer = 0;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("DengonThemeVersion", out var vObj) && vObj is int v)
            roomVer = v;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            bool readyA = player.CustomProperties.TryGetValue("OrderReady", out var oa) && oa is bool oab && oab;

            bool received = player.CustomProperties.TryGetValue("ThemeReceived", out var tr) && tr is bool trb && trb;
            bool verOk = player.CustomProperties.TryGetValue("ThemeVersion", out var tv) && tv is int tvi && tvi == roomVer;

            bool readyB = received && verOk;
            if (!readyA || !readyB) return;
        }

        gameStarted = true;
        StartGame();
    }

    private void SetOrderReady(bool isReady)
    {
        var props = new ExitGames.Client.Photon.Hashtable
        {
            { "OrderReady", isReady }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        debugText.text += $"\n順番リスト：準備完了";
    }

    private static Dictionary<int, List<int>> GenerateOrder(List<int> actors)
    {
        int n = actors.Count;
        int[,] latin = GenerateLatinSquare(n);

        // rows: インデックス行（0..n-1）
        var rows = new List<List<int>>(n);
        for (int r = 0; r < n; r++)
        {
            var row = new List<int>(n + (n % 2 == 1 ? 1 : 0));
            for (int c = 0; c < n; c++)
            {
                int actor = actors[latin[r, c] - 1]; // 1..n -> actorNumber
                row.Add(actor);
            }
            rows.Add(row);
        }

        // 「各行の先頭actor」でソート
        rows = rows.OrderBy(r => r[0]).ToList();

        // 奇数なら末尾に先頭を追加（あなたのルール維持）
        if (n % 2 == 1)
        {
            foreach (var row in rows) row.Add(row[0]);
        }

        // Dictionary: key=先頭actor, value=順番リスト
        var result = new Dictionary<int, List<int>>(n);
        foreach (var row in rows)
        {
            result[row[0]] = row;
        }
        return result;
    }

    // ランダムラテン方陣生成 (バックトラッキング)
    private static int[,] GenerateLatinSquare(int n)
    {
        int[,] square = new int[n, n];
        bool[,] rowUsed = new bool[n, n + 1];
        bool[,] colUsed = new bool[n, n + 1];

        System.Random rand = new System.Random();

        if (!Fill(square, rowUsed, colUsed, 0, 0, n, rand))
        {
            Debug.LogError("Failed to generate Latin Square!");
        }

        return square;
    }

    private static bool Fill(int[,] square, bool[,] rowUsed, bool[,] colUsed, int r, int c, int n, System.Random rand)
    {
        if (r == n) return true;

        int nextR = (c == n - 1) ? r + 1 : r;
        int nextC = (c == n - 1) ? 0 : c + 1;

        // 候補をシャッフル
        List<int> candidates = Enumerable.Range(1, n).ToList();
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        foreach (int num in candidates)
        {
            if (!rowUsed[r, num] && !colUsed[c, num])
            {
                square[r, c] = num;
                rowUsed[r, num] = true;
                colUsed[c, num] = true;

                if (Fill(square, rowUsed, colUsed, nextR, nextC, n, rand)) return true;

                // 戻す
                rowUsed[r, num] = false;
                colUsed[c, num] = false;
            }
        }
        return false;
    }

    [PunRPC]
    private void ReceiveOrderList(string orderJson)
    {
        SerializableOrderList order = JsonUtility.FromJson<SerializableOrderList>(orderJson);
        myOrderList = order.orderList;
        var lines = new[] {$"自分の順番：{currentOwnerID}", $"順番リスト: {string.Join(",", myOrderList)}" };
        debugText.text += "\n" + string.Join("\n", lines);
        SetOrderReady(true); // 自分の順番リストを受け取ったら準備完了状態にする
    }

    [System.Serializable]
    public class SerializableOrderList
    {
        public List<int> orderList;
        public SerializableOrderList(List<int> list)
        {
            orderList = list;
        }
    }

    public void SetTheme(string theme)
    {
        if (myTheme == null)
        {
            myTheme = new DengonTheme();
        }
        myTheme.theme = theme;
        UpdateThemeText();
    }

    private void StartGame()
    {
        if (myTheme == null || string.IsNullOrWhiteSpace(myTheme.theme))
        {
            // まだテーマが入ってないなら待つ（次フレームに再評価される）
            gameStarted = false;
            return;
        }

        photonView.RPC("SetGamePlay", RpcTarget.All);
        photonView.RPC("UpdateThemeText", RpcTarget.All);
        photonView.RPC("SetLoadObj", RpcTarget.All, false);
        photonView.RPC("StartTimerRPC", RpcTarget.All, PhotonNetwork.Time, drawingTime, true);
    }


    // ゲームプレイ中

    [PunRPC]
    private void StartTimerRPC(double startTime, int time, bool nextIsDrawingPhase)
    {
        if (isGameFinished) return;

        timerStopped = false;
        phaseTransitioning = false;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        isDrawingPhase = nextIsDrawingPhase; // フェーズ切り替え
        dengonUIManager.SetAnswerPhase(!isDrawingPhase);
        timeRemaining = time;

        myDrawingReady = false;
        myAnswerReady = false;
        UpdateReadyButtonUI();

        dengonUIManager.SetAnswerLocked(false);

        if (PhotonNetwork.IsMasterClient)
        {
            if (isDrawingPhase)
            {
                drawingReadyPlayers.Clear(); // お絵かきフェーズ開始時にお絵かき完了プレイヤーリストをリセット
            }
            else
            {
                answerReadyPlayers.Clear(); // 回答フェーズ開始時に回答完了プレイヤーリストをリセット
            }
        }

        timerCoroutine = StartCoroutine(TimerCoroutine((float)startTime, time));

    }

    private IEnumerator TimerCoroutine(float startTime, int time)
    {
        double endTime = startTime + time;
        while (timeRemaining > 0 && !timerStopped)
        {
            timeRemaining = (float)(endTime - PhotonNetwork.Time);
            if (timeRemaining < 0) timeRemaining = 0;
            if (isDrawingPhase)
            {
                drawingTimerText.text = $"残り\n{Mathf.CeilToInt(timeRemaining).ToString()} 秒";
            }
            else
            {
                answerTimerText.text = $"残り\n{Mathf.CeilToInt(timeRemaining).ToString()} 秒";
            }
            yield return null;
        }

        timerCoroutine = null;

        if (timerStopped) yield break; // タイマー停止中なら終了

        timerStopped = true;

        if (PhotonNetwork.IsMasterClient)
        {
            if (isDrawingPhase)
            {
                DrawRoundFinished();
            }
            else
            {
                AnswerRoundFinished();
            }
        }
    }

    [PunRPC]
    private void SyncTimeOption(int time1, int time2)
    {
        drawingTime = time1;
        answerTime = time2;
    }

    public void OnClickSubmitDrawingButton()
    {
        if (!isDrawingPhase || isGameFinished) return;

        myDrawingReady = !myDrawingReady;
        UpdateReadyButtonUI();
        photonView.RPC("SetDrawingReady", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, myDrawingReady);
    }
    public void OnClickSubmitAnswerButton()
    {
        if (isDrawingPhase || isGameFinished) return;

        // OFF→ONにする時だけ空欄チェック
        if (!myAnswerReady)
        {
            string answer = dengonUIManager.GetAnswerText();
            if (string.IsNullOrWhiteSpace(answer)) return;
        }

        myAnswerReady = !myAnswerReady;
        UpdateReadyButtonUI();

        // ★ON中は入力ロック、OFFで解除
        dengonUIManager.SetAnswerLocked(myAnswerReady);

        photonView.RPC("SetAnswerReady", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, myAnswerReady);
    }

    [PunRPC]
    private void SetDrawingReady(int actorNumber, bool isReady)
    {
        if (!PhotonNetwork.IsMasterClient || !isDrawingPhase || isGameFinished) return;

        if (isReady) drawingReadyPlayers.Add(actorNumber);
        else drawingReadyPlayers.Remove(actorNumber);

        TryAdvanceIfAllReady();
    }

    [PunRPC]
    private void SetAnswerReady(int actorNumber, bool isReady)
    {
        if (!PhotonNetwork.IsMasterClient || isDrawingPhase || isGameFinished) return;

        if (isReady) answerReadyPlayers.Add(actorNumber);
        else answerReadyPlayers.Remove(actorNumber);

        TryAdvanceIfAllReady();
    }

    private void TryAdvanceIfAllReady()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (phaseTransitioning) return;
        if (timerStopped) return;

        int needed = PhotonNetwork.PlayerList.Length;

        if (isDrawingPhase)
        {
            if (drawingReadyPlayers.Count >= needed)
            {
                ForceFinishDrawingPhaseEarly();
            }
        }
        else
        {
            if (answerReadyPlayers.Count >= needed)
            {
                ForceFinishAnswerPhaseEarly();
            }
        }
    }

    private void UpdateReadyButtonUI()
    {
        if (submitDrawingButtonText)
            submitDrawingButtonText.text = myDrawingReady ? "✓ 完了（解除）" : "完了";

        if (submitAnswerButtonText)
            submitAnswerButtonText.text = myAnswerReady ? "✓ 提出（解除）" : "提出";
    }

    [PunRPC]
    private void SetGamePlay()
    {
        if (currentRound == 0 && myTheme != null && !string.IsNullOrWhiteSpace(myTheme.theme))
        {
            photonView.RPC("ReceiveSavedAnswer", RpcTarget.MasterClient, myTheme.theme, currentOwnerID);
        }
        currentRound++;
        dengonUIManager.Initialize(); // UIリセット

        dengonUIManager.ResetAnswerSubmission();
    }

    [PunRPC]
    private void UpdateThemeText()
    {
        string t = (myTheme != null) ? myTheme.theme : "";
        themeText.text = $"お題：{t}";
    }

    [PunRPC]
    private void SetLoadObj(bool isActive)
    {
        loadObj.SetActive(isActive);
    }

    [PunRPC]
    private void MovePanel(Vector2 pos)
    {
        panels.transform.localPosition = pos;
    }

    private void DrawRoundFinished()
    {
        if (phaseTransitioning) return;
        phaseTransitioning = true;
        DrawRoundFinished_Internal();
    }

    private void DrawRoundFinished_Internal()
    {
        photonView.RPC("SendPicture", RpcTarget.All);
        photonView.RPC("MovePanel", RpcTarget.All, new Vector2(-2000, 0));
        photonView.RPC("StartTimerRPC", RpcTarget.All, PhotonNetwork.Time, answerTime, false);
    }

    private void ForceFinishDrawingPhaseEarly()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (phaseTransitioning) return;

        phaseTransitioning = true;
        timerStopped = true;

        photonView.RPC("StopCurrentTimer", RpcTarget.All);

        // ここは直接遷移処理を呼ぶ（ガードで止まらないように）
        DrawRoundFinished_Internal();
    }

    [PunRPC]
    private void SendPicture()
    {
        int nextActorNumber = myOrderList[currentRound];
        Photon.Realtime.Player nextPlayer = PhotonNetwork.CurrentRoom.GetPlayer(nextActorNumber);
        byte[] pngBytes = DengonDrawingManager.instance.GetPngBytes();
        photonView.RPC("ReceiveDrawing", nextPlayer, pngBytes, currentOwnerID, PhotonNetwork.LocalPlayer.ActorNumber);
        photonView.RPC("ReceiveSavedPicture", RpcTarget.MasterClient, pngBytes, currentOwnerID);
    }

    [PunRPC]
    private void ReceiveDrawing(byte[] imgBytes, int ownerID, int fromActorNumber)
    {
        currentOwnerID = ownerID;
        Texture2D receivedTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        receivedTex.filterMode = FilterMode.Point;
        receivedTex.LoadImage(imgBytes, true);

        // 受信画像を表示
        dengonShowPanelManager.SetShowPanel(receivedTex);
        Photon.Realtime.Player fromPlayer = PhotonNetwork.CurrentRoom.GetPlayer(fromActorNumber);
        Photon.Realtime.Player nextPlayer = PhotonNetwork.CurrentRoom.GetPlayer(myOrderList[currentRound]);
        dengonShowPanelManager.SetFromText($"{fromPlayer.NickName}");
        dengonShowPanelManager.SetToText($"{nextPlayer.NickName}");
        dengonShowPanelManager.CreateGridTexture();
    }

    private void AnswerRoundFinished()
    {
        if (phaseTransitioning) return;
        phaseTransitioning = true;
        AnswerRoundFinished_Internal();
    }

    private void AnswerRoundFinished_Internal()
    {
        photonView.RPC("SendAnswer", RpcTarget.All);

        if (currentRound == (PhotonNetwork.CurrentRoom.PlayerCount + 1) / 2)
        {
            GameFinished();
            return;
        }

        photonView.RPC("MovePanel", RpcTarget.All, new Vector2(0, 0));
        photonView.RPC("SetGamePlay", RpcTarget.All);
        photonView.RPC("StartTimerRPC", RpcTarget.All, PhotonNetwork.Time, drawingTime, true);
    }

    private void ForceFinishAnswerPhaseEarly()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (phaseTransitioning) return;

        phaseTransitioning = true;
        timerStopped = true;

        photonView.RPC("StopCurrentTimer", RpcTarget.All);

        // ここは直接遷移処理を呼ぶ（ガードで止まらないように）
        AnswerRoundFinished_Internal();
    }

    [PunRPC]
    private void StopCurrentTimer()
    {
        timerStopped = true;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        timeRemaining = 0;
    }

    [PunRPC]
    private void SendAnswer()
    {
        int nextActorNumber = myOrderList[currentRound];
        Player nextPlayer = PhotonNetwork.CurrentRoom.GetPlayer(nextActorNumber);

        string answer = dengonUIManager.GetAnswerText();
        photonView.RPC("ReceiveAnswer", nextPlayer, answer, currentOwnerID);
        photonView.RPC("ReceiveSavedAnswer", RpcTarget.MasterClient, answer, currentOwnerID);
    }

    [PunRPC]
    private void ReceiveAnswer(string answer, int ownerID)
    {
        currentOwnerID = ownerID;
        SetTheme(answer);
        UpdateThemeText();
    }

    [PunRPC]
    private void ReceiveSavedPicture(byte[] pngBytes, int ownerID, PhotonMessageInfo info)
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.LoadImage(pngBytes);

        pictureEntries.Add(new PictureEntry
        {
            ownerID = ownerID,
            actorNumber = info.Sender.ActorNumber,
            nickName = info.Sender.NickName,
            texture = tex,
            sentTime = info.SentServerTime
        });
    }

    [PunRPC]
    private void ReceiveSavedAnswer(string answer, int ownerID, PhotonMessageInfo info)
    {
        answerEntries.Add(new AnswerEntry
        {
            ownerID = ownerID,
            actorNumber = info.Sender.ActorNumber,
            nickName = info.Sender.NickName,
            answer = answer,
            sentTime = info.SentServerTime
        });
    }


    // リザルト関連
    [Header("リザルト関連")]
    [SerializeField] Transform[] tabContents; //結果表示用の親オブジェクト
    [SerializeField] GameObject dengonPicturePrefab; //結果表示用のプレハブ
    [SerializeField] GameObject dengonTextPrefab; // 結果表示用の画像プレハブ
    [SerializeField] GameObject arrowPrefab; // 矢印プレハブ

    [System.Serializable]
    public class PictureEntry
    {
        public int ownerID;
        public int actorNumber;
        public string nickName;
        public Texture2D texture;
        public double sentTime;
    }

    [System.Serializable]
    public class AnswerEntry
    {
        public int ownerID;
        public int actorNumber;
        public string nickName;
        public string answer;
        public double sentTime;
    }

    [System.Serializable]
    public class Serialization<T>
    {
        public List<T> items;
        public Serialization(List<T> items)
        {
            this.items = items;
        }
    }

    private List<RectTransform> GetActiveTabs(bool useHierarchy = true)
    {
        return tabContents
        .Where(t => t != null)
        .Take(PhotonNetwork.CurrentRoom.PlayerCount)
        .Select(t => (RectTransform)t)
        .ToList();
    }

    private List<PictureEntry> pictureEntries = new List<PictureEntry>();
    private List<AnswerEntry> answerEntries = new List<AnswerEntry>();

    private void GameFinished()
    {
        Debug.Log("ゲーム終了");
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(ResultCountdownCoroutine());   
        }
    }

    [PunRPC]
    private void ShowCountdownNumber(string text)
    { 
        dengonUIManager.ShowCountdown(text);
        dengonUIManager.SetGameFinished(true);
    }

    private IEnumerator ResultCountdownCoroutine()
    { 
        for (int i = 3; i > 0; i--)
        {
            photonView.RPC("ShowCountdownNumber", RpcTarget.All, $"ゲーム終了\n結果発表まで{i}...");
            yield return new WaitForSeconds(1f);
        }
        yield return new WaitForSeconds(0.5f);
        photonView.RPC("ShowCountdownNumber", RpcTarget.All, "");
        var d = pictureEntries;
        var a = answerEntries;

        int[] dOwners = d.Select(x => x.ownerID).ToArray();
        int[] dActors = d.Select(x => x.actorNumber).ToArray();
        string[] dNicks = d.Select(x => x.nickName).ToArray();
        byte[][] dPngs = d.Select(x => x.texture.EncodeToPNG()).ToArray();
        double[] dTimes = d.Select(x => x.sentTime).ToArray();

        int[] aOwners = a.Select(x => x.ownerID).ToArray();
        int[] aActors = a.Select(x => x.actorNumber).ToArray();
        string[] aNicks = a.Select(x => x.nickName).ToArray();
        string[] aTexts = a.Select(x => x.answer).ToArray();
        double[] aTimes = a.Select(x => x.sentTime).ToArray();

        photonView.RPC("Resultdisplay", RpcTarget.All,
        dOwners, dActors, dNicks, dPngs, dTimes,
        aOwners, aActors, aNicks, aTexts, aTimes);
    }

    [PunRPC]
    private void Resultdisplay(
    int[] dOwners, int[] dActors, string[] dNicks, byte[][] dPngs, double[] dTimes,
    int[] aOwners, int[] aActors, string[] aNicks, string[] aTexts, double[] aTimes)
    {
        pictureEntries.Clear();
        for (int i = 0; i < dActors.Length; i++)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.LoadImage(dPngs[i], true);

            pictureEntries.Add(new PictureEntry
            {
                ownerID = dOwners[i],
                actorNumber = dActors[i],
                nickName = dNicks[i],
                texture = tex,
                sentTime = dTimes[i]
            });
        }

        answerEntries.Clear();
        for (int i = 0; i < aActors.Length; i++)
        {
            answerEntries.Add(new AnswerEntry
            {
                ownerID = aOwners[i],
                actorNumber = aActors[i],
                nickName = aNicks[i],
                answer = aTexts[i],
                sentTime = aTimes[i]
            });
        }

        isGameFinished = true;
        MovePanel(new Vector2(-4000, 0));
        dengonUIManager.Initialize();
        DisplayResult();
    }

    private void DisplayResult()
    {
        // 1. お題主の一覧（ownerID昇順で固定順にする）
        var allOwnerIDs = pictureEntries.Select(p => p.ownerID)
            .Concat(answerEntries.Select(a => a.ownerID))
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var activeTabs = GetActiveTabs(useHierarchy: true);

        // 2. 各タブ＝各お題主
        for (int p = 0; p < activeTabs.Count; p++)
        {
            int ownerID = allOwnerIDs[p];
            var content = activeTabs[p];
            ClearChildren(content);

            // 該当ownerIDの流れを抽出（時系列順にソート）
            var pics = pictureEntries.Where(pe => pe.ownerID == ownerID).OrderBy(pe => pe.sentTime).ToList();
            var ans = answerEntries.Where(ae => ae.ownerID == ownerID).OrderBy(ae => ae.sentTime).ToList();

            var nodes = new List<(string kind, object payload, string nick)>();

            // 先頭：このownerIDのお題回答（お題主の最初の回答）
            AnswerEntry initial = ans.Count > 0 ? ans.OrderBy(a => a.sentTime).First() : null;
            if (initial != null)
            {
                nodes.Add(("text", initial.answer, initial.nickName));
                nodes.Add(("arrow", null, null));
            }
            var restAns = (initial == null) ? ans : ans.Where(a => !ReferenceEquals(a, initial)).ToList();

            // 画像と残り回答を時系列マージ
            int ip = 0, ia = 0;
            while (ip < pics.Count || ia < restAns.Count)
            {
                bool takePic = ia >= restAns.Count || (ip < pics.Count && pics[ip].sentTime <= restAns[ia].sentTime);

                if (nodes.Count > 0 && nodes[^1].kind != "arrow") nodes.Add(("arrow", null, null));

                if (takePic)
                {
                    nodes.Add(("pic", pics[ip].texture, pics[ip].nickName));
                    ip++;
                }
                else
                {
                    nodes.Add(("text", restAns[ia].answer, restAns[ia].nickName));
                    ia++;
                }
            }
            // 末尾の余分な矢印を削除
            if (nodes.Count > 0 && nodes[^1].kind == "arrow") nodes.RemoveAt(nodes.Count - 1);

            // ノードをUIに生成
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (n.kind == "arrow")
                {
                    Instantiate(arrowPrefab, content);
                }
                else if (n.kind == "pic")
                {
                    var go = Instantiate(dengonPicturePrefab, content);
                    go.transform.Find("RawImage").GetComponent<RawImage>().texture = (Texture2D)n.payload;
                    go.transform.Find("NameText").GetComponent<Text>().text = n.nick;
                }
                else // "text"
                {
                    var go = Instantiate(dengonTextPrefab, content);
                    go.transform.Find("DengonText").GetComponent<Text>().text = (string)n.payload;
                    var nameObj = go.transform.Find("NameText").gameObject;
                    if (i == 0)
                    {
                        // 最初のテキストなら名前非表示
                        nameObj.SetActive(false);
                    }
                    else
                    {
                        nameObj.SetActive(true);
                        nameObj.GetComponent<Text>().text = n.nick;
                    }
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
    }

    private void ClearChildren(Transform t)
    { 
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            Destroy(t.GetChild(i).gameObject);
        }
    }




    // リスタート関連

    [SerializeField] GameObject restartButton; // 設定再利用ボタン

    public void RestartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC("ResetSetting", RpcTarget.All);
        photonView.RPC("ResetThemeFlags", RpcTarget.All);
        photonView.RPC("SetLoadObj", RpcTarget.All, true);
        photonView.RPC("MovePanel", RpcTarget.All, new Vector2(0, 0));

        pictureEntries.Clear();
        answerEntries.Clear();

        dengonThemeGenerator.RegenerateAndBroadcastThemes();
    }

    private int GetRoomThemeVersion()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("DengonThemeVersion", out var vObj) && vObj is int v)
            return v;
        return 0;
    }

    [PunRPC]
    private void ResetThemeFlags()
    {
        var props = new ExitGames.Client.Photon.Hashtable
    {
        { "ThemeReceived", false },
        { "ThemeVersion", -1 },
        { "OrderReady", true },
    };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    [PunRPC]
    private void ResetSetting()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        currentRound = 0;
        isDrawingPhase = false;
        isGameFinished = false;
        gameStarted = false;
        timerStopped = true;
        phaseTransitioning = false;
        myDrawingReady = false;
        myAnswerReady = false;
        timeRemaining = 0;
        UpdateReadyButtonUI();
        dengonUIManager.SetGameFinished(false);

        myTheme = null;
        currentOwnerID = PhotonNetwork.LocalPlayer.ActorNumber;
        themeText.text = "";

        drawingReadyPlayers.Clear();
        answerReadyPlayers.Clear();
    }

    [PunRPC]
    private void ShowTabPanel(int index)
    { 
        dengonUIManager.SetActiveTab(index);
        dengonUIManager.OnTabClicked(0);
    }


    // コールバック
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        drawingReadyPlayers.Remove(otherPlayer.ActorNumber);
        answerReadyPlayers.Remove(otherPlayer.ActorNumber);

        TryAdvanceIfAllReady();
    }
}