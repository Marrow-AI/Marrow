#!/usr/bin/env python
# -*- coding: utf-8
# Create a fake video that can test synchronization features

import sys, os, time, re
sys.path.append('./stylegan')

import cv2
import numpy as np
import pickle
import PIL.Image
import dnnlib
import dnnlib.tflib as tflib

#sys.path.append('/opt/anaconda1anaconda2anaconda3/share/gir-1.0')

import gi
gi.require_version('GIRepository', '2.0')
gi.require_version('Gst', '1.0')
from gi.repository import GObject, GIRepository, Gst ,GstRtspServer

from threading import Thread
import queue
import time

class Gan(Thread):
    def __init__(self, queue, args):
        self.queue = queue

        Thread.__init__(self)

    def run(self):
        # Setup
        self.snapshot = "007743"
        self.steps = 144;
        self.shadows = 0;

        tflib.init_tf()
        self.setup_pipeline()

        self.rnd = np.random.RandomState()
        self.fmt = dict(func=tflib.convert_images_to_uint8, nchw_to_nhwc=True)

        print("Loading snapshot {}".format(self.snapshot))
        url = os.path.abspath("snapshots/network-snapshot-{}.pkl".format(self.snapshot))
        with open(url, 'rb') as f:
            self._G, self._D, self.Gs = pickle.load(f)
            #self.generator = Generator(self.Gs, batch_size=1, randomize_noise=False)

        self.load_latent_source()
        self.load_latent_dest()

        self.linespaces = np.linspace(0, 1, self.steps)
        self.linespace_i = 0

        while True:
            image = self.get_buf()
            self.push_frame(image)
            time.sleep(1./12)
            self.linespace_i += 1
            if (self.linespace_i == self.steps):
                self.linespace_i = 0

    def setup_pipeline(self):
        GObject.threads_init()
        Gst.init(None)

        self.src_v = Gst.ElementFactory.make("appsrc", "vidsrc")
        vcvt = Gst.ElementFactory.make("videoconvert", "vidcvt")

        ndisink = Gst.ElementFactory.make("ndisink", "video_sink")

        ndisink.set_property("name", "marrow-spade")

        self.pipeline = Gst.Pipeline()
        self.pipeline.add(self.src_v)
        self.pipeline.add(vcvt)
        self.pipeline.add(ndisink)

        caps = Gst.Caps.from_string("video/x-raw,format=(string)I420,width=512,height=512,framerate=12/1")
        self.src_v.set_property("caps", caps)
        self.src_v.set_property("format", Gst.Format.TIME)

        self.src_v.link(vcvt)
        vcvt.link(ndisink)

        self.pipeline.set_state(Gst.State.PLAYING)

    def push_frame(self, image):
        data = cv2.cvtColor(image, cv2.COLOR_RGB2YUV)
        #print(data.shape)
        y = data[...,0]
        u = data[...,1]
        v = data[...,2]
        u2 = cv2.resize(u, (0,0), fx=0.5, fy=0.5, interpolation=cv2.INTER_AREA)
        v2 = cv2.resize(v, (0,0), fx=0.5, fy=0.5, interpolation=cv2.INTER_AREA)
        data = y.tostring() + u2.tostring() + v2.tostring()
        buf = Gst.Buffer.new_allocate(None, len(data), None)
        assert buf is not None
        buf.fill(0, data)
        buf.pts = buf.dts = Gst.CLOCK_TIME_NONE
        print("Push")
        self.src_v.emit("push-buffer", buf)

    def load_latent_source(self):
        self.latent_source = self.rnd.randn(512)[None, :]
        print("Loaded latent source {}".format(self.latent_source.shape))

    def load_latent_dest(self):
        self.latent_dest = self.rnd.randn(512)[None, :]
        print("Loaded latent dest {}".format(self.latent_dest.shape))

    def get_buf(self):
            self.latents = (self.linespaces[self.linespace_i] * self.latent_dest + (1-self.linespaces[self.linespace_i])*self.latent_source)
            images = self.Gs.run(self.latents, None, truncation_psi=0.7, randomize_noise=False, output_transform=self.fmt)
            image = images[0]
            if self.shadows:
                gray = cv2.cvtColor(image, cv2.COLOR_RGB2GRAY)
                ret,black_white = cv2.threshold(gray,50,255,cv2.THRESH_BINARY_INV)
                data = cv2.cvtColor(black_white, cv2.COLOR_GRAY2BGR)
            else:
                #data = cv2.cvtColor(image, cv2.COLOR_RGB2YUV)
                data = image
            assert data is not None
            return data

q = queue.Queue()

if __name__ == '__main__':
    gan = Gan(q, None)
    gan.start()

