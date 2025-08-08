using UnityEngine;
using TMPro;

public class LifeManagerUI : MonoBehaviour
{
    
    //[SerializeField] private GameObject _textObject;
    private float remainingTime;
    private bool running;
    //public TMP_Text uiText;


    void Update()
    {
        RunCounter();
    }

    public void SetCounter(int time)
    {
        if (running == true) return;
        remainingTime = time;
        running = true;
    }

    void RunCounter()
    {
        if (!running) return;

        remainingTime -= Time.deltaTime;
        UpdateDisplay();

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            running = false;
            //OnCounterEnd();
        }

    }

    void UpdateDisplay()
    {
        //_textObject.SetActive(true);
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        //uiText.text = $"{minutes:00}:{seconds:00}";
    }

    //public void OnCounterEnd()
    //{
    //    _textObject.SetActive(false);
    //    Debug.Log("Termino");
    //}
}