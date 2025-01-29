public class BroadcastData
{
    public int mode = 0;
    // 0: Reset
    // 1: Test Mode
    // 2: Game Start
    // 3: Game End
    public bool resetCounters = false;
    public int sendDataDelayTime = 100;
    public int counterDelayTime = 100;
    public int linacDelayTime = 100;
    public bool isLinacAOn = false;
    public bool isLinacBOn = false;
    public bool isBlower0On = false;
    public bool isBlower1On = false;
    public bool isStartLedOn = false;
}
