import pyaudio
from scipy.io import wavfile
import wave
import time
import sys
import soundcard as sc
import numpy as np
import wave

speakers = sc.all_speakers()
print(speakers)

"""
audio = pyaudio.PyAudio()

def fill_buffer(in_data, frame_count, time_info, status_flags):
    print("MIC DATA")
    return None, pyaudio.paContinue

input_stream = audio.open(
    format=pyaudio.paInt16,
    # The API currently only supports 1-channel (mono) audio
    # https://goo.gl/z757pE
    channels=1, rate=16000,
    input=True, frames_per_buffer=int(16000 / 10),
    input_device_index=3,
    # Run the audio stream asynchronously to fill the buffer object.
    # This is necessary so that the input device's buffer doesn't
    # overflow while the calling thread makes network requests, etc.
    stream_callback=fill_buffer
)

time.sleep(5)

input_stream.stop_stream()
input_stream.close()

"""

wf = wave.open('in_ear_{}_{}.wav'.format('mom', 1), 'r')
print(wf.getparams())
mom = sc.get_speaker('2- Trekz Air by AfterShokz')
print(mom)
#[rate, data] = wavfile.read('in_ear_{}_{}.wav'.format('mom', 1))
[rate, data] = wavfile.read('recordedFile.wav')
mom.play(data/np.max(data), samplerate=rate)
