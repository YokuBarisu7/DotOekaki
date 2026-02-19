using NUnit.Framework.Internal.Commands;
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

    ThemeGenerator themeGenerator;
    CooperateUIManager dotUIManager;
    [SerializeField] QuizQuestion currentTheme;

    private List<int> questionerOrder = new List<int>(); // 出題者の順番を保持するリスト
    private int currentQuestionerIndex = 0; // 現在の出題者のインデックス
    [SerializeField] int[] questionerNumbers = new int[2]; // 出題者の番号を保持する配列
    public int[] QuestionerNumbers => questionerNumbers;

    [SerializeField] GameObject panels;
    [SerializeField] GameObject loadObj;

    [SerializeField] Text correctLabel;
    [SerializeField] Text timerText;
    [SerializeField] Text countText;

    public int questionCount;
    public int questionCountLeft;

    public int timeLimit;
    private float timeRemaining;
    private bool isTimerActive;
    private bool isTimeUp;

    [SerializeField] int mode;
    [SerializeField] bool isFinished;

    [SerializeField] Transform resultList; //結果表示用の親オブジェクト
    [SerializeField] GameObject resultPrefab; //結果表示用のプレハブ
    private List<GameObject> resultPrefabs = new List<GameObject>(); // 結果表示用のプレハブのリスト

    [SerializeField] Transform pictureList; // 結果表示用の画像の親オブジェクト
    [SerializeField] GameObject picturePrefab; // 結果表示用の画像プレハブ
    private List<Texture2D> savedPictures = new List<Texture2D>(); // 保存された画像のリスト

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

        themeGenerator = FindAnyObjectByType<ThemeGenerator>();
        dotUIManager = FindAnyObjectByType<CooperateUIManager>();
    }

    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("オンラインモードで実行");
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
        else
        {
            Debug.Log("オフラインモードで実行");
            timerText.gameObject.SetActive(false);
        }
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
            GenerateQuestionerOrder(); // 出題者の順番を生成
            photonView.RPC("SetQuestioner", RpcTarget.All);
            photonView.RPC("StartTimer", RpcTarget.All); // タイマーを開始
            photonView.RPC("SetLoadObj", RpcTarget.All, false); // ロードオブジェクトを非表示
        }
    }

    // ゲーム開始時の流れ
    // ①前シーンで設定したパラメータをSyncOptionでホストと同期
    // ②ホストが出題者の順番をGenerateQuestionerOrderで生成
    // ③ホストがSetQuestionerで出題者を設定し同期する
    // ④タイマーをセットし、スタート

    private void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (questionCountLeft < 0 && !isFinished)
                {
                    Invoke("GameFinished", 2.8f);
                    isFinished = true;
                }

                if (isTimerActive && timeRemaining > 0 && questionCountLeft >= 0)
                {
                    timeRemaining -= Time.deltaTime;
                    photonView.RPC("SyncTimer", RpcTarget.Others, timeRemaining);
                }
                else if (timeRemaining <= 0 && isTimerActive && !isTimeUp)
                {
                    isTimerActive = false;
                    isTimeUp = true;
                    TimeUp();
                }
            }
            timerText.text = $"残り\n{timeRemaining.ToString("F0")} 秒";
            countText.text = $"残り\n{questionCountLeft} 問";
            UpdateText();
        }
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        questionCount = PlayerPrefs.GetInt("CooperateCount", 5);
        timeLimit = PlayerPrefs.GetInt("CooperateTime", 180);
        mode = PlayerPrefs.GetInt("Mode", 0);

        photonView.RPC("SyncOption", RpcTarget.All, questionCount, timeLimit);
        photonView.RPC("SetQuestioner", RpcTarget.All);
        photonView.RPC("SyncSettings", RpcTarget.All);
        photonView.RPC("MoveGamePanel", RpcTarget.All, new Vector2(0, 0));
        photonView.RPC("SetLoadObj", RpcTarget.All, true);

        themeGenerator.BroadcastThemes(mode);
    }

    public void RestartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC("SyncOption", RpcTarget.All, questionCount, timeLimit);
        photonView.RPC("SetQuestioner", RpcTarget.All);
        photonView.RPC("SyncSettings", RpcTarget.All);
        photonView.RPC("MoveGamePanel", RpcTarget.All, new Vector2(0, 0));
        photonView.RPC("SetLoadObj", RpcTarget.All, true);

        themeGenerator.BroadcastThemes(mode);
    }

    // ①

    [PunRPC]
    private void SyncOption(int count, int time)
    {
        questionCount = count;
        questionCountLeft = count;
        timeLimit = time;
    }

    // ②

    private void GenerateQuestionerOrder()
    {
        questionerOrder.Clear();
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            questionerOrder.Add(player.ActorNumber);
        }

        // ランダムにシャッフル
        for (int i = questionerOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = questionerOrder[i];
            questionerOrder[i] = questionerOrder[j];
            questionerOrder[j] = temp;
        }
        currentQuestionerIndex = 0; // インデックスをリセット

        photonView.RPC("SyncQuestionerOrder", RpcTarget.All, questionerOrder.ToArray());
    }

    [PunRPC]
    private void SyncQuestionerOrder(int[] actorNumbers)
    {
        questionerOrder = new List<int>(actorNumbers);
        currentQuestionerIndex = 0;
    }

    // ③

    [PunRPC]
    private void SetQuestioner()
    {
        questionCountLeft--;
        dotUIManager.Initialize(); // UIリセット
        questionerNumbers[0] = questionerOrder[currentQuestionerIndex];
        questionerNumbers[1] = questionerOrder[(currentQuestionerIndex + 1)];
        if (PhotonNetwork.LocalPlayer.ActorNumber == questionerNumbers[0] || PhotonNetwork.LocalPlayer.ActorNumber == questionerNumbers[1])
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == questionerNumbers[0])
            {
                currentTheme = themeGenerator.GetTheme(questionCount - questionCountLeft);
                photonView.RPC("UpdateCurrentTheme", RpcTarget.Others, currentTheme.question);
            }
            CooperateDrawingManager.instance.isDrawable = true;
        }
        else
        {
            CooperateDrawingManager.instance.isDrawable = false;
        }
    }

    // ④

    [PunRPC]
    private void SyncSettings()
    {
        timeRemaining = timeLimit;
        isTimerActive = false;
        isTimeUp = false;

        InitializePlayerPoints();
        DestroyResultPrefabs();

        Invoke("StartTimer", 1.0f);
        isFinished = false; // ゲーム開始時にリセット
    }

    [PunRPC]
    private void MoveGamePanel(Vector2 pos)
    {
        panels.transform.localPosition = pos;
    }


    private void TimeUp()
    {
        photonView.RPC("SavedPicture", RpcTarget.All);
        photonView.RPC("ShowIncorrect", RpcTarget.All);
        GenerateQuestionerOrder(); // タイムアップ時に出題者の順番を再生成
        photonView.RPC("SetQuestioner", RpcTarget.All);
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
            GenerateQuestionerOrder();
            photonView.RPC("SetQuestioner", RpcTarget.All);
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
        photonView.RPC("CheckAnswer", RpcTarget.Others, answer, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void ShowCorrect()
    {
        StartCoroutine(DisplayMessage($"正解！\n「{currentTheme.question}」", 3.0f));
        isTimerActive = false;
        timeRemaining = timeLimit;
    }

    [PunRPC]
    private void ShowIncorrect()
    {
        StartCoroutine(DisplayMessage($"残念...不正解！\n正解は\n「{currentTheme.question}」", 3.0f));
        isTimerActive = false;
        timeRemaining = timeLimit;
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
            return;
        }
        var player1 = PhotonNetwork.CurrentRoom.GetPlayer(questionerNumbers[0]);
        var player2 = PhotonNetwork.CurrentRoom.GetPlayer(questionerNumbers[1]);
        if (player1 == null || player2 == null)
        {
            return;
        }
        dotUIManager.SetRoleText(player1.NickName, player2.NickName);
        if (currentTheme == null)
        {
            return;
        }
        dotUIManager.SetThemeText(IsQuestioner(), currentTheme.question);
    }

    private bool IsQuestioner()
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == questionerNumbers[0] || PhotonNetwork.LocalPlayer.ActorNumber == questionerNumbers[1])
        {
            return true;
        }
        else
        {
            return false;
        }
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

    [PunRPC]
    private void UpdateCurrentTheme(string question)
    {
        if (currentTheme == null)
        {
            currentTheme = new QuizQuestion();
        }
        currentTheme.question = question;
    }

    private string NormalizeString(string input)
    {
        // 前後の空白をトリムし、小文字変換し、全角を半角に変換
        return input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormKC);
    }

    private void DestroyResultPrefabs()
    {
        foreach (GameObject prefab in resultPrefabs)
        {
            Destroy(prefab);
        }
        resultPrefabs.Clear();

        foreach (Transform child in pictureList)
        {
            Destroy(child.gameObject);
        }
        savedPictures.Clear();
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
        Texture2D texture = new Texture2D(CooperateDrawingManager.instance.texture.width, CooperateDrawingManager.instance.texture.height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.SetPixels(CooperateDrawingManager.instance.texture.GetPixels());
        texture.Apply();
        savedPictures.Add(texture);
    }

    private void DisplaySavedPictures()
    {
        foreach (Texture2D picture in savedPictures)
        {
            GameObject pictureEntry = Instantiate(picturePrefab, pictureList);
            Transform rawImageTransform = pictureEntry.transform.Find("RawImage");
            RawImage rawImage = rawImageTransform.GetComponent<RawImage>();
            rawImage.texture = picture;
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
        dotUIManager.Initialize();
    }
}