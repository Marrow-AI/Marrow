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
import asyncio
import base64

from flask import Flask, jsonify, request, render_template
from flask_compress import Compress

from encoder.generator_model import Generator

import argparse
parser = argparse.ArgumentParser(description='Marrow StyleGAN Latent space explorer')
parser.add_argument('--shadows', action='store_true' , help='Stream shadows instead of colors')
    
args = parser.parse_args()

class Gan(Thread):
    def __init__(self, queue, loop, args):
        self.queue = queue
        self.loop = loop
        self.args = args
        Thread.__init__(self)

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
        self.number_frames = 0
        self.fmt = dict(func=tflib.convert_images_to_uint8, nchw_to_nhwc=True)
        self.forward = True
        self.push_frames()

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

    def push_frames(self):
        while True:
            (src,args) = self.queue.get()
            print("Sending to {} shadows {}".format(src,args.get('shadows')))
            image = self.get_buf(args.get('shadows'))
            ret, buf = cv2.imencode('.jpg', image)
            b64 = base64.b64encode(buf)
            b64text = b64.decode('utf-8')
            self.loop.call_soon_threadsafe(
                src.set_result, b64text
            )
            #self.last_push = now
            self.number_frames += 1

    def get_buf(self, shadows):
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

            image = images[0]
            if int(shadows):
                gray = cv2.cvtColor(image, cv2.COLOR_RGB2GRAY)
                ret,black_white = cv2.threshold(gray,50,255,cv2.THRESH_BINARY_INV)
                data = cv2.cvtColor(black_white, cv2.COLOR_GRAY2BGR)
            else:
                #data = cv2.cvtColor(image, cv2.COLOR_RGB2YUV)
                data = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            print("Got image! {}".format(data.shape))
            assert data is not None
            self.linespace_i += 1
            return data

loop = asyncio.get_event_loop()
q = queue.Queue()
gan = Gan(q, loop, args)
app = Flask(__name__)
Compress(app)
app.jinja_env.auto_reload = True

@app.route('/')
def index():
    return render_template('index.html')

@app.route('/generate')
def generate():
    future = loop.create_future()
    q.put((future,request.args))
    data = loop.run_until_complete(future)
    return jsonify(result=data)

if __name__ == '__main__':

	#print("Generating samples")
	#for t in np.arange(0, 300, 0.000001):
	#	s.gen(t)
        gan.start()
        app.run (host = "0.0.0.0", port = 9540)


