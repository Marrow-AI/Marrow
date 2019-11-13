from mozilla_tts import MozillaTTS
from script import Script

moz_tts = MozillaTTS()
script = Script()

index = 0
for line in script.data["script-lines"]:
    try:
        if "speaker" in line and line["speaker"] == "house" :
            utterance = line["lines"]
            dmoz_tts.say(utterance, "gan_new/house_{}.wav".format(index))
        if "in-ear" in line:
            inears = line["in-ear"]
            for inear in inears:
                target = inear["target"]
                utterance = inear["lines"]
                moz_tts.say(utterance, "gan_new/in_ear_{}_{}.wav".format(target,index))
    except:
        continue
    finally:
        index += 1
