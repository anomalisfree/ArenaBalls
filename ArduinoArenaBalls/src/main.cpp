#include <Arduino.h>
#include <TM1637Display.h>
#include <LiquidCrystal_I2C.h>

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// LCD
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

LiquidCrystal_I2C lcd(0x27, 16, 2);

#define QUEUE_SIZE 10

struct LcdCommand
{
  String text;
  int row;
};

LcdCommand lcdQueue[QUEUE_SIZE];
int queueHead = 0;
int queueTail = 0;

bool isQueueEmpty()
{
  return queueHead == queueTail;
}

bool isQueueFull()
{
  return (queueTail + 1) % QUEUE_SIZE == queueHead;
}

void lcdInit()
{
  lcd.init();
  lcd.backlight();
  lcd.clear();
}

void lcdPrint(String text, int row)
{
  if (row > 1)
    row = 1;

  lcd.setCursor(0, row);
  lcd.print("                ");
  lcd.setCursor(0, row);

  if (lcd.print(text) != text.length())
  {
    Serial.println("Error: LCD not working properly");
  }
}

void lcdPrint(String line1, String line2)
{
  lcd.clear();

  lcdPrint(line1, 0);
  lcdPrint(line2, 1);
}

void enqueueLcdPrint(String text, int row)
{
  if (!isQueueFull())
  {
    lcdQueue[queueTail].text = text;
    lcdQueue[queueTail].row = row;
    queueTail = (queueTail + 1) % QUEUE_SIZE;
  }
}

void enqueueLcdPrint(String line1, String line2)
{
  enqueueLcdPrint(line1, 0);
  enqueueLcdPrint(line2, 1);
}

void processLcdQueue()
{
  if (!isQueueEmpty())
  {
    LcdCommand command = lcdQueue[queueHead];
    queueHead = (queueHead + 1) % QUEUE_SIZE;
    lcdPrint(command.text, command.row);
  }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#define linac_open_time 4000 // milliseconds

#define sensor_0 2
#define sensor_1 3
#define dio_0 4 // display data pin
#define clk_0 5 // display clock pin
#define dio_1 6
#define clk_1 7
#define start_switch 8
#define start_led 9
#define linac_a 10 // drive both relays together
#define linac_b 11
#define blower_0 A0 // external relay boards
#define blower_1 A1

#define diag_led 13

TM1637Display display_0(clk_0, dio_0);
TM1637Display display_1(clk_1, dio_1);

bool initialized = false;
int current_mode = 0;
bool game_on = false;
bool counting = false;
bool start_button = false;

int last_mode = 0;

uint16_t counter_0 = 0;
uint16_t counter_1 = 0;
uint32_t run_timer = 0;

char inData[100];
char *inParse[100];
String inString = "";
int indx = 0;
bool stringComplete = false;

unsigned long linacLastUpdate = 0;
unsigned long linacDelayTime = 100;

unsigned long counterLastUpdate_0 = 0;
unsigned long counterLastUpdate_1 = 0;
unsigned long counterDelayTime = 100;

int unity_mode = 0;
bool unity_resetCounters = false;
bool unity_isLinacAOn = false;
bool unity_isLinacBOn = false;
bool unity_isBlower0On = false;
bool unity_isBlower1On = false;
bool unity_isStartLedOn = false;

void count_0()
{
  if (counting)
  {
    unsigned long currentMillis = millis();
    if (currentMillis - counterLastUpdate_0 >= counterDelayTime)
    {
      counterLastUpdate_0 = currentMillis;
      counter_0++;

      display_0.showNumberDec(counter_0, false);
      enqueueLcdPrint(String(counter_0), 0);
    }
  }
}

void count_1()
{
  if (counting)
  {
    unsigned long currentMillis = millis();
    if (currentMillis - counterLastUpdate_1 >= counterDelayTime)
    {
      counterLastUpdate_1 = currentMillis;
      counter_1++;

      display_1.showNumberDec(counter_1, false);
      enqueueLcdPrint(String(counter_1), 1);
    }
  }
}

void ResetCounters()
{
  counter_0 = 0;
  counter_1 = 0;
  display_0.showNumberDec(0, false);
  display_1.showNumberDec(0, false);
  lcdPrint("0", "0");
}

void ResetPins()
{
  digitalWrite(blower_0, LOW);
  digitalWrite(blower_1, LOW);
  digitalWrite(linac_a, LOW);
  digitalWrite(linac_b, LOW);
  digitalWrite(start_led, LOW);
}

unsigned long sendDataLastUpdate = 0;
unsigned long sendDataDelayTime = 100;

void SendData()
{
  unsigned long currentMillis = millis();
  if (currentMillis - sendDataLastUpdate >= sendDataDelayTime)
  {
    sendDataLastUpdate = currentMillis;
    Serial.println(
        String(initialized) + "," +
        String(unity_mode) + "," +
        String(game_on) + "," +
        String(counting) + "," +
        String(counter_0) + "," +
        String(counter_1) + "," +
        String(start_button));
  }
}

void parseInString()
{
  int index = 0;
  char *inputStr = strdup(inString.c_str());
  char *token = strtok(inputStr, ", ");
  while (token != nullptr && index < 10)
  {
    inData[index++] = atoi(token);
    token = strtok(nullptr, ", ");
  }
}

void serialEvent()
{
  while (Serial.available() && !stringComplete)
  {
    char inChar = Serial.read();
    inString += inChar;
    if (inChar == '\n')
    {
      stringComplete = true;
    }
  }
}

void ResetMode()
{
  if (last_mode != current_mode)
  {
    last_mode = current_mode;
    game_on = false;
    enqueueLcdPrint("Ready", "To Start");

    display_0.setBrightness(7, false);
    display_1.setBrightness(7, false);
  }

  counting = false;

  ResetCounters();
  ResetPins();
}

void TestMode()
{
  if (last_mode != current_mode)
  {
    last_mode = current_mode;
    enqueueLcdPrint("Test Mode", " ");
    game_on = false;

    display_0.setBrightness(7, true);
    display_1.setBrightness(7, true);

    ResetPins();
    ResetCounters();
  }

  if (unity_resetCounters)
  {
    ResetCounters();
  }

  counting = true;

  digitalWrite(blower_0, unity_isBlower0On);
  digitalWrite(blower_1, unity_isBlower1On);
  digitalWrite(linac_a, unity_isLinacAOn);
  digitalWrite(linac_b, unity_isLinacBOn);
  digitalWrite(start_led, unity_isStartLedOn);
}

static unsigned long startGameTimestamp = 0;
static int startGameStep = 0;

void StartGame()
{
  if (last_mode != current_mode)
  {
    last_mode = current_mode;
    enqueueLcdPrint("Game Mode", " ");
    counting = false;
    game_on = false;
    digitalWrite(start_led, LOW);
    digitalWrite(linac_a, HIGH);
    digitalWrite(linac_b, HIGH);

    display_0.setBrightness(7, false);
    display_1.setBrightness(7, false);

    startGameTimestamp = millis();
    startGameStep = 1;
  }

  if (unity_resetCounters)
  {
    ResetCounters();
  }
}

static unsigned long blinkLastTime = 0;
static bool blinkState = false;

void EndGame()
{
  if (last_mode != current_mode)
  {
    last_mode = current_mode;
    counting = false;
    game_on = false;
    digitalWrite(start_led, LOW);
    digitalWrite(blower_0, LOW);
    digitalWrite(blower_1, LOW);

    enqueueLcdPrint("Game Over", 0);

    if (counter_0 > counter_1)
    {
      enqueueLcdPrint("Team 1 Wins", 1);
    }
    else if (counter_1 > counter_0)
    {
      enqueueLcdPrint("Team 2 Wins", 1);
    }
    else
    {
      enqueueLcdPrint("Draw", 1);
    }
  }
}

void UseDataLogic()
{
  current_mode = unity_mode;

  switch (unity_mode)
  {
  case 0:
    ResetMode();
    break;
  case 1:
    TestMode();
    break;
  case 2:
    StartGame();
    break;
  case 3:
    EndGame();
    break;
  default:
    break;
  }
}

void ParseSerialData()
{
  char *p = inData;
  char *str;
  int count = 0;

  while ((str = strtok_r(p, ",", &p)) != NULL)
  {
    inParse[count] = str;
    count++;
  }

  unity_mode = inData[0];
  unity_resetCounters = inData[1];
  sendDataDelayTime = inData[2];
  counterDelayTime = inData[3];
  linacDelayTime = inData[4];
  unity_isLinacAOn = inData[5];
  unity_isLinacBOn = inData[6];
  unity_isBlower0On = inData[7];
  unity_isBlower1On = inData[8];
  unity_isStartLedOn = inData[9];

  memset(&inData[0], 0, sizeof(inData));

  UseDataLogic();
}

void GameUpdate()
{
  if (current_mode == 2)
  {
    if (startGameStep == 1 && millis() - startGameTimestamp >= linac_open_time)
    {
      digitalWrite(linac_a, LOW);
      digitalWrite(linac_b, LOW);
      startGameTimestamp = millis();
      startGameStep = 2;
    }

    if (startGameStep == 2 && millis() - startGameTimestamp >= linac_open_time + 2000)
    {
      digitalWrite(blower_0, HIGH);
      digitalWrite(blower_1, HIGH);
      digitalWrite(start_led, HIGH);

      display_0.setBrightness(7, true);
      display_1.setBrightness(7, true);

      counting = true;
      game_on = true;
      startGameStep = 3;
    }
  }
  else if (current_mode == 3)
  {
    unsigned long now = millis();
    if (now - blinkLastTime >= 500)
    {
      blinkLastTime = now;
      blinkState = !blinkState;
      display_0.setBrightness(7, blinkState);
      display_1.setBrightness(7, blinkState);
    }
  }
}

void setup()
{
  Serial.begin(9600);

  lcdInit();

  pinMode(start_switch, INPUT_PULLUP);
  pinMode(start_led, OUTPUT);
  pinMode(diag_led, OUTPUT);
  pinMode(linac_a, OUTPUT);
  pinMode(linac_b, OUTPUT);
  pinMode(blower_0, OUTPUT);
  pinMode(blower_1, OUTPUT);
  pinMode(sensor_0, INPUT_PULLUP);
  pinMode(sensor_1, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(sensor_0), count_0, RISING);
  attachInterrupt(digitalPinToInterrupt(sensor_1), count_1, RISING);

  delay(100);

  display_0.setBrightness(7, true);
  display_1.setBrightness(7, true);

  delay(100);

  digitalWrite(diag_led, HIGH);
  initialized = true;

  ResetMode();
}

void loop()
{
  SendData();

  if (Serial.available())
    serialEvent();

  if (stringComplete)
  {
    parseInString();
    ParseSerialData();
    inString = "";
    stringComplete = false;
  }

  GameUpdate();

  start_button = !digitalRead(start_switch);

  processLcdQueue();
}