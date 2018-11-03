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
        if self._callback:
            self._callback(self._command, self._timeout)

    def cancel(self):
        print("Cacnel command! {}".format(self._command))
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

        self.osc_commands = {}

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
                self.control,
                self.mood_update
        )

        self.main_loop.run_until_complete(self.server.start())
        self.main_loop.run_until_complete(self.consume_speech())

    def schedule_osc(self, timeout, client, command, args):
        osc_command = ScheduleOSC(timeout,client, command, args, self.del_osc )
        self.osc_commands[command + str(timeout)] = osc_command

    def del_osc(self,command,timeout):
        print("del {}".format(command + str(timeout)))
        del self.osc_commands[command + str(timeout)]

    def purge_osc(self):
        for command in self.osc_commands:
            self.osc_commands[command].cancel()
        self.osc_commands.clear()

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
        
        response_i = self.response_coming(index)
        if response_i != -1:
            if not self.beating:
                self.voice_client.send_message("/gan/heartbeat", 0.5)
                self.voice_client.send_message("/gan/bassheart", [0.0, 1.0]) 
                self.preload_speech("gan_responses/{}.wav".format(response_i))
        else:
            self.beating = False


        if "triggers-gan" in line:
            print("Say response!")

            # Send a pause the average person speaks at somewhere between 125 and 150 words per minute (2-2.5 per sec)
            self.say(words_ahead / 2.5 )

        else:
            # Normal react feedback
            self.voice_client.send_message("/gan/react",1)

        if "triggers-beat" in line:
            self.voice_client.send_message("/gan/beat",0.0)


        if index < self.script.length - 1:
            self.script.awaiting_index = index + 1 
            self.script.update()
            self.t2i_client.send_message("/spotlight", self.script.awaiting["speaker"])
        else:
            self.end()

    def end(self):
        print("END")
        self.t2i_client.send_message("/gan/end",1)

    def response_coming(self, index):
        for i in range(index + 3, index, -1):
            if i < self.script.length - 1 and "triggers-gan" in self.script.data["script-lines"][i]:
                print("Response coming in index {}!".format(i))
                return i
        return -1


    def say(self, delay_sec, delay_effect = True):


        print("Saying line with {} delay".format(delay_sec))

        now =  int(round(time.time() * 1000))
        self.last_react = now + math.ceil(self.speech_duration) * 1000 + delay_sec * 1000
        asyncio.ensure_future(self.server.pause_listening(math.ceil(self.speech_duration + delay_sec)))

        effect_time = 0.05
        if delay_effect:
            self.schedule_osc(delay_sec,self.voice_client, "/gan/delay", 1)
        self.schedule_osc(delay_sec,self.voice_client, "/speech/play", 1)

        self.schedule_osc(delay_sec,self.t2i_client, "/gan/speaks", 1)

        self.schedule_osc(self.speech_duration + delay_sec, self.voice_client, "/gan/heartbeat", 0)
        self.schedule_osc(self.speech_duration + delay_sec, self.voice_client, "/gan/bassheart", [1.0, 0.0])

        self.schedule_osc(self.speech_duration + delay_sec, self.t2i_client, "/gan/speaks", 0)

    def preload_speech(self, file_name):
        with contextlib.closing(wave.open(file_name,'r')) as f:
            frames = f.getnframes()
            rate = f.getframerate()
            self.speech_duration = frames / float(rate)

        shutil.copyfile(
                file_name,             
                "tmp/gan.wav"
        )
        print("Copied")
        self.voice_client.send_message("/speech/reload",1)



    def control(self, data):
        print("Control command! {}".format(data))
        if data["command"] == 'start':
            asyncio.ensure_future(self.start_intro())
        elif data["command"] == 'stop':
            self.voice_client.send_message("/control/stop",1)
            self.t2i_client.send_message("/control/stop",1)
            self.voice_client.send_message("/gan/delay",1)
            self.voice_client.send_message("/gan/feedback",0)
            self.voice_client.send_message("/gan/noisegrain", 0.035) 
            self.voice_client.send_message("/gan/bassheart", [0.0, 1.0]) 
            self.voice_client.send_message("/gan/synthmode", [0.0, 1.0]) 
            self.voice_client.send_message("/gan/beat",1.0)
            self.preload_speech("gan_intro/1.wav")
        elif data["command"] == 'skip-intro':
            self.purge_osc()
            self.voice_client.send_message("/control/start",1)
            self.voice_client.send_message("/control/synthbass",1)
            self.voice_client.send_message("/intro/end",1)
            self.voice_client.send_message("/gan/start",1)


    async def start_intro(self):
        print("Start intro!")
        self.voice_client.send_message("/control/init",1)
        self.script.reset()
        self.t2i_client.send_message("/control/start",1)
        first_speech = 28
        self.say(0, delay_effect = False)
        self.schedule_osc(first_speech - 0.5, self.voice_client, "/gan/feedback", 0.2 )
        self.schedule_osc(0 + first_speech, self.voice_client, "/control/start", 1)
        self.schedule_osc(12.1 + first_speech, self.voice_client, "/control/synthbass", 1)
        self.schedule_osc(30.1 + first_speech, self.voice_client, "/control/table", 1)
        self.schedule_osc(40.1 + first_speech, self.voice_client, "/intro/preend", 1,)
        self.schedule_osc(51.1 + first_speech, self.voice_client, "/intro/end", 1,)
        self.schedule_osc(63.1 + first_speech, self.voice_client, "/gan/start", 1)
        self.schedule_osc(63.1 + first_speech, self.voice_client, "/gan/bassheart", [1.0, 0.0])
        self.schedule_osc(63.1 + first_speech, self.voice_client, "/gan/synthmode", [1.0, 0.0])
        self.schedule_osc(63.5 + first_speech, self.voice_client, "/gan/feedback", 0)

    def mood_update(self, data):
        self.mental_state.value = float(data["value"])
        self.mental_state_updated()

    def mental_state_updated(self):
        print("Mental state {}".format(self.mental_state.value))

        ####
        self.voice_client.send_message("/gan/strings", max(0, self.mental_state.value - 0.5))
        self.voice_client.send_message("/gan/lfo2", max(0, self.mental_state.value - 0.5) * 0.8)
        self.voice_client.send_message("/gan/lfo1", max(0, 1-(self.mental_state.value * 3)))
        bassheart = [0.0, 1.0] if self.mental_state.value < 0.4 else [1.0, 0.0]
        self.voice_client.send_message("/gan/bassheart", bassheart)
        self.voice_client.send_message("/gan/noisegrain", 0.035 + 0.04 * (max(0, 0.5 - self.mental_state.value )))
        if self.mental_state.value < 0.3:
            self.voice_client.send_message("/gan/bells", 0.02)
        else:
            self.voice_client.send_message("/gan/bells", 0.01)





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



    






