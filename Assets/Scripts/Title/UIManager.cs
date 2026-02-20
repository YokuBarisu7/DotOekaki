using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using Photon.Pun;
using Photon.Realtime;

public class UIManager : MonoBehaviourPunCallbacks
{
    [SerializeField] InputField createPasswordInputField;
    [SerializeField] InputField joinPasswordInputField;
    [SerializeField] Dropdown playerCountDropdown;
    [SerializeField] Text createErrorText;
    [SerializeField] Text joinErrorText;
    [SerializeField] Text playerCountText;
    [SerializeField] Text roomNameText;
    [SerializeField] Text joinedPlayerText;
    [SerializeField] Button roomCreateButton;
    [SerializeField] Button roomJoinButton;
    [SerializeField] GameObject startButton;
    [SerializeField] Toggle modeToggle;
    [SerializeField] int playerCount;
    [SerializeField] ColorPalette textColors;
    private Button startBtn;

    // おえかきクイズモード
    [SerializeField] Button quizGameStartButton;
    [SerializeField] InputField questionCountInputField;
    [SerializeField] InputField limitTimeInputField;
    [SerializeField] int questionCount;
    [SerializeField] int limitTime;
    private bool isErrorQuestionCount;
    private bool isErrorLimitTime;

    // 協力クイズモード
    [SerializeField] Button cooperateQuizButton;
    [SerializeField] Button cooperateQuizStartButton;
    [SerializeField] InputField cooperateCountInputField;
    [SerializeField] InputField cooperateTimeInputField;
    [SerializeField] int cooperateCount;
    [SerializeField] int cooperateTime;
    private bool isErrorCooperateCount;
    private bool isErrorCooperateTime;

    // 絵しりとりモード
    [SerializeField] Button shiritoriStartButton;
    [SerializeField] InputField shiritoriTimeInputField;
    [SerializeField] InputField shiritoriAnswerTimeInputField;
    [SerializeField] int shiritoriTime;
    [SerializeField] int shiritoriAnswerTime;
    private bool isErrorShiritoriTime;
    private bool isErrorShiritoriAnswerTime;

    // 伝言ゲームモード
    [SerializeField] Button dengonStartButton;
    [SerializeField] InputField dengonTimeInputField;
    [SerializeField] InputField dengonAnswerTimeInputField;
    [SerializeField] int dengonTime;
    [SerializeField] int dengonAnswerTime;
    private bool isErrorDengonTime;
    private bool isErrorDengonAnswerTime;

    // オプションメニュー
    [SerializeField] InputField playerNameInputField;
    [SerializeField] Image textColorImage;
    [SerializeField] Text textColorText;
    [SerializeField] Dropdown resolutionDropdown;
    private Resolution[] resolutions = {
        new Resolution { width = 640, height = 360 },
        new Resolution { width = 854, height = 480 },
        new Resolution { width = 960, height = 540 },
        new Resolution { width = 1280, height = 720 },
        new Resolution { width = 1600, height = 900 },
        new Resolution { width = 1920, height = 1080 },
        new Resolution { width = 2560, height = 1440 },
        new Resolution { width = 3840, height = 2160 }
    };
    private int FullscreenIndex => resolutions.Length;
    private const string PreTextColorIndex = "TextColorIndex";

    private void Start()
    {
        InitResolution();
        InitRoomUI();
        InitModeInput();
        InitTextColor();
    }

    private void InitResolution()
    {
        int savedIndex = PlayerPrefs.GetInt("ResolutionIndex", resolutionDropdown.value);
        savedIndex = Mathf.Clamp(savedIndex, 0, FullscreenIndex);
        resolutionDropdown.value = savedIndex;
        ApplyResolution();
    }

    private void InitRoomUI()
    {
        createPasswordInputField.onValueChanged.AddListener(OnCreatePasswordInputFieldValueChanged);
        joinPasswordInputField.onValueChanged.AddListener(OnJoinPasswordInputFieldValueChanged);
        playerCountDropdown.onValueChanged.AddListener(OnPlayerCountDropdownValueChanged);
        startBtn = startButton.GetComponent<Button>();
    }

    private void InitModeInput()
    {
        BindIntInput(questionCountInputField, questionCount, OnQuestionCountInputValueChanged, ValidateQuestionCountInput);
        BindIntInput(limitTimeInputField, limitTime, OnLimitTextInputValueChanged, ValidateLimitTimeInput);

        BindIntInput(cooperateCountInputField, cooperateCount, OnCooperateCountInputValueChanged, ValidateCooperateCountInput);
        BindIntInput(cooperateTimeInputField, cooperateTime, OnCooperateTimeInputValueChanged, ValidateCooperateTimeInput);

        BindIntInput(shiritoriTimeInputField, shiritoriTime, OnShiritoriTimeInputValueChanged, ValidateShiritoriTimeInput);
        BindIntInput(shiritoriAnswerTimeInputField, shiritoriAnswerTime, OnShiritoriAnswerTimeInputValueChanged, ValidateShiritoriAnswerTimeInput);

        BindIntInput(dengonTimeInputField, dengonTime, OnDengonTimeInputValueChanged, ValidateDengonTimeInput);
        BindIntInput(dengonAnswerTimeInputField, dengonAnswerTime, OnDengonAnswerTimeInputValueChanged, ValidateDengonAnswerTimeInput);
    }

    private void InitTextColor()
    {
        int savedIndex = PlayerPrefs.GetInt(PreTextColorIndex, 0);
        savedIndex = Mathf.Clamp(savedIndex, 0, textColors.colors.Length - 1);
        ApplyTextColorByIndex(savedIndex);
    }

    private void BindIntInput(InputField field, int initialValue,
        UnityEngine.Events.UnityAction<string> onChanged,
        UnityEngine.Events.UnityAction<string> onEndEdit)
    {
        field.onValueChanged.AddListener(onChanged);
        field.onEndEdit.AddListener(onEndEdit);
        field.text = initialValue.ToString();
    }


    // --------------- ボタン ---------------
    public void OnCreatePasswordRoomButtonClick()
    {
        var roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)playerCount;
        roomOptions.IsVisible = false;

        PhotonNetwork.CreateRoom(createPasswordInputField.text, roomOptions, TypedLobby.Default);
    }

    // ルーム作成成功時のコールバック
    public override void OnCreatedRoom()
    {
        PanelController.instance.OnClickButton(4);
    }

    // ルーム作成失敗時のコールバック
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (returnCode == ErrorCode.InvalidOperation)
        {
            Debug.LogError("ルームの作成に失敗しました: " + message);
            ShowCreateErrorMessage("ルームの作成に失敗しました。パスワードが既に使用されています。");
        }
        else
        {
            Debug.LogError("ルームの作成に失敗しました: " + message);
            ShowCreateErrorMessage("ルームの作成に失敗しました。");
        }
    }

    private void ShowCreateErrorMessage(string message)
    {
        createErrorText.gameObject.SetActive(true);
        createErrorText.text = message;
    }

    public void OnJoinPasswordRoomButtonClick()
    {
        PhotonNetwork.JoinRoom(joinPasswordInputField.text);
    }

    // 参加ボタンが押されたときに呼び出される
    public void OnClickJoinRandomRoom()
    {
        Debug.Log("ランダムルームに参加します。");

        var name = PlayerPrefs.GetString("PlayerName", "");
        if (string.IsNullOrWhiteSpace(name))
            name = $"Player{Random.Range(1000, 9999)}";

        PhotonNetwork.NickName = name;
        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();

        PhotonNetwork.JoinRandomRoom();
    }

    // ルーム参加成功時のコールバック
    public override void OnJoinedRoom()
    {
        PanelController.instance.OnClickButton(4);

        roomNameText.text = $"ルーム名：{PhotonNetwork.CurrentRoom.Name}";

        // ホストのみゲームルールを選んで開始することができる
        UpdateLobbyUI();
    }

    // 協力おえかきモードはplayerが３人以上でプレイ可能(playerが入退室したときに実行)
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName}が参加しました。");
        UpdateLobbyUI();
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateLobbyUI();
    }

    public void UpdateLobbyUI()
    {
        playerCountText.text = $"現在のプレイヤー数: {PhotonNetwork.CurrentRoom.PlayerCount} / {PhotonNetwork.CurrentRoom.MaxPlayers}";
        joinedPlayerText.text = BuildPlayerListText();
        UpdateStartButtonState();
    }

    private string BuildPlayerListText()
    {
        var players = PhotonNetwork.PlayerList;
        System.Text.StringBuilder sb = new System.Text.StringBuilder(128);

        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            bool isHots = p.IsMasterClient;
            sb.Append(p.NickName);
            if (isHots) sb.Append("[HOST]");
            if (i < players.Length - 1) sb.Append('\n');
        }
        return sb.ToString();
    }

    private void UpdateStartButtonState()
    {
        bool isMaster = PhotonNetwork.IsMasterClient;
        int count = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;

        startButton.SetActive(isMaster);
        startBtn.interactable = isMaster && count >= 2;

        ResetGameButtons();
        if (!isMaster) return;

        bool canStart = count >= 2;
        bool canCoopQuiz = count >= 3;

        // 2人以上なら有効
        quizGameStartButton.interactable = canStart;
        shiritoriStartButton.interactable = canStart;
        dengonStartButton.interactable = canStart;

        // 協力クイズは3人以上で有効
        cooperateQuizButton.interactable = canCoopQuiz;
        cooperateQuizStartButton.interactable = canCoopQuiz;
    }

    private void ResetGameButtons()
    {
        quizGameStartButton.interactable = false;
        cooperateQuizButton.interactable = false;
        cooperateQuizStartButton.interactable = false;
        shiritoriStartButton.interactable = false;
        dengonStartButton.interactable = false;
    }

    // ルーム参加失敗時のコールバック
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (returnCode == ErrorCode.GameDoesNotExist)
        {
            Debug.LogError("ルームが存在しません");
            ShowJoinErrorMessage("ルームが存在しません。ルーム名を確認してください。");
        }
        else if (returnCode == ErrorCode.GameFull)
        {
            Debug.LogError("ルームが満員です。");
            ShowJoinErrorMessage("ルームが満員です。再度パスワードを入力してください。");
        }
        else if (returnCode == ErrorCode.GameClosed)
        {
            Debug.LogError("ルームはすでに終了しています");
            ShowJoinErrorMessage("ルームはすでに終了しています。");
        }
        else
        {
            Debug.LogError($"ルームへの参加に失敗しました: {message}");
            ShowJoinErrorMessage($"ルームへの参加に失敗しました: {message}");
        }
    }

    private void ShowJoinErrorMessage(string message)
    {
        joinErrorText.gameObject.SetActive(true);
        joinErrorText.text = message;
    }

    // エラーメッセージを非表示にし、インプットフィールドを初期化
    public void OnClickErrorMessageCloseButton()
    {
        createErrorText.gameObject.SetActive(false);
        joinErrorText.gameObject.SetActive(false);
        createPasswordInputField.text = "";
        joinPasswordInputField.text = "";
    }

    public void OnClickOfflineStartButton()
    {
        SceneController.instance.LoadScene("Offline");
    }

    private void StartSceneForAll(string sceneName)
    {
        photonView.RPC("LoadSceneRPC", RpcTarget.All, sceneName);
    }

    [PunRPC]
    private void LoadSceneRPC(string sceneName)
    {
        SceneController.instance.LoadScene(sceneName);
    }

    public void OnClickOekakiQuizStartButton()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            PlayerPrefs.SetInt("QuestionCount", questionCount);
            PlayerPrefs.SetInt("LimitTime", limitTime);
            PlayerPrefs.SetInt("Mode", modeToggle.isOn ? 1 : 0);
            StartSceneForAll("OekakiQuiz");
        }
        else
        {
            SceneController.instance.LoadScene("OekakiQuiz");
        }
    }

    public void OnClickCooperateQuizStartButton()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            PlayerPrefs.SetInt("CooperateCount", cooperateCount);
            PlayerPrefs.SetInt("CooperateTime", cooperateTime);
            StartSceneForAll("CooperateQuiz");
        }
        else
        {
            SceneController.instance.LoadScene("CooperateQuiz");
        }
    }

    public void OnClickShiritoriStartButton()
    {
        if (PhotonNetwork.InRoom)
        {
            PlayerPrefs.SetInt("ShiritoriTime", shiritoriTime);
            PlayerPrefs.SetInt("ShiritoriAnswerTime", shiritoriAnswerTime);
            StartSceneForAll("Eshiritori");
        }
        else
        {
            SceneController.instance.LoadScene("Eshiritori");
        }
    }

    public void OnClickDengonButton()
    {
        if (PhotonNetwork.InRoom)
        {
            PlayerPrefs.SetInt("DengonTime", dengonTime);
            PlayerPrefs.SetInt("DengonAnswerTime", dengonAnswerTime);
            StartSceneForAll("Dengon");
        }
        else
        {
            SceneController.instance.LoadScene("Dengon");
        }
    }

    public void ApplyResolution()
    {
        int index = resolutionDropdown.value;

        if (index == FullscreenIndex)
        {
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
        }
        else
        {
            Resolution resolution = resolutions[index];
            Screen.SetResolution(resolution.width, resolution.height, false);
        }

        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        PlayerPrefs.Save();
    }


    // --------------- Dropdown ---------------
    public void OnPlayerCountDropdownValueChanged(int value)
    {
        playerCount = playerCountDropdown.value + 2;
    }


    // --------------- InputField ---------------
    // ルーム作成、参加時の処理
    private void OnCreatePasswordInputFieldValueChanged(string input)
    { 
        roomCreateButton.interactable = !string.IsNullOrEmpty(createPasswordInputField.text);
    }

    private void OnJoinPasswordInputFieldValueChanged(string input)
    {
        roomJoinButton.interactable = !string.IsNullOrEmpty(joinPasswordInputField.text);
    }

    // 数字以外の入力を無効化
    private static bool IsDigitsOnly(string s) => Regex.IsMatch(s, @"^\d+$");
    private void ForceDigitsOrZero(InputField field, string input)
    {
        if (!IsDigitsOnly(input))
        {
            field.text = "0";
        }
    }

    private void ApplyIntRange(string input, int min, int max, ref int targetValue, ref bool errorFlag)
    {
        if (!int.TryParse(input, out int value))
        {
            errorFlag = true;
            return;
        }

        targetValue = value;
        errorFlag = (value < min || value > max);
    }

    // おえかきクイズモード
    private void ValidateQuestionCountInput(string input)
    {
        ForceDigitsOrZero(questionCountInputField, input);
    }
    private void ValidateLimitTimeInput(string input)
    {
        ForceDigitsOrZero(limitTimeInputField, input);
    }
    private void OnQuestionCountInputValueChanged(string input)
    {
        ApplyIntRange(input, 1, 10, ref questionCount, ref isErrorQuestionCount);
        quizGameStartButton.interactable = !isErrorQuestionCount && !isErrorLimitTime;
    }
    private void OnLimitTextInputValueChanged(string input)
    {
        ApplyIntRange(input, 30, 999, ref limitTime, ref isErrorLimitTime);
        quizGameStartButton.interactable = !isErrorQuestionCount && !isErrorLimitTime;
    }


    // 協力クイズモード
    private void ValidateCooperateCountInput(string input)
    {
        ForceDigitsOrZero(cooperateCountInputField, input);
    }
    private void ValidateCooperateTimeInput(string input)
    {
        ForceDigitsOrZero(cooperateTimeInputField, input);
    }
    private void OnCooperateCountInputValueChanged(string input)
    {
        ApplyIntRange(input, 1, 10, ref cooperateCount, ref isErrorCooperateCount);
        cooperateQuizStartButton.interactable = !isErrorCooperateCount && !isErrorCooperateTime;
    }
    private void OnCooperateTimeInputValueChanged(string input)
    {
        ApplyIntRange(input, 30, 999, ref cooperateTime, ref isErrorCooperateTime);
        cooperateQuizStartButton.interactable = !isErrorCooperateCount && !isErrorCooperateTime;
    }


    // 絵しりとりモード
    private void ValidateShiritoriTimeInput(string input)
    {
        ForceDigitsOrZero(shiritoriTimeInputField, input);
    }
    private void ValidateShiritoriAnswerTimeInput(string input)
    {
        ForceDigitsOrZero(shiritoriAnswerTimeInputField, input);
    }
    private void OnShiritoriTimeInputValueChanged(string input)
    {
        ApplyIntRange(input, 10, 999, ref shiritoriTime, ref isErrorShiritoriTime);
        shiritoriStartButton.interactable = !isErrorShiritoriTime && !isErrorShiritoriAnswerTime;
    }
    private void OnShiritoriAnswerTimeInputValueChanged(string input)
    {
        ApplyIntRange(input, 10, 300, ref shiritoriAnswerTime, ref isErrorShiritoriAnswerTime);
        shiritoriStartButton.interactable = !isErrorShiritoriTime && !isErrorShiritoriAnswerTime;
    }


    // 伝言ゲームモード
    private void ValidateDengonTimeInput(string input)
    {
        ForceDigitsOrZero(dengonTimeInputField, input);
    }
    private void ValidateDengonAnswerTimeInput(string input)
    {
        ForceDigitsOrZero(dengonAnswerTimeInputField, input);
    }
    private void OnDengonTimeInputValueChanged(string input)
    {
        ApplyIntRange(input, 10, 999, ref dengonTime, ref isErrorDengonTime);
        dengonStartButton.interactable = !isErrorDengonTime && !isErrorDengonAnswerTime;
    }
    private void OnDengonAnswerTimeInputValueChanged(string input)
    {
        ApplyIntRange(input, 10, 300, ref dengonAnswerTime, ref isErrorDengonAnswerTime);
        dengonStartButton.interactable = !isErrorDengonTime && !isErrorDengonAnswerTime;
    }

    // --------------- オプションメニュー ---------------

    // 決定ボタン押下時にプレイヤー名を保存
    public void OnClickOptionApplyButton()
    {
        string playerName = string.IsNullOrEmpty(playerNameInputField.text) ? $"Player{Random.Range(1000, 9999)}" : playerNameInputField.text;
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();
    }

    // 画面遷移時にプレイヤー名を取得・表示
    public void DisplayPlayerName()
    {
        playerNameInputField.text = PlayerPrefs.GetString("PlayerName");
        InitTextColor();
    }

    // デバッグ用
    public void OnClickClearButton()
    {
        playerNameInputField.text = "";
        PlayerPrefs.SetString("PlayerName", "");
        PlayerPrefs.Save();
    }

    public void OnTextColorButtonClick(int index)
    {
        index = Mathf.Clamp(index, 0, textColors.colors.Length - 1);

        ApplyTextColorByIndex(index);

        PlayerPrefs.SetInt(PreTextColorIndex, index);
        PlayerPrefs.Save();
    }

    private void ApplyTextColorByIndex(int index)
    {
        Color c = textColors.colors[index];
        textColorText.color = c;
        textColorImage.color = c;
    }
}