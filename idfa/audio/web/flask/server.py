from flask import Flask
from pythonosc import osc_message_builder
from pythonosc import udp_client
from flask import request

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
