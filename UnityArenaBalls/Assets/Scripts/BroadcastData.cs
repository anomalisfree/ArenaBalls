using System;

[Serializable]
public class BroadcastData
{
    public Mode mode = Mode.DEFAULT;
    public bool resetCounters = false;
    public float linacDelayTime = 4;
    public bool isLinacAOn = false;
    public bool isLinacBOn = false;
    public bool isBlower0On = false;
    public bool isBlower1On = false;
    public bool isStartLedOn = false;
}

[Serializable]
public enum Mode
{
    DEFAULT = 0,
    TEST = 1,
    GAME_START = 2,
    GAME_END = 3
}
