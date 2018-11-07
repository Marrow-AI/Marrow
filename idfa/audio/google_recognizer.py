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

# Audio recording parameters
RATE = 16000
CHUNK = int(RATE / 10)  # 100ms


class MicrophoneStream(object):
    """Opens a recording stream as a generator yielding the audio chunks."""
    def __init__(self, rate, chunk, parent, args):
        self._rate = rate
        self._chunk = chunk
        self.parent = parent
        self.args = args

        # Create a thread-safe buffer of audio data
        self._buff = queue.Queue()
        self.closed = True

    def __enter__(self):
        self._audio_interface = pyaudio.PyAudio()
        self._audio_stream = self._audio_interface.open(
            format=pyaudio.paInt16,
            # The API currently only supports 1-channel (mono) audio
            # https://goo.gl/z757pE
            channels=1, rate=self._rate,
            input=True, frames_per_buffer=self._chunk,
            # Run the audio stream asynchronously to fill the buffer object.
            # This is necessary so that the input device's buffer doesn't
            # overflow while the calling thread makes network requests, etc.
            stream_callback=self._fill_buffer,
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
        self._buff.put(None)
        self._audio_interface.terminate()

    def _fill_buffer(self, in_data, frame_count, time_info, status_flags):
        """Continuously collect data from the audio stream, into the buffer."""
        self._buff.put(in_data)
        return None, pyaudio.paContinue

    def generator(self):
        while not self.closed and not self.args.restart and not self.args.stop and not self.parent.stop_recognition:
            # Use a blocking get() to ensure there's at least one chunk of
            # data, and stop iteration if the chunk is None, indicating the
            # end of the audio stream.
            chunk = self._buff.get()
            if chunk is None:
                return
            data = [chunk]

            # Now consume whatever other data's still buffered.
            while not self.args.stop and not self.parent.stop_recognition:
                current_time = time.time()
                diff = current_time - self.start_time
                #print(diff)
                if (diff > 55):
                    self.closed = True
                    break
                try:
                    chunk = self._buff.get(block=False)
                    if chunk is None:
                        return
                    data.append(chunk)
                except queue.Empty:
                    break

            
            yield b''.join(data)



class Recognizer(Thread):

    def __init__(self, queue, args):

        Thread.__init__(self)
        self.client = speech.SpeechClient()
        self.args = args
        self.queue = queue

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

        self.last_result = None


    def stop(self):
        print("Stopping recognition")
        self.stop_recognition = True

    def start(self):
        self.stop_recognition = False
        while not self.args.stop and not self.stop_recognition:
            print("Listen again?")
            self.args.restart = False
            self.listen()


    def listen(self):
        self.start_time = time.time()

        with MicrophoneStream(RATE, CHUNK, self, self.args) as stream:
            audio_generator = stream.generator()
            requests = (types.StreamingRecognizeRequest(audio_content=content)
                        for content in audio_generator)

            responses = self.client.streaming_recognize(self.streaming_config, requests)

            # Now, put the transcription responses to use.
            self.listen_print_loop(responses)

            print("End?")


    def listen_print_loop(self,responses):
        """Iterates through server responses and prints them.

        The responses passed is a generator that will block until a response
        is provided by the server.

        Each response may contain multiple results, and each result may contain
        multiple alternatives; for details, see https://goo.gl/tjCPAU.  Here we
        print only the transcription for the top alternative of the top result.

        """
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

                if (transcript != self.last_result):
                    print("({})".format(transcript))
                    self.queue.put({
                        "action": "mid-speech",
                        "text": transcript                        
                    })
                    self.last_result = transcript

            else:
                print(" = {}".format(transcript))
                self.queue.put({
                    "action": "speech",
                    "text": transcript                        
                })
