import os
import sys

from pythonosc import osc_message_builder
from pythonosc import udp_client
from server import Server
import asyncio

from ms_speech import MSSpeech
from google_speech import GoogleSpeech

sys.path.append(os.path.abspath('./emotion'))

from offline_ser import LiveSer

#osc_client = udp_client.SimpleUDPClient("127.0.0.1", 57120)

# Load English tokenizer, tagger, parser, NER and word vectors
#nlp = spacy.load('en')
#print("Loaded English NLP")

live_ser = LiveSer()

ms_speech = MSSpeech()
ms_speech.obtain_auth_token()

google_speech = GoogleSpeech()

#google_speech.say("Yes!")
#asyncio.get_event_loop().run_until_complete(ms_speech.say("I masturbate 50 times a day to naked nerdy men"))

server = Server(ms_speech)

asyncio.get_event_loop().run_until_complete(server.start())
asyncio.get_event_loop().run_forever()




