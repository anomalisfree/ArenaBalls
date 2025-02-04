using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestMode : MonoBehaviour
{
    [SerializeField] private WebSocketClient webSocketClient;

    [SerializeField] private Canvas canvasInit;
    [SerializeField] private Canvas canvasTestMode;

    [SerializeField] private TMP_Text teamOneCount;
    [SerializeField] private TMP_Text teamTwoCount;
    [SerializeField] private Image startButtonStatus;

    [SerializeField] private Button resetCountersButton;
    [SerializeField] private Toggle linac_a;
    [SerializeField] private Toggle linac_b;
    [SerializeField] private Button openAllLinacButton;
    [SerializeField] private Button closeAllLinacButton;
    [SerializeField] private Toggle blower_0;
    [SerializeField] private Toggle blower_1;
    [SerializeField] private Button openAllBlowerButton;
    [SerializeField] private Button closeAllBlowerButton;
    [SerializeField] private Toggle startLed;

    private void Start()
    {
        canvasTestMode.enabled = false;
        canvasInit.enabled = true;
        webSocketClient.Mode = Mode.TEST;

        webSocketClient.OnReceiveData += OnReceiveData;
        resetCountersButton.onClick.AddListener(() => { webSocketClient.ResetCounters = true; });
        linac_a.onValueChanged.AddListener((isOn) => { webSocketClient.IsLinacAOn = isOn; });
        linac_b.onValueChanged.AddListener((isOn) => { webSocketClient.IsLinacBOn = isOn; });
        openAllLinacButton.onClick.AddListener(() =>
        {
            linac_a.isOn = true;
            linac_b.isOn = true;
        });
        closeAllLinacButton.onClick.AddListener(() =>
        {
            linac_a.isOn = false;
            linac_b.isOn = false;
        });
        blower_0.onValueChanged.AddListener((isOn) => { webSocketClient.IsBlower0On = isOn; });
        blower_1.onValueChanged.AddListener((isOn) => { webSocketClient.IsBlower1On = isOn; });
        openAllBlowerButton.onClick.AddListener(() =>
        {
            blower_0.isOn = true;
            blower_1.isOn = true;
        });
        closeAllBlowerButton.onClick.AddListener(() =>
        {
            blower_0.isOn = false;
            blower_1.isOn = false;
        });
        startLed.onValueChanged.AddListener((isOn) => { webSocketClient.IsStartLedOn = isOn; });

    }

    private void OnReceiveData(ReceiveData data)
    {
        if (data.mode != ((int)Mode.TEST))
        {
            canvasTestMode.enabled = false;
            canvasInit.enabled = true;
            webSocketClient.Mode = Mode.TEST;
            return;
        }
        else
        {
            canvasInit.enabled = false;
            canvasTestMode.enabled = true;

            teamOneCount.text = data.counter_0.ToString();
            teamTwoCount.text = data.counter_1.ToString();

            startButtonStatus.color = data.start_btn ? Color.yellow : Color.white;
        }
    }
}
