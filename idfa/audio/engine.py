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
        if self.lookup(text):
            self.mid_match = True
        
    def lookup(self, text):
        # print("REACT: {}".format(text))
        # update last speech time
        # First get the top line matches
        tries = []
        if self.mid_match:
            # We have to try all combinations
            words = text.split()
            for i in range(len(words) - 2, self.matched_to_word, -1):
                tries.append(" ".join(words[i:]))
        else:
            tries.append(text)

        self.matches_cache = []

        if self.lookup_index(tries, self.script.awaiting_index):
            self.react(self.script.awaiting_index)
            return True

        # try up to 2 lines aheead
        for i in range(self.script.awaiting_index + 1, self.script.awaiting_index + 4):
            if self.match_cache(i):
                self.react(i)
                return True

        # If it has been too long accept whatever
        now =  int(round(time.time() * 1000))
        if self.last_react > 0 and now - self.last_react > 1000  * 20:
            index = self.match_cache_any()
            if index:
                self.react(index)
                return True



    def lookup_index(self, tries, index):
        for s in tries:
            if self.match(s, index):
                # Which word was it?
                self.matched_to_word = len(s.split()) - 1
                return True


    def match_cache_any(self):
        for matches in self.matches_cache:
            for match in matches:
                if match["distance"] < 0.6:
                    # MAKE NOTE OF MATCHED WORD
                    print("EMERGECNY BOOM")
                    return match["index"]

    def match_cache(self, index):
        for matches in self.matches_cache:
            for match in matches:
                if match["distance"] < 0.6 and match["index"] == index:
                    # MAKE NOTE OF MATCHED WORD
                    print("BOOM")
                    return True


    def match(self, text, index):
        #print("[{}]".format(text))
        matches = self.script.match(text)
        if matches:
            for match in matches:
                self.matches_cache.append(matches)
                if match["distance"] < 0.6 and match["index"] == index:
                    print("BOOM ({})".format(text))
                    return True 
        return False





    def react(self, index):
        self.last_react = int(round(time.time() * 1000))
        print("Said {} ({})".format(index, self.script.data["script-lines"][index]["text"]))
        if index < self.script.length - 1:
            self.script.awaiting_index = index + 1 
            self.script.update()
            self.t2i_client.send_message("/spotlight", self.script.awaiting["speaker"])
        else:
            self.end()

    def end(self):
        print("END")

    def say(self, file_name):
        shutil.copyfile(
                file_name,             
                "tmp/gan.wav"
        )
        print("Copied")
        self.voice_client.send_message("/speech/reload",1)
        self.voice_client.send_message("/speech/play",1)


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

    args = parser.parse_args()

    try:
        engine = Engine(args)
        asyncio.get_event_loop().run_forever()
    except KeyboardInterrupt:
        print("Stopping everything")
        args.stop = True



    






