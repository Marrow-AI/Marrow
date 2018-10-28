import os
import sys
import argparse


from pythonosc import osc_message_builder
from pythonosc import udp_client
from server import Server

import shutil
import asyncio

from ms_speech import MSSpeech
from google_speech import GoogleSpeech
from script import Script

sys.path.append(os.path.abspath('./emotion'))

from offline_ser import LiveSer
from mental_state import MentalState


# Load English tokenizer, tagger, parser, NER and word vectors
#nlp = spacy.load('en')
#print("Loaded English NLP")

class ScheduleOSC:
    def __init__(self, timeout, command, args,  callback):
        self._timeout = timeout
        self._callback = callback
        self._command = command
        self._args = args
        self._task = asyncio.ensure_future(self._job())

    async def _job(self):
        await asyncio.sleep(self._timeout)
        print("Shoot command! {}".format(self._command))
        voice_client.send_message(self._command,self._args)
        if (self._callback):
            await self._callback()

    def cancel(self):
        self._task.cancel()




def emotion_update(data):
    print("Emotion update! {}".format(data))
    if (data["status"] == "silence"):
        mental_state.update_silence()
    else:
        mental_state.update_emotion(data["analysis"])
        
    conc = asyncio.run_coroutine_threadsafe(server.emotion_update(data, mental_state.get_current_state()), main_loop)

def gain_update(min, max):
    print("Update gain! {} : {}".format(min, max))
    live_ser.feat_ext.min = float(min)
    live_ser.feat_ext.max = float(max)

def speech_text(text):
    match = script.match(text)
    print("Speech Match: {} ({})".format(match['match'],text))
    if match:
        if match['match'] < 0.5:
            print("Match! {}".format(match))
            line = script.data["script-lines"][match["index"]]
            next_line = script.data["script-lines"][match["index"] + 1]
            if next_line:
                t2i_client.send_message("/spotlight", next_line["speaker"])
                print("Next Speaker {}".format(next_line["speaker"]))
            if "triggers-gan" in line:
                trigger = line["triggers-gan"]
                current_metnal_state = mental_state.get_current_state()
                if current_mental_state in trigger:
                    say("gan_responses/{}-{}.wav".format(
                            match['index'], 
                            current_metnal_state
                        )
                    )

        mental_state.update_script_match(match['match'])
    else:
        mental_state.update_script_match(1)

def mid_speech_text(text):
    t2i_client.send_message("/speech", text)

def say(file_name):
    shutil.copyfile(
            file_name,             
            "tmp/gan.wav"
    )
    print("Copied")
    voice_client.send_message("/speech/reload",1)
    voice_client.send_message("/speech/play",1)


def control(data):
    print("Control command! {}".format(data))
    if data["command"] == 'start':
        asyncio.ensure_future(start_intro())
    elif data["command"] == 'stop':
        voice_client.send_message("/control/stop",1)


async def start_intro():
    print("Start intro!")
    first_speech = 28
    #start_command = ScheduleOSC(27.5,"/control/start", None )
    #start_command = ScheduleOSC(27.5,"/control/start", None )
    #table_command = ScheduleOSC(47,"/control/table", None )
    #start_command = ScheduleOSC(3,"/control/stop", None )
    command = ScheduleOSC(0,"/gan/feedback", 0.0, None )
    say("gan_intro/1.wav")
    command = ScheduleOSC(first_speech - 1,"/gan/feedback", 0.2, None )
    command = ScheduleOSC(0 + first_speech,"/control/start", 1, None )
    command = ScheduleOSC(12.1 + first_speech,"/control/synthbass", 1, None )
    command = ScheduleOSC(24.1 + first_speech,"/control/table", 1,  None )
    command = ScheduleOSC(45.1 + first_speech,"/intro/end", 1, None )
    command = ScheduleOSC(55.1 + first_speech,"/gan/start", 1, None )


if __name__ == '__main__':

    parser = argparse.ArgumentParser()
    parser.add_argument("-d_id", "--device_id", dest= 'device_id', type=int, help="a device id for microphone", default=None)

    #automatic gain normalisation
    parser.add_argument("-g_min", "--gain_min", dest= 'g_min', type=float, help="the min value of automatic gain normalisation")
    parser.add_argument("-g_max", "--gain_max", dest= 'g_max', type=float, help="the max value of automatic gain normalisation")

    script = Script()
    script.load_space()

    args = parser.parse_args()
    args.stop = False

    args.callback = emotion_update

    #live_ser = LiveSer()
    #live_ser.run(args)

    ms_speech = MSSpeech()

    t2i_client = udp_client.SimpleUDPClient("127.0.0.1", 3838)
    voice_client = udp_client.SimpleUDPClient("127.0.0.1", 57120)

    mental_state = MentalState()

    #google_speech = GoogleSpeech()
    #google_speech.say("Hi")
    #asyncio.get_event_loop().run_until_complete(ms_speech.say("I masturbate 50 times a day to naked nerdy men"))

    server = Server(ms_speech, gain_update, mid_speech_text, speech_text, control)

    main_loop = asyncio.get_event_loop()

    asyncio.get_event_loop().run_until_complete(server.start())

    try:
        asyncio.get_event_loop().run_forever()
    except KeyboardInterrupt:
        print("Stopping everything")
        args.stop = True



    






