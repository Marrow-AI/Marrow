import os
import asyncio
import websockets
import json
import requests

class Server:

    def __init__(self, ms_speech):
        print("Init server {}".format(ms_speech))
        self.ms_speech = ms_speech

    async def handler(self, websocket, path):
        print("Websocket connect!")
        async for message in websocket:
            print("Websocket message!")
            print(message)
            data = json.loads(message)
            if data['action'] == 'get-token':
                await websocket.send(json.dumps({'token': obtain_auth_token()}))
            elif (data['action'] == 'speech'):
                print(data['text'])

    def start(self):
        return websockets.serve(self.handler, '127.0.0.1', 5678)

