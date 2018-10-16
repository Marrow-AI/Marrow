import os
import sys
import argparse


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


def emotion_update(data):
    print("Emotion update!")
    conc = asyncio.run_coroutine_threadsafe(server.emotion_update(data), main_loop)
    print(conc.result())

if __name__ == '__main__':

    parser = argparse.ArgumentParser()
    parser.add_argument("-d_id", "--device_id", dest= 'device_id', type=int, help="a device id for microphone", default=None)

    #automatic gain normalisation
    parser.add_argument("-g_min", "--gain_min", dest= 'g_min', type=float, help="the min value of automatic gain normalisation")
    parser.add_argument("-g_max", "--gain_max", dest= 'g_max', type=float, help="the max value of automatic gain normalisation")

    args = parser.parse_args()
    args.stop = False

    print("args: " + str(args))
    args.callback = emotion_update

    live_ser = LiveSer()
    live_ser.run(args)

    ms_speech = MSSpeech()
 #  ms_speech.obtain_auth_token()

   # google_speech = GoogleSpeech()

    #google_speech.say("Yes!")
    #asyncio.get_event_loop().run_until_complete(ms_speech.say("I masturbate 50 times a day to naked nerdy men"))


    print("STARTING SERVER?")

    server = Server(ms_speech)

    main_loop = asyncio.get_event_loop()

    asyncio.get_event_loop().run_until_complete(server.start())

    try:
        asyncio.get_event_loop().run_forever()
    except KeyboardInterrupt:
        print("Stopping everything")
        args.stop = True






