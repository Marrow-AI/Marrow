#!/usr/bin/env python
# -*- coding: utf-8
# Create a fake video that can test synchronization features

import sys, os, time, re

from google_recognizer_pi import Recognizer

from pythonosc import dispatcher
from pythonosc import osc_server
from pythonosc import udp_client

import json
import janus
import subprocess
import threading
import asyncio

class PiSpeech:
    def __init__(self, engine, loop):
        self.restart = False
        self.stop = True
        self.engine = engine
        self.loop = loop
        self.recognizer = Recognizer(self.engine, self, self.loop)
        self.recognizer.start()

    def start(self, future, role):
        self.recognizer.future = future
        self.recognizer.role = role
        self.stop = False

    def stop_rec(self):
        self.stop = True

ROLE = ""
engine_client = udp_client.SimpleUDPClient("192.168.1.22", 3954)

def popenAndCall(onExit, popenArgs):
    def runInThread(onExit, popenArgs):
        proc = subprocess.Popen(popenArgs)
        proc.wait()
        onExit()
        return

    thread = threading.Thread(target=runInThread, args=(onExit, popenArgs))
    thread.start()
    return thread

class OSCServer(threading.Thread):

    def __init__(self, loop):
        self.dispatcher = dispatcher
        self.loop = loop
        threading.Thread.__init__(self)
        self.record_future = None

    def run(self):
        self.dispatcher = dispatcher.Dispatcher()

        self.dispatcher.map("/play", self.start_play)
        self.dispatcher.map("/record-start", self.record_start)
        self.dispatcher.map("/record-stop", self.record_stop)
        
        self.server = osc_server.ThreadingOSCUDPServer(("0.0.0.0", 3954), self.dispatcher)
        print("Serving OSC on {}".format(self.server.server_address))

        self.pi_speech = PiSpeech(engine_client,self.loop)

        self.server.serve_forever()

    def start_play(self, addr, role, file_name):
        print("Start play {}".format(file_name))
        self.loop.call_soon_threadsafe(
            self.loop.create_task,self.play(role, file_name)
        )

    async def play(self, role, file_name):
        global ROLE
        print("Play file! {} : {}".format(role, file_name))
        ROLE = role
        if self.record_future != None:
            print("Waiting for recording to finish")
            await self.record_future
            self.record_future = None
        popenAndCall(self.finished_playing, ["aplay","-D", "bluealsa:PROFILE=a2dp", "/home/pi/{}".format(file_name)])    

    def finished_playing(self):
        print("Finished playing! sending finished for {}".format(ROLE))
        engine_client.send_message("/play-finished", ROLE)

    def record_start(self, addr,role):
        global ROLE
        ROLE = role
        print("Start recording {}!".format(role))

        self.record_future = self.loop.create_future()
        self.pi_speech.start(self.record_future, role)

    def record_stop(self,addr, role):
        print("Stop recording!")
        self.pi_speech.stop_rec()



async def main():


    osc_server = OSCServer(asyncio.get_running_loop())
    osc_server.start()

    signal = asyncio.get_running_loop().create_future()
    await signal

if __name__ == '__main__':

    asyncio.run(main())

    #popenAndCall(main, ["aplay","-D", "bluealsa:PROFILE=a2dp", "/home/pi/{}".format("in-ear/in_ear_mom_1.wav")])    
    #subprocess.run(["aplay","-D", "bluealsa", "audio-test/in_ear_mom_1.wav"])    


