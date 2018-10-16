import os
import asyncio
import websockets
import json
import requests
import numpy as np

class Server:

    def __init__(self, ms_speech):
        print("Init server {}".format(ms_speech))
        self.ms_speech = ms_speech
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
                    print(data['text'])

        finally:
            print("Unregistering Websocket")
            self.connected.remove(websocket)

    def start(self):
        print("Staring server")
        return websockets.serve(self.handler, '127.0.0.1', 5678)

    async def emotion_update(self,data):
        print("Sending emotion update")
        tasks = set()
        for client in self.connected:
            tasks.add(client.send(json.dumps({'action': 'emotion', 'data':data}, cls=NumpyEncoder)))
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

