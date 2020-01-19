 #!/usr/bin/env python
import os
import sys
import argparse


from pythonosc import osc_message_builder
from pythonosc import udp_client
from pythonosc import osc_server
from pythonosc import dispatcher

from server import Server

import shutil
import asyncio
from  threading import Thread, Timer
import time
import janus

import numbers
import uuid

from script import Script
from numpy import interp

sys.path.append(os.path.abspath('./emotion'))

#from offline_ser import LiveSer
from mental_state import MentalState

import wave
import pyaudio
import contextlib
import math

import functools
import signal

from scipy.io import wavfile
import sounddevice as sd
import soundfile as sf
import numpy as np

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

class OSCServer:
    def __init__(self, loop, osc_queue):
        self.osc_queue = osc_queue
        self.loop = loop

    async def start(self, future):
        self.dispatcher = dispatcher.Dispatcher()
        self.dispatcher.map("/speech", self.speech_handler)
        self.dispatcher.map("/mid-speech", self.speech_handler)
        self.dispatcher.map("/play-finished", self.finished_handler)

        self.server = osc_server.AsyncIOOSCUDPServer(("0.0.0.0", 3954), self.dispatcher, self.loop)
        print("Serving OSC on {}".format("0.0.0.0"))

        transport, protocol = await self.server.create_serve_endpoint() 
        await future

        print("Closing transport")

        transport.close()
        #print(self.server.serve())

    def speech_handler(self, addr, role, text):         
        print("OSC command! {} {} {}".format(addr, role, text))
        self.osc_queue.put_nowait({"action": addr[1:], "role": role, "text": text})

    def finished_handler(self, addr, role):
        print("OSC command! {} {}".format(addr, role))
        self.osc_queue.put_nowait({"action": addr[1:], "role": role})


class Engine:
    def __init__(self,args):

        print("Engine INIT")

        self.state = "WAITING"

        self.args = args

        self.script = Script(load_nlp = not args.no_speech)

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

        #self.t2i_client = udp_client.SimpleUDPClient("192.168.1.22", 3838)
        #self.audio_client = udp_client.SimpleUDPClient("127.0.0.1", 57120)

        self.t2i_client = udp_client.SimpleUDPClient("127.0.0.1", 3838)
        self.td_client = udp_client.SimpleUDPClient("127.0.0.1", 7002)
        self.td_sister_client = udp_client.SimpleUDPClient("127.0.0.1", 7001)
        self.audio_client = udp_client.SimpleUDPClient("192.168.1.21", 8000)
       # self.audio_client = udp_client.SimpleUDPClient("192.168.1.25", 8000)
        self.stylegan_client = udp_client.SimpleUDPClient("192.168.1.23", 3800)
        self.gaugan_client = udp_client.SimpleUDPClient("192.168.1.23", 3900)


        self.osc_clients = {
            "unity": self.t2i_client,
            "td": self.td_client,
            "td-sister": self.td_sister_client,
            "stylegan": self.stylegan_client,
            "gaugan": self.gaugan_client,
            "audio": self.audio_client
        }


        #self.mental_state = MentalState()

        #google_speech = GoogleSpeech()
        #google_speech.say("Hi")
        #asyncio.get_event_loop().run_until_complete(ms_speech.say("Pewdiepie"))

        self.speaker_counter = {
            "dad": 0,
            "mom": 0,
            "sister": 0,
            "brother": 0
        }

        self.in_ear_endpoints = {
            "mom": udp_client.SimpleUDPClient("192.168.1.39", 3954),
            "brother": udp_client.SimpleUDPClient("192.168.1.41", 3954),
            "dad": udp_client.SimpleUDPClient("192.168.1.38", 3954),
            "sister": udp_client.SimpleUDPClient("192.168.1.42", 3954)
        }

        self.play_futures = {}

    async def start(self):
        self.main_loop = asyncio.get_running_loop()

        if not args.no_speech:
            self.time_check()

        #self.queue = asyncio.Queue(loop=self.main_loop)
        self.queue = janus.Queue(loop=self.main_loop)

        tasks = []
        self.server_stop = self.main_loop.create_future()

        self.osc_server = OSCServer(self.main_loop, self.queue.async_q)
        tasks.append(asyncio.create_task(self.osc_server.start(self.server_stop)))

        self.server = Server(
                self.gain_update,
                self.queue,
                self.control,
                self.mood_update,
                self.pix2pix_update
        )
        print("Starting server")
        self.server_task = asyncio.create_task(self.server.start(self.server_stop))

        if not args.no_speech: 
            #self.recognizer = Recognizer(self.queue.sync_q, self.main_loop, self.args#)
            #fut = self.main_loop.run_in_executor(None, self.recognizer.start)
            print("Waiting on queue")
        else:
            self.recognizer = None

        tasks.append(asyncio.create_task(self.consume_speech()))
        tasks.append(self.server_task)
        print("Gathering tasks")

        self.main_loop.call_soon(self.wakeup)

        self.tasks = asyncio.gather(*tasks)
        await self.tasks
        
        print("Server done!")

    def wakeup(self):
        self.main_loop.call_later(1.0, self.wakeup)

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

    def start_google(self, endpoint, role):
        self.last_speech = time.time()
        print("Resume listening")
        endpoint.send_message("/record-start", role)

    async def consume_speech(self):
        print("Consuming speech")
        while True:
            item = await self.queue.async_q.get()
            if item["action"] == "play-finished":
                try:
                    role = item["role"]
                    print("PLAY FINISHED!! {}".format(role))
                    self.play_futures[role].set_result(1)
                except Exception as e:
                    print("Exception in play finished! Finishing all {}".format(e))
                    for future in self.play_futures.values():
                        try:
                            if future is not None:
                                future.set_result(1)
                        except:
                            continue

                    
            elif item["action"] == "speech" and "speaker" in self.script.awaiting and item["role"] == self.script.awaiting["speaker"]:
                self.speech_text(item["text"])
            elif item["action"] == "mid-speech" and "speaker" in self.script.awaiting and item["role"] == self.script.awaiting["speaker"]:
                self.mid_speech_text(item["text"])

    def time_check(self):
        # recognition timeout
        now =  time.time()
        if self.state == "SCRIPT":
            time_since_speech = now - self.last_speech
            time_since_react = now - self.last_react

            #print('{} / {}'.format(time_since_speech, self.script.awaiting_nospeech_timeout))
            #print('{} / {}'.format(time_since_react, self.script.awaiting_global_timeout))
            if (
                self.script.awaiting_type == "OPEN" and 
                time_since_speech > self.script.awaiting_nospeech_timeout and
                "timeout" in self.script.awaiting
            ):
               print("OPEN LINE TIMEOUT!")
               self.script.awaiting["in-ear"] = self.script.awaiting["timeout"]
               del self.script.awaiting["timeout"]
               self.last_speech = now
               self.pause_listening()
               self.main_loop.create_task(self.play_in_ear())

            elif (
                self.script.awaiting_type == "LINE" and not "noskip" in self.script.awaiting and
                (
                    time_since_speech > self.script.awaiting_nospeech_timeout  or 
                    time_since_react > self.script.awaiting_global_timeout
                )
            ):
                print("LINE TIMEOUT!")
                self.last_react = self.last_speech = now
                if "timeout" in self.script.awaiting:
                    print("Showing variation")
                    self.next_variation()
                else:
                    print("Showing next line")
                    self.pause_listening()
                    self.main_loop.call_soon_threadsafe(self.trigger_osc)
                    self.main_loop.call_soon_threadsafe(self.next_line)    

        if not self.args.stop:
            Timer(0.1, self.time_check).start()

    def next_variation(self):
        self.script.next_variation()
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
        print("Speech, state {}".format(self.state))

        self.t2i_client.send_message("/speech", text)
        if self.state == "SCRIPT" and self.script.awaiting_type == "LINE":
            if self.mid_text is not None:
                #print("Looking up {}".format(text))
                self.lookup(text)
            self.mid_match = False

        elif self.script.awaiting_type == "OPEN":
            self.mid_match = False
            self.question_answer = text
            print("Question answered! {}".format(self.question_answer))
            print(self.script.awaiting)
            if "save" in self.script.awaiting:
                print("Saving answer")
                self.script.question_answer = self.question_answer

            self.t2i_client.send_message("/table/dinner", self.question_answer)
            self.t2i_client.send_message("/speech", self.question_answer)
            self.t2i_client.send_message(
                    "/script",
                    [self.script.awaiting["speaker"], self.question_answer]
              )

            self.t2i_client.send_message("/openline", "clear")
            self.trigger_osc()
            self.pause_listening()
            print("Going to next line in 3s")
            self.schedule_function(3, self.next_line)

    def mid_speech_text(self, text):
        self.last_speech = time.time()
        print("Mid-speech, state {}".format(self.state))
        if self.state == "SCRIPT" and self.script.awaiting_type != "OPEN":
            self.mid_text = text
            #print("({})".format(text))
            self.t2i_client.send_message("/speech", text)
            self.lookup(text)
        elif self.state == "QUESTION":
            self.question_answer = text

    def lookup(self, text):
        #print("LOOKUP")
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
        if not self.script.awaiting_text:
            print("REACT TO NOTHING??")
            print(self.script.awaiting)
        else:
            print("REACT")
            self.pause_listening()
            # Which word was it?
            self.last_matched_word = self.matched_to_word
            self.matched_to_word = len(matched_utterance.split())
            script_text = self.script.awaiting_text
            words_ahead = max(0, len(script_text.split()) - (len(matched_utterance.split()) - self.last_matched_word))
            print("Said {} ({}) Matched: {}. Words ahead {}".format(self.script.awaiting_index, script_text, matched_utterance, words_ahead))

            # Send a pause the average person speaks at somewhere between 125 and 150 words per minute (2-2.5 per sec)
            delay = words_ahead / 2.8

            self.trigger_osc()
            self.state = "WAITING"
            print("Next line in {}s".format(delay))
            self.schedule_function(delay, self.next_line)

       # if "triggers-beat" in line:
        #    self.audio_client.send_message("/gan/beat",0.0)


    def trigger_osc(self):
        line = self.script.awaiting
        if "triggers-osc" in line:
                for trigger in line["triggers-osc"]:
                    try:
                        print("Trigger OSC: {} {}".format(trigger["target"],trigger["address"]))
                        client = self.osc_clients[trigger["target"]]
                        delay = 0
                        if "delay" in trigger:
                            delay = trigger["delay"]
                        self.schedule_osc(delay,client, trigger["address"], trigger["value"])
                    except Exception as e:
                        print("TRIGGERS OSC ERROR {}".format(e))

        if "triggers-midi" in line:
                for trigger in line["triggers-midi"]:
                    try:
                        print("Trigger MIDI: {}".format(trigger["note"]))
                        delay = 0
                        if "delay" in trigger:
                            delay = trigger["delay"]
                        self.send_midi_note(trigger["note"], delay)
                    except Exception as e:
                        print("TRIGGERS MIDI ERROR {}".format(e))

    



    def play_effect(self):
        self.audio_client.send_message("/effect/play", 1)

    def next_line(self):
        print("NEXT LINE")
        self.matched_to_word = 0
        self.last_react = self.last_speech = time.time()
        # Clear the text
        if "speaker" in self.script.awaiting:
            self.t2i_client.send_message(
                        "/script",
                        [self.script.awaiting["speaker"], ""]
            )

        if "delay" in self.script.awaiting:
            delay = self.script.awaiting["delay"]
        else:
            delay = 0
        if self.script.next_line():
            print("Calling run_line in {}".format(delay))
            self.schedule_function(delay, self.run_line)
        else:
            self.end()

    def prev_line(self, delay = 0):
        self.last_react = self.last_speech = time.time() + delay
        if self.script.prev_line():
            self.state = "SCRIPT"
            self.run_line()

    def run_line(self):
        try:
            print ("Run line: {}".format(self.script.awaiting))
            if (
                self.script.awaiting_index + 1 <= self.script.length -1 and
                "speaker" in self.script.data["script-lines"][self.script.awaiting_index + 1] and
                self.script.data["script-lines"][self.script.awaiting_index + 1]["speaker"] == "house"
            ):
                pass
                #self.preload_speech("gan_responses/{}.wav".format(self.script.awaiting_index + 1))

            self.show_next_line()

            if "in-ear" in self.script.awaiting:
                self.pause_listening()
                asyncio.create_task(self.play_in_ear())
        except Exception as e:
            print("Engine exception",e)

    def end(self):
        self.state = "END"
        print("END")

        self.schedule_osc(4, self.audio_client,"/control/strings", [0.0, 0.0])
        self.schedule_osc(4, self.audio_client,"/control/bells", [0.0, 0.0])
        self.schedule_osc(4, self.audio_client,"/control/synthbass", [0.0, 0.0, 0.0])

        self.schedule_function(10, self.stop)
        #self.pix2pix_client.send_message("/gan/end",1)

    async def play_in_ear(self):
        self.state = "IN-EAR"
        data = self.script.awaiting["in-ear"]
        print("Play in ear! {}".format(data))
        tasks = []
        if not self.args.no_inear:
            for inear in data:
                try:
                    target = inear["target"]
                    output_endpoint = self.in_ear_endpoints[target]
                    if output_endpoint:
                        file_name = 'in-ear/in_ear_{}_{}.wav'.format(target, self.script.awaiting_index)
                        tasks.append(self.play_file(file_name, target, output_endpoint))

                except Exception as e:
                    print("Audio error!")
                    print(e)
                finally:
                    self.speaker_counter[target] = self.speaker_counter[target] + 1

            print("Waiting for plays to finish")
            await asyncio.gather(*tasks)
            print("Finshed all!")

        self.trigger_osc()

        self.last_react = self.last_speech = time.time()
        if self.script.awaiting_type != "OPEN":
            """
            if "delay" in self.script.awaiting:
                delay = self.script.awaiting["delay"]
            else:
                delay = 0
            print("Calling next line in {}s".format(delay))
            self.schedule_function(delay, self.next_line)
            """
            self.next_line()
        else:
            self.show_next_line()

    def play_file(self, file_name, role, endpoint):
        print("Sending /play message! {} {}".format(role, file_name))
        future = self.main_loop.create_future()
        self.play_futures[role] = future
        endpoint.send_message("/play", [role, file_name])
        return future

    def show_open_line(self,data):
        print("Show open line! {}".format(data))
        self.t2i_client.send_message("/openline", self.script.awaiting["speaker"])


    def say(self, delay_sec = 0, delay_effect = False, callback = None, echos = None, distorts = None):

        if self.speech_duration:

            print("Saying line with {} delay".format(delay_sec))

            self.pause_listening(math.ceil(self.speech_duration + delay_sec))

            effect_time = 0.05

            self.schedule_osc(delay_sec,self.audio_client, "/speech/play", 1)
            self.schedule_osc(delay_sec + self.speech_duration + 0.2,self.audio_client, "/speech/stop", 1)
            self.schedule_osc(delay_sec,self.t2i_client, "/gan/speaks", 1)

            if echos:
                # one or many?
                if isinstance(echos[0], numbers.Number):
                    echos = [echos]

                for echo in echos:
                    self.schedule_osc(delay_sec + echo[0],self.audio_client, "/gan/echo", 3) 
                    self.schedule_osc(delay_sec + echo[1],self.audio_client, "/gan/echo", 2)


            if distorts:
                # one or many?
                if isinstance(distorts[0], numbers.Number):
                    distorts = [distorts]

                for distort in distorts:
                    self.schedule_osc(delay_sec + distort[0],self.audio_client, "/gan/distort", 1.0)
                    self.schedule_osc(delay_sec + distort[1],self.audio_client, "/gan/distort", 0.0)

            if self.state == "GAN":

                # coming back
                self.schedule_osc(delay_sec + self.speech_duration, self.audio_client, "/control/bassheart", [0.65, 0.0])
                self.schedule_osc(delay_sec + self.speech_duration, self.audio_client, "/control/musicbox", [0.65, 0.5, 0.65, 0.5])
                self.schedule_osc(delay_sec + self.speech_duration, self.audio_client, "/control/membrane", [0.6, 0.0, 0.0])
                self.schedule_osc(delay_sec + self.speech_duration, self.audio_client, "/control/beacon", [0.8, 0.26])
            #self.schedule_osc(self.speech_duration + delay_sec, self.audio_client, "/gan/heartbeat", 0)
            #self.schedule_osc(self.speech_duration + delay_sec, self.audio_client, "/gan/bassheart", [1.0, 0.0])

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
        #self.audio_client.send_message("/speech/reload",1)
        absPath = os.path.abspath(file_name)
        self.audio_client.send_message("/speech/load",absPath)

    def pause_listening(self,duration = 0):
        print("Pause listening")
        for (role, endpoint) in self.in_ear_endpoints.items():
            if endpoint:
                endpoint.send_message("/record-stop", role)

    def control(self, data):
        print("Control command! {}".format(data))
        command = data["command"]
        if command == 'start':
            #self.start_intro()
            self.start_nfb()
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
            if self.script.awaiting_type == "OPEN":
                self.speech_text("Apple")
            else:
                self.react(self.script.awaiting_text)
        elif command == 'prev':
            self.prev_line()
        elif command == 'hall-callibration':
            self.hall_callibration()
        elif command == 'hall-completed':
            self.hall_completed()
        elif command == 'hall-start':
            self.main_loop.create_task(self.hall_start())

    def ser_stop(self):
        self.args.stop = True

    def ser_start(self):
        self.args.stop = False
        self.live_ser.listen(self.args)

    def hall_callibration(self): 
        print("Play hall calibration sound!")
        self.send_midi_note(122)

    def hall_completed(self): 
        print("Play hall completed sound!")
        self.send_midi_note(123)

    async def hall_start(self): 
        print("Hall start! Playing character descriptions")
        tasks = []
        for target in ["mom", "dad", "brother", "sister"]:
            file_name = 'in-ear/in_ear_{}_hall.wav'.format(target)
            output_endpoint = self.in_ear_endpoints[target]
            tasks.append(self.play_file(file_name, target, output_endpoint))

        await asyncio.gather(*tasks)
        print("Finshed all character descriptions! Starting")
        self.start_nfb()

    def stop(self):
        print("Stopping experience")
        self.script.reset()
        self.audio_client.send_message("/control/membrane", [0.0, 0.0, 0.0])
        self.audio_client.send_message("/control/stop",1)
        self.t2i_client.send_message("/table/fadeout",1)
        self.t2i_client.send_message("/control/stop",1)
        self.td_client.send_message("/td/display", 0)
        self.td_client.send_message("/td/faces", 0)
        self.td_client.send_message("/td/lights", 0)
        self.t2i_client.send_message("/memory/state", 0)
        self.t2i_client.send_message("/camera/stylegan", 0)
        self.t2i_client.send_message("/stylegan-animation/state", 0)
        self.t2i_client.send_message("/gaugan/state", 1)
        self.send_midi_note(36)
        self.send_noise = False
        self.main_loop.create_task(self.server.control("stop"))
        self.pause_listening()
        self.state = "WAITING"
        self.schedule_function(2, self.purge_osc)
        self.purge_func()
        #self.pix2pix_client.send_message("/control/stop",1)

    def send_midi_note(self,note, delay = 0): 
        #self.schedule_osc(delay, self.audio_client, "/midi/note/1", [note,127, 1])
        #self.schedule_osc(delay + 0.5, self.audio_client, "/midi/note/1", [note,127, 0])
        self.schedule_osc(delay, self.audio_client, "/noteOn", [note,127])
        self.schedule_osc(delay + 0.5, self.audio_client, "/noteOff", [note])

    def start_intro(self):
        if self.state != "WAITING":
            print("Not starting intro twice!")
            return

        print("Start intro!")
        self.script.reset()

        self.state = "INTRO"
        self.send_noise = False
        self.audio_client.send_message("/control/stop", 1)
        self.pause_listening()

        self.audio_client.send_message("/control/bells", [0.0, 0.26])
        self.audio_client.send_message("/control/musicbox", [0.0, 0.0, 0.0, 0.5])
        self.audio_client.send_message("/control/strings", [0.0, 0.0])
        self.audio_client.send_message("/strings/effect", [2, 0.0])
        self.audio_client.send_message("/control/bassheart", [0.0, 0.0])
        self.audio_client.send_message("/control/membrane", [0.0, 0.0, 0.0])
        self.audio_client.send_message("/control/beacon", [0.0, 0.0])
        self.audio_client.send_message("/control/synthbass", [0.0, 0.0, 0.0])
        self.audio_client.send_message("/gan/distort", 0.0)
        self.audio_client.send_message("/gan/echo", 2)

        self.t2i_client.send_message("/control/start",1)
        asyncio.ensure_future(self.server.control("start"))
        self.preload_speech("gan_intro/intro.wav")
        #self.load_effect(self.script.data["intro-effect"])
        #self.schedule_function(0.5, self.play_effect)
        self.say(delay_sec = 0.5, callback=self.pre_question)
        self.schedule_osc(13.4, self.audio_client, "/control/start", 1)
        self.schedule_osc(31.51, self.audio_client, "/control/strings", [0.0, 0.95])
        self.schedule_osc(61.51, self.audio_client, "/control/synthbass", [0.0, 0.0, 0.4])
        self.schedule_function(61.51, self.start_noise)

    def start_nfb(self):
        print("Start intro ///////NFB!")
        self.script.reset()        
        self.t2i_client.send_message("/control/start",1)
        asyncio.ensure_future(self.server.control("start"))
        self.t2i_client.send_message("/table/showplates", 0)
        self.t2i_client.send_message("/table/fadein", 1)
        self.t2i_client.send_message("/enable-t2i", 1)
        self.t2i_client.send_message("/spotlight", "mom")
        self.td_client.send_message("/td/edge", 0)
        self.td_client.send_message("/td/display", 0)
        self.td_client.send_message("/td/faces", 0)
        self.td_client.send_message("/td/lights", 0)
        self.td_client.send_message("/td/face-color", 0)
        self.gaugan_client.send_message("/load-state", "beginning")
        self.t2i_client.send_message("/gaugan/state", 1)
        self.t2i_client.send_message("/memory/state", 0)
        self.t2i_client.send_message("/camera/stylegan", 0)
        self.t2i_client.send_message("/stylegan-animation/state", 0)


        self.send_midi_note(60, 2) # C3 - START
        self.send_midi_note(61, 4.7) 
        self.schedule_function(23, self.start_script)
        #self.schedule_function(0, self.start_script)

    def start_noise(self):
        self.send_noise = True

    def stop_noise(self):
        self.send_noise = False

    def hide(self):    
        asyncio.ensure_future(self.server.control("hide"))

    def pre_script(self):
        self.state = "PRE-SCRIPT"
        self.preload_speech("gan_intro/pre_script.wav")
        affects = self.script.data["question"]["affects"]
        target = self.script.data["script-lines"][affects["line"]]
        if not self.question_answer:
            self.question_answer = affects["default"]
        print("PRE SCRIPT!! Chosen food: {}".format(self.question_answer))
        self.t2i_client.send_message("/table/dinner", self.question_answer)
        self.audio_client.send_message("/control/synthbass", [0.0, 0.4, 0.0])
        target["text"] = target["text"].replace("%ANSWER%",self.question_answer)
        self.schedule_function(7, self.say_pre_script)
        self.schedule_function(13, self.show_plates)

    def say_pre_script(self):
        self.say(callback = self.spotlight_mom)

    def show_plates(self):
        print("Show plates")
        self.t2i_client.send_message("/table/showplates", 1)

        # Main theme
        self.audio_client.send_message("/control/musicbox", [0.7, 0.5, 0.0, 0.5])
        self.audio_client.send_message("/control/beacon", [0.8, 0.26])

    def spotlight_mom(self):
        print("Spotlight on mom")
        self.schedule_function(2, self.start_script)

    def start_script(self):
        print("Start script")
        self.last_react =  self.last_speech = time.time()
        self.state = "SCRIPT"
        self.run_line()

    def show_next_line(self):
        print("SHOW NEXT LINE")
        self.state = "SCRIPT"
        if self.script.awaiting_text:
            self.t2i_client.send_message(
                    "/script",
                    [self.script.awaiting["speaker"], self.script.awaiting_text]
            )
            role = self.script.awaiting["speaker"]
            endpoint = self.in_ear_endpoints[role]
            self.last_react = self.last_speech = time.time()
            print("{} , PLEASE SAY: {}".format(role, self.script.awaiting_text))
            if not self.args.no_speech:
                self.start_google(endpoint, role)
   
        elif self.script.awaiting_type == "OPEN":
            self.last_speech = time.time()
            self.show_open_line(self.script.awaiting)
            self.t2i_client.send_message(
                    "/script",
                    [self.script.awaiting["speaker"], ""]
            )
            role = self.script.awaiting["speaker"]
            endpoint = self.in_ear_endpoints[role]
            print("{} , PLEASE SAY: {}".format(role, "ANYTHING"))
            if not self.args.no_speech:
                self.start_google(endpoint, role)

    def load_effect(self, data):
        print("Load effect {}".format(data["effect"]))
        self.audio_client.send_message("/effect/fades", data["fades"])
        absPath = os.path.abspath("effects/{}.wav".format(data["effect"]))
        self.audio_client.send_message("/effect/load", absPath)


    def mood_update(self, data):
        self.mental_state.value = float(data["value"])
        self.mental_state_updated()

    def pix2pix_update(self,loss = None):
        if self.send_noise:
            self.audio_client.send_message("/noise/trigger", 1)
        if loss:
            mapped = interp(loss, [0.016, 0.03],[0,1])
            print("Sending loss function update. {} mapped to {} ".format(loss, mapped))
            self.audio_client.send_message("/synthbass/effect", mapped)


if __name__ == '__main__':

    parser = argparse.ArgumentParser()

    parser.add_argument('--no-speech', action='store_true' , help='Disable speech recognition')
    parser.add_argument('--no-inear', action='store_true' , help='Disable In-Ear voices')

    args = parser.parse_args()

    try:
        engine = Engine(args)
        asyncio.run(engine.start())
    except Exception as e:
        print("Fatal Exception",e)
        print("Stopping everything")
        args.stop = True
        engine.tasks.cancel()
        engine.tasks.exception()
    finally:
        pass
