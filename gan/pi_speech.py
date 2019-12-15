#!/usr/bin/env python
# -*- coding: utf-8
# Create a fake video that can test synchronization features

import sys, os, time, re

from google_recognizer_pi import Recognizer

from pythonosc import dispatcher
from pythonosc import osc_server

import json
import janus
import subprocess

class PiSpeech:
    def __init__(self):
        self.restart = False
        self.stop = False

    def start(self):
        self.google_queue = janus.Queue()
        self.recognizer = Recognizer(self.google_queue.sync_q, self)
        self.recognizer.start()


if __name__ == '__main__':

    """
    def osc_handler(addr,args):
        q.put({"command": addr[1:], "args": args})

    dispatcher = dispatcher.Dispatcher()
    dispatcher.set_default_handler(osc_handler)

    server = osc_server.ThreadingOSCUDPServer(("0.0.0.0", 3800), dispatcher)
    print("Serving OSC on {}".format(server.server_address))
    server.serve_forever()
    """
    #pi_speech = PiSpeech()
    #pi_speech.start()

    subprocess.run(["aplay","-D", "bluealsa:PROFILE=a2dp", "audio-test/in_ear_mom_1.wav"])    


