import os
import sys
import argparse


from pythonosc import osc_message_builder
from pythonosc import udp_client
from server import Server

import shutil
import asyncio
import threading
import time

from script import Script

sys.path.append(os.path.abspath('./emotion'))

from offline_ser import LiveSer
from mental_state import MentalState

import wave
import contextlib

import math

# Load English tokenizer, tagger, parser, NER and word vectors
#nlp = spacy.load('en')
#print("Loaded English NLP")

DISTANCE_THRESHOLD = 0.6

class ScheduleOSC:
    def __init__(self, timeout, client, command, args,  callback):
        self._timeout = timeout
        self._callback = callback
        self._command = command
        self._args = args
        self._task = asyncio.ensure_future(self._job())
        self._client = client

    async def _job(self):
        await asyncio.sleep(self._timeout)
        print("Shoot command! {}".format(self._command))
        self._client.send_message(self._command,self._args)
        if (self._callback):
            await self._callback()

    def cancel(self):
        self._task.cancel()


class Engine:
    def __init__(self,args):

        self.args = args

        self.script = Script()
        self.script.load_space()
        self.script.reset()


        self.args.stop = False

        self.args.callback = self.emotion_update

        #self.live_ser = LiveSer()
        #self.live_ser.run(self.args)

        self.mid_text = None
        self.last_react = 0
        self.mid_match = False
        self.beating = False
        self.matched_to_word = 0

        self.t2i_client = udp_client.SimpleUDPClient("127.0.0.1", 3838)
        self.voice_client = udp_client.SimpleUDPClient("127.0.0.1", 57120)

        self.mental_state = MentalState()

        #google_speech = GoogleSpeech()
        #google_speech.say("Hi")
        #asyncio.get_event_loop().run_until_complete(ms_speech.say("Pewdiepie"))


        #self.time_check()

        self.main_loop = asyncio.get_event_loop()

        self.queue = asyncio.Queue(loop=self.main_loop)

        self.server = Server(
                self.gain_update, 
                self.queue,
                self.control
        )

        self.main_loop.run_until_complete(self.server.start())
        self.main_loop.run_until_complete(self.consume_speech())

    async def consume_speech(self):
        while True:
            item = await self.queue.get()
            if item["action"] == "speech":
                self.speech_text(item["text"])
            else:
                self.mid_speech_text(item["text"])

    def time_check(self):
        # recognition timeout
        now =  int(round(time.time() * 1000))
        if self.mid_match and self.last_mid and now - self.last_mid > 1000:
            print("BABBLE TIMEOUT")
            self.last_mid = now
            self.lookup(self.mid_text)
        if not args.stop:
            threading.Timer(0.1, self.time_check).start()



    def emotion_update(self, data):
        print("Emotion update! {}".format(data))
        if (data["status"] == "silence"):
            self.mental_state.update_silence()
        else:
            self.mental_state.update_emotion(data["analysis"])
            
        conc = asyncio.run_coroutine_threadsafe(self.server.emotion_update(data, self.mental_state.get_current_state()), self.main_loop)

    def gain_update(self, min, max):
        print("Update gain! {} : {}".format(min, max))
        self.live_ser.feat_ext.min = float(min)
        self.live_ser.feat_ext.max = float(max)

    def speech_text(self, text):
        #print("<{}>".format(text))
        if self.mid_text is not None:
            self.lookup(text)

        self.mid_match = False
        self.matched_to_word = 0
        """
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
            """

    def mid_speech_text(self, text):
        #print("({})".format(text))
        self.mid_text = text
        self.t2i_client.send_message("/speech", text)
        self.lookup(text)
        
    def lookup(self, text):
        # print("REACT: {}".format(text))
        # update last speech time
        # First get the top line matches
        tries = []
        # We have to try all combinations
        words = text.split()
        for i in range(len(words) - 2, self.matched_to_word, -1):
            tries.append(" ".join(words[i:]))

        self.matches_cache = []

        try_i = self.lookup_index(tries, self.script.awaiting_index)
        if try_i != -1:
            self.react(text, self.script.awaiting_index)
            return True

        # try up to 2 lines aheead
        for i in range(self.script.awaiting_index + 1, self.script.awaiting_index + 4):
            try_i = self.match_cache(i) 
            if try_i != -1:
                self.react(text, i)
                return True

        # If it has been too long accept whatever
        now =  int(round(time.time() * 1000))
        if self.last_react > 0 and now - self.last_react > 1000  * 20:
            (index, try_i) = self.match_cache_any()
            if try_i != -1:
                self.react(text, index)
                return True



    def lookup_index(self, tries, index):
        for i in range(0, len(tries)):
            s = tries[i]
            try_i = self.match(s, index, i)
            if try_i != -1:
                return try_i

        return -1


    def match_cache_any(self):
        for matches in self.matches_cache:
            for match in matches["data"]:
                if match["distance"] < DISTANCE_THRESHOLD:
                    print("EMERGECNY BOOM")
                    return (matches["try_index"], match["index"])

        return (-1, -1)

    def match_cache(self, index):
        for matches in self.matches_cache:
            for match in matches["data"]:
                if match["distance"] < DISTANCE_THRESHOLD and match["index"] == index:
                    print("CACHE BOOM")
                    return matches["try_index"]
        return -1


    def match(self, text, index, try_index):
        #print("[{}]".format(text))
        matches = self.script.match(text)
        if matches:
            self.matches_cache.append({
                    "data": matches,
                    "try_index": try_index
            })
            #print(matches)
            for match in matches:
                if match["distance"] < DISTANCE_THRESHOLD and match["index"] == index:
                    print("BOOM")
                    return try_index
        return -1





    def react(self, matched_utterance, index):

        # Which word was it?
        self.last_matched_word = self.matched_to_word
        self.matched_to_word = len(matched_utterance.split()) - 1
        self.last_react = int(round(time.time() * 1000))
        script_text = self.script.data["script-lines"][index]["text"]
        words_ahead = max(0, len(script_text.split()) - (len(matched_utterance.split()) - self.last_matched_word))
        print("Said {} ({}) Matched: {}. Words ahead {}".format(index, script_text, matched_utterance, words_ahead))


        line = self.script.data["script-lines"][index]
        
        if self.response_coming(index):
            if not self.beating:
                self.voice_client.send_message("/gan/heartbeat", 0.5)
        else:
            self.beating = False


        if "triggers-gan" in line:
            print("Say response!")
            # Send a pause the average person speaks at somewhere between 125 and 150 words per minute (2-2.5 per sec)
        
            self.say("gan_responses/{}.wav".format(index), words_ahead / 2.5 )

        if index < self.script.length - 1:
            self.script.awaiting_index = index + 1 
            self.script.update()
            self.t2i_client.send_message("/spotlight", self.script.awaiting["speaker"])
        else:
            self.end()

    def end(self):
        print("END")

    def response_coming(self, index):
        for i in range(index + 3, index, -1):
            if i < self.script.length - 1 and "triggers-gan" in self.script.data["script-lines"][i]:
                print("Response coming in index {}!".format(i))
                return True
        return False


    def say(self, file_name, delay_sec):

        with contextlib.closing(wave.open(file_name,'r')) as f:
            frames = f.getnframes()
            rate = f.getframerate()
            duration = frames / float(rate)
            asyncio.ensure_future(self.server.pause_listening(math.ceil(duration + delay_sec)))

        shutil.copyfile(
                file_name,             
                "tmp/gan.wav"
        )
        print("Copied. Saying with {} delay".format(delay_sec))

        now =  int(round(time.time() * 1000))
        self.last_react = now + math.ceil(duration) * 1000 + delay_sec * 1000

        self.voice_client.send_message("/speech/reload",1)
        self.voice_client.send_message("/gan/feedback", 0.0)
        effect_time = 0.05
        #cmd = ScheduleOSC(delay_sec,self.voice_client, "/gan/delay", [effect_time , effect_time, effect_time], None )
        cmd2 = ScheduleOSC(delay_sec,self.voice_client, "/speech/play", 1, None )
        cmd3 = ScheduleOSC(duration + delay_sec, self.voice_client, "/gan/heartbeat", 0, None )


    def control(self, data):
        print("Control command! {}".format(data))
        if data["command"] == 'start':
            asyncio.ensure_future(self.start_intro())
        elif data["command"] == 'stop':
            self.voice_client.send_message("/control/stop",1)


    async def start_intro(self):
        print("Start intro!")
        self.script.reset()
        first_speech = 28
        #start_command = ScheduleOSC(27.5,"/control/start", None )
        #start_command = ScheduleOSC(27.5,"/control/start", None )
        #table_command = ScheduleOSC(47,"/control/table", None )
        #start_command = ScheduleOSC(3,"/control/stop", None )
        command = ScheduleOSC(0, self.voice_client, "/gan/feedback", 0.0, None )
        self.say("gan_intro/1.wav", 0)
        command = ScheduleOSC(first_speech - 1, self.voice_client, "/gan/feedback", 0.2, None )
        command = ScheduleOSC(0 + first_speech, self.voice_client, "/control/start", 1, None )
        command = ScheduleOSC(12.1 + first_speech, self.voice_client, "/control/synthbass", 1, None )
        command = ScheduleOSC(30.1 + first_speech, self.voice_client, "/control/table", 1,  None )
        command = ScheduleOSC(51.1 + first_speech, self.voice_client, "/intro/end", 1, None )
        command = ScheduleOSC(61.1 + first_speech, self.voice_client, "/gan/start", 1, None )


if __name__ == '__main__':

    parser = argparse.ArgumentParser()
    parser.add_argument("-d_id", "--device_id", dest= 'device_id', type=int, help="a device id for microphone", default=None)

    #automatic gain normalisation
    parser.add_argument("-g_min", "--gain_min", dest= 'g_min', type=float, help="the min value of automatic gain normalisation")
    parser.add_argument("-g_max", "--gain_max", dest= 'g_max', type=float, help="the max value of automatic gain normalisation")

    args = parser.parse_args()

    try:
        engine = Engine(args)
        asyncio.get_event_loop().run_forever()
    except KeyboardInterrupt:
        print("Stopping everything")
        args.stop = True



    






