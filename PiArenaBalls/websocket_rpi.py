import asyncio
import websockets
import RPi.GPIO as GPIO
import json
import threading
from enum import Enum

GPIO.setmode(GPIO.BCM)

WEB_SOCKET_PORT = 8765

SENSOR_0 = 14
SENSOR_1 = 15
BLOWER_0 = 22
BLOWER_1 = 23
LINAC_A  = 17
LINAC_B  = 18
START_LED = 24
START_BTN = 25

GPIO.setup(SENSOR_0, GPIO.IN, pull_up_down=GPIO.PUD_UP)
GPIO.setup(SENSOR_1, GPIO.IN, pull_up_down=GPIO.PUD_UP)
GPIO.setup(BLOWER_0, GPIO.OUT)
GPIO.setup(BLOWER_1, GPIO.OUT)
GPIO.setup(LINAC_A,  GPIO.OUT)
GPIO.setup(LINAC_B,  GPIO.OUT)
GPIO.setup(START_LED, GPIO.OUT)
GPIO.setup(START_BTN, GPIO.IN, pull_up_down=GPIO.PUD_UP)

counter_0 = 0
counter_1 = 0
game_on   = False
counting  = False
start_btn = False
linacDelayTime = 4.0
linacTimer = 0.0

class Mode(Enum):
    DEFAULT = 0
    TEST = 1
    GAME_START = 2
    GAME_END = 3

current_mode = Mode.DEFAULT	
last_mode = Mode.DEFAULT

def sensor_0_callback(channel):
    global counter_0, counting
    if counting:
        counter_0 += 1

def sensor_1_callback(channel):
    global counter_1, counting
    if counting:
        counter_1 += 1

GPIO.add_event_detect(SENSOR_0, GPIO.RISING, callback=sensor_0_callback, bouncetime=5)
GPIO.add_event_detect(SENSOR_1, GPIO.RISING, callback=sensor_1_callback, bouncetime=5)

def reset_counters():
    global counter_0, counter_1
    counter_0 = 0
    counter_1 = 0

def reset_pins():
    GPIO.output(BLOWER_0, GPIO.LOW)
    GPIO.output(BLOWER_1, GPIO.LOW)
    GPIO.output(LINAC_A,  GPIO.LOW)
    GPIO.output(LINAC_B,  GPIO.LOW)

def read_button():
    global start_btn
    if GPIO.input(START_BTN) == GPIO.LOW:
        start_btn = True
    else:
        start_btn = False

def run_linac():
    print("Linac timer triggered")

async def handler(websocket):
    global game_on, counting, current_mode, last_mode, linacDelayTime, linacTimer
    while True:
        read_button()
        try:
            data = await websocket.recv()
            msg = json.loads(data)

            current_mode = Mode(msg.get("mode"))
            
            if(last_mode != current_mode):
                print("Set Mode: " + current_mode.name)

            if(current_mode == Mode.DEFAULT):
                reset_counters()
                reset_pins()
                game_on = False
                counting = False
            elif(current_mode == Mode.TEST):
                if(last_mode != current_mode):
                    reset_counters()
                    reset_pins()
                    game_on = False
                    counting = True
                if(msg.get("resetCounters")):
                    reset_counters()
                GPIO.output(BLOWER_0, msg.get("isBlower0On"))
                GPIO.output(BLOWER_1, msg.get("isBlower1On"))
                GPIO.output(LINAC_A,  msg.get("isLinacAOn"))
                GPIO.output(LINAC_B,  msg.get("isLinacBOn"))
                GPIO.output(START_LED, msg.get("isStartLedOn"))
            elif(current_mode == Mode.GAME_START):
                if(last_mode != current_mode):
                    reset_counters()
                    reset_pins()
                    game_on = False
                    counting = False
                    linacDelayTime = msg.get("linacDelayTime")
                    linacTimer = threading.Timer(linacDelayTime, run_linac)
                    linacTimer.start()
                    GPIO.output(LINAC_A,  True)
                    GPIO.output(LINAC_B,  True)
                else:
                    if linacTimer is not None and not linacTimer.is_alive():
                        print("Linac timer expired")
                        linacTimer = None
                        game_on = True
                        counting = True
                        GPIO.output(LINAC_A,  False)
                        GPIO.output(LINAC_B,  False)
                        GPIO.output(BLOWER_0, True)
                        GPIO.output(BLOWER_1, True)
            elif(current_mode == Mode.GAME_END):
                game_on = False
                counting = False
                GPIO.output(BLOWER_0, False)
                GPIO.output(BLOWER_1, False)

            last_mode = current_mode

            status = {
                "counter_0": counter_0,
                "counter_1": counter_1,
                "game_on": game_on,
                "counting": counting,
                "start_btn": start_btn,
                "mode": current_mode.value
                }
            await websocket.send(json.dumps(status))
        except websockets.ConnectionClosed:
            break

async def main():
    reset_pins()
    async with websockets.serve(handler, "0.0.0.0", WEB_SOCKET_PORT):
        print(f"WebSocket server started on {WEB_SOCKET_PORT} port")
        await asyncio.Future()

if __name__ == "__main__":
    try:
        asyncio.run(main())
    finally:
        GPIO.cleanup()