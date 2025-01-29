using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System;
using System.Threading;
using System.Collections.Generic;
using TMPro;

public class UnitySerialPort : MonoBehaviour
{
    public static UnitySerialPort Instance;

    #region Properties

    public SerialPort SerialPort;
    Thread SerialLoopThread;

    [Header("SerialPort")]
    public string ComPort = "COM4";
    public int BaudRate = 115200;
    public Parity Parity = Parity.None;
    public StopBits StopBits = StopBits.One;
    public int DataBits = 8;
    public bool DtrEnable;
    public bool RtsEnable;

    private string portStatus = "";
    public string PortStatus
    {
        get { return portStatus; }
        set { portStatus = value; }
    }

    public int ReadTimeout = 10;
    public int WriteTimeout = 10;

    private bool isRunning = false;
    public bool IsRunning
    {
        get { return isRunning; }
        set { isRunning = value; }
    }

    private string rawData = "Ready";
    public string RawData
    {
        get { return rawData; }
        set { rawData = value; }
    }

    private string[] chunkData;
    public string[] ChunkData
    {
        get { return chunkData; }
        set { chunkData = value; }
    }

    [Header("GUI Fields")]
    public TMP_Text ComStatusText;
    public TMP_Text RawDataText;
    public TMP_Text StatusMsgBox;

    public delegate void SerialDataParseEventHandler(string[] data, string rawData);
    public static event SerialDataParseEventHandler SerialDataParseEvent;

    public delegate void SerialPortOpenEventHandler();
    public static event SerialPortOpenEventHandler SerialPortOpenEvent;

    public delegate void SerialPortCloseEventHandler();
    public static event SerialPortCloseEventHandler SerialPortCloseEvent;

    public delegate void SerialPortSentDataEventHandler(string data);
    public static event SerialPortSentDataEventHandler SerialPortSentDataEvent;

    public delegate void SerialPortSentLineDataEventHandler(string data);
    public static event SerialPortSentLineDataEventHandler SerialPortSentLineDataEvent;

    public enum LoopMethods { Threading, Coroutine }

    [Header("Options")]
    [SerializeField]
    public LoopMethods LoopMethod = LoopMethods.Coroutine;

    public bool OpenPortOnStart = false;
    public bool ShowDebugs = true;

    private ArrayList comPorts = new ArrayList();

    [Header("Misc")]
    public List<string> ComPorts = new List<string>();

    [Header("Data Read")]
    public ReadMethod ReadDataMethod = ReadMethod.ReadLine;
    public enum ReadMethod
    {
        ReadLine,
        ReadToChar
    }

    public string Delimiter;
    public char Separator;

    #endregion Properties

    #region Unity Frame Events

    void Awake()
    {
        Instance = this;

        if (ComStatusText != null)
        {
            ComStatusText.text = "ComStatus: Closed";
        }
    }

    void Start()
    {
        SerialPortOpenEvent += new SerialPortOpenEventHandler(UnitySerialPort_SerialPortOpenEvent);
        SerialPortCloseEvent += new SerialPortCloseEventHandler(UnitySerialPort_SerialPortCloseEvent);
        SerialPortSentDataEvent += new SerialPortSentDataEventHandler(UnitySerialPort_SerialPortSentDataEvent);
        SerialPortSentLineDataEvent += new SerialPortSentLineDataEventHandler(UnitySerialPort_SerialPortSentLineDataEvent);
        SerialDataParseEvent += new SerialDataParseEventHandler(UnitySerialPort_SerialDataParseEvent);

        PopulateComPorts();

        if (OpenPortOnStart)
        {
            OpenSerialPort();
        }
    }

    void OnDestroy()
    {
        if (SerialDataParseEvent != null)
            SerialDataParseEvent -= UnitySerialPort_SerialDataParseEvent;

        if (SerialPortOpenEvent != null)
            SerialPortOpenEvent -= UnitySerialPort_SerialPortOpenEvent;

        if (SerialPortCloseEvent != null)
            SerialPortCloseEvent -= UnitySerialPort_SerialPortCloseEvent;

        if (SerialPortSentDataEvent != null)
            SerialPortSentDataEvent -= UnitySerialPort_SerialPortSentDataEvent;

        if (SerialPortSentLineDataEvent != null)
            SerialPortSentLineDataEvent -= UnitySerialPort_SerialPortSentLineDataEvent;
    }

    void Update()
    {
        if (SerialPort == null || SerialPort.IsOpen == false)
        {
            return;
        }

        try
        {
            if (RawDataText != null)
                RawDataText.text = RawData;
        }
        catch (Exception ex)
        {
            Debug.Log("Error: " + ex.Message);
        }
    }

    void OnApplicationQuit()
    {
        CloseSerialPort();

        Thread.Sleep(100);

        if (LoopMethod == LoopMethods.Coroutine)
            StopSerialCoroutine();

        if (LoopMethod == LoopMethods.Threading)
            StopSerialThreading();

        Thread.Sleep(100);
    }

    #endregion Unity Frame Events

    #region Notification Events

    void UnitySerialPort_SerialDataParseEvent(string[] Data, string RawData)
    {
        if (ShowDebugs)
            print("Data Received via port: " + RawData);
    }

    void UnitySerialPort_SerialPortOpenEvent()
    {
        portStatus = "The serial port: " + ComPort + " is now open!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    void UnitySerialPort_SerialPortCloseEvent()
    {
        portStatus = "The serial port: " + ComPort + " is now closed!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    void UnitySerialPort_SerialPortSentDataEvent(string Data)
    {
        portStatus = "Sent data: " + Data;

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    void UnitySerialPort_SerialPortSentLineDataEvent(string Data)
    {
        portStatus = "Sent data as line: " + Data;

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    #endregion Notification Events

    #region Object Serial Port

    public void OpenSerialPort()
    {
        try
        {
            SerialPort = new SerialPort(ComPort, BaudRate, Parity, DataBits, StopBits);

            SerialPort.ReadTimeout = ReadTimeout;
            SerialPort.WriteTimeout = WriteTimeout;

            SerialPort.DtrEnable = DtrEnable;
            SerialPort.RtsEnable = RtsEnable;

            SerialPort.Open();

            if (Instance != null && Instance.ComStatusText != null)
            {
                Instance.ComStatusText.text = "ComStatus: Open";
            }

            if (LoopMethod == LoopMethods.Coroutine)
            {
                if (isRunning)
                {
                    StopSerialCoroutine();
                }

                StartSerialCoroutine();
            }

            if (LoopMethod == LoopMethods.Threading)
            {
                if (isRunning)
                {
                    StopSerialThreading();
                }

                StartSerialThread();
            }

            portStatus = "The serial port is now open!";

            if (ShowDebugs)
                ShowDebugMessages(portStatus);
        }
        catch (Exception ex)
        {
            Debug.Log("Error: " + ex.Message);
        }

        if (SerialPortOpenEvent != null)
            SerialPortOpenEvent();
    }

    public void CloseSerialPort()
    {
        try
        {
            SerialPort.Close();

            if (Instance.ComStatusText != null)
            {
                Instance.ComStatusText.text = "ComStatus: Closed";
            }
        }
        catch (Exception ex)
        {
            if (SerialPort == null || SerialPort.IsOpen == false)
            {
                // Port is already closed
            }
            else
            {
                Debug.Log("Error: " + ex.Message);
            }
        }

        if (LoopMethod == LoopMethods.Coroutine)
            StopSerialCoroutine();

        if (LoopMethod == LoopMethods.Threading)
            StopSerialThreading();

        portStatus = "Serial port closed!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);

        if (SerialPortCloseEvent != null)
            SerialPortCloseEvent();
    }

    #endregion Object Serial Port

    #region Serial Threading

    void StartSerialThread()
    {
        isRunning = true;

        SerialLoopThread = new Thread(SerialThreadLoop);
        SerialLoopThread.Start();
    }

    void SerialThreadLoop()
    {
        while (isRunning)
        {
            if (isRunning == false)
                break;

            GenericSerialLoop();
        }

        portStatus = "Ending Serial Thread!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    public void StopSerialThreading()
    {
        isRunning = false;

        Thread.Sleep(100);

        if (SerialLoopThread != null && SerialLoopThread.IsAlive)
            SerialLoopThread.Abort();

        Thread.Sleep(100);

        if (SerialLoopThread != null)
            SerialLoopThread = null;

        if (SerialPort != null)
        {
            SerialPort = null;
        }

        portStatus = "Ended Serial Loop Thread!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    #endregion Serial Threading

    #region Serial Coroutine

    public void StartSerialCoroutine()
    {
        isRunning = true;

        StartCoroutine("SerialCoroutineLoop");
    }

    public IEnumerator SerialCoroutineLoop()
    {
        while (isRunning)
        {
            GenericSerialLoop();
            yield return null;
        }

        portStatus = "Ending Coroutine!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    public void StopSerialCoroutine()
    {
        isRunning = false;

        Thread.Sleep(100);

        try
        {
            StopCoroutine("SerialCoroutineLoop");
        }
        catch (Exception ex)
        {
            portStatus = "Error: " + ex.Message;

            if (ShowDebugs)
                ShowDebugMessages(portStatus);
        }

        if (SerialPort != null)
        {
            SerialPort = null;
        }

        portStatus = "Ended Serial Loop Coroutine!";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    #endregion Serial Coroutine

    private void GenericSerialLoop()
    {
        try
        {
            if (SerialPort.IsOpen)
            {
                string rData = string.Empty;

                switch (ReadDataMethod)
                {
                    case ReadMethod.ReadLine:
                        rData = SerialPort.ReadLine();
                        break;
                    case ReadMethod.ReadToChar:
                        rData = SerialPort.ReadTo(Delimiter);
                        break;
                }

                if (rData != null && rData != "")
                {
                    RawData = rData;
                    ChunkData = RawData.Split(Separator);

                    ParseSerialData(ChunkData, RawData);
                }
            }
        }
        catch (TimeoutException)
        {
            // Ignoring timeout exceptions
        }
        catch (Exception ex)
        {
            if (SerialPort.IsOpen)
            {
                Debug.Log("Error: " + ex.Message);
            }
            else
            {
                Debug.Log("Error: Port Closed Exception!");
            }
        }
    }

    #region Methods

    public void SendSerialDataAsLine(string data)
    {
        if (SerialPort != null)
        {
            SerialPort.WriteLine(data);
        }

        portStatus = "Sent data: " + data;

        if (ShowDebugs)
            ShowDebugMessages(portStatus);

        if (SerialPortSentLineDataEvent != null)
            SerialPortSentLineDataEvent(data);
    }

    public void SendSerialData(string data)
    {
        if (SerialPort != null)
        {
            SerialPort.Write(data);
        }

        portStatus = "Sent data: " + data;

        if (ShowDebugs)
            ShowDebugMessages(portStatus);

        if (SerialPortSentDataEvent != null)
            SerialPortSentDataEvent(data);
    }

    private void ParseSerialData(string[] data, string rawData)
    {
        if (data != null && rawData != string.Empty)
        {
            if (SerialDataParseEvent != null)
                SerialDataParseEvent(data, rawData);
        }
    }

    public void PopulateComPorts()
    {
        foreach (string cPort in SerialPort.GetPortNames())
        {
            ComPorts.Add(cPort);
            comPorts.Add(cPort);
        }

        portStatus = "ComPort list population complete";

        if (ShowDebugs)
            ShowDebugMessages(portStatus);
    }

    public string UpdateComPort()
    {
        if (SerialPort != null && SerialPort.IsOpen)
        {
            CloseSerialPort();
        }

        int currentComPort = comPorts.IndexOf(ComPort);

        if (currentComPort + 1 <= comPorts.Count - 1)
        {
            ComPort = (string)comPorts[currentComPort + 1];
        }
        else
        {
            ComPort = (string)comPorts[0];
        }

        portStatus = "ComPort set to: " + ComPort;

        if (ShowDebugs)
            ShowDebugMessages(portStatus);

        return ComPort;
    }

    public void ShowDebugMessages(string portStatus)
    {
        if (StatusMsgBox != null)
            StatusMsgBox.text = portStatus;

        print(portStatus);
    }

    #endregion Methods
}
