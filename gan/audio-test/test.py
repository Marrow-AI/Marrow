"""PyAudio Example: Play a wave file (callback version)."""

import pyaudio
import wave
import time
import sys


import sounddevice as sd
import soundfile as sf

audio = pyaudio.PyAudio()


print("----------------------output device list---------------------")
info = audio.get_host_api_info_by_index(0)
numdevices = info.get('deviceCount')
for i in range(0, numdevices):
        if (audio.get_device_info_by_host_api_device_index(0, i).get('maxOutputChannels')) > 1:
            device = audio.get_device_info_by_host_api_device_index(0, i)
            #print(device)
            print("output Device id ", i, " - ", audio.get_device_info_by_host_api_device_index(0, i).get('name'))

print("-------------------------------------------------------------")


wf = wave.open('in_ear_{}_{}.wav'.format('mom', 1), 'rb')

def fill_buffer(in_data, frame_count, time_info, status_flags):
    """Continuously collect data from the audio stream, into the buffer."""
    print("MIC DATA")
    return None, pyaudio.paContinue

input_stream = audio.open(
    format=pyaudio.paInt16,
    # The API currently only supports 1-channel (mono) audio
    # https://goo.gl/z757pE
    channels=1, rate=16000,
    input=True, frames_per_buffer=int(16000 / 10),
    input_device_index=2,
    # Run the audio stream asynchronously to fill the buffer object.
    # This is necessary so that the input device's buffer doesn't
    # overflow while the calling thread makes network requests, etc.
    stream_callback=fill_buffer
)


"""
def callback(in_data, frame_count, time_info, status):
    data = wf.readframes(frame_count)
    print("Audio status: {}".format(status))
    return (data, pyaudio.paContinue)


# open stream using callback (3)
stream = audio.open(format=audio.get_format_from_width(wf.getsampwidth()),
                channels=wf.getnchannels(),
                rate=wf.getframerate(),
                output=True,
                output_device_index=17,
                stream_callback=callback)

# start the stream (4)
stream.start_stream()

# wait for stream to finish (5)
while stream.is_active():
    time.sleep(0.1)

# stop stream (6)
stream.stop_stream()
stream.close()
wf.close()

# close PyAudio (7)
p.terminate()

"""

"""
devices = sd.query_devices()
for device in devices:
    print(device)
"""


data, fs = sf.read('in_ear_{}_{}.wav'.format('mom', 1), dtype='float32')
sd.play(data, fs, device=17)
print("Playing something")
sd.wait()

"""


# instantiate PyAudio (1)
p = pyaudio.PyAudio()


wf1 = wave.open('test.wav', 'rb')
wf2 = wave.open('test2.wav', 'rb')
wf3 = wave.open('test2.wav', 'rb')

interfaces = []
for i in range(p.get_device_count()):
    data = p.get_device_info_by_index(i)
    print(data)

# define callback (2)
def callback1(in_data, frame_count, time_info, status):
    data = wf1.readframes(frame_count)
    return (data, pyaudio.paContinue)

def callback2(in_data, frame_count, time_info, status):
    data = wf2.readframes(frame_count)
    return (data, pyaudio.paContinue)

def callback3(in_data, frame_count, time_info, status):
    data = wf3.readframes(frame_count)
    return (data, pyaudio.paContinue)

# open stream using callback (3)
stream1 = p.open(format=p.get_format_from_width(wf1.getsampwidth()),
                channels=wf1.getnchannels(),
                rate=wf1.getframerate(),
                output=True,
                output_device_index=13,
                stream_callback=callback1)

# start the stream (4)
stream1.start_stream()

stream2 = p.open(format=p.get_format_from_width(wf2.getsampwidth()),
                channels=wf2.getnchannels(),
                rate=wf2.getframerate(),
                output=True,
                output_device_index=17,
                stream_callback=callback2)

# start the stream (4)
stream2.start_stream()

stream3 = p.open(format=p.get_format_from_width(wf3.getsampwidth()),
                channels=wf3.getnchannels(),
                rate=wf3.getframerate(),
                output=True,
                output_device_index=9,
                stream_callback=callback3)

# start the stream (4)
stream3.start_stream()

# wait for stream to finish (5)
while stream1.is_active():
    time.sleep(0.1)

# stop stream (6)
stream1.stop_stream()
stream1.close()
stream2.stop_stream()
stream2.close()
stream3.stop_stream()
stream3.close()
wf1.close()
wf2.close()
wf3.close()


# close PyAudio (7)
p.terminate()
"""