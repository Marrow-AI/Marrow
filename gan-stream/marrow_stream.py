#!/usr/bin/env python
# -*- coding: utf-8
# Create a fake video that can test synchronization features

import sys, os, time, re
import cv2
import numpy as np
import pickle
import PIL.Image
import dnnlib
import dnnlib.tflib as tflib

import gi
gi.require_version('Gst', '1.0')
from gi.repository import GObject, Gst

if __name__ == '__main__':
	GObject.threads_init()
	Gst.init(None)

	audio = False

	src_v = Gst.ElementFactory.make("appsrc", "vidsrc")
	vcvt = Gst.ElementFactory.make("videoconvert", "vidcvt")
	venc = Gst.ElementFactory.make("vp8enc", "videnc")
	src_s = Gst.ElementFactory.make("appsrc", "subsrc")
	vpay = Gst.ElementFactory.make("rtpvp8pay", "vpay")
	vpay = Gst.ElementFactory.make("rtpvp8pay", "vpay")

	if audio:
		src_a = Gst.ElementFactory.make("appsrc", "audsrc")
		acvt = Gst.ElementFactory.make("audioconvert", "audcvt")
		aenc = Gst.ElementFactory.make("wavenc", "audenc")


	webmmux = Gst.ElementFactory.make("webmmux", "mux")
	#filesink = Gst.ElementFactory.make("filesink", "sink")
	#filesink.set_property("location", "test.webm")

	udpsink = Gst.ElementFactory.make("udpsink", "sink")
	udpsink.set_property("host", "incarnation.hitodama.online")
	udpsink.set_property("port", 8004)
	udpsink.set_property("sync", False)


	pipeline = Gst.Pipeline()
	pipeline.add(src_v)
	pipeline.add(vcvt)
	pipeline.add(venc)
	if audio:
		pipeline.add(src_a)
		pipeline.add(acvt)
		pipeline.add(aenc)
	#pipeline.add(src_s)
	pipeline.add(vpay)
	pipeline.add(udpsink)

	caps = Gst.Caps.from_string("video/x-raw,format=(string)I420,width=512,height=512,framerate=30/1")
	src_v.set_property("caps", caps)
	src_v.set_property("format", Gst.Format.TIME)

	if audio:
		caps_str = "audio/x-raw,rate=48000,channels=1"
		caps_str += ",format=S16LE"
		caps_str += ",layout=interleaved"
		#caps_str += ",channels=1"
		caps = Gst.Caps.from_string(caps_str)
		src_a.set_property("caps", caps)
		src_a.set_property("format", Gst.Format.TIME)

	caps = Gst.Caps.from_string("text/x-raw,format=(string)utf8")
	src_s.set_property("caps", caps)
	src_s.set_property("format", Gst.Format.TIME)

	src_v.link(vcvt)
	vcvt.link(venc)
	venc.link(vpay)

	if audio:
		res = src_a.link(acvt)
		assert res
		res = acvt.link(aenc)
		assert res
		res = aenc.link(vpay)
		print(res)
		assert res

	#src_s.link(webmmux)

	vpay.link(udpsink)


	pipeline.set_state(Gst.State.PLAYING)

	class SampleGenerator:

		def __init__(self):
			self._last_t_v = -31337
			self._last_t_a = -31337
			self._last_t_s = -31337
			tflib.init_tf()

			# Load pre-trained network.
			url = os.path.abspath("results/00021-sgan-dense512-8gpu/network-snapshot-009247.pkl")
			with open(url, 'rb') as f:
			    self._G, self._D, self.Gs = pickle.load(f)
			self.Gs.print_layers()
			self.rnd = np.random.RandomState()


		def gen(self, t):
		  if t - self._last_t_v >= 1.0/30:
			    latents = self.rnd.randn(1, self.Gs.input_shape[1])
			# Generate image.
			    fmt = dict(func=tflib.convert_images_to_uint8, nchw_to_nhwc=True)
			    images = self.Gs.run(latents, None, truncation_psi=0.7, randomize_noise=True, output_transform=fmt)
			    data = cv2.cvtColor(images[0], cv2.COLOR_RGB2YUV)
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
			    buf.pts = buf.dts = int(t * 1e9)
			    src_v.emit("push-buffer", buf)
			    self._last_t_v = t
		"""
		    if t - self._last_t_v >= 1.0/30:
			    data = np.zeros((240, 320, 3), dtype=np.uint8)
			    data = cv2.cvtColor(data, cv2.COLOR_RGB2YUV)

			    fontFace = cv2.FONT_HERSHEY_SIMPLEX
			    fontScale = 1
			    thickness = 1
			    color = (0, 255, 255)
			    text = "%6f" % t
			    oh = 0#v[0][1]*2
			    v = cv2.getTextSize(text, fontFace, fontScale, thickness)
			    cl = int(round(160 - v[0][0]/2))
			    cb = int(round(120 + oh - v[1] - v[0][1]/2))
			    cv2.putText(data, text, (cl, cb), fontFace, fontScale, color, thickness)

			    y = data[...,0]
			    u = data[...,1]
			    v = data[...,2]
			    u2 = cv2.resize(u, (0,0), fx=0.5, fy=0.5, interpolation=cv2.INTER_AREA)
			    v2 = cv2.resize(v, (0,0), fx=0.5, fy=0.5, interpolation=cv2.INTER_AREA)
			    data = y.tostring() + u2.tostring() + v2.tostring()
			    buf = Gst.Buffer.new_allocate(None, len(data), None)
			    assert buf is not None
			    buf.fill(0, data)
			    buf.pts = buf.dts = int(t * 1e9)
			    src_v.emit("push-buffer", buf)
			    self._last_t_v = t
		    """


	s = SampleGenerator()
	print("Generating samples")
	for t in np.arange(0, 300, 0.000001):
		s.gen(t)

	src_v.emit("end-of-stream")
	if audio:
		src_a.emit("end-of-stream")
	#src_s.emit("end-of-stream")

	bus = pipeline.get_bus()
	print("Polling")
	while True:
		msg = bus.poll(Gst.MessageType.ANY, Gst.CLOCK_TIME_NONE)
		t = msg.type
		if t == Gst.MessageType.EOS:
			print("EOS")
			break
			pipeline.set_state(Gst.State.NULL)
		elif t == Gst.MessageType.ERROR:
			err, debug = msg.parse_error()
			print("Error: %s" % err, debug)
			break
		elif t == Gst.MessageType.WARNING:
			err, debug = msg.parse_warning()
			print("Warning: %s" % err, debug)
		elif t == Gst.MessageType.STATE_CHANGED:
			pass
		elif t == Gst.MessageType.STREAM_STATUS:
			type_, owner = msg.parse_stream_status()
			print('Stream status changed to {} (owner={})'.format(type_.value_name, owner.name))
			pass
		else:
			print(t)
			print("Unknown message: %s" % msg)

	print("Bye")

