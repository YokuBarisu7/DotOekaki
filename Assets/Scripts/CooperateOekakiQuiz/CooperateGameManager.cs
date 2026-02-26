using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;


public class CooperateGameManager : MonoBehaviourPunCallbacks
{
    public static CooperateGameManager instance;

    [SerializeField] ThemeGenerator themeGenerator;
    [SerializeField] CooperateUIManager dotUIManager;
    [SerializeField] QuizQuestion currentTheme;

    [SerializeField] int[] questionerNumbers = new int[2]; // 出題者の番号を保持する配列
    public int[] QuestionerNumbers => questionerNumbers;

    int lastQ1 = -1;
    int lastQ2 = -1;

    [SerializeField] GameObject panels;
    [SerializeField] GameObject loadObj;

    [SerializeField] Text correctLabel;
    [SerializeField] Text timerText;
    [SerializeField] Text countText;

    [SerializeField] int questionCount;
    [SerializeField] int currentRound;

    [SerializeField] int timeLimit;
    private float timeRemaining;
    private float timerSyncCooldown;
    private bool isTimerActive;
    private bool isTimeUp;

    [SerializeField] int difficulty;
    [SerializeField] bool isFinished;

    [SerializeField] Transform resultList; //結果表示用の親オブジェクト
    [SerializeField] GameObject resultPrefab; //結果表示用のプレハブ
    private List<GameObject> resultPrefabs = new List<GameObject>(); // 結果表示用のプレハブのリスト

    [SerializeField] Transform pictureList; // 結果表示用の画像の親オブジェクト
    [SerializeField] GameObject picturePrefab; // 結果表示用の画像プレハブ
    private List<SavedResult> savedPictures = new List<SavedResult>(); // 保存された画像のリスト

    [SerializeField] GameObject changeSettingButton; // 設定変更ボタン
    [SerializeField] GameObject reuseSettingButton; // 設定再利用ボタン

    private Dictionary<int, int> correctPoints = new Dictionary<int, int>(); // プレイヤーの正解数を保持する辞書
    private Dictionary<int, int> correctedPoints = new Dictionary<int, int>(); // プレイヤーの正解された回数を保持する辞書


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

    private void OnDestroy()
    {
        themeGenerator.OnThemeApplied -= OnThemeApplied;
    }

    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("オンラインモードで実行");
            themeGenerator.OnThemeApplied += OnThemeApplied;
            loadObj.SetActive(true); // ロードオブジェクトを表示

            if (PhotonNetwork.IsMasterClient)
            {
                questionCount = PlayerPrefs.GetInt("CooperateCount", 5);
                timeLimit = PlayerPrefs.GetInt("CooperateTime", 180);
                photonView.RPC("SyncOption", RpcTarget.All, questionCount, timeLimit);

                changeSettingButton.SetActive(true); // 設定変更ボタンを表示
                reuseSettingButton.SetActive(true); // 設定再利用ボタンを表示
            }

            timeRemaining = timeLimit;
            isTimerActive = false;
            isTimeUp = false;

            InitializePlayerPoints();
        }
    }

    private void OnThemeApplied(int ver)
    {
        if (themeGenerator.GetTheme(0) == null)
        {
            Debug.LogWarning("配布されたQuizQuestionが不正");
            return;
        }
        SetReady(true);
    }

    private void SetReady(bool isReady)
    {
        var props = new ExitGames.Client.Photon.Hashtable
        {
            { "Ready", isReady }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Ready"))
        {
            CheckAllPlayerReady();
        }
    }

    private void CheckAllPlayerReady()
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("Ready", out object isReadyObj))
            {
                // 1人でも未準備のプレイヤーがいる場合はreturn
                if (!(isReadyObj is bool isReady) || !isReady)
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        // 全員が準備完了状態ならば、ゲーム開始の処理を行う
        if (PhotonNetwork.IsMasterClient)
        {
            currentRound = -1;
            MasterAdvanceRound();
            photonView.RPC("StartTimer", RpcTarget.All); // タイマーを開始
            photonView.RPC("SetLoadObj", RpcTarget.All, false); // ロードオブジェクトを非表示
        }
    }

    private void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (isTimerActive && timeRemaining > 0 && currentRound < questionCount)
                {
                    timeRemaining -= Time.deltaTime;
                    timerSyncCooldown -= Time.deltaTime;
                    if (timerSyncCooldown <= 0f)
                    {
                        timerSyncCooldown = 0.2f; // 0,2秒毎にタイマー同期
                        photonView.RPC("SyncTimer", RpcTarget.Others, timeRemaining);
                    }
                }
                else if (timeRemaining <= 0 && isTimerActive && !isTimeUp)
                {
                    isTimerActive = false;
                    isTimeUp = true;
                    TimeUp();
                }
            }
            timerText.text = $"残り\n{timeRemaining.ToString("F0")} 秒";
        }
    }

    private void TimeUp()
    {
        photonView.RPC("SavedPicture", RpcTarget.All);
        photonView.RPC("ShowIncorrect", RpcTarget.All);
        MasterAdvanceRound();
    }

    public void OnClickStartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        questionCount = PlayerPrefs.GetInt("CooperateCount", 5);
        timeLimit = PlayerPrefs.GetInt("CooperateTime", 180);
        difficulty = PlayerPrefs.GetInt("Difficulty", 0);

        photonView.RPC("SyncOption", RpcTarget.All, questionCount, timeLimit);
        photonView.RPC("SyncSettings", RpcTarget.All);
        photonView.RPC("MoveGamePanel", RpcTarget.All, new Vector2(0, 0));
        photonView.RPC("SetLoadObj", RpcTarget.All, true);

        SetReady(false);

        themeGenerator.BroadcastThemes(difficulty);
    }

    public void OnClickRestartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC("SyncOption", RpcTarget.All, questionCount, timeLimit);
        photonView.RPC("SyncSettings", RpcTarget.All);
        photonView.RPC("MoveGamePanel", RpcTarget.All, new Vector2(0, 0));
        photonView.RPC("SetLoadObj", RpcTarget.All, true);

        SetReady(false);

        themeGenerator.BroadcastThemes(difficulty);
    }

    [PunRPC]
    private void SyncOption(int count, int time)
    {
        questionCount = count;
        timeLimit = time;
    }

    [PunRPC]
    private void SyncSettings()
    {
        timeRemaining = timeLimit;
        isTimerActive = false;
        isTimeUp = false;

        InitializePlayerPoints();
        DestroyResultPrefabs();
        isFinished = false;

        SetReady(false);
    }

    [PunRPC]
    private void MoveGamePanel(Vector2 pos)
    {
        panels.transform.localPosition = pos;
    }

    // 前回と同じペアにならないようにランダムに２人選出
    private void PickTwoQuestionersAvoidLast(out int q1, out int q2)
    {
        var players = PhotonNetwork.PlayerList;
        int n = players.Length;

        if (n < 3)
        {
            q1 = q2 = -1;
            Debug.LogWarning("協力モードは3人以上必要");
            return;
        }

        // 試行回数に上限を付けて無限ループ防止
        const int MaxTries = 50;

        for (int t = 0; t < MaxTries; t++)
        {
            int a = players[Random.Range(0, n)].ActorNumber;
            int b;
            do { b = players[Random.Range(0, n)].ActorNumber; }
            while (b == a);

            if (!IsSamePairUnordered(a, b, lastQ1, lastQ2))
            {
                q1 = a;
                q2 = b;
                return;
            }
        }

        // ここに来たら「回避できなかった」ので諦めて出す（3人なら基本ここには来ない）
        q1 = players[0].ActorNumber;
        q2 = players[1].ActorNumber;
    }

    private bool IsSamePairUnordered(int a1, int a2, int b1, int b2)
    {
        return (a1 == b1 && a2 == b2) || (a1 == b2 && a2 == b1);
    }

    private void MasterAdvanceRound()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        currentRound++;

        if (currentRound >= questionCount)
        {
            if (!isFinished)
            {
                Invoke("GameFinished", 3.0f);
                isFinished = true;
            }
            return;
        }

        PickTwoQuestionersAvoidLast(out int q1, out int q2);
        if (q1 < 0 || q2 < 0) return;

        lastQ1 = q1;
        lastQ2 = q2;

        photonView.RPC("ApplyRoundState", RpcTarget.All, q1, q2, currentRound);
    }

    [PunRPC]
    private void ApplyRoundState(int newQ1, int newQ2, int round)
    {
        questionerNumbers[0] = newQ1;
        questionerNumbers[1] = newQ2;
        currentRound = round;

        dotUIManager.Initialize(); // UIの初期化
        CooperateDrawingManager.instance.InitializeDrawField(); // DrawFieldの初期化
        CooperateDrawingManager.instance.SetDrawable(IsQuestioner); // 出題者のみ描けるように

        currentTheme = themeGenerator.GetTheme(currentRound);
        UpdateText();
        countText.text = $"残り\n{questionCount - currentRound} 問";
    }

    private bool IsQuestioner => PhotonNetwork.LocalPlayer.ActorNumber == questionerNumbers[0] || PhotonNetwork.LocalPlayer.ActorNumber == questionerNumbers[1];

    [PunRPC]
    private void RequestAdvanceRound()
    {
        MasterAdvanceRound();
    }

    // 出題者のみが正誤判定を行う
    [PunRPC]
    private void CheckAnswer(string answer, int actorNumber)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == questionerNumbers[0] && IsCorrectAnswer(answer))
        {
            photonView.RPC("ShowCorrect", RpcTarget.All);
            photonView.RPC("AddCorrectPoints", RpcTarget.MasterClient, actorNumber); // 正解したプレイヤーの正解数を加算
            photonView.RPC("AddCorrectedPoints", RpcTarget.MasterClient, questionerNumbers[0]); // 出題者の正解された回数を加算
            photonView.RPC("AddCorrectedPoints", RpcTarget.MasterClient, questionerNumbers[1]); // 出題者の正解された回数を加算
            photonView.RPC("SavedPicture", RpcTarget.All); // 正解時に画像を保存

            // TODO:残り秒数などでポイント増やすか検討（実際にプレイした所感で決めたい）

            // お題と出題者の再設定
            if (PhotonNetwork.IsMasterClient)
            {
                MasterAdvanceRound();
            }
            else
            {
                photonView.RPC("RequestAdvanceRound", RpcTarget.MasterClient);
            }
        }
    }

    private bool IsCorrectAnswer(string answer)
    {
        foreach (string correctAnswer in currentTheme.answerList)
        {
            if (NormalizeString(answer) == NormalizeString(correctAnswer))
            {
                return true;
            }
        }
        return false;
    }

    public void SubmitAnswer(string answer)
    {
        var q = PhotonNetwork.CurrentRoom.GetPlayer(questionerNumbers[0]);
        photonView.RPC("CheckAnswer", q, answer, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void ShowCorrect()
    {
        isTimerActive = false;
        timeRemaining = timeLimit;
        StartCoroutine(DisplayMessage($"正解！\n「{currentTheme.question}」", 3.0f));
    }

    [PunRPC]
    private void ShowIncorrect()
    {
        isTimerActive = false;
        timeRemaining = timeLimit;
        StartCoroutine(DisplayMessage($"残念...不正解！\n正解は\n「{currentTheme.question}」", 3.0f));
    }

    private IEnumerator DisplayMessage(string message, float duration)
    {
        correctLabel.text = message;
        correctLabel.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        correctLabel.gameObject.SetActive(false);
        StartTimer();
        isTimeUp = false;
    }

    private void UpdateText()
    {
        if(dotUIManager == null || questionerNumbers == null)
        {
            Debug.Log("出題者が決まってないよ");
            return;
        }
        var q1 = PhotonNetwork.CurrentRoom.GetPlayer(questionerNumbers[0]);
        var q2 = PhotonNetwork.CurrentRoom.GetPlayer(questionerNumbers[1]);
        if (q1 == null || q2 == null)
        {
            Debug.Log("出題者がいないよ");
            return;
        }
        dotUIManager.SetRoleText(q1.NickName, q2.NickName);
        if (currentTheme == null)
        {
            Debug.Log("お題がないよ");
            return;
        }
        dotUIManager.SetThemeText(IsQuestioner, currentTheme.question);
    }

    [PunRPC]
    private void StartTimer()
    {
        isTimerActive = true;
    }

    [PunRPC]
    private void SyncTimer(float time)
    {
        timeRemaining = time;
    }

    private string NormalizeString(string input)
    {
        // 前後の空白をトリムし、小文字変換し、全角を半角に変換
        return input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormKC);
    }

    [PunRPC]
    private void SetLoadObj(bool isActive)
    {
        loadObj.SetActive(isActive);
    }



    // リザルト関連

    private void InitializePlayerPoints()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            correctPoints[player.ActorNumber] = 0;
            correctedPoints[player.ActorNumber] = 0;
        }
    }

    private void DestroyResultPrefabs()
    {
        foreach (var prefab in resultPrefabs)
        {
            Destroy(prefab);
        }
        resultPrefabs.Clear();

        foreach (Transform child in pictureList)
        {
            Destroy(child.gameObject);
        }
        foreach (var picture in savedPictures)
        {
            Destroy(picture.texture);
        }
        savedPictures.Clear();
    }

    [PunRPC]
    private void AddCorrectPoints(int playerActorNumber)
    {
        if (!correctPoints.ContainsKey(playerActorNumber))
        {
            correctPoints[playerActorNumber] = 0;
        }
        correctPoints[playerActorNumber]++;
    }

    [PunRPC]
    private void AddCorrectedPoints(int playerActorNumber)
    {
        if (!correctedPoints.ContainsKey(playerActorNumber))
        {
            correctedPoints[playerActorNumber] = 0;
        }
        correctedPoints[playerActorNumber]++;
    }

    private int GetCorrectPoints(int playerActorNumber)
    {
        if (correctPoints.TryGetValue(playerActorNumber, out int points))
        {
            return points;
        }
        return 0;
    }

    private int GetCorrectedPoints(int playerActorNumber)
    {
        if (correctedPoints.TryGetValue(playerActorNumber, out int points))
        {
            return points;
        }
        return 0;
    }

    private void DisplayResults()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject entry = Instantiate(resultPrefab, resultList);
            resultPrefabs.Add(entry);
            ResultPrefab resultPref = entry.GetComponent<ResultPrefab>();

            string playerName = player.NickName;
            int correctPoints = GetCorrectPoints(player.ActorNumber);
            int correctedPoints = GetCorrectedPoints(player.ActorNumber);
            int point = (correctPoints + correctedPoints) * 10;

            resultPref.SetResult(playerName, correctPoints, correctedPoints, point);
        }
    }

    [PunRPC]
    private void SyncResults(int[] actorNumbers, int[] correctPointsArray, int[] correctedPointsArray)
    {
        for (int i = 0; i < actorNumbers.Length; i++)
        {
            correctedPoints[actorNumbers[i]] = correctedPointsArray[i];
            correctPoints[actorNumbers[i]] = correctPointsArray[i];
        }
    }

    private void SendResultsToOthers()
    {
        // 辞書を配列に変換
        int[] actorNumbers = new int[correctPoints.Count];
        int[] correctPointsArray = new int[correctPoints.Count];
        int[] correctedPointsArray = new int[correctedPoints.Count];

        int index = 0;
        foreach (var kvp in correctPoints)
        {
            actorNumbers[index] = kvp.Key;
            correctPointsArray[index] = kvp.Value;
            correctedPointsArray[index] = correctedPoints[kvp.Key];
            index++;
        }

        // RPCでデータを送信
        photonView.RPC("SyncResults", RpcTarget.Others, actorNumbers, correctPointsArray, correctedPointsArray);
    }

    [PunRPC]
    private void SavedPicture()
    {
        var src = CooperateDrawingManager.instance.texture;
        Texture2D copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        copy.filterMode = FilterMode.Point;
        copy.SetPixels32(src.GetPixels32());
        copy.Apply();

        string theme = currentTheme != null ? currentTheme.question : "(no theme)";
        savedPictures.Add(new SavedResult(theme, copy));
    }

    private void DisplaySavedPictures()
    {
        foreach (var picture in savedPictures)
        {
            GameObject pictureEntry = Instantiate(picturePrefab, pictureList);
            var rawImage = pictureEntry.GetComponentInChildren<RawImage>();
            rawImage.texture = picture.texture;

            var themeText = pictureEntry.transform.Find("ThemeText").GetComponent<Text>();
            themeText.text = picture.theme;

            var fitter = rawImage.GetComponent<AspectRatioFitter>();
            fitter.aspectRatio = (float)picture.texture.width / picture.texture.height;
        }
    }

    private void GameFinished()
    {
        Debug.Log("ゲーム終了");
        SendResultsToOthers();
        photonView.RPC("Resultdisplay", RpcTarget.All);
    }

    [PunRPC]
    private void Resultdisplay()
    {
        MoveGamePanel(new Vector2(-2000, 0));
        DisplayResults();
        DisplaySavedPictures();
    }
}