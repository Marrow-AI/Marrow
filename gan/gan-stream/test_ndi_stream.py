#!/usr/bin/env python
# -*- coding: utf-8
# Create a fake video that can test synchronization features

import sys, os, time, re
import cv2
import numpy as np
import pickle
import PIL.Image
import random

#sys.path.append('/opt/anaconda1anaconda2anaconda3/share/gir-1.0')

import gi
gi.require_version('Gst', '1.0')
from gi.repository import GObject, GIRepository, Gst 

print(GIRepository.Repository.get_search_path())

if __name__ == '__main__':
	GObject.threads_init()
	Gst.init(None)

	audio = False

	src_v = Gst.ElementFactory.make("appsrc", "vidsrc")
	vcvt = Gst.ElementFactory.make("videoconvert", "vidcvt")
	venc = Gst.ElementFactory.make("vp8enc", "videnc")
	src_s = Gst.ElementFactory.make("appsrc", "subsrc")
	#vpay = Gst.ElementFactory.make("rtpvp8pay", "vpay")

	if audio:
            src_a = Gst.ElementFactory.make("appsrc", "audsrc")
            acvt = Gst.ElementFactory.make("audioconvert", "audcvt")
            aenc = Gst.ElementFactory.make("wavenc", "audenc")


	#webmmux = Gst.ElementFactory.make("webmmux", "mux")
	#filesink = Gst.ElementFactory.make("filesink", "sink")
	#filesink.set_property("location", "test.webm")

	#udpsink = Gst.ElementFactory.make("udpsink", "sink")
	#udpsink.set_property("host", "incarnation.hitodama.online")
	#udpsink.set_property("port", 8004)
	#udpsink.set_property("sync", False)

	ndisink = Gst.ElementFactory.make("ndisink", "video_sink")
	ndisink.set_property("name", "marrow")
	#udpsink.set_property("sync", False)


	pipeline = Gst.Pipeline()
	pipeline.add(src_v)
	pipeline.add(vcvt)
	#pipeline.add(venc)
	if audio:
		pipeline.add(src_a)
		pipeline.add(acvt)
		pipeline.add(aenc)
	#pipeline.add(src_s)
	#pipeline.add(vpay)
	#pipeline.add(udpsink)

	pipeline.add(ndisink)

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
	vcvt.link(ndisink)
	#venc.link(vpay)

	#src_s.link(webmmux)

	#vpay.link(udpsink)


	pipeline.set_state(Gst.State.PLAYING)

	class SampleGenerator:

		def __init__(self):
			self._last_t_v = -31337
			self._last_t_a = -31337
			self._last_t_s = -31337


		def gen(self, t):
		  if t - self._last_t_v >= 1.0/30:
			    blank_image = np.zeros((512,512,3), np.uint8)
			    blank_image[:,0:random.randint(1,512)//2] = (255,0,0)
			    blank_image[:,random.randint(1,512)//2:512] = (0,255,0)
			    data = cv2.cvtColor(blank_image, cv2.COLOR_RGB2YUV)
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
			    print("Push")
			    src_v.emit("push-buffer", buf)
			    self._last_t_v = t

	s = SampleGenerator()
	print("Generating samples")
	for t in np.arange(0, 300, 0.000001):
            s.gen(t)

	src_v.emit("end-of-stream")
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

