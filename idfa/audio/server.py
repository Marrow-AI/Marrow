import asyncio
import websockets
import json

async def handler(websocket, path):
    print("Websocket connect!")
    async for message in websocket:
        print("Websocket message!")
        print(message)
        data = json.loads(message)
        if data['action'] == 'get-key':
            await websocket.send(json.dumps({'key': os.environ["MICROSOFT-KEY"]}))

def start():
    return websockets.serve(handler, '127.0.0.1', 5678)

