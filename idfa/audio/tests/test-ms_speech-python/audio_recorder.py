import pyaudio
import wave


class AudioRecorder:
    def __init__(self):
        self.format = pyaudio.paInt16
        self.channels = 1
        self.rate = 16000
        self.chunk = 256
        self.record_seconds = 10
        self.output_filename = 'data/recording.wav'
        self.audio = pyaudio.PyAudio()

        self.stream = None
        self.frames = []


    # start recording
    def start(self):
        self.stream = self.audio.open(format=self.format,
                                      channels=self.channels,
                                      rate=self.rate,
                                      input=True,
                                      frames_per_buffer=self.chunk)

        print('\nRecording...', end=' ')

        for i in range(int(self.rate / self.chunk * self.record_seconds)):
            data = self.stream.read(self.chunk)
            self.frames.append(data)

        print('Done!')

        # stop recording
        self.stream.stop_stream()
        self.stream.close()
        self.audio.terminate()

        self.__save()

        return self.output_filename


    # save the recording to a file
    def __save(self):
        wave_file = wave.open(self.output_filename, 'wb')

        wave_file.setnchannels(self.channels)
        wave_file.setsampwidth(self.audio.get_sample_size(self.format))
        wave_file.setframerate(self.rate)
        wave_file.writeframes(b''.join(self.frames))

        wave_file.close()
