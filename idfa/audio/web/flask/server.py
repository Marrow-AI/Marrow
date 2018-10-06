from flask import Flask
from pythonosc import osc_message_builder
from pythonosc import udp_client
from flask import request
import spacy


import subprocess

app = Flask(__name__)
osc_client = udp_client.SimpleUDPClient("127.0.0.1", 57120)



@app.route('/speech')
def speech():
    text = request.args.get('text');
    print('Text:' + text)
    command = 'gtts-cli ' + text + '| sox -t mp3 - /tmp/sctest.wav'
    subprocess.run(command, shell=True, check=True)
    osc_client.send_message("/speech", 666)
    return 'Hello 2 World! Sent OSC'

if __name__ == "__main__":
    # Load English tokenizer, tagger, parser, NER and word vectors
    nlp = spacy.load('en')

    # Determine semantic similarities
    doc1 = nlp(u"my fries were super gross")
    doc2 = nlp(u"How are you today?")

    similarity = doc1.similarity(doc2)
    print(doc1.text, doc2.text, similarity)

