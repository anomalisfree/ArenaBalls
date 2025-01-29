using UnityEngine;
using UnityEngine.Events;

public class ArenaBallsController : MonoBehaviour
{
    public int Mode
    {
        get { return _broadcastData.mode; }
        set
        {
            _broadcastData.mode = value;
            SendData();
        }
    }

    public bool ResetCounters
    {
        get { return _broadcastData.resetCounters; }
        set
        {
            _broadcastData.resetCounters = value;
            SendData();
        }
    }

    public int SendDataDelayTime
    {
        get { return _broadcastData.sendDataDelayTime; }
        set
        {
            _broadcastData.sendDataDelayTime = value;
            SendData();
        }
    }

    public int CounterDelayTime
    {
        get { return _broadcastData.counterDelayTime; }
        set
        {
            _broadcastData.counterDelayTime = value;
            SendData();
        }
    }

    public int LinacDelayTime
    {
        get { return _broadcastData.linacDelayTime; }
        set
        {
            _broadcastData.linacDelayTime = value;
            SendData();
        }
    }

    public bool IsLinacAOn
    {
        get { return _broadcastData.isLinacAOn; }
        set
        {
            _broadcastData.isLinacAOn = value;
            SendData();
        }
    }

    public bool IsLinacBOn
    {
        get { return _broadcastData.isLinacBOn; }
        set
        {
            _broadcastData.isLinacBOn = value;
            SendData();
        }
    }

    public bool IsBlower0On
    {
        get { return _broadcastData.isBlower0On; }
        set
        {
            _broadcastData.isBlower0On = value;
            SendData();
        }
    }

    public bool IsBlower1On
    {
        get { return _broadcastData.isBlower1On; }
        set
        {
            _broadcastData.isBlower1On = value;
            SendData();
        }
    }

    public bool IsStartLedOn
    {
        get { return _broadcastData.isStartLedOn; }
        set
        {
            _broadcastData.isStartLedOn = value;
            SendData();
        }
    }

    public void SetAllLinac(bool isOn)
    {
        _broadcastData.isLinacAOn = isOn;
        _broadcastData.isLinacBOn = isOn;
        SendData();
    }

    public void SetAllBlower(bool isOn)
    {
        _broadcastData.isBlower0On = isOn;
        _broadcastData.isBlower1On = isOn;
        SendData();
    }

    public UnityAction<ReceiveData> OnReceiveData;

    [SerializeField]
    private float _sendDataInterval = 0.1f;

    public int[] ParsedEvtData;

    private UnitySerialPort _unitySerialPort;
    private BroadcastData _broadcastData = new();
    private float _sendDataTimer;

    private void Start()
    {
        StartSession();
    }


    private void StartSession()
    {
        _unitySerialPort = UnitySerialPort.Instance;
        _unitySerialPort.OpenSerialPort();

        UnitySerialPort.SerialDataParseEvent +=
           UnitySerialPortSerialDataParseEvent;
    }

    private void UnitySerialPortSerialDataParseEvent(string[] data, string rawData)
    {
        ParsedEvtData = new int[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            ParsedEvtData[i] = int.Parse(data[i]);
        }

        ReceiveData receiveData = new ReceiveData
        {
            initialized = data[0] == "1",
            currentMode = int.Parse(data[1]),
            gameOn = data[2] == "1",
            counting = data[3] == "1",
            counter_0 = int.Parse(data[4]),
            counter_1 = int.Parse(data[5]),
            startButton = data[6] == "1"
        };

        if (ResetCounters == true && receiveData.counter_0 == 0 && receiveData.counter_1 == 0)
        {
            ResetCounters = false;
        }

        UnityMainThreadDispatcher.Enqueue(() => OnReceiveData?.Invoke(receiveData));
    }

    private void SendData()
    {
        _sendDataTimer = _sendDataInterval;

        if (_unitySerialPort != null)
        {
            string data = string.Empty;
            data += _broadcastData.mode + ",";
            data += (_broadcastData.resetCounters ? 1 : 0) + ",";
            data += _broadcastData.sendDataDelayTime + ",";
            data += _broadcastData.counterDelayTime + ",";
            data += _broadcastData.linacDelayTime + ",";
            data += (_broadcastData.isLinacAOn ? 1 : 0) + ",";
            data += (_broadcastData.isLinacBOn ? 1 : 0) + ",";
            data += (_broadcastData.isBlower0On ? 1 : 0) + ",";
            data += (_broadcastData.isBlower1On ? 1 : 0) + ",";
            data += (_broadcastData.isStartLedOn ? 1 : 0) + ",";

            _unitySerialPort.SendSerialDataAsLine(data);
        }
    }
}
