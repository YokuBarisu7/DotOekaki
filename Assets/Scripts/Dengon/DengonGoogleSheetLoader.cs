using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class DengonGoogleSheetLoader : MonoBehaviour
{
    private string googleSheetUrl = "https://docs.google.com/spreadsheets/d/1-ZkSlHxNQVkk2DJVHGwUO6oNraK1ibwEig9aRI-bN0I/gviz/tq?tqx=out:csv";
    public List<DengonTheme> themes = new List<DengonTheme>();

    public void LoadDataFromGoogleSheetDengon(int mode, System.Action onLoaded)
    {
        StartCoroutine(LoadQuizData(mode, onLoaded));
    }

    // ホストのみが実行する
    private IEnumerator LoadQuizData(int mode, System.Action onLoaded)
    {
        UnityWebRequest request = UnityWebRequest.Get(googleSheetUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string csvData = request.downloadHandler.text;
            ParseCSVData(csvData, mode);
            Debug.Log("Data loaded successfully");
            onLoaded?.Invoke();
        }
        else
        {
            Debug.LogError("Failed to load CSV data: " + request.error);
        }
    }

    private void ParseCSVData(string csvData, int mode)
    {
        themes = new List<DengonTheme>();
        string[] dataLines = csvData.Split('\n');
        for (int i = 2; i < dataLines.Length; i++) // 1行目はヘッダー
        {
            string[] data = dataLines[i].Split(',');

            int themeIndex;
            int answerStartIndex;
            switch (mode)
            {
                case 0: themeIndex = 10; answerStartIndex = 11; break; // かんたん
                case 1: themeIndex = 15; answerStartIndex = 16; break; // ふつう
                case 2: themeIndex = 20; answerStartIndex = 21; break; // むずかしい
                default: themeIndex = 10; answerStartIndex = 11; break;
            }

            string themeText = (data.Length > themeIndex) ? ClearString(data[themeIndex]) : "";

            // 列数チェック＆お題が完全に空文字でないか
            if (!string.IsNullOrWhiteSpace(themeText))
            {
                var answerList = new List<string>();
                for (int j = 0; j < 3; j++)
                {
                    int idx = answerStartIndex + j;
                    if (data.Length > idx)
                        answerList.Add(ClearString(data[idx]));
                    else
                        answerList.Add("");
                }
                DengonTheme theme = new DengonTheme
                {
                    theme = ClearString(data[themeIndex]),
                    answerList = answerList
                };
                themes.Add(theme);
            }
        }
    }

    private string ClearString(string str)
    {
        return str.Trim().Replace("\"", "");
    }
}

[System.Serializable]
public class DengonTheme
{
    public string theme; // お題
    public List<string> answerList; // 答えのリスト
}