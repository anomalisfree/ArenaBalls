using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;
using System;
using System.Collections;

public class WebSocketClient : MonoBehaviour
{
    [SerializeField] private string ip = "192.168.1.32";
    [SerializeField] private int port = 8765;
    [SerializeField] private float sentDataInterval = 0.1f;

    public UnityAction<ReceiveData> OnReceiveData;

    public Mode Mode
    {
        get => _broadcastData.mode;
        set => _broadcastData.mode = value;
    }

    public bool ResetCounters
    {
        get => _broadcastData.resetCounters;
        set => _broadcastData.resetCounters = value;
    }

    public float LinacDelayTime
    {
        get => _broadcastData.linacDelayTime;
        set => _broadcastData.linacDelayTime = value;
    }

    public bool IsLinacAOn
    {
        get => _broadcastData.isLinacAOn;
        set => _broadcastData.isLinacAOn = value;
    }

    public bool IsLinacBOn
    {
        get => _broadcastData.isLinacBOn;
        set => _broadcastData.isLinacBOn = value;
    }

    public bool IsBlower0On
    {
        get => _broadcastData.isBlower0On;
        set => _broadcastData.isBlower0On = value;
    }

    public bool IsBlower1On
    {
        get => _broadcastData.isBlower1On;
        set => _broadcastData.isBlower1On = value;
    }

    public bool IsStartLedOn
    {
        get => _broadcastData.isStartLedOn;
        set => _broadcastData.isStartLedOn = value;
    }

    public void AllLinacActive(bool value)
    {
        IsLinacAOn = value;
        IsLinacBOn = value;
    }

    public void AllBlowerActive(bool value)
    {
        IsBlower0On = value;
        IsBlower1On = value;
    }

    private WebSocket ws;
    private BroadcastData _broadcastData = new();

    private void Start()
    {
        StartSession();
        StartCoroutine(StartSessionCoroutine());
    }

    IEnumerator StartSessionCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(sentDataInterval);
            SendData();
        }
    }

    private void StartSession()
    {
        ws = new WebSocket($"ws://{ip}:{port}");
        ws.OnMessage += OnMessageReceived;
        ws.OnOpen += OnWebSocketOpen;
        ws.OnClose += OnWebSocketClose;
        ws.Connect();
    }

    private void OnWebSocketOpen(object sender, EventArgs e)
    {
        Debug.Log("WebSocket connected");
    }

    private void OnWebSocketClose(object sender, CloseEventArgs e)
    {
        Debug.Log("WebSocket disconnected");
    }

    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        try
        {
            ReceiveData receiveData = JsonUtility.FromJson<ReceiveData>(e.Data);

            UnityMainThreadDispatcher.Enqueue(() =>
            {
                if (_broadcastData.resetCounters && receiveData.counter_0 == 0 && receiveData.counter_1 == 0)
                    _broadcastData.resetCounters = false;

                OnReceiveData?.Invoke(receiveData);
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("Error processing message: " + ex.Message);
        }
    }

    private void SendData()
    {
        if (ws != null && ws.IsAlive)
        {
            string jsonData = JsonUtility.ToJson(_broadcastData);
            ws.Send(jsonData);
        }
        else
        {
            Debug.LogWarning("WebSocket not connected.");
        }
    }
}
