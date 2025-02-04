using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayLogic : MonoBehaviour
{
    [SerializeField] private WebSocketClient webSocketClient;
    [SerializeField] private Canvas canvasInit;
    [SerializeField] private Canvas canvasGame;

    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text mainText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject counters;
    [SerializeField] private TMP_Text teamOneCount;
    [SerializeField] private TMP_Text teamTwoCount;

    [SerializeField] private float gameDuration = 30f;

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

        webSocketClient.Mode = Mode.DEFAULT;
        webSocketClient.OnReceiveData += OnReceiveData;
        startButton.onClick.AddListener(() => { webSocketClient.Mode = Mode.GAME_START; });
    }

    private void OnReceiveData(ReceiveData data)
    {
        if(data.mode == ((int)Mode.DEFAULT))
        {
            canvasGame.enabled = true;
            canvasInit.enabled = false;
            startButton.gameObject.SetActive(true);
            return;
        }
        else if(data.mode == ((int)Mode.GAME_START))
        {
            canvasGame.enabled = true;
            canvasInit.enabled = false;
            startButton.gameObject.SetActive(false);

            if (!data.game_on)
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

                counters.SetActive(true);
                teamOneCount.text = data.counter_0.ToString();
                teamTwoCount.text = data.counter_1.ToString();
            }

            return;
        }
        else if(data.mode == ((int)Mode.GAME_END))
        {
            startButton.gameObject.SetActive(true);
            counters.SetActive(false);

            if (data.counter_0 > data.counter_1)
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
        else
        {
            canvasGame.enabled = false;
            canvasInit.enabled = true;

            return;
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
                webSocketClient.Mode = Mode.GAME_END;
            }
        }
    }
}
