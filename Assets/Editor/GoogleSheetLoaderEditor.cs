using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GoogleSheetLoader))]
public class GoogleSheetLoaderEditor : Editor
{
    [SerializeField] int mode;

    public override void OnInspectorGUI()
    {
        // デフォルトのインスペクタ描画
        DrawDefaultInspector();

        // カスタムインスペクタの描画
        GoogleSheetLoader myScript = (GoogleSheetLoader)target;
        if (GUILayout.Button("Load Data From Google Sheet Normal"))
        {
            myScript.LoadDataFromGoogleSheet(mode, null);
        }

        if (myScript.questions != null && myScript.questions.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quiz Data", EditorStyles.boldLabel);

            // 表形式のヘッダー
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Question", EditorStyles.boldLabel, GUILayout.Width(200));
            EditorGUILayout.LabelField("Answer 1", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("Answer 2", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("Answer 3", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // データの描画
            foreach (QuizQuestion question in myScript.questions)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(question.question, GUILayout.Width(200));
                for (int i = 0; i < question.answerList.Count; i++)
                {
                    EditorGUILayout.LabelField(question.answerList[i], GUILayout.Width(100));
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}