#!/usr/bin/env python

# coding: utf-8
#
# Author: Kazuto Nakashima
# URL:    https://kazuto1011.github.io
# Date:   07 January 2019

from __future__ import absolute_import, division, print_function
import sys

sys.path.append('./deeplab-pytorch')

import click
import cv2
import matplotlib
import matplotlib.cm as cm
import matplotlib.pyplot as plt
import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F
import yaml
from addict import Dict

from libs.models import *
from libs.utils import DenseCRF

from PIL import Image

import gi
gi.require_version('GIRepository', '2.0')
gi.require_version('Gst', '1.0')
from gi.repository import GObject, GIRepository, Gst
from threading import Thread
import queue
from collections import deque

import requests
from requests.auth import HTTPDigestAuth

import json

from pythonosc import dispatcher
from pythonosc import osc_server

THETA_ID = 'THETAYN14100015'
THETA_PASSWORD = '14100015'  # default password. may have been changed
THETA_URL = 'http://192.168.1.29/osc/'

LABELMAP = \
    {0: 'unlabeled',
     1: 'person',
     2: 'bicycle',
     3: 'car',
     4: 'motorcycle',
     5: 'airplane',
     6: 'bus',
     7: 'train',
     8: 'truck',
     9: 'boat',
     10: 'traffic light',
     11: 'fire hydrant',
     12: 'street sign',
     13: 'stop sign',
     14: 'parking meter',
     15: 'bench',
     16: 'bird',
     17: 'cat',
     18: 'dog',
     19: 'horse',
     20: 'sheep',
     21: 'cow',
     22: 'elephant',
     23: 'bear',
     24: 'zebra',
     25: 'giraffe',
     26: 'hat',
     27: 'backpack',
     28: 'umbrella',
     29: 'shoe',
     30: 'eye glasses',
     31: 'handbag',
     32: 'tie',
     33: 'suitcase',
     34: 'frisbee',
     35: 'skis',
     36: 'snowboard',
     37: 'sports ball',
     38: 'kite',
     39: 'baseball bat',
     40: 'baseball glove',
     41: 'skateboard',
     42: 'surfboard',
     43: 'tennis racket',
     44: 'bottle',
     45: 'plate',
     46: 'wine glass',
     47: 'cup',
     48: 'fork',
     49: 'knife',
     50: 'spoon',
     51: 'bowl',
     52: 'banana',
     53: 'apple',
     54: 'sandwich',
     55: 'orange',
     56: 'broccoli',
     57: 'carrot',
     58: 'hot dog',
     59: 'pizza',
     60: 'donut',
     61: 'cake',
     62: 'chair',
     63: 'couch',
     64: 'potted plant',
     65: 'bed',
     66: 'mirror',
     67: 'dining table',
     68: 'window',
     69: 'desk',
     70: 'toilet',
     71: 'door',
     72: 'tv',
     73: 'laptop',
     74: 'mouse',
     75: 'remote',
     76: 'keyboard',
     77: 'cell phone',
     78: 'microwave',
     79: 'oven',
     80: 'toaster',
     81: 'sink',
     82: 'refrigerator',
     83: 'blender',
     84: 'book',
     85: 'clock',
     86: 'vase',
     87: 'scissors',
     88: 'teddy bear',
     89: 'hair drier',
     90: 'toothbrush',
     91: 'hair brush',  # last class of thing
     92: 'banner',  # beginning of stuff
     93: 'blanket',
     94: 'branch',
     95: 'bridge',
     96: 'building-other',
     97: 'bush',
     98: 'cabinet',
     99: 'cage',
     100: 'cardboard',
     101: 'carpet',
     102: 'ceiling-other',
     103: 'ceiling-tile',
     104: 'cloth',
     105: 'clothes',
     106: 'clouds',
     107: 'counter',
     108: 'cupboard',
     109: 'curtain',
     110: 'desk-stuff',
     111: 'dirt',
     112: 'door-stuff',
     113: 'fence',
     114: 'floor-marble',
     115: 'floor-other',
     116: 'floor-stone',
     117: 'floor-tile',
     118: 'floor-wood',
     119: 'flower',
     120: 'fog',
     121: 'food-other',
     122: 'fruit',
     123: 'furniture-other',
     124: 'grass',
     125: 'gravel',
     126: 'ground-other',
     127: 'hill',
     128: 'house',
     129: 'leaves',
     130: 'light',
     131: 'mat',
     132: 'metal',
     133: 'mirror-stuff',
     134: 'moss',
     135: 'mountain',
     136: 'mud',
     137: 'napkin',
     138: 'net',
     139: 'paper',
     140: 'pavement',
     141: 'pillow',
     142: 'plant-other',
     143: 'plastic',
     144: 'platform',
     145: 'playingfield',
     146: 'railing',
     147: 'railroad',
     148: 'river',
     149: 'road',
     150: 'rock',
     151: 'roof',
     152: 'rug',
     153: 'salad',
     154: 'sand',
     155: 'sea',
     156: 'shelf',
     157: 'sky-other',
     158: 'skyscraper',
     159: 'snow',
     160: 'solid-other',
     161: 'stairs',
     162: 'stone',
     163: 'straw',
     164: 'structural-other',
     165: 'table',
     166: 'tent',
     167: 'textile-other',
     168: 'towel',
     169: 'tree',
     170: 'vegetable',
     171: 'wall-brick',
     172: 'wall-concrete',
     173: 'wall-other',
     174: 'wall-panel',
     175: 'wall-stone',
     176: 'wall-tile',
     177: 'wall-wood',
     178: 'water-other',
     179: 'waterdrops',
     180: 'window-blind',
     181: 'window-other',
     182: 'wood'}


ID_TO_LABEL = {id-1 : label for (id,label) in LABELMAP.items()}
LABEL_TO_ID = {label: id for (id,label) in ID_TO_LABEL.items()}

def get_device(cuda):
    cuda = cuda and torch.cuda.is_available()
    device = torch.device("cuda" if cuda else "cpu")
    if cuda:
        current_device = torch.cuda.current_device()
        print("Device:", torch.cuda.get_device_name(current_device))
    else:
        print("Device: CPU")
    return device


def get_classtable(CONFIG):
    with open(CONFIG.DATASET.LABELS) as f:
        classes = {}
        for label in f:
            label = label.rstrip().split("\t")
            classes[int(label[0])] = label[1].split(",")[0]
    return classes


def setup_postprocessor(CONFIG):
    # CRF post-processor
    postprocessor = DenseCRF(
        iter_max=CONFIG.CRF.ITER_MAX,
        pos_xy_std=CONFIG.CRF.POS_XY_STD,
        pos_w=CONFIG.CRF.POS_W,
        bi_xy_std=CONFIG.CRF.BI_XY_STD,
        bi_rgb_std=CONFIG.CRF.BI_RGB_STD,
        bi_w=CONFIG.CRF.BI_W,
    )
    return postprocessor


def preprocessing(image, device, CONFIG):
    # Resize
    scale = CONFIG.IMAGE.SIZE.TEST / max(image.shape[:2])
    image = cv2.resize(image, dsize=None, fx=scale, fy=scale)
    raw_image = image.astype(np.uint8)

    # Subtract mean values
    image = image.astype(np.float32)
    image -= np.array(
        [
            float(CONFIG.IMAGE.MEAN.B),
            float(CONFIG.IMAGE.MEAN.G),
            float(CONFIG.IMAGE.MEAN.R),
        ]
    )

    # Convert to torch.Tensor and add "batch" axis
    image = torch.from_numpy(image.transpose(2, 0, 1)).float().unsqueeze(0)
    image = image.to(device)

    return image, raw_image


def inference(model, image, raw_image=None, postprocessor=None):
    _, _, H, W = image.shape

    # Image -> Probability map
    logits = model(image)
    logits = F.interpolate(logits, size=(H, W), mode="bilinear", align_corners=False)
    probs = F.softmax(logits, dim=1)[0]
    probs = probs.cpu().numpy()

    # Refine the prob map with CRF
    if postprocessor and raw_image is not None:
        probs = postprocessor(raw_image, probs)

    labelmap = np.argmax(probs, axis=0)

    return labelmap


class NDIStreamer(Thread):
    def __init__(self, width, height):
        Thread.__init__(self)
        self.setup_pipeline(width, height)

    def setup_pipeline(self, width, height):
        Gst.init(None)

        self.src_v = Gst.ElementFactory.make("appsrc", "vidsrc")
        vcvt = Gst.ElementFactory.make("videoconvert", "vidcvt")

        ndisink = Gst.ElementFactory.make("ndisink", "video_sink")
        ndisink.set_property("name", "marrow-spade")

        self.pipeline = Gst.Pipeline()
        self.pipeline.add(self.src_v)
        self.pipeline.add(vcvt)
        self.pipeline.add(ndisink)

        caps = Gst.Caps.from_string("video/x-raw,format=(string)I420,width={},height={},framerate=12/1".format(width, height))
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
        #print("Push")
        self.src_v.emit("push-buffer", buf)


class Gan(NDIStreamer):
    def __init__(self, queue, osc_queue, deeplab_opt, spade_opt):
        super().__init__(1280,256)
        self.queue = queue
        self.osc_queue = osc_queue
        self.deeplab_opt = deeplab_opt
        self.spade_opt = spade_opt
        self.maps = []
        self.gaugan_masks = []
        self.deeplab_masks = []
        self.show_raw = False;
        self.map_deeplab = False;
        self.current_state = 'clear'

    def run(self):
        # Setup

        CONFIG = self.deeplab_opt['CONFIG']
        self.CONFIG = CONFIG
        model_path = self.deeplab_opt['model_path']
        cuda = self.deeplab_opt['cuda']
        crf = self.deeplab_opt['crf']
        camera_id = self.deeplab_opt['camera_id']

        device = get_device(cuda)
        torch.set_grad_enabled(False)
        torch.backends.cudnn.benchmark = True

        classes = get_classtable(CONFIG)
        postprocessor = setup_postprocessor(CONFIG) if crf else None

        model = eval(CONFIG.MODEL.NAME)(n_classes=CONFIG.DATASET.N_CLASSES)
        state_dict = torch.load(model_path, map_location=lambda storage, loc: storage)
        model.load_state_dict(state_dict)
        model.eval()
        model.to(device)
        print("Model:", CONFIG.MODEL.NAME)

        #cv2.namedWindow(window_name, cv2.WINDOW_NORMAL)


        #np.set_printoptions(threshold=sys.maxsize)

        while True:
            while not self.osc_queue.empty():
                item = self.osc_queue.get()
                self.process_queue(item)

            if len(self.queue) > 0:
                frame = self.queue.pop()
                #print("Original Image shape {}".format(frame.shape))
                image, raw_image = preprocessing(frame, device, CONFIG)
                raw_image  = cv2.cvtColor(raw_image,cv2.COLOR_BGR2RGB)
                #print("Image shape {}".format(raw_image.shape))
                labelmap = inference(model, image, raw_image, postprocessor)

                uniques = np.unique(labelmap)
                print([ID_TO_LABEL[unique] for unique in uniques])

                final = colormap

                #self.push_frame(raw_image_resized)
                #raw_rgb = cv2.cvtColor(raw_image, cv2.COLOR_BGR2RGB)

                #print("Final shape: {}".format(final.shape))

                #print("Gans shape {}, colormap shape {}, Final shape {}".format(generated_np.shape, color_resized.shape, final.shape))

                self.push_frame(final)

    def mouse_event(event, x, y, flags, labelmap):
        # Show a class name of a mouse-overed pixel
        label = labelmap[y, x]
        name = classes[label]
        print(name)


    def expand_mask(self, input, iters):
	    """
	    Expands the True area in an array 'input'.

	    Expansion occurs in the horizontal and vertical directions by one
	    cell, and is repeated 'iters' times.
	    """
	    yLen,xLen = input.shape
	    output = input.copy()
	    for iter in range(iters):
		    for y in range(yLen):
				    for x in range(xLen):
					    if (y > 0        and input[y-1,x]) or \
					       (y < yLen - 1 and input[y+1,x]) or \
					       (x > 0        and input[y,x-1]) or \
					       (x < xLen - 1 and input[y,x+1]): output[y,x] = True
		    input = output.copy()
	    return output

    def test_bowl(self,labelmap):
        if np.any(labelmap == LABEL_TO_ID['bowl']):
            print("FOUND BOWL!!")
            self.osc_queue.put({"command": "load-state", "args": "found-bowl"})


    def process_queue(self,item):
        print("Process command {}".format(item))
        if item['command'] == 'load-state':
            self.load_state(item['args'])

    def colorize(self,labelmap):
        #print(labelmap.shape)
        # Assign a unique color to each label
        labelmap = labelmap.astype(np.float32) / self.CONFIG.DATASET.N_CLASSES
        colormap = cm.jet_r(labelmap)[..., :-1] * 255.0
        return np.uint8(colormap)

    def get_buf(self):
        pass

q = deque()
osc_queue = queue.Queue()

class OSCServer(Thread):
    def __init__(self, osc_queue):
        Thread.__init__(self)
        self.osc_queue = osc_queue

    def run(self):
        self.gan_dispatcher = dispatcher.Dispatcher()
        self.gan_dispatcher.set_default_handler(self.osc_handler)
        self.server = osc_server.ThreadingOSCUDPServer(("0.0.0.0", 3900), self.gan_dispatcher)
        print("Serving OSC on {}".format(self.server.server_address))
        self.server.serve_forever()

    def osc_handler(self, addr,args):
        print("OSC command! {}".format(addr))
        self.osc_queue.put({"command": addr[1:], "args": args})




@click.group()
@click.pass_context
def main(ctx):
    """
    Demo with a trained model
    """

    print("Mode:", ctx.invoked_subcommand)


@main.command(name='live', context_settings=dict(
    ignore_unknown_options=True,
    allow_extra_args=True,
))
@click.option(
    "-c",
    "--config-path",
    type=click.File(),
    required=True,
    help="Dataset configuration file in YAML",
)
@click.option(
    "-m",
    "--model-path",
    type=click.Path(exists=True),
    required=True,
    help="PyTorch model to be loaded",
)
@click.option(
    "--cuda/--cpu", default=True, help="Enable CUDA if available [default: --cuda]"
)
@click.option("--crf", is_flag=True, show_default=True, help="CRF post-processing")
@click.option("--camera-id", type=int, default=0, show_default=True, help="Device ID")
def live(config_path, model_path, cuda, crf, camera_id):
    """
    Inference from camera stream
    """
    GObject.threads_init()

    print("Deeplab config at {}".format(config_path))
    CONFIG = Dict(yaml.load(config_path))

    deeplab_opt = {
        'CONFIG': CONFIG,
        'model_path' : model_path,
        'cuda' : cuda,
        'crf' : crf,
        'camera_id': camera_id
    }
    spade_opt = {}

    gan = Gan(q, osc_queue, deeplab_opt, spade_opt)
    gan.start()

    osc_server = OSCServer(osc_queue)
    osc_server.start()

    url = THETA_URL + 'commands/execute'
    payload = {"name": "camera.getLivePreview"}
    buffer = bytes()

    out = cv2.VideoWriter('capture.avi',cv2.VideoWriter_fourcc('M','J','P','G'), 24, (1024,512))

    try:
        with requests.post(url,
            json=payload,
            auth=(HTTPDigestAuth(THETA_ID, THETA_PASSWORD)),
            stream=True) as r:
            for chunk in r.iter_content(chunk_size=1024):
                buffer += chunk
                a = buffer.find(b'\xff\xd8')
                b = buffer.find(b'\xff\xd9')
                if a != -1 and b != -1:
                    jpg = buffer[a:b+2]
                    buffer = buffer[b+2:]
                    if len(q) > 1:
                        continue
                    else:
                        frame = cv2.imdecode(np.fromstring(jpg, dtype=np.uint8), cv2.IMREAD_COLOR)
                        image = frame.astype(np.uint8)
                        print("Frame {}".format(image.shape))
                        out.write(image)
                        cv2.imshow('frame',image)
                        #q.append(frame)
    finally:
        print("Record done")
        out.release()


if __name__ == "__main__":
    main()
