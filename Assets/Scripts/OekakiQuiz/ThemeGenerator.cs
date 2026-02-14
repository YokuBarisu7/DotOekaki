using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class ThemeGenerator : MonoBehaviourPunCallbacks
{
    [SerializeField] GoogleSheetLoader googleSheetLoader;
    List<QuizQuestion> themeList;
    List<int> themeListIndex;

    [SerializeField] int tsuyuMode; // 0: 通常, 1: つゆモード

    void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                tsuyuMode = PlayerPrefs.GetInt("Tsuyu", 0); // デフォルトは通常モード

                // ホストはスプレッドシートから問題リストを取得し、同期する
                googleSheetLoader.LoadDataFromGoogleSheet(tsuyuMode);
                themeList = googleSheetLoader.questions;

                // 問題の数分のインデックスを生成し、同期する
                GenerateAndShuffleIndex();

                SetReady(true); // ホストはこのタイミングで準備完了状態になる
            }
            else
            {
                // 入室時点でSharedQuestionsが既に存在していれば取得
                if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SharedQuestions"))
                {
                    string serializedQuestions = PhotonNetwork.CurrentRoom.CustomProperties["SharedQuestions"] as string;
                    themeList = googleSheetLoader.DeserializeQuestions(serializedQuestions);
                    Debug.Log("入室時にSharedQuestionsを取得しました");
                }
            }
        }
    }

    // ルームのカスタムプロパティが変更されると自動的に呼ばれるコールバック
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("SharedQuestions"))
        {
            string serializedQuestions = propertiesThatChanged["SharedQuestions"] as string;
            themeList = googleSheetLoader.DeserializeQuestions(serializedQuestions);
            Debug.Log("クイズリストをルームから受信しました！");

            SetReady(true); // クイズリストを受信したら、プレイヤーを準備完了状態にする
        }
    }

    private void SetReady(bool isReady)
    { 
        var props = new ExitGames.Client.Photon.Hashtable
        {
            { "Ready", isReady }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void GenerateAndShuffleIndex()
    {
        themeListIndex = new List<int>();
        for (int i = 0; i < themeList.Count; i++)
        {
            themeListIndex.Add(i);
        }
        for (int i = themeListIndex.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = themeListIndex[i];
            themeListIndex[i] = themeListIndex[j];
            themeListIndex[j] = temp;
        }
        photonView.RPC("SyncIndex", RpcTarget.All, themeListIndex.ToArray());
    }

    [PunRPC]
    public void SyncIndex(int[] numbers)
    {
        themeListIndex = new List<int>(numbers);
    }

    // 重複を許さずにランダムなお題を取得する
    public QuizQuestion GetRandomTheme(int index)
    {
        QuizQuestion theme = themeList[themeListIndex[index]];
        return theme;
    }
}
