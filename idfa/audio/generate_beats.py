from mozilla_tts import MozillaTTS
from script import Script

moz_tts = MozillaTTS()
script = Script()

index = 0
for line in script.data["script-lines"]:
    try:
        if "triggers-beat" in line:
            utterance = line["triggers-beat"]
            moz_tts.say([utterance], "gan_beats/{}-{}.wav".format(index, utterance["text"]))
    except:
        continue
    finally:
        index += 1

