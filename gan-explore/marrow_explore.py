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
import tensorflow as tf

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
        self.steps = 100
        self.current_snapshot = args.snapshot
        Thread.__init__(self)

    def run(self):
        self.load_snapshot(self.current_snapshot)
        #self.load_latent_source_file('9086.npy')
        self.load_latent_source()
        self.load_latent_dest()
        self.linespaces = np.linspace(0, 1, self.steps)
        print("Loaded linespaces {}".format(self.linespaces.shape))
        self.linespace_i = -1;
        self.number_frames = 0
        self.fmt = dict(func=tflib.convert_images_to_uint8, nchw_to_nhwc=True)
        self.forward = True
        self.push_frames()

    def load_latent_source_file(self,f):
        self.latent_source = np.load(f).reshape((1, 16, 512))
        self.original_source = np.copy(self.latent_source)

    def load_latent_source(self):
        self.latent_source = self.rnd.randn(512)[None, :]
        print("Loaded latent source {}".format(self.latent_source.shape))

    def load_latent_dest(self):
        self.latent_dest = self.rnd.randn(512)[None, :]
        print("Loaded latent dest {}".format(self.latent_dest.shape))

    def load_latent_dest_dlatents(self):
        qlatent1 = self.rnd.randn(512)[None, :]
        self.latent_dest = self.Gs.components.mapping.run(qlatent1, None)

    def load_latent_dest_from_source(self,source):
        pass
        

    def load_snapshot(self, snapshot):
        tflib.init_tf()
        self.rnd = np.random.RandomState()

        print("Loading snapshot {}".format(snapshot))
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-final.pkl")
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-009247.pkl")
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-008044.pkl")
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-024287.pkl")
        url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-{}.pkl".format(snapshot))
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-010450.pkl")
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-015263.pkl")
        #url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-011653.pkl")
        with open(url, 'rb') as f:
            self._G, self._D, self.Gs = pickle.load(f)
            self.generator = Generator(self.Gs, batch_size=1, randomize_noise=False)
        print(self.Gs)

    def push_frames(self):
        while True:
            (future,request,args) = self.queue.get()
            if request == "generate":
                print(
                    "Generating to {} direction {}, shadows {}".
                    format(future,args.get('direction'),args.get('shadows'))
                )
                if args.get('direction') == "forward":
                    self.linespace_i = min(self.steps-1, self.linespace_i + 1)
                else:
                    self.linespace_i = max(0,self.linespace_i - 1)

                image = self.get_buf(args.get('shadows'))
                ret, buf = cv2.imencode('.jpg', image)
                b64 = base64.b64encode(buf)
                b64text = b64.decode('utf-8')
                self.loop.call_soon_threadsafe(
                    future.set_result, b64text
                )
                #self.last_push = now
                self.number_frames += 1

            elif request == "shuffle":
                print("Shuffling to {} steps {} snapshot {}".format(future, args['steps'],args['snapshot']))
                self.steps = int(args['steps'])
                self.linespaces = np.linspace(0, 1, self.steps)
                if args['snapshot'] != self.current_snapshot:
                    self.current_snapshot = args['snapshot']
                    tf.get_default_session().close()
                    tf.reset_default_graph()
                    print('New snapshot, quiting GAN thread')
                    break
                else:
                    self.load_latent_source()
                    self.load_latent_dest()
                    self.linespace_i = -1
                    self.loop.call_soon_threadsafe(
                        future.set_result, "OK"
                    )
            elif request == "save":
                print("Saving current animation, name: {}".format(args['name']))
                self.loop.call_soon_threadsafe(
                    future.set_result, "OK"
                )

    def get_buf(self, shadows):
            # Generate image.
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
            return data

loop = asyncio.get_event_loop()
q = queue.Queue()
args.snapshot = "013458"
gan = Gan(q, loop, args)
app = Flask(__name__)
Compress(app)
app.jinja_env.auto_reload = True
gan.start()

@app.route('/')
def index():
    return render_template('index.html')

@app.route('/generate')
def generate():
    future = loop.create_future()
    q.put((future, "generate", request.args))
    data = loop.run_until_complete(future)
    return jsonify(result=data)

@app.route('/shuffle',  methods = ['POST'])
def shuffle():
    future = loop.create_future()
    params = request.get_json()
    q.put((future, "shuffle", params))
    if params['snapshot'] == args.snapshot:
        data = loop.run_until_complete(future)
        return jsonify(result=data)
    else:
        print('Reloading GAN for new snapshot')
        global gan
        gan.join()
        args.snapshot = params['snapshot']
        gan = Gan(q, loop, args)
        gan.start()
        return jsonify(result="OK")

@app.route('/save',  methods = ['POST'])
def save():
    future = loop.create_future()
    params = request.get_json()
    q.put((future, "save", params))
    data = loop.run_until_complete(future)
    return jsonify(result=data)

if __name__ == '__main__':

	#print("Generating samples")
	#for t in np.arange(0, 300, 0.000001):
	#	s.gen(t)
        app.run (host = "0.0.0.0", port = 9540)


