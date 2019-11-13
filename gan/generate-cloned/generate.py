from pathlib import Path
RTVC_PATH = Path('/home/avnerus/Code/Real-Time-Voice-Cloning')

import sys
sys.path.append(str(RTVC_PATH))

import argparse
from synthesizer.inference import Synthesizer
from encoder import inference as encoder
from vocoder import inference as vocoder
import numpy as np
import librosa
import scipy

parser = argparse.ArgumentParser(description='Marrow line generator using cloned voices')
parser.add_argument('voice_sample', metavar='Voice sample file', help='Voice sample file')
args = parser.parse_args()


if __name__ == '__main__':
    print(args.voice_sample)

    encoder_weights = RTVC_PATH / "encoder/saved_models/pretrained.pt"
    vocoder_weights = RTVC_PATH / "vocoder/saved_models/pretrained/pretrained.pt"
    syn_dir = RTVC_PATH / "synthesizer/saved_models/logs-pretrained/taco_pretrained"

    encoder.load_model(encoder_weights)
    synthesizer = Synthesizer(syn_dir)
    vocoder.load_model(vocoder_weights)

    original_wav, sampling_rate = librosa.load(args.voice_sample)
    print("Voice sampling rate: {}".format(sampling_rate))
    print("Default hop size: {}".format(Synthesizer.hparams.hop_size))

    encoder_wav = encoder.preprocess_wav(Path(args.voice_sample))
    embed, partial_embeds, _ = encoder.embed_utterance(encoder_wav, return_partials=True)
    text = "This is not my own voice, but it's quite close isn't it?"
    specs = synthesizer.synthesize_spectrograms([text], [embed])
    generated_wav = vocoder.infer_waveform(specs[0])
    generated_wav = np.pad(generated_wav, (0, synthesizer.sample_rate), mode="constant")
    print("Done")
    scipy.io.wavfile.write('output.wav', Synthesizer.sample_rate, generated_wav)


