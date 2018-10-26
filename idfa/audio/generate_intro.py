from mozilla_tts import MozillaTTS
from script import Script

moz_tts = MozillaTTS()
script = Script()

index = 0
for line in script.data["intro"]:
    try:
        moz_tts.say(script.data["intro"][line], "gan_intro/{}.wav".format(line))
    except:
        continue
    finally:
        index += 1

