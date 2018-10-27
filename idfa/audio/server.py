import os
import asyncio
import websockets
import json
import requests
import ssl
import pathlib
import numpy as np

class Server:

    def __init__(self, ms_speech,gain_callback, mid_speech_callback, speech_callback, control_callback):
        print("Init server {}".format(ms_speech))
        self.ms_speech = ms_speech
        self.gain_callback = gain_callback
        self.speech_callback = speech_callback
        self.mid_speech_callback = mid_speech_callback
        self.control_callback = control_callback

        self.connected = set()

    async def handler(self, websocket, path):
        print("Websocket connect!")
        self.connected.add(websocket)
        try:
            async for message in websocket:
                print("Websocket message!")
                print(message)
                data = json.loads(message)
                if data['action'] == 'get-token':
                    await websocket.send(json.dumps({'token': self.ms_speech.obtain_auth_token()}))
                elif (data['action'] == 'speech'):
                    self.speech_callback(data['text'])
                elif (data['action'] == 'mid-speech'):
                    self.mid_speech_callback(data['text'])
                elif (data["action"] == 'update-gain'):
                    self.gain_callback(data["min"], data["max"])
                elif (data["action"] == 'control'):
                    self.control_callback(data)

        finally:
            print("Unregistering Websocket")
            self.connected.remove(websocket)

    def start(self):
        print("Staring server")
        ssl_context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
        ssl_context.load_cert_chain(certfile='localhost.pem', keyfile='localkey.pem')
        return websockets.serve(self.handler, 'localhost', 9540, ssl=ssl_context)

    async def emotion_update(self,data, state):
        print("Sending emotion update")
        tasks = set()
        for client in self.connected:
            tasks.add(client.send(json.dumps({'action': 'emotion', 'data':data, 'state': state}, cls=NumpyEncoder)))
        await asyncio.gather(*tasks)
        

class NumpyEncoder(json.JSONEncoder):
   # https://stackoverflow.com/questions/26646362/numpy-array-is-not-json-serializable

    def default(self, obj):
        if isinstance(obj, (np.int_, np.intc, np.intp, np.int8,
            np.int16, np.int32, np.int64, np.uint8,
            np.uint16, np.uint32, np.uint64)):
            return int(obj)
        elif isinstance(obj, (np.float_, np.float16, np.float32, 
            np.float64)):
            return float(obj)
        elif isinstance(obj,(np.ndarray,)): #### This is the fix
            return obj.tolist()
        return json.JSONEncoder.default(self, obj)

