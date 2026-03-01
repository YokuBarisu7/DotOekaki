using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanelController : MonoBehaviour
{
    [SerializeField] Text firstCharacter;
    [SerializeField] Text lastCharacter;
    [SerializeField] Text resultText;

    [SerializeField] Transform parent;
    [SerializeField] Transform lastObj;
    [SerializeField] GameObject imageViewPrefab;

    [SerializeField] Sprite maruSprite;
    [SerializeField] Sprite batsuSprite;
    [SerializeField] Image marubatsu;

    private List<ImageView> imageViews = new List<ImageView>();

    static readonly char[] chars =
    {
        'あ','い','う','え','お',
        'か','き','く','け','こ',
        'が','ぎ','ぐ','げ','ご',
        'さ','し','す','せ','そ',
        'ざ','じ','ず','ぜ','ぞ',
        'た','ち','つ','て','と',
        'だ','ぢ','づ','で','ど',
        'な','に','ぬ','ね','の',
        'は','ひ','ふ','へ','ほ',
        'ば','び','ぶ','べ','ぼ',
        'ぱ','ぴ','ぷ','ぺ','ぽ',
        'ま','み','む','め','も',
        'や','ゆ','よ',
        'ら','り','る','れ','ろ',
        'わ'
    };

    public char GetRandomHiragana()
    { 
        return chars[Random.Range(0, chars.Length)];
    }

    public void SetHiragana(string hiragana1, string hiragana2)
    {
        firstCharacter.text = hiragana1;
        lastCharacter.text = hiragana2;
    }

    public void CreateNewImageView()
    {
        var prefab = Instantiate(imageViewPrefab, parent.transform);

        int lastIndex = lastObj.GetSiblingIndex();
        prefab.transform.SetSiblingIndex(lastIndex);

        var imageView = prefab.GetComponent<ImageView>();
        imageView.SetText("", "");
        imageViews.Add(imageView);
    }

    public void SetText(string answer, string playerName, int index)
    {
        if (index < 0 || index >= imageViews.Count) return;
        imageViews[index].SetText(answer, playerName);
    }

    public void SetTexture(Texture2D texture, int index)
    {
        if (index <= 0 || index > imageViews.Count) return;
        imageViews[index - 1].Set(texture);
    }

    // 最後の結果発表
    public void DisplayResult(List<string> texts)
    {
        //textsの中身を確認
        Debug.Log("texts: " + string.Join(", ", texts));

        int n = imageViews.Count;
        List<bool> isCorrects = JudgeChain(texts);

        for (int i = 0; i < n; i++)
        { 
            var v = imageViews[i];

            string answer = texts[i + 1];
            v.SetAnswerText(answer); // 伏字の解除
            v.SetMaruBatsu(isCorrects[i]);
        }

        bool isCorrect = isCorrects.Count > n ? isCorrects[n] : false;
        SetMaruBatsu(isCorrect);
        resultText.gameObject.SetActive(true);
        ShowAccuracy(isCorrects);
    }

    // 正誤判定
    private List<bool> JudgeChain(List<string> texts) 
    {
        var results = new List<bool>(Mathf.Max(0, texts.Count - 1));
        for (int i = 1; i < texts.Count; i++)
        {
            string prev = texts[i - 1];
            string cur = texts[i];

            // 空欄が絡んだら問答無用で×
            if (string.IsNullOrEmpty(prev) || string.IsNullOrEmpty(cur))
            {
                results.Add(false);
                continue;
            }

            char prevLast = ShiritoriKana.GetTail(prev);
            char curFirst = ShiritoriKana.GetHead(cur);

            if (prevLast == '\0' || curFirst == '\0') { results.Add(false); }
            else
            {
                results.Add(prevLast == curFirst);
            }
        }
        return results;
    }

    private void ShowAccuracy(List<bool> results) 
    {
        int total = results.Count;
        if (total == 0)
        {
            resultText.text = "正解率：0%（ 0/0 ）";
        }

        int correct = 0;
        for (int i = 0; i < total; i++)
        {
            if (results[i]) correct++;
        }

        float percent = (float)correct / total * 100f;
        resultText.text = $"正解率：{percent:0.#}% ({correct}/{total})";
    }

    private void SetMaruBatsu(bool isMaru)
    {
        marubatsu.gameObject.SetActive(true);
        marubatsu.sprite = isMaru ? maruSprite : batsuSprite;
    }

    public void ClearAllImageView()
    { 
        foreach (var v in  imageViews) 
        {
            if (v != null) Destroy(v.gameObject);
        }
        imageViews.Clear();
    }
}
