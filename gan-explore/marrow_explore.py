#!/usr/bin/env python
# -*- coding: utf-8

import sys
sys.path.insert(0,'../stylegan-encoder')

import os, time, re
import cv2
import numpy as np
import pickle
import PIL.Image
import dnnlib
import dnnlib.tflib as tflib
from threading import Thread
import queue
import time
import random

from flask import Flask, jsonify, request, render_template

from encoder.generator_model import Generator

import argparse
parser = argparse.ArgumentParser(description='Marrow StyleGAN Latent space explorer')
parser.add_argument('--shadows', action='store_true' , help='Stream shadows instead of colors')
    
args = parser.parse_args()

class Gan():
    def __init__(self, args):
        self.args = args

    def run(self):
        self.load_snapshot()
        #self.load_latent_source_file('9086.npy')
        self.load_latent_source()
        print("Loaded latent source {}".format(self.latent_source.shape))
        self.load_latent_dest()
        print("Loaded latent dest {}".format(self.latent_dest.shape))
        self.linespaces = np.linspace(0, 1, 100)
        print("Loaded linespaces {}".format(self.linespaces.shape))
        self.linespace_i = 0;
        self.fmt = dict(func=tflib.convert_images_to_uint8, nchw_to_nhwc=True)
        self.forward = True

    def load_latent_source_file(self,f):
        self.latent_source = np.load(f).reshape((1, 16, 512))
        self.original_source = np.copy(self.latent_source)

    def load_latent_source(self):
        self.latent_source = self.rnd.randn(512)[None, :]

    def load_latent_dest(self):
        self.latent_dest = self.rnd.randn(512)[None, :]

    def load_latent_dest_dlatents(self):
        qlatent1 = self.rnd.randn(512)[None, :]
        self.latent_dest = self.Gs.components.mapping.run(qlatent1, None)

    def load_snapshot(self):
        # Load pre-trained network.
        tflib.init_tf()

        self.rnd = np.random.RandomState()
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-final.pkl")
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-009247.pkl")
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-008044.pkl")
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-024287.pkl")
        url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-013458.pkl")
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-010450.pkl")
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-015263.pkl")
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-011653.pkl")
        with open(url, 'rb') as f:
            self._G, self._D, self.Gs = pickle.load(f)
            self.generator = Generator(self.Gs, batch_size=1, randomize_noise=False)
        print(self.Gs)



    def get_buf(self):
            # Generate image.
            if self.linespace_i == 100:
                if self.forward:
                   print('---------------------------BACKWARD-----------------------')
                   #self.forward = False
                   self.latent_source = self.latent_dest
                   #self.latent_dest = self.original_source
                   self.load_latent_dest()

                   print("Latent source {}".format(self.latent_source))
                   print("Latent dest {}".format(self.latent_dest))
                else:
                    print('---------------------------FORWARD-----------------------')
                    self.forward = True
                    self.latent_source = self.original_source
                    self.load_latent_dest()

                self.linespace_i = 0

            self.latents = (self.linespaces[self.linespace_i] * self.latent_dest + (1-self.linespaces[self.linespace_i])*self.latent_source)

            #self.generator.set_dlatents(self.latents)
            #mages = self.Gs.components.synthesis.run(self.latents, randomize_noise=False, output_transform=self.fmt)
            images = self.Gs.run(self.latents, None, truncation_psi=0.7, randomize_noise=True, output_transform=self.fmt)

            print("Got image!")
            image = images[0]
            if self.args.shadows:
                gray = cv2.cvtColor(image, cv2.COLOR_RGB2GRAY)
                ret,black_white = cv2.threshold(gray,3,255,cv2.THRESH_BINARY_INV)
                bgr = cv2.cvtColor(black_white, cv2.COLOR_GRAY2BGR)
                data = cv2.cvtColor(bgr, cv2.COLOR_BGR2YUV)
            else:
                data = cv2.cvtColor(image, cv2.COLOR_RGB2YUV)
            assert data is not None
            self.linespace_i += 1
            return data


gan = Gan(args)
app = Flask(__name__)
app.jinja_env.auto_reload = True

@app.route('/')
def index():
    return render_template('index.html')

@app.route('/generate')
def generate():
    buf = gan.get_buf()
    return jsonify(result=buf)

if __name__ == '__main__':

	#print("Generating samples")
	#for t in np.arange(0, 300, 0.000001):
	#	s.gen(t)
        gan.run()
        app.run (host = "0.0.0.0", port = 9540)


