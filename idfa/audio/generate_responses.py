from mozilla_tts import MozillaTTS
from script import Script

moz_tts = MozillaTTS()
script = Script()

for line in script.data["script-lines"]:
    if "triggers-gan" in line:
        utterance = line["triggers-gan"]
        for state in utterance:
            print("State: {}".format(state))
            moz_tts.say(utterance[state], "pause_test.wav")
        break

