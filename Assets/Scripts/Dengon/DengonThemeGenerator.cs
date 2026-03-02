using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;

public class DengonThemeGenerator : MonoBehaviourPunCallbacks
{
    [SerializeField] DengonGoogleSheetLoader dengonGoogleSheetLoader;
    List<DengonTheme> themeList; // 開始時にホストがスプシから取得したお題だけのリスト

    [SerializeField] int mode; // 0: かんたん、1: ふつう、2: むずかしい

    Coroutine retryCo;

    const string KEY_JSON = "DengonThemesJson";
    const string KEY_VER = "DengonThemeVersion";

    [System.Serializable]
    public class DengonThemeListWrapper
    {
        public List<DengonTheme> themes;
        public DengonThemeListWrapper(List<DengonTheme> t) { themes = t; }
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // ホストはスプレッドシートから問題リストを取得し、同期する
            GenerateAndBroadcastThemes(true);
        }

        TryApplyThemesOrRetry();
    }

    public void RegenerateAndBroadcastThemes()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        GenerateAndBroadcastThemes(false);
    }

    private void GenerateAndBroadcastThemes(bool isFirst)
    {
        mode = PlayerPrefs.GetInt("Difficulty", 0);

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
            TryApplyThemesOrRetry();
        });
    }

    private void TryApplyThemesOrRetry()
    {
        if (TryApplyRoomThemes())
        {
            StopRetry();
            return;
        }

        if (retryCo == null) retryCo = StartCoroutine(RetryApplyCoroutine());
    }

    private void StopRetry()
    {
        if (retryCo != null)
        {
            StopCoroutine(retryCo);
            retryCo = null;
        }
    }

    private IEnumerator RetryApplyCoroutine()
    {
        int maxTries = 20;
        float interval = 0.5f;

        for (int i = 0; i < maxTries; i++) 
        {
            if (TryApplyRoomThemes())
            {
                StopRetry();
                yield break;
            }
            yield return new WaitForSeconds(interval);
        }

        Debug.LogWarning("Theme Readyに失敗しました。");
        StopRetry();
    }

    private bool TryApplyRoomThemes()
    {
        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_JSON, out var obj)) return false;
        string json = obj as string;
        if (string.IsNullOrEmpty(json)) return false;

        var wrapper = JsonUtility.FromJson<DengonThemeListWrapper>(json);
        themeList = wrapper.themes;

        return SetThemeReady();
    }

    private bool SetThemeReady()
    {
        var ordered = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToList();
        int index = ordered.FindIndex(p => p.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber);
        if (index < 0 || index >= themeList.Count) return false;

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
        return true;
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
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changed)
    {
        if (changed.ContainsKey(KEY_JSON) || changed.ContainsKey(KEY_VER))
        {
            TryApplyThemesOrRetry();
        }
    }
}
