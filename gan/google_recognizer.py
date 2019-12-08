from __future__ import division

import re
import sys
import time

from google.cloud import speech
from google.cloud.speech import enums
from google.cloud.speech import types
import pyaudio
from six.moves import queue
from threading import Thread
import asyncio
import janus
import sounddevice as sd
import soundfile as sf

# Audio recording parameters
RATE = 16000
CHUNK = int(RATE / 10)  # 100ms


class MicrophoneStream(object):
    """Opens a recording stream as a generator yielding the audio chunks."""
    def __init__(self, rate, chunk, parent, args, main_loop,audio_interface, device_index):
        self._rate = rate
        self._chunk = chunk
        self.parent = parent
        self.args = args
        self.main_loop = main_loop
        self.device_index = device_index
        self._audio_interface = audio_interface

        #self._buff = queue.Queue()
        self._buff = asyncio.Queue(loop=main_loop)
        self.closed = True

    def __enter__(self):

        self._audio_stream = self._audio_interface.open(
            format=pyaudio.paInt16,
            # The API currently only supports 1-channel (mono) audio
            # https://goo.gl/z757pE
            channels=1, rate=self._rate,
            input=True, frames_per_buffer=self._chunk,
            input_device_index=self.device_index,
            # Run the audio stream asynchronously to fill the buffer object.
            # This is necessary so that the input device's buffer doesn't
            # overflow while the calling thread makes network requests, etc.
            stream_callback=self._fill_buffer
        )

        self.start_time = time.time()

        self.closed = False


        return self

    def __exit__(self, type, value, traceback):
        print("Generator exit!!")
        self._audio_stream.stop_stream()
        self._audio_stream.close()
        self.closed = True
        # Signal the generator to terminate so that the client's
        # streaming_recognize method will not block the process termination.
        self._buff.put_nowait(None)
        #self._audio_interface.terminate()

    def _fill_buffer(self, in_data, frame_count, time_info, status_flags):
        """Continuously collect data from the audio stream, into the buffer."""
        self.main_loop.call_soon_threadsafe(self._buff.put_nowait,in_data)
        return None, pyaudio.paContinue

    async def generator(self):
        while not self.closed and not self.args.restart and not self.args.stop and not self.parent.stop_recognition:
            # Use a blocking get() to ensure there's at least one chunk of
            # data, and stop iteration if the chunk is None, indicating the
            # end of the audio stream.
            chunk = await self._buff.get()
            if chunk is None:
                return
            data = [chunk]

            # Now consume whatever other data's still buffered.
            while not self.args.stop and not self.parent.stop_recognition:
                current_time = time.time()
                diff = current_time - self.start_time
                #print(diff)
                #if (diff > 15):
                #    print("GOOGLE TIMEOUT, Closing")
                #    self.closed = True
                #    break
                try:
                    chunk = self._buff.get_nowait()
                    print("CHUNK")
                    if chunk is None:
                        return
                    data.append(chunk)
                except asyncio.queues.QueueEmpty:
                    break


            yield b''.join(data)

def queue_generator(sync_queue):
    while True:
        value = sync_queue.get()
        if value == None:
            break
        else:
            yield value 

def google_thread(client, streaming_config, sync_queue, main_queue):
    print("Google thread running")
    responses = client.streaming_recognize(streaming_config, queue_generator(sync_queue))
    listen_print_loop(responses, main_queue.sync_q)
    print("Finished google!")

def listen_print_loop(responses, main_queue):
    """Iterates through server responses and prints them.

    The responses passed is a generator that will block until a response
    is provided by the server.

    Each response may contain multiple results, and each result may contain
    multiple alternatives; for details, see https://goo.gl/tjCPAU.  Here we
    print only the transcription for the top alternative of the top result.

    """

    last_result = None

    for response in responses:
        if not response.results:
            continue

        # The `results` list is consecutive. For streaming, we only care about
        # the first result being considered, since once it's `is_final`, it
        # moves on to considering the next utterance.
        result = response.results[0]
        if not result.alternatives:
            continue

        # Display the transcription of the top alternative.
        transcript = result.alternatives[0].transcript


        if not result.is_final:
            #sys.stdout.write(transcript + overwrite_chars + '\r')
            #sys.stdout.flush()

            if (transcript != last_result):
                print("({})".format(transcript))
                main_queue.put_nowait({
                    "action": "mid-speech",
                    "text": transcript
                })
                last_result = transcript

        else:
            print(" = {}".format(transcript))
            main_queue.put_nowait({
                "action": "speech",
                "text": transcript
            })

class Recognizer():

    def __init__(self, speech_queue, main_loop, args):

        self.client = speech.SpeechClient()
        self.args = args
        self.queue = speech_queue
        self.main_loop = main_loop

        self.stop_recognition = False

        # See http://g.co/cloud/speech/docs/languages
        # for a list of supported languages.
        language_code = 'en-US'  # a BCP-47 language tag

        config = types.RecognitionConfig(
            encoding=enums.RecognitionConfig.AudioEncoding.LINEAR16,
            sample_rate_hertz=RATE,
            language_code=language_code)

        self.streaming_config = types.StreamingRecognitionConfig(
            config=config,
            interim_results=True)

    def stop(self):
        self.stop_recognition = True

    async def start(self, audio_interface, device_index):
        self.stop_recognition = False
        self.device_index = device_index
        self.audio_interface = audio_interface
        #print(self.queue)
        #print("Listening on device index {}".format(device_index))
        self.args.restart = False
        while not self.stop_recognition:
            await self.listen()


    async def listen(self):
        self.start_time = time.time()

        with MicrophoneStream(RATE, CHUNK, self, self.args, self.main_loop, self.audio_interface, self.device_index) as stream:
            audio_generator = stream.generator()

            requests = (types.StreamingRecognizeRequest(audio_content=content)
                        async for content in audio_generator)
            
            mic_queue = janus.Queue(loop=self.main_loop)

            self.main_loop.run_in_executor(
                    None, 
                    google_thread, 
                    self.client, 
                    self.streaming_config, 
                    mic_queue.sync_q, 
                    self.queue
            )

            async for request in requests:
                if not self.stop_recognition:
                    mic_queue.sync_q.put(request)
                else:
                    print("Stop mic!")
                    mic_queue.sync_q.put(None)
            print("End!")
            mic_queue.sync_q.put(None)
