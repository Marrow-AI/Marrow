#!/usr/bin/env python
# -*- coding: utf-8

import sys

import os, time, re
import cv2
import numpy as np
import pickle
import PIL.Image
from threading import Thread
import queue
import time
import random
import asyncio
import base64

from flask import Flask, jsonify, request, render_template, send_file
from flask_compress import Compress
from flask_cors import CORS
import argparse
import json

from flask_compress import Compress
from flask_cors import CORS, cross_origin
from flask_socketio import SocketIO,send,emit,join_room

parser = argparse.ArgumentParser(description='Marrow StyleGAN Latent space explorer')

parser.add_argument('--dummy', action='store_true' , help='Use a Dummy GAN')
    
args = parser.parse_args()

if not args.dummy:
    import dnnlib
    import dnnlib.tflib as tflib
    import tensorflow as tf

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
        self.load_latent_source()
        self.load_latent_dest()
        self.linespaces = np.linspace(0, 1, self.steps)
        print("Loaded linespaces {}".format(self.linespaces.shape))
        self.linespace_i = -1;
        self.number_frames = 0
        self.fmt = dict(func=tflib.convert_images_to_uint8, nchw_to_nhwc=True)
        self.forward = True
        self.push_frames()

    def load_latent_source(self):
        self.latent_source = self.rnd.randn(512)[None, :]
        print("Loaded latent source {}".format(self.latent_source.shape))

    def load_latent_dest(self):
        self.latent_dest = self.rnd.randn(512)[None, :]
        print("Loaded latent dest {}".format(self.latent_dest.shape))

    def load_latent_dest_dlatents(self):
        qlatent1 = self.rnd.randn(512)[None, :]
        self.latent_dest = self.Gs.components.mapping.run(qlatent1, None)

    def load_snapshot(self, snapshot):
        tflib.init_tf()
        self.rnd = np.random.RandomState()

        print("Loading snapshot {}".format(snapshot))
        url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-{}.pkl".format(snapshot))
        with open(url, 'rb') as f:
            self._G, self._D, self.Gs = pickle.load(f)
        print(self.Gs)

    def push_frames(self):
        while True:
            (future,request,args) = self.queue.get()
            if request == "generate":
                print(
                    "Generating to {} direction {} steps {}, shadows {}".
                    format(future,args.get('direction'),args.get('steps'), args.get('shadows'))
                )
                steps = int(args.get('steps'))
                if args.get('direction') == "forward":
                    self.linespace_i = min(self.steps-1, self.linespace_i + steps)
                else:
                    self.linespace_i = max(0,self.linespace_i - steps)

                image = self.get_buf(args.get('shadows'))
                ret, buf = cv2.imencode('.jpg', image)
                b64 = base64.b64encode(buf)
                b64text = b64.decode('utf-8')
                self.loop.call_soon_threadsafe(
                    future.set_result, b64text
                )
                self.number_frames += 1

            elif request == "shuffle":
                print("Shuffling to {} steps {} snapshot {} type {}".format(future, args['steps'],args['snapshot'], args['type']))
                self.steps = int(args['steps'])
                self.linespaces = np.linspace(0, 1, self.steps)
                if args['snapshot'] != self.current_snapshot:
                    self.current_snapshot = args['snapshot']
                    tf.get_default_session().close()
                    tf.reset_default_graph()
                    print('New snapshot, quiting GAN thread')
                    break
                else:
                    try:
                        if args['type'] == 'both':
                            self.load_latent_source()
                            self.load_latent_dest()
                        elif args['type'] == 'keep_source':
                            self.load_latent_dest()

                        elif args['type'] == 'use_dest':
                            self.latent_source = self.latent_dest
                            self.load_latent_dest()
                        else:
                            raise Exception('Invalid generation type')

                        self.linespace_i = -1
                        self.loop.call_soon_threadsafe(
                            future.set_result, "OK"
                        )

                    except Exception as e:
                        self.loop.call_soon_threadsafe(
                            future.set_result, str(e)
                        )

            elif request == "save":
                print("Saving current animation, name: {}".format(args['name']))
                try:
                    if ('name' not in args or len(args['name']) == 0):
                        raise Exception('Name cannot be empty')
                    try:
                        os.mkdir('animations/{}'.format(args['name']))
                    except FileExistsError:
                        pass

                    info = {
                        'name': args['name'],
                        'snapshot': self.current_snapshot,
                        'steps': self.steps
                    }
                    with open('animations/{}/info.json'.format(args['name']), 'w+') as info_file:
                        json.dump(info, info_file)                        

                    with open('animations/{}/source.npy'.format(args['name']), 'wb+') as source_file:
                        np.save(source_file, self.latent_source)

                    with open('animations/{}/dest.npy'.format(args['name']), 'wb+') as dest_file:
                        np.save(dest_file, self.latent_dest)

                    self.loop.call_soon_threadsafe(
                        future.set_result, "OK"
                    )

                except Exception as e:
                    self.loop.call_soon_threadsafe(
                        future.set_result, str(e)
                    )
            elif request == "load":
                print("Loading animation {}".format(args['animation']))
                with open('animations/{}/info.json'.format(args['animation'])) as json_file:
                    data = json.load(json_file)
                    print(data);

                    if data['snapshot'] != self.current_snapshot:
                        self.current_snapshot = data['snapshot']
                        tf.get_default_session().close()
                        tf.reset_default_graph()
                        print('New snapshot, quiting GAN thread')
                        self.loop.call_soon_threadsafe(
                            future.set_result, {'status' : 'new_snapshot', 'snapshot': data['snapshot']}
                        )
                        break
                    else:
                        self.latent_source = np.load('animations/{}/source.npy'.format(args['animation']))
                        print('Loaded source {}'.format(self.latent_source.shape))
                        self.latent_dest = np.load('animations/{}/dest.npy'.format(args['animation']))
                        print('Loaded dest {}'.format(self.latent_dest.shape))
                        self.steps = int(data['steps'])
                        self.linespaces = np.linspace(0, 1, self.steps)
                        self.linespace_i = 0
                        self.loop.call_soon_threadsafe(
                                future.set_result, {'status' : 'OK', 'steps': data['steps']}
                        )


            elif request == "video":
                print("Download animation video. Shadows:  {}".format(args.get('shadows')))
                try:
                    writer = cv2.VideoWriter("output.avi",cv2.VideoWriter_fourcc(*"MJPG"), 30,(512,512))
                    shadows = int(args.get('shadows'))
                    for i in range(self.steps):
                        self.linespace_i = i
                        print(self.linespace_i)
                        img = self.get_buf(shadows)
                        writer.write(img)
                    writer.release()
                    self.linespace_i = 0


                    self.loop.call_soon_threadsafe(
                        future.set_result, "OK"
                    )

                except Exception as e:
                    self.loop.call_soon_threadsafe(
                        future.set_result, str(e)
                    )



    def get_buf(self, shadows):
            # Generate image.
            self.latents = (self.linespaces[self.linespace_i] * self.latent_dest + (1-self.linespaces[self.linespace_i])*self.latent_source)

            images = self.Gs.run(self.latents, None, truncation_psi=0.7, randomize_noise=False, output_transform=self.fmt)
            image = images[0]

            if int(shadows):
                gray = cv2.cvtColor(image, cv2.COLOR_RGB2GRAY)
                ret,black_white = cv2.threshold(gray,50,255,cv2.THRESH_BINARY_INV)
                data = cv2.cvtColor(black_white, cv2.COLOR_GRAY2BGR)
            else:
                data = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

            print("Got image! {}".format(data.shape))
            assert data is not None
            return data

class DummyGan(Thread):
    def __init__(self, queue, loop, args):
        self.queue = queue
        self.loop = loop
        self.args = args
        Thread.__init__(self)

    def run(self):
        print("Running dummy GAN")
        self.push_frames()

    def push_frames(self):
        while True:
            (future,request,args) = self.queue.get()
            if request == "generate":
                image = self.get_sample()
                ret, buf = cv2.imencode('.jpg', image)
                b64 = base64.b64encode(buf)
                b64text = b64.decode('utf-8')
                self.loop.call_soon_threadsafe(
                    future.set_result, b64text
                )

            elif request == "load":
                self.loop.call_soon_threadsafe(
                    future.set_result, {'status' : 'OK', 'steps': 666}
                )
            else:
                self.loop.call_soon_threadsafe(
                    future.set_result, "OK"
                )

    def get_sample(self):
        blank_image = np.zeros((512,512,3), np.uint8)
        blank_image[:,0:random.randint(1,512)//2] = (255,0,0)
        blank_image[:,random.randint(1,512)//2:512] = (0,255,0)
        data = cv2.cvtColor(blank_image, cv2.COLOR_BGR2RGB)
        #data = blank_image
        return data

loop = asyncio.get_event_loop()
q = queue.Queue()
args.snapshot = "007743"

if not args.dummy:
    gan = Gan(q, loop, args)
else:
    gan = DummyGan(q,loop,args)

app = Flask(__name__)
Compress(app)
#CORS(app)
app.jinja_env.auto_reload = True
gan.start()

app.config['SECRET_KEY'] = 'mysecret'
socketio = SocketIO(app, cors_allowed_origins="*")

@socketio.on('connect')
def on_connect():
    print("Client connected {}".format(request.sid))
    join_room(request.sid)
    

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
    print(params)
    q.put((future, "shuffle", params))
    print("placing on q")
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

@app.route('/video',  methods = ['GET'])
def video():
    future = loop.create_future()
    q.put((future, "video", request.args))
    data = loop.run_until_complete(future)
    if data == 'OK':
        return send_file('output.avi', as_attachment=True, attachment_filename='output.avi')
    else:
        return jsonify(result=data)

@app.route('/list',  methods = ['GET'])
def list():
    subfolders = [os.path.basename(f.path) for f in os.scandir('./animations') if f.is_dir() ]    
    return jsonify(animations=subfolders)

@app.route('/load',  methods = ['POST'])
def load():
    future = loop.create_future()
    params = request.get_json()
    q.put((future, "load", params))
    data = loop.run_until_complete(future)
    print(data)
    if data['status'] == 'OK':
        return jsonify(result=data)
    elif data['status'] == 'new_snapshot':
        print('Reloading GAN for new snapshot {}'.format(data['snapshot']))
        global gan
        gan.join()
        args.snapshot = data['snapshot']
        gan = Gan(q, loop, args)
        gan.start()
        return jsonify(result=data)
    else:
        return jsonify(result=data)

@app.route('/', defaults={'path': ''})
@app.route('/<path:path>')
def catch_all(path):
    return render_template('index.html')

if __name__ == '__main__':

	#print("Generating samples")
	#for t in np.arange(0, 300, 0.000001):
	#	s.gen(t)
        app.run (host = "0.0.0.0", port = 8540)


