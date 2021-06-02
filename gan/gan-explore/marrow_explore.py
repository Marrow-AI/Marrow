#!/usr/bin/env python
# -*- coding: utf-8

import sys

import os, time, re
import cv2
import numpy as np
import pickle
from PIL import Image
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
import io
import tempfile
from tqdm import tqdm

from flask_compress import Compress
from flask_cors import CORS, cross_origin
from flask_socketio import SocketIO,send,emit,join_room

parser = argparse.ArgumentParser(description='Marrow StyleGAN Latent space explorer')

parser.add_argument('--dummy', action='store_true' , help='Use a Dummy GAN')
    
args = parser.parse_args()

landmarks_detector = None

if not args.dummy:
    sys.path.append("./stylegan-encoder")
    import dnnlib
    import dnnlib.tflib as tflib
    import tensorflow as tf
    from ffhq_dataset.face_alignment import image_align
    from ffhq_dataset.landmarks_detector import LandmarksDetector
    from encoder.generator_model import Generator
    from encoder.perceptual_model import PerceptualModel, PerceptualModelOld
    landmarks_detector = LandmarksDetector('marrow/shape_predictor_68_face_landmarks.dat')

class Gan(Thread):
    def __init__(self, queue, app, loop, args):
        self.queue = queue
        self.app = app
        self.loop = loop
        self.args = args
        self.steps = int(args.steps) if 'steps' in args else 100
        self.current_snapshot = args.snapshot
        Thread.__init__(self)

    def run(self):
        print("GAN Running")
        self.load_snapshot(self.current_snapshot)
        self.load_latent_source_dlatents()
        self.load_latent_dest_dlatents()
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

    def load_latent_source_dlatents(self):
        qlatent1 = self.rnd.randn(512)[None, :]
        self.latent_source = self.Gs.components.mapping.run(qlatent1, None)
        print("Loaded latent source {}".format(self.latent_source.shape))

    def load_latent_dest_dlatents(self):
        qlatent1 = self.rnd.randn(512)[None, :]
        self.latent_dest = self.Gs.components.mapping.run(qlatent1, None)
        print("Loaded latent dest {}".format(self.latent_dest.shape))

    def load_snapshot(self, snapshot):
        tflib.init_tf()
        self.rnd = np.random.RandomState()

        print("Loading snapshot {}".format(snapshot))
        url = os.path.abspath("marrow/00021-sgan-dense512-8gpu/network-snapshot-{}.pkl".format(snapshot))

        if snapshot == 'ffhq':
            dimension =  18
            img_size = 1024
        else:
            dimension = 16
            img_size = 512

        with open(url, 'rb') as f:
            self._G, self._D, self.Gs = pickle.load(f)
            self.encoder_generator = Generator(self.Gs, 1, dimension, randomize_noise=False)

            if snapshot == 'ffhq':
                print("Building encoder perceptual model")
                #self.perceptual_model = PerceptualModel(256, layer=9, batch_size=1)
                self.perceptual_model = PerceptualModelOld(256, batch_size=1)
                self.perceptual_model.build_perceptual_model(self.encoder_generator.generated_image)
            else:
                self.perceptual_model = None
        print(self.Gs)

    def emit_frame(self, topic, index):
        self.linespace_i = index 
        print(self.linespace_i)
        img = self.get_buf(False)
        ret, buf = cv2.imencode('.jpg', img)
        b64 = base64.b64encode(buf)
        b64text = b64.decode('utf-8')
        emit(topic,{'step':index, 'image': b64text},namespace='/',broadcast=True)

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

            elif request == "publish":
                print(
                    "Publishing {} step to sockets".
                    format(self.steps)
                )
                self.loop.call_soon_threadsafe(
                    future.set_result, "OK"
                )
                with self.app.app_context():
                    emit('publishStart',{'steps':self.steps},namespace='/',broadcast=True)
                    for i in range(self.steps):
                        self.emit_frame('animationStep', i)
                        time.sleep(0.5)
                    emit('publishStop',{'steps':self.steps},namespace='/',broadcast=True)

                    self.linespace_i = 0



            elif request == "shuffle":
                print("Shuffling to {} steps {} snapshot {} type {}".format(future, args['steps'],args['snapshot'], args['type']))
                if args['snapshot'] != self.current_snapshot:
                    self.current_snapshot = args['snapshot']
                    tf.get_default_session().close()
                    tf.reset_default_graph()
                    print('New snapshot, quiting GAN thread')
                    break
                else:
                    try:
                        if args['type'] == 'both':
                            self.load_latent_source_dlatents()
                            self.load_latent_dest_dlatents()
                        elif args['type'] == 'keep_source':
                            self.load_latent_dest_dlatents()

                        elif args['type'] == 'use_dest':
                            self.latent_source = self.latent_dest
                            self.load_latent_dest_dlatents()
                        elif args['type'] == 'use_step':
                            source_step = int(args['currentStep'])
                            print("Use step {} as source".format(source_step))
                            self.latent_source = (self.linespaces[source_step] * self.latent_dest + (1-self.linespaces[source_step])*self.latent_source)
                            self.load_latent_dest_dlatents()
                        else:
                            raise Exception('Invalid generation type')

                        self.steps = int(args['steps'])
                        self.linespaces = np.linspace(0, 1, self.steps)

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
                try:
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
                except Exception as e:
                    self.loop.call_soon_threadsafe(
                        future.set_result, {'status' : 'Error', 'message': str(e)}
                    )


            elif request == "video":
                print("Download animation video. Shadows:  {}".format(args.get('shadows')))
                try:
                    writer = cv2.VideoWriter("output.avi",cv2.VideoWriter_fourcc(*"MJPG"), 30,(1024,1024))
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
            elif request == "encode":
                print("Encode uploaded image!")
                try:
                    if not self.perceptual_model:
                        raise Exception('No perceptual model for this snapshot.')
                    with self.app.app_context():
                        emit('nowEncoding',{'file':args["fileName"]},namespace='/',broadcast=True)
                    image = Image.open(
                            io.BytesIO(base64.b64decode(
                                args["data"][args["data"].find(",") + 1:]
                            )
                        )
                    )
                    f_src = tempfile.NamedTemporaryFile(suffix='.jpg').name
                    f_aligned = tempfile.NamedTemporaryFile(suffix='.png').name
                    f_gen = tempfile.NamedTemporaryFile(suffix='.png').name

                    cv2.imwrite(
                        f_src,
                        cv2.cvtColor(
                            np.array(image),
                            cv2.COLOR_BGR2RGB
                        )
                    )
                    print("Wrote image to {}".format(f_src))
                    landmarks_iter = enumerate(landmarks_detector.get_landmarks(f_src),start=1)
                    face_landmarks = next(landmarks_iter)[1]
                    print("Face Landmarks {}".format(face_landmarks))
                    image_align(f_src, f_aligned, face_landmarks, output_size=256)
                    print("Wrote face to {}".format(f_aligned))

                    iterations = 750
                    self.encode_old(iterations, f_aligned)

                    generated_images = self.encoder_generator.generate_images()
                    generated_dlatents = self.encoder_generator.get_dlatents()

                    gen_img = Image.fromarray(generated_images[0], 'RGB')
                    gen_img.save(f_gen, 'PNG')

                    print("Wrote generated image to {}".format(f_gen))
                    source_step = int(args['currentStep'])
                    print("Use step {} as source".format(source_step))
                    self.latent_source = (self.linespaces[source_step] * self.latent_dest + (1-self.linespaces[source_step])*self.latent_source)

                    self.latent_dest = generated_dlatents
                    print("Set latent dest to {}".format(self.latent_dest.shape))
                    self.linespaces = np.linspace(0, 1, self.steps)
                    self.linespace_i = 0;

                    with self.app.app_context():
                        emit('nowEncoding',{'file': None},namespace='/',broadcast=True)
                    
                    self.loop.call_soon_threadsafe(
                        future.set_result, "OK"
                    )
                    
                    future = self.loop.create_future()
                    self.queue.put((future, "publish", None))

                except Exception as e:
                    print("EXCEPTION {}".format(e))
                    with self.app.app_context():
                        emit('nowEncoding',{'file': None},namespace='/',broadcast=True)
                    self.loop.call_soon_threadsafe(
                        future.set_result, str(e)
                    )

    def encode(self, iterations, image):
        self.perceptual_model.set_reference_images([image])
        op = self.perceptual_model.optimize(self.encoder_generator.dlatent_variable, iterations=iterations, learning_rate=1)
        pbar = tqdm(op, leave=False, total=iterations)
        for loss in pbar:
            pbar.set_description('Loss: %.2f' % loss)

    def encode_old(self, iterations, image):
        im = Image.open(image)
        self.perceptual_model.set_reference_images_from_image(np.array([np.array(im)]))
        op = self.perceptual_model.optimize(self.encoder_generator.dlatent_variable, iterations=iterations, learning_rate=1)
        best_loss = None
        best_dlatent = None
        with tqdm(total=iterations) as pbar:
            for iteration, loss in enumerate(op):
                pbar.set_description('Loss: %.2f' % loss)
                if best_loss is None or loss < best_loss:
                    best_loss = loss
                    best_dlatent = self.encoder_generator.get_dlatents()
                self.encoder_generator.stochastic_clip_dlatents()
                pbar.update()
        print(f"final loss {loss}")
        self.encoder_generator.set_dlatents(best_dlatent)


    def get_buf(self, shadows):
            # Generate image.
            self.latents = (self.linespaces[self.linespace_i] * self.latent_dest + (1-self.linespaces[self.linespace_i])*self.latent_source)

            #images = self.Gs.run(self.latents, None, truncation_psi=0.7, randomize_noise=False, output_transform=self.fmt)
            images = self.encoder_generator.generate_images(self.latents)
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
app = Flask(__name__)
Compress(app)

#args.snapshot = "007743"
args.snapshot = "ffhq"

if not args.dummy:
    gan = Gan(q, app, loop, args)
    gan.daemon = True
else:
    gan = DummyGan(q,loop,args)

CORS(app)
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

@app.route('/publish', methods=['POST'])
def publish():
    future = loop.create_future()
    q.put((future, "publish", request.args))
    data = loop.run_until_complete(future)
    return jsonify(result=data)

@app.route('/shuffle',  methods = ['POST'])
def shuffle():
    future = loop.create_future()
    params = request.get_json()
    print(params)
    q.put((future, "shuffle", params))
    try:
        if params['snapshot'] == args.snapshot:
            data = loop.run_until_complete(future)
            return jsonify(result=data)
        else:
            print('Reloading GAN for new snapshot')
            global gan
            gan.join()
            args.snapshot = params['snapshot']
            args.steps = params['steps']
            gan = Gan(q, app, loop, args)
            gan.daemon = True
            gan.start()
            return jsonify(result="OK")
    except Exception as e:
        return jsonify(result="BUSY")


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
        gan = Gan(q, app, loop, args)
        gan.start()
        return jsonify(result=data)
    else:
        return jsonify(result=data)

@app.route('/encode',  methods = ['POST'])
def encode():
    try:
        future = loop.create_future()
        params = request.get_json()
        q.put((future, "encode", params))
        data = loop.run_until_complete(future)
        if data == 'OK':
            return jsonify(result=data)
        else:
            return jsonify(result=data)
    except Exception as e:
        return jsonify(result="BUSY")

@app.route('/', defaults={'path': ''})
@app.route('/<path:path>')
def catch_all(path):
    return render_template('index.html')

try:
    if __name__ == '__main__':

            #print("Generating samples")
            #for t in np.arange(0, 300, 0.000001):
            #	s.gen(t)
      app.run (host = "0.0.0.0", port = 8541, use_reloader=False)

except KeyboardInterrupt:
    print("Shutting down")
    gan.join()


