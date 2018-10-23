from mozilla_tts import MozillaTTS
from script import Script

moz_tts = MozillaTTS()
script = Script()

index = 0
for line in script.data["script-lines"]:
    try:
        if "triggers-gan" in line:
            utterance = line["triggers-gan"]
            for state in utterance:
                print("State: {}".format(state))
                moz_tts.say(utterance[state], "gan_responses/{}-{}.wav".format(index, state))
    except:
        continue
    finally:
        index += 1

