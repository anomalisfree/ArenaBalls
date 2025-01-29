using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestMode : MonoBehaviour
{
    [SerializeField] private ArenaBallsController arenaBallsController;
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
        arenaBallsController.Mode = 1;
        arenaBallsController.OnReceiveData += OnReceiveData;
        resetCountersButton.onClick.AddListener(() => { arenaBallsController.ResetCounters = true; });
        linac_a.onValueChanged.AddListener((isOn) => { arenaBallsController.IsLinacAOn = isOn; });
        linac_b.onValueChanged.AddListener((isOn) => { arenaBallsController.IsLinacBOn = isOn; });
        openAllLinacButton.onClick.AddListener(() => { 
            linac_a.isOn = true;
            linac_b.isOn = true;
        });
        closeAllLinacButton.onClick.AddListener(() => { 
            linac_a.isOn = false;
            linac_b.isOn = false;
        });
        blower_0.onValueChanged.AddListener((isOn) => { arenaBallsController.IsBlower0On = isOn; });
        blower_1.onValueChanged.AddListener((isOn) => { arenaBallsController.IsBlower1On = isOn; });
        openAllBlowerButton.onClick.AddListener(() => { 
            blower_0.isOn = true;
            blower_1.isOn = true;
        });
        closeAllBlowerButton.onClick.AddListener(() => {
            blower_0.isOn = false;
            blower_1.isOn = false;
        });
        startLed.onValueChanged.AddListener((isOn) => { arenaBallsController.IsStartLedOn = isOn; });
    }

    private void OnReceiveData(ReceiveData data)
    {
        if (!data.initialized)
        {
            canvasInit.enabled = true;
            canvasTestMode.enabled = false;
            return;
        }

        if (data.currentMode == 1)
        {
            canvasInit.enabled = false;
            canvasTestMode.enabled = true;
        }
        else
        {
            canvasInit.enabled = true;
            canvasTestMode.enabled = false;
            arenaBallsController.Mode = 1;
            return;
        }

        teamOneCount.text = data.counter_0.ToString();
        teamTwoCount.text = data.counter_1.ToString();

        startButtonStatus.color = data.startButton ? Color.yellow : Color.white;
    }
}
