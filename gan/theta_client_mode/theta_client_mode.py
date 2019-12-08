"""
Test for THETA V client mode. Your workstation and the THETA V need to be 
connected to the same WiFi router.
This test script uses Python modules requests and pprint.
To install requests and pprint
  
      $ pip install requests
      $ pip install pprint

Once connected with WiFi, use the API here:
https://developers.theta360.com/en/docs/v2.1/api_reference/getting_started.html

The IP address is hardcoded in these tests. To get the IP address, I 
am using a separate program that finds the IP of the THETA with NSD 
http://lists.theta360.guide/t/developing-theta-client-mode-applications/2450

"""
import requests
from requests.auth import HTTPDigestAuth
import pprint
import cv2
import numpy as np

# global constants specific to your THETA. Change for your camera.
THETA_ID = 'THETAYN14100015'
THETA_PASSWORD = '14100015'  # default password. may have been changed
THETA_URL = 'http://192.168.2.183/osc/'


def get(osc_command):
    url = THETA_URL + osc_command
    resp = requests.get(url, auth=(HTTPDigestAuth(THETA_ID, THETA_PASSWORD)))
    pprint.pprint(resp.json())


def post(osc_command):
    url = THETA_URL + osc_command
    resp = requests.post(url,
                         auth=(HTTPDigestAuth(THETA_ID, THETA_PASSWORD)))
    pprint.pprint(resp.json())

def takePicture():
    url = THETA_URL + 'commands/execute'
    payload = {"name": "camera.takePicture"}
    req = requests.post(url,
                        json=payload,
                        auth=(HTTPDigestAuth(THETA_ID, THETA_PASSWORD)))
                        
    response = req.json()


def livePreview():
    url = THETA_URL + 'commands/execute'
    payload = {"name": "camera.getLivePreview"}
    buffer = bytes()
    with requests.post(url,
                        json=payload,
                        auth=(HTTPDigestAuth(THETA_ID, THETA_PASSWORD)),
                        stream=True) as r:
        for chunk in r.iter_content(chunk_size=1024):
            buffer += chunk
            a = buffer.find(b'\xff\xd8')
            b = buffer.find(b'\xff\xd9')
            if a != -1 and b != -1:
                jpg = buffer[a:b+2]
                buffer = buffer[b+2:]
                i = cv2.imdecode(np.fromstring(jpg, dtype=np.uint8), cv2.IMREAD_COLOR)
                cv2.imshow('i', i)
                if cv2.waitKey(1) == 27:
                    exit(0)
                        
get("info")
livePreview()
#post("state")
#post("state")
#takePicture()
