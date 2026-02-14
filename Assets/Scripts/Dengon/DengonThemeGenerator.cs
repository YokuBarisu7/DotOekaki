using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;
using System.Linq;

public class DengonThemeGenerator : MonoBehaviourPunCallbacks
{
    [SerializeField] DengonGoogleSheetLoader dengonGoogleSheetLoader;
    List<DengonTheme> themeList; // 開始時にホストがスプシから取得したお題だけのリスト

    [SerializeField] int mode; // 0: かんたん、1: ふつう、2: むずかしい

    const string KEY_JSON = "DengonThemesJson";
    const string KEY_VER = "DengonThemeVersion";

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // ホストはスプレッドシートから問題リストを取得し、同期する
            GenerateAndBroadcastThemes(true);
        }
        else
        {
            // 参加者用
            TryApplyRoomThemes();
        }
    }

    public void RegenerateAndBroadcastThemes()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        GenerateAndBroadcastThemes(false);
    }

    private void GenerateAndBroadcastThemes(bool isFirst)
    {
        mode = PlayerPrefs.GetInt("Mode", 0);

        dengonGoogleSheetLoader.LoadDataFromGoogleSheetDengon(mode, () =>
        {
            var pool = dengonGoogleSheetLoader.themes;
            var selected = GetRandomThemeFrom(pool, PhotonNetwork.PlayerList.Length);

            string json = JsonUtility.ToJson(new DengonThemeListWrapper(selected));

            int nextVer = 1;
            if (!isFirst &&
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_VER, out var vObj) &&
                vObj is int v)
            {
                nextVer = v + 1;
            }

            var roomProps = new ExitGames.Client.Photon.Hashtable
            {
                { KEY_JSON, json },
                { KEY_VER,  nextVer }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        });
    }

    private void TryApplyRoomThemes()
    {
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_JSON, out var obj)) return;
        string json = obj as string;
        if (string.IsNullOrEmpty(json)) return;

        var wrapper = JsonUtility.FromJson<DengonThemeListWrapper>(json);
        themeList = wrapper.themes;

        SetThemeReady();
    }

    private void SetThemeReady()
    {
        var ordered = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToList();
        int index = ordered.FindIndex(p => p.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber);
        if (index < 0 || index >= themeList.Count) return;

        DengonGameManager.instance.SetTheme(themeList[index].theme);

        int roomVer = 0;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_VER, out var vObj) && vObj is int v)
            roomVer = v;

        var props = new ExitGames.Client.Photon.Hashtable
        {
            { "ThemeReceived", true },
            { "ThemeVersion", roomVer }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // DengonTheme[]をラップするクラス（JsonUtility用）
    [System.Serializable]
    public class DengonThemeListWrapper
    {
        public List<DengonTheme> themes;
        public DengonThemeListWrapper(List<DengonTheme> t) { themes = t; }
    }

    // 重複を許さずにランダムなお題を取得する
    private List<DengonTheme> GetRandomThemeFrom(List<DengonTheme> source, int number)
    {
        var list = new List<DengonTheme>(source);
        var rng = new System.Random();
        for (int n = list.Count; n > 1; n--)
        {
            int k = rng.Next(n);
            (list[n - 1], list[k]) = (list[k], list[n - 1]);
        }
        return list.GetRange(0, Mathf.Min(number, list.Count));
    }


    // コールバック：ルームプロパティが更新されたとき

    public override void OnRoomPropertiesUpdate(Hashtable changed)
    {
        if (changed.ContainsKey(KEY_JSON) || changed.ContainsKey(KEY_VER))
        {
            TryApplyRoomThemes();
        }
    }
}
