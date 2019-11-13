import os
import json
import requests
import aiohttp
from watson_developer_cloud import AuthorizationV1 as Authorization
from watson_developer_cloud import SpeechToTextV1 as SpeechToText

class WatsonSpeech:
    def obtain_auth_token(self):
        STT_USERNAME = os.environ['STT_USERNAME'] # '<Speech to Text username>'
        STT_PASSWORD = os.environ['STT_PASSWORD'] # '<Speech to Text password>'
        authorization = Authorization(username=STT_USERNAME, password=STT_PASSWORD)
        return authorization.get_token(url=SpeechToText.default_url)


