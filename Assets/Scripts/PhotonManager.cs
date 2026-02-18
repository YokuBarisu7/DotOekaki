using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager instance;

    [SerializeField] GameObject startButton;
    [SerializeField] Text playerCountText;
    [SerializeField] Text roomNameText;
    [SerializeField] Text joinedPlayerText;

    private Button startBtn;

    private enum LeaveDestination
    { 
        None,         // 戻り先：なし
        PrevUI,  // 戻り先：同シーン内でひとつ前の画面
        TitleScene,   // 戻り先：Titleシーン
    }
    private LeaveDestination leaveDest = LeaveDestination.None;

    private bool IsInTitleScene => SceneManager.GetActiveScene().name == "Title";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (IsInTitleScene)
        {
            startBtn = startButton.GetComponent<Button>();
        }
    }

    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("既にPhotonに接続されています。");
            return;
        }

        PhotonNetwork.ConnectUsingSettings();
    }

    // === 接続成功時に呼ばれるコールバック ===
    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon に接続成功！");
    }

    // 参加ボタンが押されたときに呼び出される
    public void JoinRandomRoom()
    {
        Debug.Log("ランダムルームに参加します。");
        PhotonNetwork.JoinRandomRoom();
    }

    // === ランダムルームが存在しない場合、新しいルームを作成 ===
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("ランダムルームが存在しないため、新しいルームを作成します。");

        var roomOptions = new RoomOptions
        {
            MaxPlayers = 2, // 最大プレイヤー数4人
            IsOpen = true, // ルームを一般公開する
            IsVisible = true, // ルームがロビーで表示される
        };
        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    // === ルームに参加したときのコールバック ===
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LocalPlayer.NickName = PlayerPrefs.GetString("PlayerName", $"Player{Random.Range(1000, 9999)}");
        PlayerPrefs.Save();

        roomNameText.text = $"ルーム名：{PhotonNetwork.CurrentRoom.Name}";

        // ホストのみゲームルールを選んで開始することができる
        startButton.SetActive(PhotonNetwork.IsMasterClient);

        UpdateLobbyUI();
    }

    private void UpdateLobbyUI()
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
        startButton.SetActive (PhotonNetwork.IsMasterClient);
        startBtn.interactable = PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers;
    }

    // 新しいプレイヤーが参加したときのコールバック
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName}が参加しました。");
        UpdateLobbyUI();
    }

    // プレイヤーが退出したときのコールバック
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName}がルームから退出しました。");

        if (IsInTitleScene)
        {
            UpdateLobbyUI();
            return;
        }

        // 誰かがルームを抜けてしまった場合、ルームを解散してタイトル画面に戻る
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false; // ルームを閉じる
            PhotonNetwork.CurrentRoom.IsVisible = false; // ルームを非表示にする
        }
        LeaveRoomToTitleScene();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"ホストが {newMasterClient.NickName} に変更されました。");

        if (IsInTitleScene)
        {
            UpdateLobbyUI();
            return;
        }

        // ゲーム中にホストが落ちた＝解散
        Debug.Log("ホストが落ちたので解散します。");
        LeaveRoomToTitleScene();
    }

    public void LeaveLobbyToPrevUI()
    {
        leaveDest = LeaveDestination.PrevUI;
        PhotonNetwork.LeaveRoom();
    }

    public void LeaveRoomToTitleScene()
    {
        Debug.Log("ルームから退出します（Titleシーンへ）。");
        leaveDest = LeaveDestination.TitleScene;

        // Masterはついでに閉じる（ベストエフォート）
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }

        PhotonNetwork.LeaveRoom();
    }

    // ルームから退出する
    public void OnLeaveRoom()
    {
        if (IsInTitleScene)
        {
            LeaveLobbyToPrevUI();
        }
        else
        {
            LeaveRoomToTitleScene();
        }
    }

    // ルームから退出する(タイトルシーンに戻る)
    public void OnLeaveRoomAndDestroy()
    {
        PhotonNetwork.LeaveRoom();
    }

    // === ルームから退出したときのコールバック ===
    public override void OnLeftRoom()
    {
        Debug.Log("ルームから退出しました。");

        switch (leaveDest)
        { 
            case LeaveDestination.PrevUI:
                break;

            case LeaveDestination.TitleScene:
            default:
                SceneController.instance.LoadScene("Title");
                Destroy(gameObject);
                break;
        }
        leaveDest = LeaveDestination.None;
    }

    // === 接続失敗時に呼ばれるコールバック ===
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Photon の接続に失敗: {cause}");
    }
}
