from mozilla_tts import MozillaTTS
from script import Script

moz_tts = MozillaTTS()
script = Script()

#intro
#try:
#    moz_tts.say(script.data["intro"], "gan_intro/intro.wav")
#except Exception as e:
#    print("Error generating intro {}".format(e))


#question
try:
    moz_tts.say(script.data["question"]["line"], "gan_question/line.wav")
except Exception as e:
    print("Error generating question line {}".format(e))
index = 0
for timeout in script.data["question"]["timeouts"]:
    try:
        moz_tts.say(timeout["line"], "gan_question/timeout{}.wav".format(index))
    except Exception as e:
        print("Error generating question {}".format(e))
    finally:
        index += 1

