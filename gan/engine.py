#!/usr/bin/env python
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

import numbers
import uuid

from script import Script
from numpy import interp

sys.path.append(os.path.abspath('./emotion'))

#from offline_ser import LiveSer
from mental_state import MentalState

from google_recognizer import Recognizer

import wave
import pyaudio
import contextlib
import math

import sounddevice as sd
import soundfile as sf

import functools

# Load English tokenizer, tagger, parser, NER and word vectors
#nlp = spacy.load('en')
#print("Loaded English NLP")

DISTANCE_THRESHOLD = 0.92

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
    def __init__(self, timeout, callback, finally_callback):
        self._callback = callback
        self._task = asyncio.ensure_future(self._job())
        self._timeout = timeout
        self._uuid = uuid.uuid4()
        self._finally_callback = finally_callback

    async def _job(self):
        await asyncio.sleep(self._timeout)
        if self._callback:
            self._callback()
        else:
            print("ERROR no function")
        if self._finally_callback:
            self._finally_callback(self._uuid)

    def cancel(self):
        print("Cacnel function {}".format(self._uuid))
        self._task.cancel()


SPEAKER_CHANNELS = {
    "dad" : 13,
    "mom": 14,
    "brother": 15,
    "sister": 16
}


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
        self.func_sched = {}

        #self.lock = asyncio.Lock()

        self.t2i_client = udp_client.SimpleUDPClient("192.168.1.22", 3838)
        #self.pix2pix_client = udp_client.SimpleUDPClient("127.0.0.1", 8383)
        #self.voice_client = udp_client.SimpleUDPClient("127.0.0.1", 57120)
        self.voice_client = udp_client.SimpleUDPClient("172.16.195.167", 8000)

        #self.mental_state = MentalState()

        #google_speech = GoogleSpeech()
        #google_speech.say("Hi")
        #asyncio.get_event_loop().run_until_complete(ms_speech.say("Pewdiepie"))

        self.audio_interface = pyaudio.PyAudio()


        self.speaker_counter = {
            "dad": 0,
            "mom": 0,
            "sister": 0,
            "brother": 0
        }


        self.in_ear_devices = {
            "dad": ['Headphones (Trekz Air by AfterS',1], #blue
            "mom": ['Headphones (2- Trekz Air by Aft',3], #red
            "brother": ['Headphones (3- Trekz Air by Aft',4], #black
            "sister": ['Headphones (Air by AfterShokz S',5] #green
        }



        self.time_check()

        self.main_loop = asyncio.get_event_loop()

        #self.queue = janus.Queue(loop=self.main_loop)
        self.queue = asyncio.Queue(loop=self.main_loop)

        self.server = Server(
                self.gain_update,
                self.queue,
                self.control,
                self.mood_update,
                self.pix2pix_update
        )




    def start(self):
        print("Starting server")
        tasks = []
        #tasks.append(asyncio.create_task(self.server.start()))
        self.main_loop.run_until_complete(self.server.start())
        #asyncio.ensure_future(self.server.start())

        if not args.no_speech: 
            print("Consuming speech")
            self.recognizer = Recognizer(self.queue, self.args)
            #fut = self.main_loop.run_in_executor(None, self.recognizer.start)
            tasks.append(self.consume_speech())
            #self.main_loop.run_until_complete(self.consume_speech())
            print("Waiting on queue")
            #tasks.append(asyncio.create_task(self.consume_speech()))

        asyncio.gather(*tasks)

        #await asyncio.gather(*tasks, return_exceptions=True)

    async def produce(self, some_queue):
        await asyncio.sleep(5)
        print('producing')
        # put the item in the queue
        await some_queue.put({"WHAT": "AA"})

    def schedule_osc(self, timeout, client, command, args):
        osc_command = ScheduleOSC(timeout,client, command, args, self.del_osc )
        self.osc_commands[command + str(timeout)] = osc_command

    def schedule_function(self, timeout, callback):
        func = ScheduleFunction(timeout, callback, self.del_func)
        self.func_sched[func._uuid] = func

    def del_osc(self,command,timeout):
        #print("del {}".format(command + str(timeout)))
        del self.osc_commands[command + str(timeout)]

    def del_func(self,uid):
        #print("del {}".format(command + str(timeout)))
        del self.func_sched[uid]

    def purge_osc(self):
        for command in self.osc_commands:
            self.osc_commands[command].cancel()
        self.osc_commands.clear()

    def purge_func(self):
        for uid in self.func_sched:
            self.func_sched[uid].cancel()
        self.func_sched.clear()

    def start_google(self, device_index):
        print("Resume listening")
        print(self.queue)
        self.main_loop.run_in_executor(None, self.recognizer.start, self.audio_interface, device_index)
        #asyncio.create_task(self.produce(self.queue))
        
        #self.queue.put_nowait({"hello": "hello"})

    async def consume_speech(self):
        while True:
            item = await self.queue.get()
            if item["action"] == "speech":
                self.speech_text(item["text"])
            else:
                self.mid_speech_text(item["text"])

    def time_check(self):
        # recognition timeout
        now =  time.time()
        if self.state == "SCRIPT" and self.script.awaiting_global_timeout:
            if (
                self.script.awaiting_index > -1
                and (
                        (now - self.last_react) > self.script.awaiting_global_timeout
                        or (now - self.last_speech) > self.script.awaiting_nospeech_timeout
                    )
            ):
                print("Script TIMEOUT! {}, {}, {}".format(now, self.last_react, self.last_speech))
                asyncio.set_event_loop(self.main_loop)
                self.last_react = self.last_speech = now
                if self.timeout_response():
                    self.last_react = self.last_speech = time.time() + self.speech_duration
                    # say it
                    self.say(delay_sec = 1, callback = self.next_variation)
                elif self.script.next_variation():
                    self.show_next_line()
                else:
                    self.react("")

        if not self.args.stop:
            threading.Timer(0.1, self.time_check).start()

    def next_variation(self):
        if self.script.next_variation():
            self.show_next_line()

    def timeout_response(self):
        if "timeout-response" in self.script.awaiting:
            del self.script.awaiting["timeout-response"]
            self.preload_speech("gan_responses/timeout{}.wav".format(self.script.awaiting_index))
            return True
        else:
            return False

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
                #print("Looking up {}".format(text))
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
            #print("({})".format(text))
            self.t2i_client.send_message("/speech", text)
            self.lookup(text)
        elif self.state == "QUESTION":
            self.question_answer = text

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
                if match["distance"] >= DISTANCE_THRESHOLD:
                    print("EMERGECNY BOOM")
                    return (matches["try_index"], match["index"])

        return (-1, -1)

    def match_cache(self, index):
        for matches in self.matches_cache:
            for match in matches["data"]:
                if match["distance"] >= DISTANCE_THRESHOLD and match["index"] == index:
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
                if match["distance"] >= DISTANCE_THRESHOLD and match["index"] == index:
                    print("BOOM")
                    return try_index
        return -1


    def match(self, text, try_index):
        match = self.script.match(text)
        if match >= DISTANCE_THRESHOLD:
            print("MATCH {}".format(match));
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

        # Send a pause the average person speaks at somewhere between 125 and 150 words per minute (2-2.5 per sec)
        delay = words_ahead / 2.8

        if "triggers-end" in line:
            self.schedule_osc(delay, self.voice_client, "/control/musicbox", [0.0, 0.0, 0.0, 0.0])
            self.schedule_osc(delay, self.voice_client, "/control/synthbass", [0.0, 0.0, 0.0])
            self.schedule_function(delay,self.stop_noise)
            self.schedule_function(delay,self.hide)

        if "triggers-transition" in line:
            # Transition sequence
            self.schedule_osc(delay,self.voice_client, "/control/musicbox", [0.0, 0.5, 0.8, 0.0])
            self.schedule_osc(delay + 1,self.voice_client, "/control/beacon", [0.9, 0.0])
            self.schedule_osc(delay + 1 ,self.voice_client, "/control/bassheart", [0.9, 0.9])
            self.schedule_osc(delay + 1,self.voice_client, "/control/membrane", [0.9, 0.45, 0.0])
            self.schedule_osc(delay + 4,self.voice_client, "/control/membrane", [0.9, 0.45, 0.2])
            self.schedule_osc(delay + 5,self.voice_client, "/control/musicbox", [0.7, 0.0, 0.8, 0.0])




        if "triggers-gan" in line:
            print("Say response!")
            self.last_react = self.last_speech = time.time()  + delay + self.speech_duration
            self.state = "GAN"
            self.matched_to_word = 0
            echo = None
            if "triggers-echo" in line:
               echo = line["triggers-echo"]
            distort = None
            if "triggers-distort" in line:
               distort = line["triggers-distort"]

            if self.script.awaiting_index == self.script.length -1:
                print("Ending sequence!!")
                self.state = "END"
                self.schedule_osc(delay, self.t2i_client, "/table/fadeout", 1)
                self.schedule_osc(delay, self.voice_client, "/control/musicbox", [0.0, 0.0, 0.0, 0.5])
                self.schedule_osc(delay, self.voice_client, "/control/beacon", [0.0, 0.0])
                self.schedule_osc(delay, self.voice_client, "/control/strings", [0.0, 0.0])
                self.schedule_osc(delay, self.voice_client, "/control/bells", [0.0, 0.0])
                self.schedule_osc(delay, self.voice_client, "/control/synthbass", [0.0, 0.0, 0.0])


                self.schedule_osc(delay + 2, self.voice_client, "/control/stop", 1)

                self.schedule_osc(delay + 5.5, self.voice_client, "/strings/effect", [3, 0.0])
                self.schedule_osc(delay + 5.5, self.voice_client, "/control/strings", [0.0, 1.0])
                #self.schedule_osc(delay + 5.5, self.voice_client, "/control/bells", [0.0, 0.2])
                #self.schedule_osc(delay + 5.5, self.voice_client, "/control/synthbass", [0.0, 0.0, 0.2])

                self.say(delay + 2, callback = self.next_line, echos = echo, distorts = distort)
                self.schedule_osc(delay + 12, self.voice_client, "/control/start", 1)
                self.schedule_osc(delay + 19, self.t2i_client, "/table/titles", 1)
            else:
                self.say(delay, callback = self.next_line, echos = echo, distorts = distort)
            if "triggers-effect" in line:
                self.load_effect(line["triggers-effect"])
                self.schedule_function(delay + line["triggers-effect"]["time"], self.play_effect)


        else:
            self.next_line(delay)

       # if "triggers-beat" in line:
        #    self.voice_client.send_message("/gan/beat",0.0)


    def play_effect(self):
        self.voice_client.send_message("/effect/play", 1)

    def next_line(self, delay = 0):
        self.last_react = self.last_speech = time.time() + delay
        if self.script.next_line():
            self.state = "SCRIPT"
            self.run_line(delay)
        else:
            self.end()

    def prev_line(self, delay = 0):
        self.last_react = self.last_speech = time.time() + delay
        if self.script.prev_line():
            self.state = "SCRIPT"
            self.run_line(0)

    def run_line(self, delay):
        print ("Preload {}/{}?".format(self.script.awaiting_index +1, self.script.length))
        if (
            self.script.awaiting_index + 1 <= self.script.length -1 and
            "speaker" in self.script.data["script-lines"][self.script.awaiting_index + 1] and
            self.script.data["script-lines"][self.script.awaiting_index + 1]["speaker"] == "house"
        ):
            pass
            #self.preload_speech("gan_responses/{}.wav".format(self.script.awaiting_index + 1))

        if self.script.awaiting_text:
            self.schedule_function(delay, self.show_next_line)
        elif self.script.awaiting_type == "open-line":
            self.show_open_line(self.script.awaiting)

        if "in-ear" in self.script.awaiting:
            self.pause_listening()
            self.schedule_function(3, self.play_in_ear)

    def end(self):
        self.state = "END"
        print("END")

        self.schedule_osc(4, self.voice_client,"/control/strings", [0.0, 0.0])
        self.schedule_osc(4, self.voice_client,"/control/bells", [0.0, 0.0])
        self.schedule_osc(4, self.voice_client,"/control/synthbass", [0.0, 0.0, 0.0])

        self.schedule_function(10, self.stop)
        #self.pix2pix_client.send_message("/gan/end",1)



    def play_in_ear(self):
        data = self.script.awaiting["in-ear"]
        print("Play in ear! {}".format(data))
        for inear in data:
            try:
                target = inear["target"]
                output_device = self.in_ear_devices[target][0]
                print("Play in-ear: {}-{} on {}".format(target, self.script.awaiting_index,output_device))

                print("----------------------output device list---------------------")
                info = self.audio_interface.get_host_api_info_by_index(0)
                numdevices = info.get('deviceCount')
                for i in range(0, numdevices):
                   if (self.audio_interface.get_device_info_by_host_api_device_index(0, i).get('maxOutputChannels')) > 1:
                       device = self.audio_interface.get_device_info_by_host_api_device_index(0, i)
                        #print(device)
                       print("output Device id ", i, " - ", self.audio_interface.get_device_info_by_host_api_device_index(0, i).get('name'))

                print("-------------------------------------------------------------")
                
                wf = wave.open('in-ear/in_ear_{}_{}.wav'.format(target, self.script.awaiting_index), 'rb')

                def callback(in_data, frame_count, time_info, status):
                    data = wf.readframes(frame_count)
                    print("Audio status: {}".format(status))
                    return (data, pyaudio.paContinue)

                # open stream using callback (3)
                stream = self.audio_interface.open(format=self.audio_interface.get_format_from_width(wf.getsampwidth()),
                                channels=wf.getnchannels(),
                                rate=wf.getframerate(),
                                output=True,
                                output_device_index=17,
                                stream_callback=callback)

                # start the stream (4)
                stream.start_stream()


                #data, fs = sf.read('in-ear/in_ear_{}_{}.wav'.format(target, self.script.awaiting_index), dtype='float32')
                #sd.play(data, fs, device=17)

                
                """
                index = self.speaker_counter[target] + 36
                channel = SPEAKER_CHANNELS[target]
                print("Send OSC on channel {} note index {}".format(channel, index))
                #self.voice_client.send_message("/midi/note/{}".format(16),[index])
                self.voice_client.send_message("/midi/note/{}".format(channel),[index,127, 1])
                self.schedule_osc(0.5, self.voice_client,"/midi/note/{}".format(channel), [index,127,0])
                #self.voice_client.send_message("/test/{}".format(16),[index, 127.0, 1])
                """
            except Exception as e:
                print("Audio error!")
                print(e)
            finally:
                self.speaker_counter[target] = self.speaker_counter[target] + 1

    def show_open_line(self,data):
        print("Show open line! {}".format(data))

    def say(self, delay_sec = 0, delay_effect = False, callback = None, echos = None, distorts = None):

        if self.speech_duration:

            print("Saying line with {} delay".format(delay_sec))

            self.pause_listening(math.ceil(self.speech_duration + delay_sec))

            effect_time = 0.05

            self.schedule_osc(delay_sec,self.voice_client, "/speech/play", 1)
            self.schedule_osc(delay_sec + self.speech_duration + 0.2,self.voice_client, "/speech/stop", 1)
            self.schedule_osc(delay_sec,self.t2i_client, "/gan/speaks", 1)

            if echos:
                # one or many?
                if isinstance(echos[0], numbers.Number):
                    echos = [echos]

                for echo in echos:
                    self.schedule_osc(delay_sec + echo[0],self.voice_client, "/gan/echo", 3) 
                    self.schedule_osc(delay_sec + echo[1],self.voice_client, "/gan/echo", 2)


            if distorts:
                # one or many?
                if isinstance(distorts[0], numbers.Number):
                    distorts = [distorts]

                for distort in distorts:
                    self.schedule_osc(delay_sec + distort[0],self.voice_client, "/gan/distort", 1.0)
                    self.schedule_osc(delay_sec + distort[1],self.voice_client, "/gan/distort", 0.0)

            if self.state == "GAN":

                # coming back
                self.schedule_osc(delay_sec + self.speech_duration, self.voice_client, "/control/bassheart", [0.65, 0.0])
                self.schedule_osc(delay_sec + self.speech_duration, self.voice_client, "/control/musicbox", [0.65, 0.5, 0.65, 0.5])
                self.schedule_osc(delay_sec + self.speech_duration, self.voice_client, "/control/membrane", [0.6, 0.0, 0.0])
                self.schedule_osc(delay_sec + self.speech_duration, self.voice_client, "/control/beacon", [0.8, 0.26])
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
        try:
            #asyncio.ensure_future(self.server.pause_listening(duration))
            print("Pause listening for {}".format(duration))
            self.recognizer.stop()
            if self.state != "INTRO" and duration >= 1:
                # Minus 1 for the time it takes to start listening
                self.schedule_function(duration - 0.5, self.start_google)
        except Exception as e:
            pass

    def control(self, data):
        print("Control command! {}".format(data))
        command = data["command"]
        if command == 'start':
            #self.start_intro()
            self.start_test()
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
        elif command == 'hide':
            self.hide()
        elif command == 'next':
            self.next_line()
        elif command == 'prev':
            self.prev_line()


    def ser_stop(self):
        self.args.stop = True

    def ser_start(self):
        self.args.stop = False
        self.live_ser.listen(self.args)

    def stop(self):
        print("Stopping experience")
        self.script.reset()
        self.voice_client.send_message("/control/membrane", [0.0, 0.0, 0.0])
        self.voice_client.send_message("/control/stop",1)
        self.t2i_client.send_message("/table/fadeout",1)
        self.t2i_client.send_message("/control/stop",1)
        self.voice_client.send_message("/speech/stop",1)
        self.send_noise = False
        asyncio.ensure_future(self.server.control("stop"))
        self.pause_listening()
        self.state = "WAITING"
        self.purge_osc()
        self.purge_func()
        #self.pix2pix_client.send_message("/control/stop",1)


    def start_intro(self):
        if self.state != "WAITING":
            print("Not starting intro twice!")
            return

        print("Start intro!")
        self.script.reset()

        self.state = "INTRO"
        self.send_noise = False
        self.voice_client.send_message("/control/stop", 1)
        self.pause_listening()

        self.voice_client.send_message("/control/bells", [0.0, 0.26])
        self.voice_client.send_message("/control/musicbox", [0.0, 0.0, 0.0, 0.5])
        self.voice_client.send_message("/control/strings", [0.0, 0.0])
        self.voice_client.send_message("/strings/effect", [2, 0.0])
        self.voice_client.send_message("/control/bassheart", [0.0, 0.0])
        self.voice_client.send_message("/control/membrane", [0.0, 0.0, 0.0])
        self.voice_client.send_message("/control/beacon", [0.0, 0.0])
        self.voice_client.send_message("/control/synthbass", [0.0, 0.0, 0.0])
        self.voice_client.send_message("/gan/distort", 0.0)
        self.voice_client.send_message("/gan/echo", 2)

        self.t2i_client.send_message("/control/start",1)
        asyncio.ensure_future(self.server.control("start"))
        self.preload_speech("gan_intro/intro.wav")
        #self.load_effect(self.script.data["intro-effect"])
        #self.schedule_function(0.5, self.play_effect)
        self.say(delay_sec = 0.5, callback=self.pre_question)
        self.schedule_osc(13.4, self.voice_client, "/control/start", 1)
        self.schedule_osc(31.51, self.voice_client, "/control/strings", [0.0, 0.95])
        self.schedule_osc(61.51, self.voice_client, "/control/synthbass", [0.0, 0.0, 0.4])
        self.schedule_function(61.51, self.start_noise)

    def start_test(self):
        print("Start intro!")
        self.script.reset()
        self.t2i_client.send_message("/control/start",1)
        asyncio.ensure_future(self.server.control("start"))
        self.t2i_client.send_message("/table/showplates", 1)
        self.t2i_client.send_message("/table/fadein", 1)
        self.t2i_client.send_message("/spotlight", "mom")
        self.t2i_client.send_message("/table/dinner", "Pasta")
        self.start_script()

    def start_noise(self):
        self.send_noise = True

    def stop_noise(self):
        self.send_noise = False

    def hide(self):    
        asyncio.ensure_future(self.server.control("hide"))

    ########### QUESTION ###############

    def pre_question(self):
        self.preload_speech("gan_question/line.wav")
        self.schedule_function(6, self.start_question)
        self.schedule_osc(8.5, self.voice_client, "/control/strings", [0.5, 0.0])
        self.schedule_osc(8.5, self.voice_client, "/control/bells", [0.7, 0.0])
        self.schedule_osc(8.5, self.voice_client, "/control/synthbass", [0.8, 0.0, 0.0])


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
        self.voice_client.send_message("/control/synthbass", [0.0, 0.4, 0.0])
        target["text"] = target["text"].replace("%ANSWER%",self.question_answer)
        self.schedule_function(7, self.say_pre_script)
        self.schedule_function(13, self.show_plates)

    def say_pre_script(self):
        self.say(callback = self.spotlight_mom)

    def show_plates(self):
        print("Show plates")
        self.t2i_client.send_message("/table/showplates", 1)

        # Main theme
        self.voice_client.send_message("/control/musicbox", [0.7, 0.5, 0.0, 0.5])
        self.voice_client.send_message("/control/beacon", [0.8, 0.26])

    def spotlight_mom(self):
        print("Spotlight on mom")
        self.schedule_function(2, self.start_script)

    def start_script(self):
        print("Start script")
        self.last_react =  self.last_speech = time.time()
        self.state = "SCRIPT"
        self.run_line(0)

    def show_next_line(self):
        if self.script.awaiting_text:
            self.t2i_client.send_message(
                    "/script",
                    [self.script.awaiting["speaker"], self.script.awaiting_text]
            )
            device_index = self.in_ear_devices[self.script.awaiting["speaker"]][1]
            print("{} (index {}), PLEASE SAY: {}".format(self.script.awaiting["speaker"], device_index, self.script.awaiting_text))
            self.start_google(device_index)

    def load_effect(self, data):
        print("Load effect {}".format(data["effect"]))
        self.voice_client.send_message("/effect/fades", data["fades"])
        absPath = os.path.abspath("effects/{}.wav".format(data["effect"]))
        self.voice_client.send_message("/effect/load", absPath)


    def mood_update(self, data):
        self.mental_state.value = float(data["value"])
        self.mental_state_updated()

    def pix2pix_update(self,loss = None):
        if self.send_noise:
            self.voice_client.send_message("/noise/trigger", 1)
        if loss:
            mapped = interp(loss, [0.016, 0.03],[0,1])
            print("Sending loss function update. {} mapped to {} ".format(loss, mapped))
            self.voice_client.send_message("/synthbass/effect", mapped)


if __name__ == '__main__':

    parser = argparse.ArgumentParser()

    parser.add_argument('--no-speech', action='store_true' , help='Disable speech recognition')

    args = parser.parse_args()

    try:
        engine = Engine(args)
        engine.start()
        asyncio.get_event_loop().run_forever()
    except KeyboardInterrupt:
        print("Stopping everything")
        args.stop = True
