import os
import sys
import io
import torch 
import time
import numpy as np
from collections import OrderedDict

sys.path.append(os.getcwd() + '/TTS/')  # change here if you don't install TTS by setup.py
sys.path.append(os.getcwd() + '/TTS/notebooks/')  # change here if you don't install TTS by setup.py

import librosa
import librosa.display

from TTS.models.tacotron import Tacotron 
from TTS.layers import *
from TTS.utils.data import *
from TTS.utils.audio import AudioProcessor
from TTS.utils.generic_utils import load_config
from TTS.utils.text import text_to_sequence
from TTS.utils.synthesis import *

class MozillaTTS:

    def __init__(self):
        # Set constants

        #ROOT_PATH = '/home/avnerus/Code/TTS-Data'
        ROOT_PATH = '/home/avnerus/Code/TTS-Data'
        #ROOT_PATH = '/Users/avnerus/Code/TTS-Data'
        CONFIG_PATH = ROOT_PATH + '/config.json'
        OUT_FOLDER = ROOT_PATH + '/test'
        self.CONFIG = load_config(CONFIG_PATH)
        self.MODEL_PATH = ROOT_PATH + '/best_model.pth.tar'
        self.use_cuda = False



    def say(self, text, output):
        try:
            # load the model
            model = Tacotron(self.CONFIG.embedding_size, self.CONFIG.num_freq, self.CONFIG.num_mels, self.CONFIG.r)

            # load the audio processor

            ap = AudioProcessor(self.CONFIG.sample_rate, self.CONFIG.num_mels, self.CONFIG.min_level_db,
                        self.CONFIG.frame_shift_ms, self.CONFIG.frame_length_ms,
                        self.CONFIG.ref_level_db, self.CONFIG.num_freq, self.CONFIG.power, self.CONFIG.preemphasis,
                        60)     

            # load model state

            if self.use_cuda:
                cp = torch.load(self.MODEL_PATH)
            else:
                cp = torch.load(self.MODEL_PATH, map_location=lambda storage, loc: storage)

            # load the model
            print(cp['model'])
            model.load_state_dict(cp['model'])

            if self.use_cuda:
                model.cuda()
            model.eval()

            model.decoder.max_decoder_steps = 400
            wavs = self.text2audio(text, model, self.CONFIG, self.use_cuda, ap)

            audio = np.concatenate(wavs)
            ap.save_wav(audio, output)

            return
        except Exception as ex:
            print(ex)

    def tts(self, model, text, CONFIG, use_cuda, ap, figures=True):
        waveform, alignment, spectrogram, stop_tokens = create_speech(model, text, CONFIG, use_cuda, ap) 
        return waveform

    def text2audio(self, data, model, CONFIG, use_cuda, ap):
        wavs = []
        for segment in data:
            for sen in segment["text"].split('.'):
                if len(sen) < 2:
                    continue
                sen+='.'
                sen = sen.strip()
                wav = self.tts(model, sen, CONFIG, use_cuda, ap)
                wavs.append(wav)
                if not "pause" in segment:
                    wavs.append(np.zeros(10000))
            if "pause" in segment:
                print(segment["pause"] * 10)
                wavs.append(np.zeros(segment["pause"] * 10))
    #     audio = np.stack(wavs)
    #     IPython.display.display(Audio(audio, rate=CONFIG.sample_rate))  
        return wavs



