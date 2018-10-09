from pythonosc import osc_message_builder
from pythonosc import udp_client
import os
import server
import asyncio

#osc_client = udp_client.SimpleUDPClient("127.0.0.1", 57120)

# Load English tokenizer, tagger, parser, NER and word vectors
#nlp = spacy.load('en')
#print("Loaded English NLP")

asyncio.get_event_loop().run_until_complete(server.start())
asyncio.get_event_loop().run_forever()




