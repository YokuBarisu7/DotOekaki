using UnityEngine;
using UnityEngine.UI;

public class TimerController : MonoBehaviour
{
    [SerializeField] Text timerText;

    // 制限時間
    private float timer;
    private bool timerStarted;


    public void StartTimer(int time)
    {
        timer = time;
        timerStarted = true;
    }

    public void StopTimer()
    { 
        timerStarted = false;
        timer = 0f;
        timerText.text = "残り: 0秒";
    }

    void Update()
    {
        if (timerStarted)
        {
            timer -= Time.deltaTime;
            int timerInt = (int)timer;
            timerText.text = "残り: " + timerInt.ToString() + "秒";
            if (timer <= 0 && timerStarted)
            {
                StopTimer();
                EshiritoriManager.instance.TimeUp();
            }
        }
    }
}
