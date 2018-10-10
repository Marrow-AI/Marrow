import os
import asyncio
import websockets
import json
import requests

async def handler(websocket, path):
    print("Websocket connect!")
    async for message in websocket:
        print("Websocket message!")
        print(message)
        data = json.loads(message)
        if data['action'] == 'get-token':
            await websocket.send(json.dumps({'token': obtain_auth_token(os.environ["MICROSOFT_KEY"])}))
        elif (data['action'] == 'speech'):
            print(data['text'])

def obtain_auth_token(api_key):
    url = 'https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken'
    headers = {
        'Content-type': 'application/x-www-form-urlencoded',
        'Content-Length': '0',
        'Ocp-Apim-Subscription-Key': api_key
    }

    response = requests.post(url, headers=headers)

    if response.status_code == 200:
        data = response.text
        print(data)
    elif response.status_code == 403 or response.status_code == 401:
        print('Error: access denied. Please, make sure you provided a valid API key.')
        exit()
    else:
        response.raise_for_status()

    return data

def start():
    return websockets.serve(handler, '127.0.0.1', 5678)

