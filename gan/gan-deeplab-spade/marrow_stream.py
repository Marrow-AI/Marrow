#!/usr/bin/env python

# coding: utf-8
#
# Author: Kazuto Nakashima
# URL:    https://kazuto1011.github.io
# Date:   07 January 2019

from __future__ import absolute_import, division, print_function
import sys

sys.path.append('./SPADE')
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

from options.test_options import TestOptions
from models.pix2pix_model import Pix2PixModel
from data.coco_dataset import CocoDataset
from PIL import Image
from util import util

import gi
gi.require_version('GIRepository', '2.0')
gi.require_version('Gst', '1.0')
from gi.repository import GObject, GIRepository, Gst
from threading import Thread
import queue
from collections import deque

import requests
from requests.auth import HTTPDigestAuth

from pythonosc import dispatcher
from pythonosc import osc_server
from pythonosc import udp_client

import json
import time
from scipy import ndimage

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
    def __init__(self, width, height, name):
        Thread.__init__(self)
        self.setup_pipeline(width, height, name)

    def setup_pipeline(self, width, height, name):
        Gst.init(None)

        self.src_v = Gst.ElementFactory.make("appsrc", "vidsrc")
        vcvt = Gst.ElementFactory.make("videoconvert", "vidcvt")

        ndisink = Gst.ElementFactory.make("ndisink", "video_sink")
        ndisink.set_property("name", name)

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

class Camera(NDIStreamer):
    def __init__(self, queue):
        super().__init__(1024,512, "marrow-theta")
        self.queue = queue

    def run(self):

        while True:
            if len(self.queue) > 0:
                frame = self.queue.pop()
                #print("Original Image shape {}".format(frame.shape))
                final  = cv2.cvtColor(frame,cv2.COLOR_BGR2RGB)
                self.push_frame(final)
                time.sleep(1./12)

class Gan(NDIStreamer):
    def __init__(self, queue, osc_queue, deeplab_opt, spade_opt):
        super().__init__(1280,256, "marrow-spade")
        self.queue = queue
        self.osc_queue = osc_queue
        self.deeplab_opt = deeplab_opt
        self.spade_opt = spade_opt
        self.maps = []
        self.gaugan_masks = []
        self.deeplab_masks = []
        self.show_raw = False
        self.map_deeplab = False
        self.autumn = False
        self.current_state = 'clear'
        self.show_gaugan = True
        self.show_labels = False
        self.send_bowl = False

        self.t2i_client = udp_client.SimpleUDPClient("192.168.1.22", 3838)


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

        # SPADE model
        spade_model = Pix2PixModel(self.spade_opt)
        spade_model.eval()
        spade_model.to(device)
        print("Spade!")
        print(spade_model)

        coco_dataset = CocoDataset()
        coco_dataset.initialize(self.spade_opt)
        print(coco_dataset)

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

                if self.current_state == "test-bowl":
                    self.test_bowl(labelmap)

                uniques = np.unique(labelmap)
                print([ID_TO_LABEL[unique] for unique in uniques])

                if self.send_bowl and LABEL_TO_ID['bowl'] in uniques:
                    box = self.get_bounding_box_of(LABEL_TO_ID['bowl'], labelmap)
                    (rmin, cmin, rmax, cmax) = box
                    coords = (int((cmin + cmax) / 2), int((rmin + rmax) / 2))
                    print("Bowl coords {} out of {} ".format(coords,labelmap.shape))
                    self.t2i_client.send_message("/deeplab/bowl", [coords[0], coords[1]])


                if not self.map_deeplab:
                    colormap = self.colorize(labelmap)
                    for masking in self.deeplab_masks:
                        mask = np.isin(labelmap, masking['items'], invert=masking['invert'])
                        colormap[mask, :] = [0, 0, 0];

                for mapping in self.maps:
                    mask = np.isin(labelmap, mapping['from'], invert=mapping['invert'])
                    if mapping['expand'] > 0:
                        mask = self.expand_mask(mask, mapping['expand'])
                    labelmap[mask] = mapping['to']

                if self.map_deeplab:
                    colormap = self.colorize(labelmap)
                    for masking in self.deeplab_masks:
                        mask = np.isin(labelmap, masking['items'], invert=masking['invert'])
                        colormap[mask, :] = [0, 0, 0];
                    if self.show_raw:
                         for masking in self.deeplab_masks:
                             mask = np.isin(labelmap, masking['items'], invert=masking['invert'])
                             raw_image[mask, :] = [0, 0, 0];

                if self.show_labels:
                    for unique in uniques:
                        box = self.get_bounding_box_of(unique, labelmap)
                        self.put_text_in_center(colormap,box,ID_TO_LABEL[unique])


                #color_resized = cv2.cvtColor(np.array(Image.fromarray(colormap).resize((256,256), Image.NEAREST)),cv2.COLOR_BGR2RGB)


                if self.show_gaugan:
                    uniques = np.unique(labelmap)
                    instance_counter = 0
                    instancemap = np.zeros(labelmap.shape)

                    for label_id in uniques:
                        mask = (labelmap == label_id)
                        instancemap[mask] = instance_counter
                        instance_counter += 1

                    instanceimg = Image.fromarray(np.uint8(instancemap),'L')

                    labelimg = Image.fromarray(np.uint8(labelmap), 'L')
                    label_resized = np.array(labelimg.resize((256,256), Image.NEAREST))

                    item = coco_dataset.get_item_from_images(labelimg, instanceimg)

                    generated = spade_model(item, mode='inference')
                    generated_np = util.tensor2im(generated[0])

                    for masking in self.gaugan_masks:
                        mask = np.isin(label_resized, masking['items'], invert=masking['invert'])
                        generated_np[mask, :] = [0, 0, 0];
                    print("SPADE Shape {}".format(generated_np.shape))
                else:
                    generated_np = np.uint8(np.zeros((256,256,3)))

                final = np.concatenate((generated_np, colormap, raw_image), axis=1)
                #final = np.concatenate((generated_np, colormap), axis=1)

                self.push_frame(final)

    def put_text_in_center(self, data, box, text):
        fontFace = cv2.FONT_HERSHEY_SIMPLEX
        fontScale = 0.5
        thickness = 1 
        color = (255, 255, 255)
        (rmin, cmin, rmax, cmax) = box
        cv2.putText(data, text, (int((cmin + cmax) / 2), int((rmin + rmax) / 2)), fontFace, fontScale, color, thickness)   


    def get_bounding_box_of(self, label_id, labelmap):
        print("Bounding box of {}:".format(ID_TO_LABEL[label_id]))
        img = labelmap == label_id
        rows = np.any(img, axis=1)
        cols = np.any(img, axis=0)
        rmin, rmax = np.where(rows)[0][[0, -1]]
        cmin, cmax = np.where(cols)[0][[0, -1]]

        result = (rmin, cmin, rmax, cmax)
        print(result)

        return result

        """
        x_components, _ = ndimage.measurements.label(image, np.ones((3, 3)))
        bboxes = ndimage.measurements.find_objects(x_components)
        print("Bounding boxes:")
        for bbox in bboxes:
            print(bbox)
        """

    def mouse_event(self, event, x, y, flags, labelmap):
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


    def load_state(self, name):
        self.current_state = name
        if name == 'clear':
            self.maps.clear()
            self.gaugan_masks.clear()
            self.deeplab_masks.clear()
            self.autumn = False
            self.send_bowl = False
        else:
            try:
                with open('states/{}.json'.format(name)) as json_file:
                    data = json.load(json_file)
                    print(data);
                    self.maps = list(map(lambda m: {
                        'from': [LABEL_TO_ID[id] for id in m['from']],
                        'to': LABEL_TO_ID[m['to']],
                        'invert': m['invert'],
                        'expand': m['expand'] if 'expand' in m else 0
                    }, data['map']))

                    self.gaugan_masks = list(map(lambda m: {
                        'items': [LABEL_TO_ID[id] for id in m['items']],
                        'invert': m['invert']
                    }, data['gaugan']['mask']))

                    self.show_raw = data['showRaw']
                    self.map_deeplab = data['mapDeeplab']
                    if 'autumn' in data:
                        self.autumn = data['autumn']
                    else:
                        self.autumn = False

                    if 'sendBowl' in data:
                        self.send_bowl = data['sendBowl']
                    else:
                        self.send_bowl = False

                    print("Maps: {} GauGAN Masks: {}, Show raw: {} Map deeplab: {}".format(self.maps,self.gaugan_masks, self.show_raw, self.map_deeplab))

                    self.deeplab_masks = list(map(lambda m: {
                        'items': [LABEL_TO_ID[id] for id in m['items']],
                        'invert': m['invert']
                    }, data['deeplab']['mask']))

                    print("Deeplab Masks: {}".format(self.deeplab_masks))



            except Exception as e:
                print("Error loading state! {}".format(e))


    def process_queue(self,item):
        print("Process command {}".format(item))
        if item['command'] == 'load-state':
            self.load_state(item['args'])

    def colorize(self,labelmap):
        #print(labelmap.shape)
        # Assign a unique color to each label
        labelmap = labelmap.astype(np.float32) / self.CONFIG.DATASET.N_CLASSES
        if self.autumn:
            colormap = cm.autumn(labelmap)[..., :-1] * 255.0
        else:
            colormap = cm.jet_r(labelmap)[..., :-1] * 255.0
            
        return np.uint8(colormap)

    def get_buf(self):
        pass

q = deque()
cam_q = deque()

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
@click.option(
    "-i",
    "--image-path",
    type=click.Path(exists=True),
    required=False,
    help="Image to be processed",
)
def live(config_path, model_path, cuda, crf, camera_id, image_path):
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
    spade_opt = TestOptions().parse()
    print(spade_opt)
    spade_opt.use_vae = False

    gan = Gan(q, osc_queue, deeplab_opt, spade_opt)
    gan.start()

    cam = Camera(cam_q)
    cam.start()

    osc_server = OSCServer(osc_queue)
    osc_server.start()

    url = THETA_URL + 'commands/execute'
    payload = {"name": "camera.getLivePreview"}
    buffer = bytes()


    if image_path:
        print("Reading from image")
        frame = cv2.imread(image_path, cv2.IMREAD_COLOR)
        while True:
            if len(q) > 1:
                continue
            else:
                q.append(frame)
                time.sleep(1.0 / 12.0)

    else:
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
                    frame = cv2.imdecode(np.fromstring(jpg, dtype=np.uint8), cv2.IMREAD_COLOR)
                    if len(q) <= 1:
                        q.append(frame)
                    if len(cam_q) <= 1:
                        cam_q.append(frame)

if __name__ == "__main__":
    main()
