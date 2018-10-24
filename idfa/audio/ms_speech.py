import os
import json
import requests
import aiohttp

class MSSpeech:
    def obtain_auth_token(self):
        url = 'https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken'
        headers = {
            'Content-type': 'application/x-www-form-urlencoded',
            'Content-Length': '0',
            'Ocp-Apim-Subscription-Key': os.environ["MICROSOFT_KEY"]
        }

        response = requests.post(url, headers=headers)

        if response.status_code == 200:
            data = response.text
            self.token = data

            print(data)
        elif response.status_code == 403 or response.status_code == 401:
            print('Error: access denied. Please, make sure you provided a valid API key.')
            exit()
        else:
            response.raise_for_status()

        return data


    async def say(self, text):
        print("Say {}".format(text))
        data = await self.synthesize_speech(text)
        with open('/tmp/avner.wav', 'wb') as fd:
            fd.write(data)


    async def synthesize_speech(self, text):
        body = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{}'><voice name='{}'>{}</voice></speak>".format(
        #body = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{}'><voice name='{}'><prosody pitch='low'>{}</prosody></voice></speak>".format(

            'en-US',
            #"Microsoft Server Speech Text to Speech Voice (en-US, BenjaminRUS)", 
            #"Microsoft Server Speech Text to Speech Voice (en-US, Guy24kRUS)",
            "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)",
            text
        )

        headers = {
            "Content-type": "application/ssml+xml; charset=utf-8",
            "X-Microsoft-OutputFormat": "riff-24khz-16bit-mono-pcm",
            "User-Agent": "Marrow",
            "Authorization": self.token
        }
        async with aiohttp.ClientSession() as session:
            async with session.post('https://westus.tts.speech.microsoft.com/cognitiveservices/v1', data=body,headers=headers) as resp:
                print(resp.status)
                return await resp.read()


