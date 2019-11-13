from pythonosc import osc_message_builder
from pythonosc import udp_client
import os
from recognizer import Recognizer

#osc_client = udp_client.SimpleUDPClient("127.0.0.1", 57120)

# Load English tokenizer, tagger, parser, NER and word vectors
#nlp = spacy.load('en')
#print("Loaded English NLP")

def callback(text):
    print("Text! {}".format(text))

try:
    recognizer = Recognizer(callback)
    recognizer.start()

except (KeyboardInterrupt, SystemExit):
    print("Exiting")
