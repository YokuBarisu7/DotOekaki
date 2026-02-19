using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class GoogleSheetLoader : MonoBehaviour
{
    private string googleSheetUrlNormal = "https://docs.google.com/spreadsheets/d/1-ZkSlHxNQVkk2DJVHGwUO6oNraK1ibwEig9aRI-bN0I/gviz/tq?tqx=out:csv";
    public List<QuizQuestion> questions = new List<QuizQuestion>();

    [ContextMenu("Load Data From Google Sheet")]
    public void LoadDataFromGoogleSheet(int mode, System.Action onLoaded)
    {
        StartCoroutine(LoadQuizData(mode, onLoaded));
    }

    // ホストのみが実行する
    private IEnumerator LoadQuizData(int mode, System.Action onLoaded)
    {
        using var request = UnityWebRequest.Get(googleSheetUrlNormal);
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
        questions = new List<QuizQuestion>();
        string[] dataLines = csvData.Split('\n');
        for (int i = 2; i < dataLines.Length; i++) // 1,2行目はヘッダー
        {
            string[] data = dataLines[i].Split(',');

            int questionIndex;
            int answerStartIndex;
            switch (mode)
            {
                case 0: questionIndex = 0; answerStartIndex = 1; break; // ふつう
                case 1: questionIndex = 5; answerStartIndex = 6; break; // むずかしい
                default: questionIndex = 0; answerStartIndex = 1; break;
            }

            string themeText = (data.Length > questionIndex) ? ClearString(data[questionIndex]) : "";

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
                QuizQuestion question = new QuizQuestion
                {
                    question = ClearString(data[questionIndex]),
                    answerList = answerList
                };
                questions.Add(question);
            }
        }
    }

    private string ClearString(string str)
    {
        return str.Trim().Replace("\"", "");
    }
}

[System.Serializable]
public class QuizQuestion
{
    public string question; // お題
    public List<string> answerList; // 答えのリスト
}