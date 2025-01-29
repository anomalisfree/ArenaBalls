using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GamePlayLogic : MonoBehaviour
{
    [SerializeField] private ArenaBallsController arenaBallsController;
    [SerializeField] private Canvas canvasInit;
    [SerializeField] private Canvas canvasGame;

    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text mainText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject counters;
    [SerializeField] private TMP_Text teamOneCount;
    [SerializeField] private TMP_Text teamTwoCount;

    [SerializeField] private float gameDuration = 30f;

    private bool _arenaBallsReadyToStart;
    private float _timer;
    private bool _timerRunning;

    private void Start()
    {
        canvasGame.enabled = false;
        canvasInit.enabled = true;

        startButton.gameObject.SetActive(false);
        mainText.text = "";
        timerText.text = "";
        counters.SetActive(false);

        arenaBallsController.Mode = 0;
        arenaBallsController.OnReceiveData += OnReceiveData;
        startButton.onClick.AddListener(() => { arenaBallsController.Mode = 2; });
    }



    private void OnReceiveData(ReceiveData data)
    {
        if (!data.initialized)
        {
            _arenaBallsReadyToStart = false;
            canvasInit.enabled = true;
            canvasGame.enabled = false;
            return;
        }

        if (!_arenaBallsReadyToStart && data.currentMode == 0)
        {
            _arenaBallsReadyToStart = true;
            canvasInit.enabled = false;
            canvasGame.enabled = true;
        }

        if (_arenaBallsReadyToStart)
        {
            if (data.currentMode == 0)
            {
                startButton.gameObject.SetActive(true);
                mainText.text = "";
                timerText.text = "";
                counters.SetActive(false);
            }
            else if (data.currentMode == 2)
            {
                startButton.gameObject.SetActive(false);
                counters.SetActive(data.counting);

                if (!data.gameOn)
                {
                    mainText.text = "Ready?";
                    timerText.text = "";
                    _timer = gameDuration;
                    _timerRunning = false;
                }
                else
                {
                    mainText.text = "Go!";
                    _timerRunning = true;

                    teamOneCount.text = data.counter_0.ToString();
                    teamTwoCount.text = data.counter_1.ToString();
                }
            }
            else if (data.currentMode == 3)
            {
                startButton.gameObject.SetActive(true);

                if(data.counter_0 > data.counter_1)
                {
                    mainText.text = "Team 1 Wins!";
                }
                else if (data.counter_1 > data.counter_0)
                {
                    mainText.text = "Team 2 Wins!";
                }
                else
                {
                    mainText.text = "It's a tie!";
                }

                timerText.text = "";
                counters.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (_timerRunning)
        {
            _timer -= Time.deltaTime;
            timerText.text = _timer.ToString("00");

            if (_timer <= 0)
            {
                _timer = 0;
                _timerRunning = false;
                arenaBallsController.Mode = 3;
            }
        }
    }
}
