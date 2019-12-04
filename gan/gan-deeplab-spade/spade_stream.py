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

class Gan(Thread):
    def __init__(self, queue, deeplab_opt, spade_opt):
        self.queue = queue
        self.deeplab_opt = deeplab_opt
        self.spade_opt = spade_opt

        Thread.__init__(self)

    def run(self):
        # Setup

        
        CONFIG = self.deeplab_opt['CONFIG']
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
        print("Spade!")
        print(spade_model)

        coco_dataset = CocoDataset()
        coco_dataset.initialize(self.spade_opt)
        print(coco_dataset)



        # UVC camera stream
        cap = cv2.VideoCapture(camera_id)
        cap.set(cv2.CAP_PROP_FOURCC, cv2.VideoWriter_fourcc(*"YUYV"))

        window_name = "{} + {}".format(CONFIG.MODEL.NAME, CONFIG.DATASET.NAME)
        cv2.namedWindow(window_name, cv2.WINDOW_NORMAL)

        self.setup_pipeline()

        #np.set_printoptions(threshold=sys.maxsize)

        while True:
            _, frame = cap.read()
            image, raw_image = preprocessing(frame, device, CONFIG)
            #print("Image shape {}".format(image.shape))
            labelmap = inference(model, image, raw_image, postprocessor)

            # Frisby and more to sea?
            #labelmap[labelmap == 33] = 154
            #labelmap[labelmap == 66] = 154
            #labelmap[labelmap == 80] = 154
            #Bottle to flower?
            #labelmap[labelmap == 43] = 118
            # Person to rock?
            #labelmap[labelmap == 0] = 168
            #dog to person
            #labelmap[labelmap == 17] = 0

            # Sky grass and bottle flower
            #bottle_mask = (labelmap == 43)
            #labelmap[0:193,:] = 156
            #labelmap[:,:] = 123
            #labelmap[bottle_mask] = 118

            #print(labelmap.shape)

            #colormap = colorize(labelmap)
            uniques = np.unique(labelmap)
            instance_counter = 0
            instancemap = np.zeros(labelmap.shape)
            print(uniques)
            for label_id in uniques:
                mask = (labelmap == label_id)
                instancemap[mask] = instance_counter
                instance_counter += 1

            labelimg = Image.fromarray(np.uint8(labelmap), 'L')
            instanceimg = Image.fromarray(np.uint8(instancemap),'L')

            
            #labelimg.show()

            item = coco_dataset.get_item_from_images(labelimg, instanceimg)
            generated = spade_model(item, mode='inference')

            generated_np = util.tensor2im(generated[0])

            # Masking
            #print("Generated image shape {} label resize shape {}".format(generated_np.shape, label_resized.shape))
            #label_resized = np.array(labelimg.resize((256,256), Image.NEAREST))
            #generated_np[label_resized != 118, :] = [0, 0, 0];

            generated_rgb = cv2.cvtColor(generated_np, cv2.COLOR_BGR2RGB)
            self.push_frame(generated_np)

            #print("raw image shape {}".format(raw_image.shape))
            #print("Generated image {}".format(generated_np))
            #print("raw image  {}".format(raw_image))

            # Register mouse callback function
            #cv2.setMouseCallback(window_name, self.mouse_event, labelmap)

            # Overlay prediction
            #cv2.addWeighted(colormap, 1.0, raw_image, 0.0, 0.0, raw_image)

            # Quit by pressing "q" key
            #cv2.imshow(window_name, generated_rgb)
            #cv2.resizeWindow(window_name, 1024,1024)
            #if cv2.waitKey(10) == ord("q"):
            #    break

    def mouse_event(event, x, y, flags, labelmap):
        # Show a class name of a mouse-overed pixel
        label = labelmap[y, x]
        name = classes[label]
        print(name)

    def load_models(self):
        pass

    def colorize(labelmap):
        print(labelmap.shape)
        # Assign a unique color to each label
        labelmap = labelmap.astype(np.float32) / CONFIG.DATASET.N_CLASSES
        colormap = cm.jet_r(labelmap)[..., :-1] * 255.0
        return np.uint8(colormap)

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


    def get_buf(self):
        pass

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

        caps = Gst.Caps.from_string("video/x-raw,format=(string)I420,width=256,height=256,framerate=30/1")
        self.src_v.set_property("caps", caps)
        self.src_v.set_property("format", Gst.Format.TIME)

        self.src_v.link(vcvt)
        vcvt.link(ndisink)

        self.pipeline.set_state(Gst.State.PLAYING)


q = queue.Queue()

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

    gan = Gan(q, deeplab_opt, spade_opt)
    gan.start()


if __name__ == "__main__":
    main()
