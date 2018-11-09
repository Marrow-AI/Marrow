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
import janus

from script import Script

sys.path.append(os.path.abspath('./emotion'))

from offline_ser import LiveSer
from mental_state import MentalState

from google_recognizer import Recognizer

import wave
import contextlib

import math

# Load English tokenizer, tagger, parser, NER and word vectors
#nlp = spacy.load('en')
#print("Loaded English NLP")

DISTANCE_THRESHOLD = 0.75
SCRIPT_TIMEOUT_GLOBAL = 20
SCRIPT_TIMEOUT_NOSPEECH = 10

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

class ScheduleFunction:
    def __init__(self, timeout, callback):
        self._callback = callback
        self._task = asyncio.ensure_future(self._job())
        self._timeout = timeout

    async def _job(self):
        await asyncio.sleep(self._timeout)
        if self._callback:
            self._callback()
        else:
            print("ERROR no function")

    def cancel(self):
        print("Cacnel function")
        self._task.cancel()


class Engine:
    def __init__(self,args):

        print("Engine INIT")

        self.state = "WAITING"

        self.args = args

        self.script = Script()

        self.send_noise = False

        self.args.stop = False
        self.args.restart = False

        self.args.callback = self.emotion_update

        #self.live_ser = LiveSer()
        #self.live_ser.run(self.args)

        self.mid_text = None
        self.last_react = 0
        self.last_speech = 0
        self.mid_match = False
        self.beating = False
        self.matched_to_word = 0

        self.osc_commands = {}

        self.t2i_client = udp_client.SimpleUDPClient("127.0.0.1", 3838)
        #self.pix2pix_client = udp_client.SimpleUDPClient("127.0.0.1", 8383)
        self.voice_client = udp_client.SimpleUDPClient("127.0.0.1", 57120)

        self.mental_state = MentalState()

        #google_speech = GoogleSpeech()
        #google_speech.say("Hi")
        #asyncio.get_event_loop().run_until_complete(ms_speech.say("Pewdiepie"))


        self.time_check()

        self.main_loop = asyncio.get_event_loop()

        self.queue = janus.Queue(loop=self.main_loop)

        self.recognizer = Recognizer(self.queue.sync_q, self.args)
        #fut = self.main_loop.run_in_executor(None, self.recognizer.start)

        self.server = Server(
                self.gain_update,
                self.queue.async_q,
                self.control,
                self.mood_update,
                self.pix2pix_update
        )

        print("Starting server")
        self.main_loop.run_until_complete(self.server.start())

        print("Consuming speech")
        self.main_loop.run_until_complete(self.consume_speech())

    def schedule_osc(self, timeout, client, command, args):
        osc_command = ScheduleOSC(timeout,client, command, args, self.del_osc )
        self.osc_commands[command + str(timeout)] = osc_command

    def schedule_function(self, timeout, callback):
        func = ScheduleFunction(timeout, callback)

    def del_osc(self,command,timeout):
        #print("del {}".format(command + str(timeout)))
        del self.osc_commands[command + str(timeout)]

    def purge_osc(self):
        for command in self.osc_commands:
            self.osc_commands[command].cancel()
        self.osc_commands.clear()

    def start_google(self):
        print("Resume listening")
        fut = self.main_loop.run_in_executor(None, self.recognizer.start)

    async def consume_speech(self):
        while True:
            item = await self.queue.async_q.get()
            if item["action"] == "speech":
                self.speech_text(item["text"])
            else:
                self.mid_speech_text(item["text"])

    def time_check(self):
        # recognition timeout
        now =  time.time()
        if self.state == "SCRIPT":
            if (
                self.script.awaiting_index > -1
                and (
                        (now - self.last_react) > SCRIPT_TIMEOUT_GLOBAL
                        or (now - self.last_speech) > SCRIPT_TIMEOUT_NOSPEECH
                    )
            ):
                print("Script TIMEOUT! {}, {}, {}".format(now, self.last_react, self.last_speech))
                self.last_react = self.last_speech = now
                if self.script.next_variation():
                    self.show_next_line()
                else:
                    self.next_line()

        if not self.args.stop:
            threading.Timer(0.1, self.time_check).start()



    def emotion_update(self, data):
        print("Emotion update! {}".format(data))
        if (data["status"] == "silence"):
            self.mental_state.update_silence()
        else:
            self.mental_state.update_emotion(data["analysis"])
            #self.mental_state_updated()

        conc = asyncio.run_coroutine_threadsafe(self.server.emotion_update(data, self.mental_state.value), self.main_loop)

    def gain_update(self, min, max):
        print("Update gain! {} : {}".format(min, max))
        self.live_ser.feat_ext.min = float(min)
        self.live_ser.feat_ext.max = float(max)

    def speech_text(self, text):
        #print("<{}>".format(text))
        self.t2i_client.send_message("/speech", text)
        if self.state == "SCRIPT":
            if self.mid_text is not None:
                print("Looking up {}".format(text))
                self.lookup(text)

            self.mid_match = False
            self.matched_to_word = 0

        elif self.state == "QUESTION":
            self.question_answer = text
            self.pre_script()

    def mid_speech_text(self, text):
        self.last_speech = time.time()
        if self.state == "SCRIPT":
            self.mid_text = text
            print("({})".format(text))
            self.t2i_client.send_message("/speech", text)
            self.lookup(text)
        elif self.state == "QUESTION":
            question_answer = text

    def lookup(self, text):
        # print("REACT: {}".format(text))
        # update last speech time
        # First get the top line matches
        tries = []
        # We have to try all combinations
        words = text.split()
        for i in range(len(words) - 2, self.matched_to_word -1, -1):
            tries.append(" ".join(words[i:]))

        self.matches_cache = []

        try_i = self.lookup_all(tries)

        if try_i != -1:
            self.react(text)
            return True
#
#        # try up to 2 lines aheead
#        for i in range(self.script.awaiting_index + 1, self.script.awaiting_index + 4):
#            try_i = self.match_cache(i)
#            if try_i != -1:
#                self.react(text, i)
#                return True
#
        # If it has been too long accept whatever
        #now =  int(round(time.time() * 1000))
        #if self.last_react > 0 and now - self.last_react > 1000  * 20:
        #    (index, try_i) = self.match_cache_any()
        #    if try_i != -1:
        #        self.react(text, index)
        #        return True


    def lookup_all(self, tries):
        for i in range(0, len(tries)):
            s = tries[i]
            try_i = self.match(s,i)
            if try_i != -1:
                return try_i

        return -1

    def lookup_index_space(self, tries, index):
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


    def match_space(self, text, index, try_index):
        #print("[{}]".format(text))
        matches = self.script.match_space(text)
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


    def match(self, text, try_index):
        match = self.script.match(text)
        if match >= DISTANCE_THRESHOLD:
            return try_index
        else:
            return -1



    def react(self, matched_utterance):

        # restart google
        self.args.restart = True

        # Which word was it?
        self.last_matched_word = self.matched_to_word
        self.matched_to_word = len(matched_utterance.split())
        script_text = self.script.awaiting_text
        words_ahead = max(0, len(script_text.split()) - (len(matched_utterance.split()) - self.last_matched_word))
        print("Said {} ({}) Matched: {}. Words ahead {}".format(self.script.awaiting_index, script_text, matched_utterance, words_ahead))


        line = self.script.awaiting

        response_i = self.response_coming(self.script.awaiting_index)
        if response_i != -1:
            if not self.beating:
                self.voice_client.send_message("/gan/heartbeat", 0.5)
                self.voice_client.send_message("/gan/bassheart", [0.0, 1.0])
                self.preload_speech("gan_responses/{}.wav".format(response_i))
        else:
            self.beating = False

        # Send a pause the average person speaks at somewhere between 125 and 150 words per minute (2-2.5 per sec)
        delay = words_ahead / 2.5

        if "triggers-end" in line:
            self.schedule_osc(delay, self.voice_client, "/control/strings", [0.8, 0.0])
            self.schedule_osc(delay, self.voice_client, "/control/bells", [0.5, 0.0, 0.01])
            self.schedule_osc(delay, self.voice_client, "/control/synthbass", [0.75, 0.0, 0.0])

        if "triggers-gan" in line:
            print("Say response!")
            self.last_react = self.last_speech = time.time()  + delay + self.speech_duration
            self.state = "GAN"
            self.matched_to_word = 0
            echo = None
            if "triggers-echo" in line:
               echo = line["triggers-echo"]
            self.say(delay, callback = self.resume_script, echo = echo)
            if "triggers-effect" in line:
                self.load_effect(line["triggers-effect"])
                self.schedule_function(delay + line["triggers-effect"]["time"], self.play_effect)

        else:
            self.next_line(delay)

       # if "triggers-beat" in line:
        #    self.voice_client.send_message("/gan/beat",0.0)


    def resume_script(self):
        self.state = "SCRIPT"
        self.next_line()

    def play_effect(self):
        self.voice_client.send_message("/effect/play", 1)

    def next_line(self, delay = 0):
        self.last_react = self.last_speech = time.time() + delay
        if self.script.next_line():
            self.schedule_function(delay, self.show_next_line)
        else:
            self.end()

    def end(self):
        print("END")
        self.t2i_client.send_message("/gan/end",1)
        #self.pix2pix_client.send_message("/gan/end",1)

    def response_coming(self, index):
        for i in range(index + 3, index, -1):
            if i < self.script.length - 1 and "triggers-gan" in self.script.data["script-lines"][i]:
                print("Response coming in index {}!".format(i))
                return i
        return -1


    def say(self, delay_sec = 0, delay_effect = False, callback = None, echo = None):

        if self.speech_duration:

            print("Saying line with {} delay".format(delay_sec))

            self.pause_listening(math.ceil(self.speech_duration + delay_sec))

            effect_time = 0.05

            if delay_effect:
                self.schedule_osc(delay_sec,self.voice_client, "/gan/delay", 1)

            self.schedule_osc(delay_sec,self.voice_client, "/speech/play", 1)
            self.schedule_osc(delay_sec + self.speech_duration + 0.2,self.voice_client, "/speech/stop", 1)
            self.schedule_osc(delay_sec,self.t2i_client, "/gan/speaks", 1)

            if echo:
                self.schedule_osc(delay_sec + echo[0],self.voice_client, "/gan/echo", 3)
                self.schedule_osc(delay_sec + echo[1],self.voice_client, "/gan/echo", 2)


            if self.state == "GAN":
                self.schedule_osc(delay_sec,self.voice_client, "/control/bassheart", [0.8, 0.5])
                self.schedule_osc(delay_sec,self.voice_client, "/control/membrane", [0.8, 0.3])
                self.schedule_osc(delay_sec,self.voice_client, "/control/bells", [0.5, 0.0, 0.01])

                self.schedule_osc(delay_sec + self.speech_duration, self.voice_client, "/control/bassheart", [0.3, 0.0])
                self.schedule_osc(delay_sec + self.speech_duration, self.voice_client, "/control/bells", [0.8, 0.1, 0.01])
                self.schedule_osc(delay_sec + self.speech_duration, self.voice_client, "/control/membrane", [0.3, 0.0])
            #self.schedule_osc(self.speech_duration + delay_sec, self.voice_client, "/gan/heartbeat", 0)
            #self.schedule_osc(self.speech_duration + delay_sec, self.voice_client, "/gan/bassheart", [1.0, 0.0])

            self.schedule_osc(self.speech_duration + delay_sec, self.t2i_client, "/gan/speaks", 0)

            if callback:
                self.schedule_function(self.speech_duration + delay_sec, callback)

        else:
            print("Nothing to say!")

    def preload_speech(self, file_name):
        print("Preload speech {}", file_name)
        with contextlib.closing(wave.open(file_name,'r')) as f:
            frames = f.getnframes()
            rate = f.getframerate()
            self.speech_duration = frames / float(rate)

        #shutil.copyfile(
        #        file_name,
        #        "tmp/gan.wav"
        #)
        #print("Copied")
        #self.voice_client.send_message("/speech/reload",1)
        absPath = os.path.abspath(file_name)
        self.voice_client.send_message("/speech/load",absPath)

    def pause_listening(self,duration = 0):
            #asyncio.ensure_future(self.server.pause_listening(duration))
            print("Pause listening for {}".format(duration))
            self.recognizer.stop()
            if self.state != "INTRO" and duration >= 1:
                # Minus 1 for the time it takes to start listening
                self.schedule_function(duration - 1, self.start_google)

    def control(self, data):
        print("Control command! {}".format(data))
        command = data["command"]
        if command == 'start':
            self.start_intro()
        elif command == 'stop':
            self.stop()
        elif command == 'skip-intro':
            self.purge_osc()
        elif command == 'stop-ser':
            self.ser_stop()
        elif command == 'start-ser':
            self.ser_start()
        elif command == 'start-question':
            self.preload_speech("gan_question/line.wav")
            self.schedule_function(0.5, self.start_question)
        elif command == 'start-script':
            self.start_script()

    def ser_stop(self):
        self.args.stop = True

    def ser_start(self):
        self.args.stop = False
        self.live_ser.listen(self.args)

    def stop(self):
        self.script.reset()
        self.voice_client.send_message("/control/stop",1)
        self.t2i_client.send_message("/control/stop",1)
        self.t2i_client.send_message("/table/fadeout",1)
        self.voice_client.send_message("/speech/stop",1)
        self.send_noise = False
        #self.pix2pix_client.send_message("/control/stop",1)


    def start_intro(self):
        print("Start intro!")
        self.state = "INTRO"
        self.send_noise = False
        self.voice_client.send_message("/control/stop", 1)
        self.pause_listening()

        self.voice_client.send_message("/control/bells", [0.0, 0.2, 0.0])
        self.voice_client.send_message("/control/strings", [0.0, 0.0])
        self.voice_client.send_message("/control/bassheart", [0.0, 0.0])
        self.voice_client.send_message("/control/membrane", [0.0, 0.0])
        self.voice_client.send_message("/control/synthbass", [0.0, 0.0, 0.0])

        self.t2i_client.send_message("/control/start",1)
        self.preload_speech("gan_intro/intro.wav")
        #self.load_effect(self.script.data["intro-effect"])
        #self.schedule_function(0.5, self.play_effect)
        self.say(delay_sec = 0.5, callback=self.pre_question)
        self.schedule_osc(13.4, self.voice_client, "/control/start", 1)
        self.schedule_osc(31.5, self.voice_client, "/control/strings", [0.0, 0.5])
        self.schedule_osc(61.5, self.voice_client, "/control/synthbass", [0.0, 0.0, 0.2])
        self.schedule_function(61.5, self.start_noise)

        """
        self.voice_client.send_message("/control/init",1)
        self.script.reset()
        self.pix2pix_client.send_message("/control/start",1)
        first_speech = 28
        self.say(0, delay_effect = Faself.speech_duration - 0.5lse)
        self.schedule_osc(first_speech - 0.5, self.voice_client, "/gan/feedback", 0.2 )
        self.schedule_osc(0 + first_speech, self.voice_client, "/control/start", 1)
        self.schedule_osc(12.1 + first_speech, self.voice_client, "/control/synthbass", 1)
        self.schedule_osc(30.1 + first_speech, self.voice_client, "/control/table", 1)
        self.schedule_osc(40.1 + first_speech, self.voice_client, "/intro/preend", 1,)
        self.schedule_osc(51.1 + first_speech, self.voice_client, "/intro/end", 1,)
        self.schedule_osc(51.2 + first_speech, self.pix2pix_client, "/intro/end", 1,)
        self.schedule_osc(63.1 + first_speech, self.voice_client, "/gan/start", 1)
        self.schedule_osc(63.1 + first_speech, self.voice_client, "/gan/bassheart", [1.0, 0.0])
        self.schedule_osc(63.1 + first_speech, self.voice_client, "/gan/synthmode", [1.0, 0.0])
        self.schedule_osc(63.5 + first_speech, self.voice_client, "/gan/feedback", 0)
"""

    def start_noise(self):
        self.send_noise = True

    ########### QUESTION ###############

    def pre_question(self):
        self.preload_speech("gan_question/line.wav")
        self.schedule_function(6, self.start_question)
        self.schedule_osc(6, self.voice_client, "/control/strings", [0.8, 0.0])
        self.schedule_osc(6, self.voice_client, "/control/bells", [0.75, 0.0, 0.0])
        self.schedule_osc(6, self.voice_client, "/control/synthbass", [0.75, 0.0, 0.0])


    def start_question(self):
        print("Start question")
        self.send_noise = True
        self.current_question_timeout = None
        self.last_asked = time.time() + self.speech_duration
        self.question_answer = None
        self.state = "QUESTION"
        self.question_timeout_index = 0
        self.say()
        self.schedule_function(self.speech_duration - 0.5, self.table_fadein)
        self.schedule_function(self.speech_duration + 1, self.load_next_question_timeout)

    def table_fadein(self):
        self.t2i_client.send_message("/table/fadein", 1)

    def check_question(self):
        if self.state != "QUESTION":
            return

        if self.question_answer is not None:
            self.pre_script()
        else:
            self.question_timed_out()

    def load_next_question_timeout(self):
        print("Load question timeout")
        if self.question_timeout_index < len(self.script.data["question"]["timeouts"]):
            self.current_question_timeout = self.script.data["question"]["timeouts"][self.question_timeout_index]
            self.preload_speech("gan_question/timeout{}.wav".format(self.question_timeout_index))
            self.schedule_function(self.current_question_timeout["after"], self.check_question)
        else:
            self.current_question_timeout = None
            self.pre_script()

    def question_timed_out(self):
        if self.question_answer == None:
            print("Question timed out!")
            self.question_timeout_index += 1
            self.last_asked = time.time()  + self.speech_duration
            self.say(callback = self.load_next_question_timeout)


    ######### QUESTION ################


    def pre_script(self):
        self.state = "PRE-SCRIPT"
        self.preload_speech("gan_intro/pre_script.wav")
        affects = self.script.data["question"]["affects"]
        target = self.script.data["script-lines"][affects["line"]]
        if not self.question_answer:
            self.question_answer = affects["default"]
        print("PRE SCRIPT!! Chosen food: {}".format(self.question_answer))
        self.t2i_client.send_message("/table/dinner", self.question_answer)
        self.voice_client.send_message("/control/synthbass", [0.0, 0.2, 0.0])
        target["text"] = target["text"].replace("%ANSWER%",self.question_answer)
        self.schedule_function(7, self.say_pre_script)
        self.schedule_function(13, self.show_plates)

    def say_pre_script(self):
        self.say(callback = self.spotlight_mom)

    def show_plates(self):
        print("Show plates")
        self.t2i_client.send_message("/table/showplates", 1)
        self.voice_client.send_message("/control/bells", [0.8, 0.1, 0.01])

    def spotlight_mom(self):
        print("Spotlight on mom")
        self.t2i_client.send_message("/spotlight", "mom")
        self.schedule_function(2, self.start_script)

    def start_script(self):
        print("Start script")
        self.last_react =  self.last_speech = time.time()
        self.script.reset()
        self.state = "SCRIPT"
        self.show_next_line()

    def show_next_line(self):
        self.t2i_client.send_message(
                "/script",
                [self.script.awaiting["speaker"], self.script.awaiting_text]
        )

        print("{}, PLEASE SAY: {}".format(self.script.awaiting["speaker"], self.script.awaiting_text))

    def load_effect(self, data):
        print("Load effect {}".format(data["effect"]))
        self.voice_client.send_message("/effect/fades", data["fades"])
        absPath = os.path.abspath("effects/{}.wav".format(data["effect"]))
        self.voice_client.send_message("/effect/load", absPath)


    def mood_update(self, data):
        self.mental_state.value = float(data["value"])
        self.mental_state_updated()

    def pix2pix_update(self):
        if self.send_noise:
            self.voice_client.send_message("/noise/trigger", 1)

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
