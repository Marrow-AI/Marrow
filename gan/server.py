import os
import asyncio
import websockets
import json
import requests
import ssl
import pathlib
import numpy as np
from ms_speech import MSSpeech
from watson_speech import WatsonSpeech

class Server:

    def __init__(self, gain_callback, queue, control_callback, mood_callback, pix2pix_callback):
        print("Init server")
        self.gain_callback = gain_callback
        self.control_callback = control_callback
        self.queue = queue
        self.ms_speech = MSSpeech()
        self.watson_speech = WatsonSpeech()
        self.mood_callback = mood_callback
        self.pix2pix_callback = pix2pix_callback

        self.connected = set()

    async def handler(self, websocket, path):
        print("Websocket connect!")
        self.connected.add(websocket)
        try:
            async for message in websocket:
                data = json.loads(message)
                action = data["action"]
                if action == 'pix2pix':
                    if "loss" in data:
                        self.pix2pix_callback(data["loss"])
                    else:
                        self.pix2pix_callback()
                        
                elif action == 'get-token':
                    await websocket.send(json.dumps({'token': self.ms_speech.obtain_auth_token()}))
                elif action == 'get-watson-token':
                    token = self.watson_speech.obtain_auth_token()
                    print(token)
                    await websocket.send(json.dumps({'token': token}))
                    
                elif (action == 'speech' or action == 'mid-speech'):
                    #print(data['text'])
                    await self.queue.put(data)
                elif (action == 'update-gain'):
                    self.gain_callback(data["min"], data["max"])
                elif (action == 'control'):
                    self.control_callback(data)
                elif action == 'update-mood':
                    self.mood_callback(data)

        finally:
            print("Unregistering Websocket")
            self.connected.remove(websocket)

    def start(self):
        ssl_context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
        ssl_context.load_cert_chain(certfile='../server.crt', keyfile='../server.key')
        print("Websocket listening")
        return websockets.serve(self.handler, '0.0.0.0', 9540, ssl=None)

    async def emotion_update(self,data, state):
        print("Sending emotion update")
        tasks = set()
        for client in self.connected:
            tasks.add(client.send(json.dumps({'action': 'emotion', 'data':data, 'state': state}, cls=NumpyEncoder)))
        await asyncio.gather(*tasks)
    
    async def control(self, command):
        print("Sending control command {}".format(command))
        tasks = set()
        for client in self.connected:
            tasks.add(client.send(json.dumps({'action': 'control', 'command': command})))
        await asyncio.gather(*tasks)

    async def pause_listening(self,seconds):
        print("Sending pause listening")
        tasks = set()
        for client in self.connected:
            tasks.add(client.send(json.dumps({'action': 'pause', 'seconds':seconds})))
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

