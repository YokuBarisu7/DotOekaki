using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using ExitGames.Client.Photon;
using System;

public class ThemeGenerator : MonoBehaviourPunCallbacks
{
    [SerializeField] GoogleSheetLoader googleSheetLoader;

    List<QuizQuestion> themeList;
    List<int> themeListIndex;

    [SerializeField] int difficulty; // 0: ふつう、1: むずかしい
    int lastMode = -1;

    const string KEY_JSON = "OekakiQuizThemeJson";
    const string KEY_SEED = "OekakiQuizShuffleSeed";
    const string KEY_VER = "OekakiQuizThemeVer";

    public event Action<int> OnThemeApplied;

    [System.Serializable]
    private class QuizThemeListWrapper
    {
        public List<QuizQuestion> themes;
        public QuizThemeListWrapper(List<QuizQuestion> t) { themes = t; }
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            difficulty = PlayerPrefs.GetInt("Difficulty", 0);
            BroadcastThemes(difficulty);
        }

        TryApplyRoomTheme();
    }

    // 再プレイ時に呼ばれる
    public void BroadcastThemes(int difficulty)
    {
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        bool hasJson = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_JSON);

        // 初回 or mode変更 or ルームにまだJSONがない　➔　取得してJSONを更新
        if (!hasJson || lastMode != difficulty || themeList == null)
        {
            lastMode = difficulty;

            googleSheetLoader.LoadDataFromGoogleSheet(difficulty, () =>
            {
                var list = googleSheetLoader.questions; // クイズリストを取得
                RemoveInvalidQuestions(list); // 空欄を無くす

                themeList = list ?? new List<QuizQuestion>();

                string json = JsonUtility.ToJson(new QuizThemeListWrapper(themeList));

                int nextVer = GetRoomVer() + 1;
                int seed = CreateNewSeed();

                var props = new Hashtable
                {
                    { KEY_JSON, json },
                    { KEY_SEED, seed },
                    { KEY_VER, nextVer }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);

                ApplyThemeFromRoom(json, seed, nextVer); // ホストはここで ThemeReady
            });

            return;
        }

        // 同じmodeで再プレイ　➔　seedだけ更新
        int nextVer = GetRoomVer() + 1;
        int seed = CreateNewSeed();

        var props = new Hashtable
            {
                { KEY_SEED, seed },
                { KEY_VER,  nextVer }
            };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        // ホスト自身も即適用
        if (TryGetRoomJson(out var json))
        {
            ApplyThemeFromRoom(json, seed, nextVer);
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey(KEY_JSON) || changedProps.ContainsKey(KEY_SEED) || changedProps.ContainsKey(KEY_VER))
        {
            TryApplyRoomTheme();
        }
    }

    private void TryApplyRoomTheme()
    {
        if (!PhotonNetwork.InRoom) return;
        if (!TryGetRoomJson(out var json)) return;
        int seed = GetRoomSeed();
        int ver = GetRoomVer();
        
        ApplyThemeFromRoom(json, seed, ver);
    }

    private void ApplyThemeFromRoom(string json, int seed, int ver)
    {
        if (string.IsNullOrEmpty(json)) return;

        var wrapper = JsonUtility.FromJson<QuizThemeListWrapper>(json);
        themeList = wrapper?.themes ?? new List<QuizQuestion>();
        RemoveInvalidQuestions(themeList);

        // seedでシャッフルされた themeListIndex を作る
        themeListIndex = BuildShuffledIndex(themeList.Count, seed);

        // GameManagerへ ThemeReady 通知
        OnThemeApplied?.Invoke(ver);
    }

    public QuizQuestion GetTheme(int roundIndex)
    {
        if (themeList == null || themeListIndex == null) return null;
        if (roundIndex < 0 || roundIndex >= themeListIndex.Count) return null;

        int real = themeListIndex[roundIndex];
        if (real < 0 || real >= themeList.Count) return null;

        return themeList[real];
    }

    // -------------------- utils -----------------------

    private bool TryGetRoomJson(out string json)
    {
        json = null;
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_JSON, out var obj)) return false;
        json = obj as string;
        return !string.IsNullOrEmpty(json);
    }

    private int GetRoomSeed()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_SEED, out var obj) && obj is int s)
            return s;
        return 0; // seed未設定でも動くように（0固定）
    }

    private int GetRoomVer()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_VER, out var obj) && obj is int v)
            return v;
        return 0;
    }

    private int CreateNewSeed()
    {
        // 乱数の質はそこまで要らない。毎回変わればOK。
        return UnityEngine.Random.Range(int.MinValue, int.MaxValue);
    }

    private static List<int> BuildShuffledIndex(int count, int seed)
    {
        var idx = new List<int>(count);
        for (int i = 0; i < count; i++) idx.Add(i);

        var rng = new System.Random(seed);
        for (int i = count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (idx[i], idx[j]) = (idx[j], idx[i]);
        }
        return idx;
    }

    // お題が空になることを防ぐ
    private void RemoveInvalidQuestions(List<QuizQuestion> list)
    {
        if (list == null) return;
        list.RemoveAll(q => q == null || string.IsNullOrWhiteSpace(q.question) || q.answerList == null || q.answerList.Count == 0);
    }
}
